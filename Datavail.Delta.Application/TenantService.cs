using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;
using Datavail.Delta.Domain.Specifications;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Infrastructure.Resources;
using Datavail.Delta.Infrastructure.Util;

using System.Linq;

namespace Datavail.Delta.Application
{
    public class TenantService : ITenantService
    {
        private readonly IRepository _repository;
        private readonly IServerService _serverService;

        public TenantService(IRepository tenantRepository, IServerService serverService)
        {
            _repository = tenantRepository;
            _serverService = serverService;
        }

        public void Create(string name)
        {
            var tenant = Tenant.NewTenant(name);
            _repository.Add(tenant);
            _repository.UnitOfWork.SaveChanges();
        }

        public void UpdateStatus(Guid serverId, Domain.Status status)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Tenant> GetTenantList()
        {
            return _repository.GetAll<Tenant>().Where(t => t.Status != Status.Inactive);
        }

        public string GetAlertThresholds(Guid tenantId)
        {
            //Get All Servers For Tenant
            var tenant = _repository.GetByKey<Tenant>(tenantId);
            if (tenant == null) throw new InvalidOperationException(ApplicationErrors.InvalidServerId);

            var servers = _repository.Find<Server>(new ServersByTenantIdSpecification(tenantId));
            var xml = new XElement("MetricThresholds", new XAttribute("TenantId", tenant.Id.ToString()));


            //For Each Server, Iterate Its MetricInstances
            foreach (var server in servers)
            {
                //For Each MetricInstance, Find Its Prevailing Configuration [using Same Logic As ServerService.GetConfig()])
                foreach (var metricInstance in server.MetricInstances)
                {
                    var metricConfiguration = _serverService.GetActiveConfiguration(server, metricInstance);

                    //For Each MetricConfiguration, Iterate Its MetricThresholds
                    foreach (var metricThreshold in metricConfiguration.MetricThresholds)
                    {
                        var mcXml = new XElement("MetricThreshold");
                        mcXml.Add(new XAttribute("MetricInstanceId", metricInstance.Id.ToString()));
                        mcXml.Add(new XAttribute("Severity", metricThreshold.Severity.Value.ToString()));
                        mcXml.Add(new XAttribute("FloorValue", metricThreshold.FloorValue.ToString()));
                        mcXml.Add(new XAttribute("CeilingValue", metricThreshold.CeilingValue.ToString()));
                        mcXml.Add(new XAttribute("Function", metricThreshold.ThresholdComparisonFunction.Value.ToString()));
                        mcXml.Add(new XAttribute("Count", metricThreshold.NumberOfOccurrences.ToString()));
                        mcXml.Add(new XAttribute("ValueType", metricThreshold.ThresholdValueType.Value.ToString()));
                        mcXml.Add(new XAttribute("TimePeriod", metricThreshold.TimePeriod.ToString()));

                        xml.Add(mcXml);

                    }
                }

                //Return an XML Doc of All of the MetricThresholds

                /*
                 * <MetricThresholds TenantId="{F5B00ED2-2851-4ECE-AA52-FB65B9D592FD}">
                 * 
                 *  <!-- Alert if Percentage is >=90 && <=100 3 times within 5 minutes open a critial alert -->
                 *  <MetricThreshold MetricInstanceId="{F7A64A82-52DD-4889-8E98-B83D816B5B23}" Severity="Critical" FloorValue="90" CeilingValue="100" Function="Count" Count="3"  ValueType="Percentage" TimePeriod="300" /> 
                 *  
                 *  <!-- Alert if Percentage is >=90 && <=100 once open a critial alert -->
                 *  <MetricThreshold MetricInstanceId="{F7A64A82-52DD-4889-8E98-B83D816B5B23}" Severity="Critical" FloorValue="90" CeilingValue="100" Function="Count" Count="1"  ValueType="Percentage" />
                 *  
                 *  <!-- Alert if Value is >=150000000000 && <=9999999999999 once open a critial alert -->
                 *  <MetricThreshold MetricInstanceId="{F7A64A82-52DD-4889-8E98-B83D816B5B23}" Severity="Critical" FloorValue="150000000000" CeilingValue="9999999999999" Function="Count" Count="1"  ValueType="Value" />
                 *  
                 *  <!-- Alert if Average Value is >=80 && <=100 for ten minutes open a critial alert -->
                 *  <MetricThreshold MetricInstanceId="{F7A64A82-52DD-4889-8E98-B83D816B5B23}" Severity="Critical" FloorValue="80" CeilingValue="100" Function="Average" ValueType="Value" TimePeriod="600" />
                 *  
                 * </MetricThresholds>
                 */

            }
            return xml.ToString();
        }
    }
}
