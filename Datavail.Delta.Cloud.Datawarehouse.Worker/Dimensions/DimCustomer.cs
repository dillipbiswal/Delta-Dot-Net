using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Repository;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Dimensions
{
    public class DimCustomer : Type2Dimension
    {
        [Key]
        public int CustomerKey { get; set; }
        public Guid CustomerId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }

        private readonly IRepository _repository;
        private readonly IDeltaLogger _logger;

        private DimCustomer()
        {

        }

        public DimCustomer(IRepository repository, IDeltaLogger logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public static DimCustomer CreateNewCustomerDim(Guid naturalKey, string name, string status)
        {
            var newCustomerDim = new DimCustomer
            {
                CustomerId = naturalKey,
                Name = name,
                Status = status,
                RowStart = new DateTime(2011, 1, 1, 0, 0, 0),
                IsRowCurrent = true
            };

            return newCustomerDim;
        }

        [SqlAzureRetry]
        public void Update()
        {
            try
            {
                var customers = _repository.GetAll<Customer>();

                using (var ctx = new DeltaDwContext())
                {
                    foreach (var customer in customers)
                    {
                        var customerDim = ctx.DimCustomers.FirstOrDefault(t => t.CustomerId == customer.Id && t.IsRowCurrent);
                        if (customerDim == null)
                        {
                            var newCustomerDimRow = CreateNewCustomerDim(customer.Id, customer.Name, customer.Status.Enum.ToString());
                            ctx.DimCustomers.Add(newCustomerDimRow);
                            ctx.SaveChanges();

                        }
                        else
                        {
                            if (customerDim.Name != customer.Name || customerDim.Status != customer.Status.Enum.ToString())
                            {
                                customerDim.RowEnd = DateTime.UtcNow;
                                customerDim.IsRowCurrent = false;

                                var newCustomerDimRow = CreateNewCustomerDim(customer.Id, customer.Name, customer.Status.Enum.ToString());

                                ctx.DimCustomers.Add(newCustomerDimRow);
                                ctx.SaveChanges();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in DimCustomer::Update", ex);
            }
        }

        [SqlAzureRetry]
        public static int GetSurrogateKeyFromNaturalKey(Guid naturalKey, DateTime timestamp)
        {
            using (var ctx = new DeltaDwContext())
            {
                var customer = ctx.DimCustomers.FirstOrDefault(t => t.CustomerId == naturalKey && (t.RowStart <= timestamp && t.IsRowCurrent || t.RowStart <= timestamp && t.RowEnd >= timestamp));
                return customer != null ? customer.CustomerKey : -1;
            }
        }

        [SqlAzureRetry]
        public static int GetSurrogateKeyFromServerSurrogateKey(int serverKey, DateTime timestamp)
        {
            using (var ctx = new DeltaDwContext())
            {
                var server = ctx.DimServers.FirstOrDefault(t => t.ServerKey == serverKey && (t.RowStart <= timestamp && t.IsRowCurrent || t.RowStart <= timestamp && t.RowEnd >= timestamp));
                return (int)(server != null ? server.CustomerKey : -1);
            }
        }

        [SqlAzureRetry]
        public static Guid? GetNaturalKeyFromServerSurrogateKey(int serverKey, DateTime timestamp)
        {
            using (var ctx = new DeltaDwContext())
            {
                var server = ctx.DimServers.FirstOrDefault(t => t.ServerKey == serverKey && (t.RowStart <= timestamp && t.IsRowCurrent || t.RowStart <= timestamp && t.RowEnd >= timestamp));
                return (server != null && server.CustomerId != null ? server.CustomerId : Guid.Empty);
            }
        }
    }
}
