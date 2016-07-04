using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml.Linq;
using ArgGuard;
using AutoMapper;
using Datavail.Delta.Application;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.RestServices.Models;

namespace Datavail.Delta.RestServices.Controllers
{
    public class MetricInstancesController : ApiController
    {

        private readonly IDeltaLogger _logger;
        private readonly IRepository _repository;

        #region Constructors
        static MetricInstancesController()
        {
            //Mapper.CreateMap<CustomerModel, Customer>()
            //    .ForMember(dest => dest.Id, opt => opt.Ignore())
            //    .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.StatusId))
            //    .ForMember(dest => dest.Tenant, opt => opt.Ignore());

            //Mapper.CreateMap<Customer, CustomerModel>()
            //        .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.Status))
            //        .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.Tenant.Id));
        }

        public MetricInstancesController(IDeltaLogger logger, IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }
        #endregion



        #region Helper Methods
        private void GetMetricInstanceDataAndLabel(Guid metricId, Guid serverId, Guid metricParentId, MetricData metricData, out string xmlData, out string label)
        {
            DatabaseInstance instance = null;
            Database database = null;

            var server = _repository.GetByKey<Server>(serverId);
            Guard.NotNull(server, "server");

            var metric = _repository.GetByKey<Metric>(metricId);
            Guard.NotNull(metric, "metric");

            //out params
            label = string.Empty;
            xmlData = string.Empty;
            XElement xml = null;

            metric = _repository.GetByKey<Metric>(metricId);
            Guard.NotNull(metric, "metricId");

            switch (metric.MetricType)
            {
                case MetricType.Server:
                case MetricType.VirtualServer:
                case MetricType.Server | MetricType.VirtualServer:
                    server = _repository.GetByKey<Server>(metricParentId);
                    Guard.NotNull(server, "metricParentId");
                    break;
                case MetricType.Instance:
                    instance = _repository.GetByKey<DatabaseInstance>(metricParentId);
                    server = instance.Server;
                    Guard.NotNull(instance, "metricParentId");
                    break;
                case MetricType.Database:
                    database = _repository.GetByKey<Database>(metricParentId);
                    instance = database.Instance;
                    server = instance.Server;
                    Guard.NotNull(database, "metricParentId");
                    break;
                default:
                    break;
            }

            var connectionString = "";
            switch (metric.AdapterClass)
            {
                case "CheckInPlugin":
                    label = "Check-In";
                    xml = new XElement("CheckInPluginInput");
                    break;
                case "CpuPlugin":
                    label = "CPU Utilization";
                    xml = new XElement("CpuPluginInput");
                    break;
                case "DiskInventoryPlugin":
                    label = "Disk Inventory";
                    xml = new XElement("DiskInventoryPluginInput");
                    break;
                case "WebSiteInventoryPlugin":
                    label = "Web Site Inventory";
                    xml = new XElement("WebSiteInventoryPluginInput");
                    break;
                case "ApplicationPoolInventoryPlugin":
                    label = "Application Pool Inventory";
                    xml = new XElement("ApplicationPoolInventoryPluginInput");
                    break;
                case "RamPlugin":
                    label = "RAM Status";
                    xml = new XElement("RamPluginInput");
                    break;
                case "DatabaseInventoryPlugin":
                    if (instance != null)
                    {
                        label = "Database Inventory on Instance '" + instance.Name + "'";
                        connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "master",
                                                                          instance.UseIntegratedSecurity,
                                                                          instance.Username, instance.Password);


                        xml = new XElement("DatabaseInventoryPluginInput");
                        xml.Add(new XAttribute("ConnectionString", connectionString));
                        xml.Add(new XAttribute("InstanceName", instance.Name));
                        xml.Add(new XAttribute("InstanceId", instance.Id));
                    }
                    break;
                case "SqlAgentJobInventoryPlugin":
                    // ReSharper disable PossibleNullReferenceException
                    label = "SQL Agent Job Inventory on Instance '" + instance.Name + "'";

                    connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "msdb",
                                                                      instance.UseIntegratedSecurity,
                                                                      instance.Username, instance.Password);
                    // ReSharper restore PossibleNullReferenceException

