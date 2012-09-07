using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Repository;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
using Microsoft.Practices.TransientFaultHandling;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Dimensions
{
    public class DimTenant : Type2Dimension
    {
        [Key]
        public int TenantKey { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }

        private readonly IRepository _repository;
        private readonly IDeltaLogger _logger;

        private DimTenant()
        {
            
        }

        public DimTenant(IRepository repository, IDeltaLogger logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public static DimTenant CreateNewTenantDim(Guid naturalKey, string name, string status)
        {
            var newTenantDim = new DimTenant
            {
                TenantId = naturalKey,
                Name = name,
                Status = status,
                RowStart = new DateTime(2011, 1, 1, 0, 0, 0),
                RowEnd = null,
                IsRowCurrent = true
            };

            return newTenantDim;
        }

        [SqlAzureRetry]
        public void Update()
        {
            try
            {
                var tenants = _repository.GetAll<Tenant>();

                using (var ctx = new DeltaDwContext())
                {
                    foreach (var tenant in tenants)
                    {
                        var tenantDim = ctx.DimTenants.FirstOrDefault(t => t.TenantId == tenant.Id && t.IsRowCurrent);
                        if (tenantDim == null)
                        {
                            var newTenantDim = CreateNewTenantDim(tenant.Id, tenant.Name,
                                                                  tenant.Status.Enum.ToString());
                            ctx.DimTenants.Add(newTenantDim);
                            ctx.SaveChanges();
                        }
                        else
                        {
                            if (tenantDim.Name != tenant.Name || tenantDim.Status != tenant.Status.Enum.ToString())
                            {
                                tenantDim.RowEnd = DateTime.UtcNow;
                                tenantDim.IsRowCurrent = false;

                                var newTenantDim = CreateNewTenantDim(tenant.Id, tenant.Name,
                                                                      tenant.Status.Enum.ToString());
                                ctx.DimTenants.Add(newTenantDim);
                                ctx.SaveChanges();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in DimTenant::Update", ex);
            }
        }

        [SqlAzureRetry]
        public static int GetSurrogateKeyFromNaturalKey(Guid naturalKey, DateTime timestamp)
        {
            using (var ctx = new DeltaDwContext())
            {
                var tenant = ctx.DimTenants.FirstOrDefault(t => t.TenantId == naturalKey && (t.RowStart <= timestamp && t.IsRowCurrent || t.RowStart <= timestamp && t.RowEnd >= timestamp));
                return tenant != null ? tenant.TenantKey : -1;
            }
        }
    }
}