                    xml = new XElement("SqlAgentJobInventoryPluginInput");
                    xml.Add(new XAttribute("ConnectionString", connectionString));
                    xml.Add(new XAttribute("InstanceName", instance.Name));
                    xml.Add(new XAttribute("InstanceId", instance.Id));

                    break;
                case "DatabaseServerBlockingPlugin":
                    // ReSharper disable PossibleNullReferenceException
                    label = "Blocking Status on Instance '" + instance.Name + "'";

                    connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "master",
                                                                      instance.UseIntegratedSecurity,
                                                                      instance.Username, instance.Password);
                    // ReSharper restore PossibleNullReferenceException

                    xml = new XElement("DatabaseServerBlockingPluginInput");
                    xml.Add(new XAttribute("ConnectionString", connectionString));
                    xml.Add(new XAttribute("InstanceName", instance.Name));

                    break;
                case "DatabaseServerPerformanceCountersPlugin":
                    // ReSharper disable PossibleNullReferenceException
                    label = "Performance Counters for Instance '" + instance.Name + "'";

                    connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "master",
                                                                      instance.UseIntegratedSecurity,
                                                                      instance.Username, instance.Password);
                    // ReSharper restore PossibleNullReferenceException

                    xml = new XElement("DatabaseServerPerformanceCountersPluginInput");
                    xml.Add(new XAttribute("ConnectionString", connectionString));
                    xml.Add(new XAttribute("InstanceName", instance.Name));
                    break;
                case "DatabaseBackupStatusPlugin":
                    // ReSharper disable PossibleNullReferenceException
                    label = "Backup Status for Database '" + database.Name + "' on Instance '" + instance.Name + "'";

                    connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, database.Name,
                                                                      instance.UseIntegratedSecurity,
                                                                      instance.Username, instance.Password);
                    // ReSharper restore PossibleNullReferenceException

                    xml = new XElement("DatabaseBackupStatusPluginInput");
                    xml.Add(new XAttribute("DatabaseName", database.Name));
                    xml.Add(new XAttribute("ConnectionString", connectionString));
                    xml.Add(new XAttribute("InstanceName", instance.Name));
                    break;
                case "DatabaseStatusPlugin":
                    // ReSharper disable PossibleNullReferenceException
                    label = "Database Status for for Database '" + database.Name + "' on Instance '" + instance.Name + "'";

                    connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "master",
                                            instance.UseIntegratedSecurity, instance.Username, instance.Password);
                    // ReSharper restore PossibleNullReferenceException

                    xml = new XElement("DatabaseStatusPluginInput");
                    xml.Add(new XAttribute("DatabaseName", database.Name));
                    xml.Add(new XAttribute("ConnectionString", connectionString));
                    xml.Add(new XAttribute("InstanceName", instance.Name));
                    break;
                case "MsClusterGroupStatusPlugin":
                    // ReSharper disable PossibleNullReferenceException
                    label = "Cluster Group Status for '" + server.ClusterGroupName + "'";
                    // ReSharper restore PossibleNullReferenceException
                    xml = new XElement("MsClusterGroupStatus");
                    break;
                case "MsClusterNodeStatusPlugin":
                    // ReSharper disable PossibleNullReferenceException
                    label = "Cluster Node Status for '" + server.ClusterGroupName + "'";
                    // ReSharper restore PossibleNullReferenceException
                    xml = new XElement("MsClusterNodeStatus");
                    break;
                case "DatabaseFileSizePlugin":
                    // ReSharper disable PossibleNullReferenceException
                    label = "File Size for Database '" + database.Name + "' on Instance '" + instance.Name + "'";

                    connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, database.Name,
                                            instance.UseIntegratedSecurity, instance.Username, instance.Password);
                    // ReSharper restore PossibleNullReferenceException

                    xml = new XElement("DatabaseFileSizePluginInput");
                    xml.Add(new XAttribute("DatabaseName", database.Name));
                    xml.Add(new XAttribute("ConnectionString", connectionString));
                    xml.Add(new XAttribute("InstanceName", instance.Name));
                    break;
                case "DatabaseServerJobsPlugin":
                    var jobnameMetricData = metricData.Data.FirstOrDefault(x => x.TagName == "JobName");
                    Guard.NotNull(jobnameMetricData, "DatabaseServerJobsPlugin");
                    // ReSharper disable PossibleNullReferenceException
                    label = "Job Status for '" + jobnameMetricData.Value + "' on Instance '" + instance.Name + "'";

                    connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "msdb",
                                            instance.UseIntegratedSecurity, instance.Username, instance.Password);
                    // ReSharper restore PossibleNullReferenceException

                    xml = new XElement("DatabaseServerJobsPluginInput");
                    xml.Add(new XAttribute("JobName", jobnameMetricData.Value));
                    xml.Add(new XAttribute("ConnectionString", connectionString));
                    xml.Add(new XAttribute("InstanceName", instance.Name));
                    break;
                case "DatabaseServerLongRunningProcessPlugin":
                    label = "Long Running Process Status on Instance '" + instance.Name + "'";
                    var thresholdMetricData = metricData.Data.FirstOrDefault(x => x.TagName == "Threshold");
                    connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "master",
                                            instance.UseIntegratedSecurity, instance.Username, instance.Password);

                    xml = new XElement("DatabaseServerLongRunningProcessPluginInput");
                    xml.Add(new XAttribute("Threshold", thresholdMetricData.Value));
                    xml.Add(new XAttribute("ConnectionString", connectionString));
                    xml.Add(new XAttribute("InstanceName", instance.Name));
                    break;
                case "DatabaseServerMergeReplicationPlugin":
                    var distDBMetricData = metricData.Data.FirstOrDefault(x => x.TagName == "DistributionDatabaseName");
                    label = "Merge Replication Status for Distribution Database '" + distDBMetricData.Value + "' on Instance '" + instance.Name + "'";
                    connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "master",
                                            instance.UseIntegratedSecurity, instance.Username, instance.Password);

                    xml = new XElement("DatabaseServerMergeReplicationPlugin");
                    xml.Add(new XAttribute("DistributionDatabaseName", distDBMetricData.Value));
                    xml.Add(new XAttribute("ConnectionString", connectionString));
                    xml.Add(new XAttribute("InstanceName", instance.Name));
                    break;
                case "DatabaseServerTransactionalReplicationPlugin":
                    var distribDBRepMetricData = metricData.Data.FirstOrDefault(x => x.TagName == "DistributionDatabaseName");
                    label = "Transactional Replication Status for Distribution Database '" + distribDBRepMetricData.Value + "' on Instance '" + instance.Name + "'";
                    connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "master",
                                            instance.UseIntegratedSecurity, instance.Username, instance.Password);

                    xml = new XElement("DatabaseServerTransactionalReplicationPlugin");
                    xml.Add(new XAttribute("DistributionDatabaseName", distribDBRepMetricData.Value));
                    xml.Add(new XAttribute("ConnectionString", connectionString));
                    xml.Add(new XAttribute("InstanceName", instance.Name));
                    break;
                case "DiskPlugin":
                    var pathMetricData = metricData.Data.FirstOrDefault(x => x.TagName == "Path");

                    pathMetricData.Value = pathMetricData.Value.ToUpper();
                    label = "Disk Status for '" + pathMetricData.Value + "'";
                    xml = new XElement("DiskPlugin");
                    xml.Add(new XAttribute("Path", pathMetricData.Value));
                    break;
                case "WebSitePlugin":
                    var pathMetricWebsiteData = metricData.Data.FirstOrDefault(x => x.TagName == "Site");

                    pathMetricWebsiteData.Value = pathMetricWebsiteData.Value.ToUpper();
                    label = "Web Site Status for '" + pathMetricWebsiteData.Value + "'";
                    xml = new XElement("WebSitePlugin");
                    xml.Add(new XAttribute("Site", pathMetricWebsiteData.Value));
                    break;
                case "ApplicationPoolPlugin":
                    var pathMetricApPoolData = metricData.Data.FirstOrDefault(x => x.TagName == "Pool");

                    pathMetricApPoolData.Value = pathMetricApPoolData.Value.ToUpper();
                    label = "Application Pool Status for '" + pathMetricApPoolData.Value + "'";
                    xml = new XElement("ApplicationPoolPlugin");
                    xml.Add(new XAttribute("Pool", pathMetricApPoolData.Value));
                    break;

                case "LogWatcherPlugin":
                    var fileName = metricData.Data.Where(x => x.TagName == "FileNameToWatch").FirstOrDefault().Value;
                    var matchExprData = metricData.Data.FirstOrDefault(x => x.TagName == "MatchExpressions");
                    var excludeExprData = metricData.Data.FirstOrDefault(x => x.TagName == "ExcludeExpressions");
                    label = "Log Watcher for '" + fileName + "'";
                    xml = new XElement("LogWatcherPluginInput");
                    xml.Add(new XAttribute("FileNameToWatch", fileName));
                    var matchExpressionElem = new XElement("MatchExpressions");

                    if (matchExprData != null && matchExprData.Children != null && matchExprData.Children.Any())
                    {
                        foreach (var item in matchExprData.Children)
                        {
                            var matchExpr = new XElement("MatchExpression");
                            matchExpr.Add(new XAttribute("expression", item.Value));
                            matchExpressionElem.Add(matchExpr);
                        }
                    }

                    var excludeExpressionElem = new XElement("ExcludeExpressions");

                    if (excludeExprData != null && excludeExprData.Children != null && excludeExprData.Children.Any())
                    {
                        foreach (var item in excludeExprData.Children)
                        {
                            var excludeExpr = new XElement("ExcludeExpression");
                            excludeExpr.Add(new XAttribute("expression", item.Value));
                            excludeExpressionElem.Add(excludeExpr);
                        }
                    }

                    xml.Add(matchExpressionElem);
                    xml.Add(excludeExpressionElem);
                    break;
                case "ServiceStatusPlugin":
                    var serviceName = metricData.Data.FirstOrDefault(x => x.TagName == "ServiceName").Value;
                    label = "Service Status for Service '" + serviceName + "'";
                    xml = new XElement("ServiceStatusPluginInput");
                    xml.Add(new XAttribute("ServiceName", serviceName));
                    break;
                case "SqlAgentStatusPlugin":
                    label = "SQL Agent Status for Instance '" + instance.Name + "'";
                    connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "master",
                                            instance.UseIntegratedSecurity, instance.Username, instance.Password);
                    var programNameMetricData = metricData.Data.FirstOrDefault(x => x.TagName == "ProgramNames");

                    xml = new XElement("SqlAgentStatusPluginInput");
                    xml.Add(new XAttribute("ConnectionString", connectionString));
                    xml.Add(new XAttribute("InstanceName", instance.Name));

                    var programNamesElem = new XElement("ProgramNames");

                    if (programNameMetricData != null && programNameMetricData.Children != null && programNameMetricData.Children.Any())
                    {
                        foreach (var item in programNameMetricData.Children)
                        {
                            var programName = new XElement("ProgramName");
                            programName.Add(new XAttribute("Value", item.Value));
                            programNamesElem.Add(programName);
                        }
                    }
                    xml.Add(programNamesElem);
                    break;
                default:
                    break;
            }

            // ReSharper disable PossibleNullReferenceException
            xml.Add(new XAttribute("Label", label));

            if (server.IsVirtual)
            {
                xml.Add(new XAttribute("ClusterGroupName", server.ClusterGroupName));
                xml.Add(new XAttribute("VirtualServerId", server.Id));
            }

            xmlData = xml.ToString();
            // ReSharper restore PossibleNullReferenceException
        }

        private static string BuildEncryptedConnectionString(string hostName, string instanceName, string initialCatalog, bool useIntegratedSecurity, string userId, string password)
        {
            var connectionString = "";

            if (useIntegratedSecurity)
            {
                connectionString = @"Data Source={0};Initial Catalog={1};Integrated Security=true;";
                connectionString = string.Format(connectionString, instanceName, initialCatalog);
            }
            else
            {
                connectionString = @"Data Source={0};Initial Catalog={1};User Id={2};Password={3}";
                connectionString = string.Format(connectionString, instanceName, initialCatalog, userId, password);
            }

            //var crypto = new Encryption();
            //return crypto.EncryptToString(connectionString);
            return string.Empty;
        }

        #endregion
    }
}
