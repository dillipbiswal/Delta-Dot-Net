using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using AutoMapper;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;
using Datavail.Delta.Domain.Specifications;
using Datavail.Delta.Infrastructure.Agent.Common;

using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Resources;
using Datavail.Delta.Infrastructure.Specification;
using Datavail.Delta.Repository.Interface;
using Guard = Datavail.Delta.Infrastructure.Util.Guard;

namespace Datavail.Delta.Application
{
    
    
    public class ServerService : IServerService
    {
        private readonly IDeltaLogger _logger;
        private readonly IServerRepository _repository;

        public ServerService(IDeltaLogger logger, IServerRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        #region Thresholds
        public IEnumerable<MetricThreshold> GetThresholds(Guid metricInstanceId)
        {
            var metricInstance = _repository.GetMetricInstanceById(metricInstanceId);
            if (metricInstance == null) throw new InvalidOperationException(ApplicationErrors.InvalidMetricInstanceId);

            var configuration = GetActiveConfiguration(metricInstance.Server, metricInstance);
            var thresholds = new List<MetricThreshold>();

            foreach (var threshold in configuration.MetricThresholds)
            {
                thresholds.Add(threshold);
            }

            return thresholds;
        }
        #endregion

        #region Config
        public string GetConfig(Guid serverId)
        {
            var server = _repository.GetByKey<Server>(serverId);
            if (server == null)
                return "<?xml version=\"1.0\" encoding=\"utf-8\" ?><AgentConfiguration ServerId=\"{00000000-0000-0000-0000-000000000000}\"><MetricInstance Id=\"00000000-0000-0000-0000-000000000000\" AdapterAssembly=\"Datavail.Delta.Agent.Plugin.CheckIn\" AdapterClass=\"CheckInPlugin\" AdapterVersion=\"4.0.0000.0\" Label=\"CheckIn\" Data=\"\" ScheduleType=\"0\" ScheduleInterval=\"300\" MetricConfigurationId=\"00000000-0000-0000-0000-000000000000\" /></AgentConfiguration>";


            var xml = new XElement("AgentConfiguration", new XAttribute("serverId", server.Id.ToString()));

            var metricInstanceList = new List<MetricInstance>();

            //Add all of the server's direct metric instances
            metricInstanceList.AddRange(_repository.Find(new ActiveMetricInstancesForServerSpecification(serverId)));

            //Add metric instances from each VirtualServer of the cluster that this server belongs to (if any)
            if (server.Cluster != null)
            {
                foreach (var virtualServer in server.Cluster.VirtualServers)
                {
                    metricInstanceList.AddRange(_repository.Find(new ActiveMetricInstancesForServerSpecification(virtualServer.Id)));
                }
            }

            //Get the active config for each Metric Instance
            foreach (var metricInstance in metricInstanceList)
            {
                var metricConfigurations = GetActiveConfiguration(server, metricInstance);

                //Call Helper Method with metricInstance, config;
                var miXml = CreateMetricInstanceNodes(metricInstance, metricConfigurations);

                //Add to Xml config object
                xml.Add(miXml);
            }

            return xml.ToString();
        }

        public MetricInstance GetMetricInstance(Guid metricInstanceId)
        {
            var metricInstance = _repository.GetByKey<MetricInstance>(metricInstanceId);
            if (metricInstance == null) throw new InvalidOperationException(ApplicationErrors.InvalidMetricInstanceId);

            return metricInstance;
        }

        public MetricInstance GetMetricInstance(Guid metricInstanceId, Guid parentId, out MetricData metricData)
        {
            MetricInstance metricInstance = null;

            metricData = GetMetricInstanceData(metricInstanceId, parentId, out metricInstance);

            return metricInstance;
        }

        public MetricConfiguration GetActiveConfiguration(Server server, MetricInstance metricInstance)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            //Check MetricInstance Level
            //if (metricInstance.MetricConfigurations.Any(mc => mc.Schedules.Any() || mc.MetricThresholds.Any()))
            //{
            //    return metricInstance.MetricConfigurations.FirstOrDefault();
            //}
            var metricInstanceLevelConfig = _repository.GetQuery<MetricConfiguration>(mc => mc.ParentMetricInstance.Id == metricInstance.Id && (mc.Schedules.Any() || mc.MetricThresholds.Any())).FirstOrDefault();
            if (metricInstanceLevelConfig != null)
                return metricInstanceLevelConfig;

            Debug.WriteLine("     MetricInstance Level took {0}ms to execute", stopWatch.ElapsedMilliseconds);
            stopWatch.Stop();
            stopWatch.Reset();
            stopWatch.Start();


            //Check Server Level
            //if (server.MetricConfigurations.Any(mc => mc.Metric.Id == metricInstance.Metric.Id && (mc.Schedules.Any() || mc.MetricThresholds.Any())))
            //{
            //    return server.MetricConfigurations.FirstOrDefault(s => s.Metric.Id == metricInstance.Metric.Id);
            //}
            var serverLevelConfig = _repository.GetQuery<MetricConfiguration>(mc => mc.ParentServer.Id == server.Id && mc.Metric.Id == metricInstance.Metric.Id && (mc.Schedules.Any() || mc.MetricThresholds.Any())).FirstOrDefault();
            if (serverLevelConfig != null)
                return serverLevelConfig;

            Debug.WriteLine("     Server Level took {0}ms to execute", stopWatch.ElapsedMilliseconds);
            stopWatch.Stop();
            stopWatch.Reset();
            stopWatch.Start();

            //Check Server Group Level
            //foreach (var serverGroup in server.ServerGroups.OrderBy(s => s.Priority))
            //{
            //    if (serverGroup.MetricConfigurations.Any(mc => mc.Metric.Id == metricInstance.Metric.Id && (mc.Schedules.Any() || mc.MetricThresholds.Any())))
            //    {
            //        return serverGroup.MetricConfigurations.FirstOrDefault(s => s.Metric.Id == metricInstance.Metric.Id);
            //    }
            //}

            foreach (var serverGroup in server.ServerGroups.OrderBy(s => s.Priority).ToList())
            {
                var serverGroupLevelConfig = _repository.GetQuery<MetricConfiguration>(mc => mc.ParentServerGroup.Id == serverGroup.Id && mc.Metric.Id == metricInstance.Metric.Id && (mc.Schedules.Any() || mc.MetricThresholds.Any())).FirstOrDefault();
                if (serverGroupLevelConfig != null)
                    return serverGroupLevelConfig;
            }

            Debug.WriteLine("     Server Group Level took {0}ms to execute", stopWatch.ElapsedMilliseconds);
            stopWatch.Stop();
            stopWatch.Reset();
            stopWatch.Start();

            //Check Customer Level
            //if (server.Customer != null &&
            //    server.Customer.MetricConfigurations.Any(mc => mc.Metric.Id == metricInstance.Metric.Id && (mc.Schedules.Any() || mc.MetricThresholds.Any())))
            //{
            //    return server.Customer.MetricConfigurations.FirstOrDefault(s => s.Metric.Id == metricInstance.Metric.Id);
            //}
            if (server.Customer != null)
            {
                var customerLevelConfig = _repository.GetQuery<MetricConfiguration>(mc => mc.ParentCustomer.Id == server.Customer.Id && mc.Metric.Id == metricInstance.Metric.Id && (mc.Schedules.Any() || mc.MetricThresholds.Any())).FirstOrDefault();
                if (customerLevelConfig != null)
                    return customerLevelConfig;
            }

            Debug.WriteLine("     Customer Level took {0}ms to execute", stopWatch.ElapsedMilliseconds);
            stopWatch.Stop();
            stopWatch.Reset();
            stopWatch.Start();

            //Check Tenant Level
            //if (server.Tenant.MetricConfigurations.Any(mc => mc.Metric.Id == metricInstance.Metric.Id && (mc.Schedules.Any() || mc.MetricThresholds.Any())))
            //{
            //    return server.Tenant.MetricConfigurations.FirstOrDefault(s => s.Metric.Id == metricInstance.Metric.Id);
            //}
            var tenantLevelConfig = _repository.GetQuery<MetricConfiguration>(mc => mc.ParentTenant.Id == server.Tenant.Id && mc.Metric.Id == metricInstance.Metric.Id && (mc.Schedules.Any() || mc.MetricThresholds.Any())).FirstOrDefault();
            if (tenantLevelConfig != null)
                return tenantLevelConfig;

            Debug.WriteLine("     Tenant Level took {0}ms to execute", stopWatch.ElapsedMilliseconds);
            stopWatch.Stop();
            stopWatch.Reset();
            stopWatch.Start();

            //Check Metric Level
            //if (metricInstance.Metric.MetricConfigurations.Any(mc => mc.Metric.Id == metricInstance.Metric.Id && mc.Parent.Id == mc.Metric.Id))
            //{
            //    return metricInstance.Metric.MetricConfigurations.FirstOrDefault(s => s.Metric.Id == metricInstance.Metric.Id && s.Parent.Id == s.Metric.Id);
            //}
            var metricLevelConfig = _repository.GetQuery<MetricConfiguration>(mc => mc.ParentMetric.Id == metricInstance.Metric.Id).FirstOrDefault();
            if (metricLevelConfig != null)
                return metricLevelConfig;

            Debug.WriteLine("     Metric Level took {0}ms to execute", stopWatch.ElapsedMilliseconds);
            stopWatch.Stop();
            stopWatch.Reset();
            stopWatch.Start();


            throw new InvalidOperationException(ApplicationErrors.NoConfigurationFound);
        }


        #endregion

        #region Queries
        /// <summary> 
        /// Gets a list of servers for a particular customer 
        /// </summary> 
        /// <param name="customerId">A <see cref="T:System.Guid"/> representing the customer for which we want a list of servers.</param> 
        public IEnumerable<Server> GetServerListForCustomer(Guid customerId)
        {
            var customer = GetCustomerById(customerId);
            return _repository.Find(new ServersByCustomerIdSpecification(customer.Id));
        }

        public IEnumerable<Role> GetRoles()
        {
            return _repository.Find<Role>(new Specification<Role>(x => x.Name != string.Empty));
        }

        public IEnumerable<Metric> GetMetrics(MetricConfigurationParentType parentType, Guid parentId)
        {
            var criteria = new Specification<Metric>(x => x.Status.Value != (int)Status.Deleted);

            switch (parentType)
            {
                case MetricConfigurationParentType.Tenant:
                case MetricConfigurationParentType.Customer:
                case MetricConfigurationParentType.ServerGroup:
                case MetricConfigurationParentType.Server:
                    break;
                case MetricConfigurationParentType.MetricInstance:
                    var metricInstance = _repository.GetByKey<MetricInstance>(parentId);
                    var metricId = metricInstance.Metric.Id;
                    criteria = criteria.And(x => x.Id.Equals(metricId));
                    break;
                case MetricConfigurationParentType.Metric:
                    var metric = _repository.GetByKey<Metric>(parentId);
                    criteria = criteria.And(x => x.Id.Equals(metric.Id));
                    break;
                default:
                    break;
            }

            IEnumerable<Metric> metrics = _repository.Find<Metric>(criteria).ToList();

            return metrics;
        }

        public IEnumerable<Metric> GetMetrics(MetricType metricType, Guid itemId)
        {
            var criteria = new Specification<Metric>(x => x.Status.Value != (int)Status.Deleted);
            var databaseVersion = DatabaseVersion.None;
            var existingMetricIds = new List<Guid>();

            switch (metricType)
            {
                case MetricType.VirtualServer:
                case MetricType.Server:
                    var server = _repository.GetByKey<Server>(itemId);
                    existingMetricIds = server.MetricInstances.Where(x => x.Status.Value != (int)Status.Deleted
                                                                            && x.Database == null
                                                                            && x.DatabaseInstance == null
                                                                            && x.Metric.AdapterClass != "LogWatcherPlugin"
                                                                            && x.Metric.AdapterClass != "ServiceStatusPlugin"
                                                                            && x.Metric.AdapterClass != "DiskPlugin").Select(x => x.Metric.Id).ToList();
                    criteria = criteria.And(x => (((x.MetricType.Value & (int)MetricType.VirtualServer) == (int)MetricType.VirtualServer) ||
                                            (x.MetricType.Value & (int)MetricType.Server) == (int)MetricType.Server));
                    criteria = criteria.And(x => !existingMetricIds.Contains(x.Id));
                    break;
                case MetricType.Instance:
                    var instance = _repository.GetByKey<DatabaseInstance>(itemId);
                    databaseVersion = instance.DatabaseVersion;
                    criteria = criteria.And(x => ((x.MetricType.Value & (int)metricType) == (int)metricType));
                    criteria = criteria.And(x => x.DatabaseVersion.Value == (int)databaseVersion);
                    break;
                case MetricType.Database:
                    var database = _repository.GetByKey<Database>(itemId);
                    databaseVersion = database.Instance.DatabaseVersion;
                    criteria = criteria.And(x => x.DatabaseVersion.Value == (int)databaseVersion);
                    criteria = criteria.And(x => ((x.MetricType.Value & (int)metricType) == (int)metricType));
                    break;
            }

            IEnumerable<Metric> metrics = _repository.Find<Metric>(criteria);

            return metrics;
        }

        public IEnumerable<Metric> GetMetrics()
        {
            var criteria = new Specification<Metric>(x => x.Status.Value != (int)Status.Deleted);
            IEnumerable<Metric> metrics = _repository.Find<Metric>(criteria);

            return metrics;
        }

        public MetricConfiguration GetMetricConfiguration(Guid metricId, Guid metricConfigParentId)
        {
            if (metricId.Equals(Guid.Empty) || metricConfigParentId.Equals(Guid.Empty))
                return null;

            var criteria = new Specification<MetricConfiguration>(x => x.Metric.Id.Equals(metricId));
            criteria = criteria.And(new MetricConfigurationsByParentSpecification(metricConfigParentId));

            var metricConfiguration = _repository.Find(criteria).FirstOrDefault();

            return metricConfiguration;
        }

        public bool SaveMetricConfiguration(ref MetricConfiguration metricConfiguration)
        {
            var success = true;

            _repository.Update(metricConfiguration);
            _repository.UnitOfWork.SaveChanges();

            return success;
        }

        public bool SaveMetricInstance(Guid metricInstanceId, Guid metricId, Guid metricParentId, MetricData metricData, Status status, MetricInstanceParentType parentType)
        {
            var success = true;
            var label = "";
            var xmlData = "";
            Metric metric;
            Server server;

            success = GetMetricInstanceDataAndLabel(metricId, metricParentId, metricData, out xmlData, out label, out server, out metric);

            if (success)
            {
                if (metricInstanceId == Guid.Empty)
                {
                    var metricInstance = MetricInstance.NewMetricInstance(label, metric);

                    switch (parentType)
                    {
                        case MetricInstanceParentType.Instance:
                            var databaseInstance = _repository.GetByKey<DatabaseInstance>(metricParentId);
                            metricInstance.DatabaseInstance = databaseInstance;
                            break;
                        case MetricInstanceParentType.Database:
                            var database = _repository.GetByKey<Database>(metricParentId);
                            metricInstance.Database = database;
                            break;
                        default:
                            break;
                    }

                    metricInstance.Id = Guid.NewGuid();
                    metricInstance.Data = xmlData;
                    metricInstance.Status = status;

                    server.MetricInstances.Add(metricInstance);
                    _repository.Add(metricInstance);
                    _repository.Update(server);
                }
                else
                {
                    var metricInstance = _repository.GetByKey<MetricInstance>(metricInstanceId);

                    metricInstance.Status = status;
                    metricInstance.Data = xmlData;
                    metricInstance.Label = label;
                    _repository.Update(metricInstance);
                }

                _repository.UnitOfWork.SaveChanges();
            }

            return success;
        }

        public MetricData GetMetricInstanceData(Guid metricInstanceId, Guid parentId, out MetricInstance metricInstance)
        {
            metricInstance = _repository.GetByKey<MetricInstance>(metricInstanceId);
            MetricData metricData = null;

            if (metricInstance != null)
            {
                metricData = GetMetricData(metricInstance.Metric.AdapterClass, metricInstance.Data, parentId);
            }

            return metricData;
        }

        public MetricData GetMetricData(Guid metricId, Guid parentId)
        {
            MetricData metricData = null;
            var metric = _repository.GetByKey<Metric>(metricId);

            if (metric != null)
            {
                metricData = GetMetricData(metric.AdapterClass, string.Empty, parentId);
            }

            return metricData;
        }

        /// <summary> 
        /// Gets a list of servers for a particular tenant 
        /// </summary> 
        /// <param name="tenantId">A <see cref="T:System.Guid"/> representing the tenant for which we want a list of servers.</param> 
        public IEnumerable<Server> GetServerListForTenant(Guid tenantId)
        {
            //var customer = GetTenantById(customerId);
            return _repository.Find(new ServersByTenantIdSpecification(tenantId));
        }

        public IEnumerable<Server> GetActiveServersForTenant(Guid tenantId)
        {
            return _repository.Find(new ServersByTenantAndStatusSpecification(tenantId, Status.Active));
        }

        public IEnumerable<Server> GetServersNotCheckedInForTenant(Guid tenantId, int minutes)
        {
            if (minutes <= 0)
            {
                throw new InvalidOperationException(ApplicationErrors.InvalidMinutes);
            }

            return _repository.Find(new ServersByTenantIdNotCheckedInSpecification(tenantId, minutes));
        }

        public IEnumerable<Server> GetServersNotCheckedInForCustomer(Guid customerId, int minutes)
        {
            if (minutes <= 0)
            {
                throw new InvalidOperationException(ApplicationErrors.InvalidMinutes);
            }


            var servers = _repository.Find(new ServersByCustomerIdNotCheckedInSpecification(customerId, minutes));
            return servers;
        }

        public IEnumerable<Server> GetUnknownServers(Guid tenantId)
        {
            return _repository.Find(new ServersByStatusSpecification(Status.Unknown));
        }

        public TEntity GetByKey<TEntity>(Guid id) where TEntity : class
        {
            var item = _repository.GetByKey<TEntity>(id);

            return item;
        }


        public IEnumerable<TEntity> Find<TEntity>(Specification<TEntity> criteria) where TEntity : class
        {
            var item = _repository.Find<TEntity>(criteria);

            return item;
        }

        public IEnumerable<Server> GetClusterNodes(Guid clusterId)
        {
            var servers = _repository.Find<Server>(x => x.Cluster.Id.Equals(clusterId));
            return servers;
        }

        public int GetPagedEntities<TEntity>(int page, int pageSize, Specification<TEntity> criteria, Expression<Func<TEntity, string>> orderBy, out IEnumerable<TEntity> entities) where TEntity : class, IDomainObject
        {
            var totalRecords = _repository.Count(criteria);
            entities = _repository.Get(criteria, orderBy, page, pageSize);

            return totalRecords;
        }


        public bool SaveCustomerServerGroup(ref ServerGroup serverGroup, Guid parentId)
        {
            var saveSuccess = true;

            //Add new
            if (serverGroup.Id == Guid.Empty)
            {
                var customer = _repository.FindOne<Customer>(x => x.Id.Equals(parentId));
                serverGroup = ServerGroup.NewServerGroup(customer, serverGroup.Name, serverGroup.Priority);

                _repository.Add(serverGroup);
                _repository.UnitOfWork.SaveChanges();
            }
            else
            {
                _repository.Update(serverGroup);
                _repository.UnitOfWork.SaveChanges();
            }

            return saveSuccess;
        }

        public bool SaveServer(ref Server server)
        {
            var saveSuccess = true;

            //Edit Only!
            if (!server.Id.Equals(Guid.Empty))
            {
                if (server == null)
                {
                    saveSuccess = false;
                }
                else
                {
                    _repository.Update(server);
                    _repository.UnitOfWork.SaveChanges();
                }
            }

            return saveSuccess;
        }

        public bool SaveUser(ref User user, List<Guid> roleIds)
        {
            var saveSuccess = true;
            var updatedRoles = _repository.Find<Role>(x => roleIds.Contains(x.Id)).ToList();

            //Add new
            if (user.Id == Guid.Empty)
            {
                user.Id = Guid.NewGuid();
                user.Roles = updatedRoles;
                _repository.Add(user);
                _repository.UnitOfWork.SaveChanges();
            }
            else
            {
                var oldUser = _repository.GetByKey<User>(user.Id);
                oldUser.FirstName = user.FirstName;
                oldUser.LastName = user.LastName;
                oldUser.EmailAddress = user.EmailAddress;

                var rolesToRemove = oldUser.Roles.Where(x => !roleIds.Contains(x.Id)).ToList();

                //Only update password if is changes
                if (!string.IsNullOrEmpty(user.PasswordHash) && !string.IsNullOrEmpty(user.PasswordSalt))
                {
                    oldUser.PasswordHash = user.PasswordHash;
                    oldUser.PasswordSalt = user.PasswordSalt;
                }

                foreach (var deletedRoles in rolesToRemove)
                {
                    oldUser.Roles.Remove(deletedRoles);
                }

                foreach (var addedRole in updatedRoles)
                {
                    if (!oldUser.Roles.Any(x => x.Id.Equals(addedRole.Id)))
                    {
                        oldUser.Roles.Add(addedRole);
                    }
                }

                _repository.Update(oldUser);
                _repository.UnitOfWork.SaveChanges();
            }

            return saveSuccess;
        }

        /// <summary>
        /// Save the supplied customer object
        /// </summary>
        /// <param name="customer">a ref param containing the customer entity to save</param>
        /// <param name="tenantId">the tenant id of the customer entity to save</param>
        /// <returns></returns>
        public bool SaveCustomer(ref Customer customer, Guid tenantId)
        {
            var saveSuccess = true;

            //Add new
            if (customer.Id == Guid.Empty)
            {
                var tenant = _repository.GetByKey<Tenant>(tenantId);
                customer.Tenant = tenant;
                customer.Id = Guid.NewGuid();

                //Create the default server group
                var serverGroup = ServerGroup.NewServerGroup(customer, "Default");

                _repository.Add(customer);
                _repository.Add(serverGroup);
                _repository.UnitOfWork.SaveChanges();
            }
            else
            {
                //Workaround because ServiceDeskData isn't passed down to the client's browser in the MVC layer.
                var dbCustomer = _repository.GetByKey<Customer>(customer.Id);
                Mapper.CreateMap<Customer, Customer>()
                    .ForMember(dest => dest.ServiceDeskData, opt => opt.Ignore());

                Mapper.Map(customer, dbCustomer);

                _repository.Update(dbCustomer);
                _repository.UnitOfWork.SaveChanges();
            }

            return saveSuccess;
        }

        public bool SaveMaintenanceWindow(ref MaintenanceWindow maintenanceWindow, Guid maintenanceWindowParentId, MaintenanceWindowParentType parentType)
        {
            var saveSuccess = true;

            //Add new
            if (maintenanceWindow.Id == Guid.Empty)
            {
                maintenanceWindow.Id = Guid.NewGuid();
                IMaintenanceWindowParent parent = GetMaintenanceWindowParent(maintenanceWindowParentId, parentType);
                maintenanceWindow.ParentPreviousStatus = parent.Status;
                parent.MaintenanceWindows.Add(maintenanceWindow);

                _repository.Add(maintenanceWindow);
                _repository.Update(parent);
                _repository.UnitOfWork.SaveChanges();
            }
            else
            {
                var prevMaintWindow = _repository.GetByKey<MaintenanceWindow>(maintenanceWindow.Id);
                prevMaintWindow.BeginDate = maintenanceWindow.BeginDate;
                prevMaintWindow.EndDate = maintenanceWindow.EndDate;

                _repository.Update(prevMaintWindow);
                _repository.UnitOfWork.SaveChanges();
            }

            return saveSuccess;
        }

        public bool SaveMetricThreshold(ref MetricThreshold metricThreshold, Guid metricConfigurationId)
        {
            var saveSuccess = true;
            var metricConfig = _repository.GetByKey<MetricConfiguration>(metricConfigurationId);

            if (metricConfig != null)
            {
                //Add new
                if (metricThreshold.Id == Guid.Empty)
                {
                    metricThreshold.Id = Guid.NewGuid();
                    metricConfig.MetricThresholds.Add(metricThreshold);

                    _repository.Add(metricThreshold);
                    _repository.Update(metricConfig);
                    _repository.UnitOfWork.SaveChanges();
                }
                else
                {
                    _repository.Update(metricThreshold);
                    _repository.UnitOfWork.SaveChanges();
                }
            }
            else
            {
                saveSuccess = false;
            }

            return saveSuccess;
        }

        public bool SaveSchedule(ref Schedule schedule, Guid metricConfigurationId)
        {
            var saveSuccess = true;
            var metricConfig = _repository.GetByKey<MetricConfiguration>(metricConfigurationId);

            if (metricConfig != null)
            {
                //Add new
                if (schedule.Id == Guid.Empty)
                {
                    schedule.Id = Guid.NewGuid();
                    metricConfig.Schedules.Add(schedule);

                    _repository.Add(schedule);
                    _repository.Update(metricConfig);
                    _repository.UnitOfWork.SaveChanges();
                }
                else
                {
                    _repository.Update(schedule);
                    _repository.UnitOfWork.SaveChanges();
                }
            }
            else
            {
                saveSuccess = false;
            }

            return saveSuccess;
        }

        public bool SaveCluster(ref Cluster cluster, Guid customerId)
        {
            var saveSuccess = true;

            //Add new
            if (cluster.Id == Guid.Empty)
            {
                var customer = _repository.GetByKey<Customer>(customerId);
                cluster.Customer = customer;
                cluster.Id = Guid.NewGuid();

                _repository.Add(cluster);
                _repository.UnitOfWork.SaveChanges();
            }
            else
            {
                _repository.Update(cluster);
                _repository.UnitOfWork.SaveChanges();
            }

            return saveSuccess;
        }

        public bool SaveDatabaseInstance(ref DatabaseInstance databaseInstance, Guid serverId)
        {
            var saveSuccess = true;

            //Add new
            if (databaseInstance.Id == Guid.Empty)
            {
                var server = _repository.GetByKey<Server>(serverId);
                databaseInstance.Server = server;
                databaseInstance.Id = Guid.NewGuid();

                _repository.Add(databaseInstance);
                _repository.UnitOfWork.SaveChanges();

                AddDefaultDatabaseInstanceMetrics(databaseInstance.Id);
            }
            else
            {
                _repository.Update(databaseInstance);
                _repository.UnitOfWork.SaveChanges();
            }

            return saveSuccess;
        }

        public bool SaveDatabase(ref Database database, Guid instanceId)
        {
            var saveSuccess = true;

            //Add new
            if (database.Id == Guid.Empty)
            {
                var databaseInstance = _repository.GetByKey<DatabaseInstance>(instanceId);
                database.Instance = databaseInstance;
                database.Id = Guid.NewGuid();

                _repository.Add(database);
                _repository.UnitOfWork.SaveChanges();

                AddDefaultDatabaseMetrics(database.Id);
            }
            else
            {
                _repository.Update(database);
                _repository.UnitOfWork.SaveChanges();
            }

            return saveSuccess;
        }

        public bool SetClusterNodes(List<Guid> serverIds, Guid clusterId)
        {
            var success = true;
            var cluster = _repository.GetByKey<Cluster>(clusterId);

            if (cluster != null)
            {
                //Remove servers not in the list
                var serversToRemoveIds = cluster.Nodes.Where(x => !serverIds.Contains(x.Id)).Select(x => x.Id)
                                                        .Concat(cluster.VirtualServers.Where(x => !serverIds.Contains(x.Id)).Select(x => x.Id)).ToList();
                RemoveNodesFromCluster(serversToRemoveIds, clusterId);

                foreach (var serverId in serverIds)
                {
                    if (cluster.Nodes.Any(x => x.Id.Equals(serverId)) || cluster.VirtualServers.Any(x => x.Id.Equals(serverId)))
                    {
                        continue;
                    }

                    var server = _repository.GetByKey<Server>(serverId);

                    if (server != null)
                    {
                        //Associate the server to the cluster
                        server.Cluster = cluster;
                        _repository.Update(server);
                    }
                    else
                    {
                        success = false;
                        break;
                    }
                }
            }
            else
            {
                success = false;
            }

            _repository.UnitOfWork.SaveChanges();

            return success;
        }

        public bool SetServerGroupServers(List<Guid> serverIds, Guid serverGroupdId)
        {
            var success = true;
            var serverGroup = _repository.GetByKey<ServerGroup>(serverGroupdId);

            if (serverGroup != null)
            {
                //Remove servers not in the list
                var serversToRemoveIds = serverGroup.Servers.Where(x => !serverIds.Contains(x.Id)).Select(x => x.Id).ToList();
                RemoveServersFromServerGroup(serversToRemoveIds, serverGroupdId);

                foreach (var serverId in serverIds)
                {
                    if (serverGroup.Servers.Any(x => x.Id.Equals(serverId)))
                    {
                        continue;
                    }

                    var server = _repository.GetByKey<Server>(serverId);

                    if (server != null)
                    {
                        //Make sure it doesn't already exist
                        var existingId = serverGroup.Servers.Where(x => x.Id.Equals(server.Id)).FirstOrDefault();

                        if (existingId == null)
                        {
                            serverGroup.Servers.Add(server);
                            _repository.Update(serverGroup);
                        }
                    }
                    else
                    {
                        success = false;
                        break;
                    }
                }
            }
            else
            {
                success = false;
            }

            _repository.UnitOfWork.SaveChanges();

            return success;
        }

        public bool SetActiveServers(List<Guid> serverIds, Guid customerId)
        {
            var success = true;
            var customer = _repository.GetByKey<Customer>(customerId);

            if (customer != null)
            {
                //Remove servers not in the list
                var serversToRemoveIds = customer.Servers.Where(x => !serverIds.Contains(x.Id)).Select(x => x.Id).ToList();
                RemoveServersFromCustomer(serversToRemoveIds, customerId);

                foreach (var serverId in serverIds)
                {
                    if (customer.Servers.Any(x => x.Id.Equals(serverId)))
                    {
                        continue;
                    }

                    var server = _repository.GetByKey<Server>(serverId);

                    if (server != null)
                    {
                        //Associate the server to the custer and set active
                        server.Customer = customer;
                        server.Status = Status.Active;

                        //Add the server to the default server group
                        var defaultServerGroup = customer.ServerGroups.Where(x => x.Name == "Default").FirstOrDefault();

                        if (!defaultServerGroup.Servers.Any(x => x.Id.Equals(server.Id)))
                        {
                            defaultServerGroup.Servers.Add(server);

                            _repository.Update(customer);
                            _repository.Update(server);

                            AddDefaultServerMetrics(server.Id);
                        }
                    }
                    else
                    {
                        success = false;
                        break;
                    }
                }
            }
            else
            {
                success = false;
            }

            _repository.UnitOfWork.SaveChanges();

            return success;
        }

        /// <summary>
        /// Sets the server referenced by the given server id list to active, and associates it 
        /// to the customer referenced by the given customer id
        /// </summary>
        /// <param name="customerId">Id of the customer to which the server belongs</param>
        /// <param name="serverIds">Ids of the servers to activate</param>
        /// <returns></returns>
        public bool ActivateServers(List<Guid> serverIds, Guid customerId)
        {
            var activateSuccess = true;
            var customer = _repository.GetByKey<Customer>(customerId);

            if (customer != null && serverIds != null && serverIds.Count != 0)
            {
                foreach (var serverId in serverIds)
                {
                    var server = _repository.GetByKey<Server>(serverId);

                    if (server != null)
                    {
                        //Associate the server to the custer and set active
                        server.Customer = customer;
                        server.Status = Status.Active;

                        //Add the server to the default server group
                        var defaultServerGroup = customer.ServerGroups.Where(x => x.Name == "Default").FirstOrDefault();

                        if (defaultServerGroup != null)
                        {
                            defaultServerGroup.Servers.Add(server);
                        }

                        _repository.Update(customer);
                        _repository.Update(server);

                        AddDefaultServerMetrics(server.Id);
                    }
                    else
                    {
                        activateSuccess = false;
                        break;
                    }
                }
            }
            else
            {
                activateSuccess = false;
            }

            _repository.UnitOfWork.SaveChanges();

            return activateSuccess;
        }

        public bool AddMetricConfiguration(Guid metricId, Guid configParentId, MetricConfigurationParentType parentType, ref MetricConfiguration metricConfiguration)
        {
            var success = true;

            if (metricId.Equals(Guid.Empty) || configParentId.Equals(Guid.Empty))
            {
                success = false;
            }
            else
            {
                //Get the metric object
                var metric = _repository.GetByKey<Metric>(metricId);

                if (metric == null)
                {
                    success = false;
                }
                else
                {
                    IMetricConfigurationParent parent = GetMetricConfigurationParent(configParentId, parentType);

                    if (parent == null)
                    {
                        success = false;
                    }
                    else
                    {
                        metricConfiguration = MetricConfiguration.NewMetricConfiguration(parent, metric, metric.Name);
                        metric.MetricConfigurations.Add(metricConfiguration);
                        _repository.Add(metricConfiguration);
                        _repository.Update(metric);
                        _repository.UnitOfWork.SaveChanges();
                    }
                }
            }
            return success;
        }

        public bool AddVirtualServer(Guid clusterId, ref Server virtualServer)
        {
            var saveSuccess = true;

            var cluster = _repository.GetByKey<Cluster>(clusterId);

            if (cluster != null)
            {
                var serverGroup = _repository.Find<ServerGroup>(x => x.ParentCustomer.Id.Equals(cluster.Customer.Id) && x.Name.Equals("Default")).FirstOrDefault();

                virtualServer.Id = Guid.NewGuid();
                virtualServer.VirtualServerParent = cluster;
                virtualServer.IsVirtual = true;
                virtualServer.Status = Status.Active;
                virtualServer.Tenant = cluster.Customer.Tenant;
                virtualServer.LastCheckIn = DateTime.Now;
                virtualServer.Customer = cluster.Customer;
                virtualServer.IpAddress = string.Empty;

                serverGroup.Servers.Add(virtualServer);

                _repository.Update(virtualServer);
                _repository.Update(serverGroup);
                _repository.UnitOfWork.SaveChanges();
            }
            else
            {
                saveSuccess = false;
            }

            saveSuccess = AddDefaultVirtualServerMetrics(virtualServer.Id);

            return saveSuccess;
        }

        public bool AddNodesToCluster(List<Guid> serverIds, Guid clusterId)
        {
            var activateSuccess = true;
            var cluster = _repository.GetByKey<Cluster>(clusterId);

            if (cluster != null && serverIds != null && serverIds.Count != 0)
            {
                foreach (var serverId in serverIds)
                {
                    var server = _repository.GetByKey<Server>(serverId);

                    if (server != null)
                    {
                        //Associate the server to the cluster
                        server.Cluster = cluster;
                        _repository.Update(server);
                    }
                    else
                    {
                        activateSuccess = false;
                        break;
                    }
                }
            }
            else
            {
                activateSuccess = false;
            }

            _repository.UnitOfWork.SaveChanges();

            return activateSuccess;
        }

        public bool RemoveNodesFromCluster(List<Guid> nodeIds, Guid clusterId)
        {
            var removeSuccess = true;
            var cluster = _repository.GetByKey<Cluster>(clusterId);

            if (cluster != null && nodeIds != null && nodeIds.Count != 0)
            {
                foreach (var serverId in nodeIds)
                {
                    var server = _repository.GetByKey<Server>(serverId);

                    if (server != null)
                    {
                        if (server.IsVirtual)
                        {
                            cluster.VirtualServers.Remove(server);
                        }
                        else
                        {
                            cluster.Nodes.Remove(server);
                        }
                        _repository.Update(cluster);
                    }
                    else
                    {
                        removeSuccess = false;
                        break;
                    }
                }
            }
            else
            {
                removeSuccess = false;
            }

            _repository.UnitOfWork.SaveChanges();

            return removeSuccess;
        }

        public bool RemoveServersFromServerGroup(List<Guid> serverIds, Guid serverGroupId)
        {
            var removeSuccess = true;
            var serverGroup = _repository.GetByKey<ServerGroup>(serverGroupId);

            if (serverGroup != null && serverIds != null && serverIds.Count != 0)
            {
                foreach (var serverId in serverIds)
                {
                    var server = _repository.GetByKey<Server>(serverId);

                    if (server != null)
                    {
                        //Make sure it isn't the default group
                        if (serverGroup.Name != "Default")
                        {
                            serverGroup.Servers.Remove(server);
                            _repository.Update(serverGroup);
                        }
                        else
                        {
                            removeSuccess = false;
                        }
                    }
                    else
                    {
                        removeSuccess = false;
                        break;
                    }
                }
            }
            else
            {
                removeSuccess = false;
            }

            _repository.UnitOfWork.SaveChanges();

            return removeSuccess;
        }

        public bool RemoveServersFromCustomer(List<Guid> serverIds, Guid customerId)
        {
            var removeSuccess = true;
            var customer = _repository.GetByKey<Customer>(customerId);

            if (customer != null && serverIds != null && serverIds.Count != 0)
            {
                foreach (var serverId in serverIds)
                {
                    var server = _repository.GetByKey<Server>(serverId);

                    if (server != null)
                    {
                        customer.Servers.Remove(server);
                        server.Status = Status.Unknown;

                        foreach (var serverGroup in server.ServerGroups)
                        {
                            serverGroup.Servers.Remove(server);
                        }

                        if (server.Cluster != null)
                        {
                            server.Cluster.Nodes.Remove(server);
                        }

                        //Delete the associated Instances, Metric Instances, Configs, and Mainenance Windows
                        var instanceIds = server.Instances.Select(x => x.Id).ToList();
                        DeleteInstances(instanceIds);

                        var metricInstanceIds = server.MetricInstances.Where(x => ((x.Metric.MetricType.Value & (int)MetricType.Server) == (int)MetricType.Server &&
                                                                                                        x.Server.Id.Equals(server.Id) &&
                                                                                                        x.Status.Value != (int)Status.Deleted))
                                                                                                .Select(x => x.Id).ToList();
                        DeleteMetricInstances(metricInstanceIds);

                        var metricConfigurationIds = server.MetricConfigurations.Select(x => x.Id).ToList();
                        DeleteMetricConfigurations(metricConfigurationIds);

                        var maintenanceWindowIds = server.MaintenanceWindows.Select(x => x.Id).ToList();
                        DeleteMaintenanceWindows(maintenanceWindowIds);


                        _repository.Update(server);
                        _repository.Update(customer);
                    }
                    else
                    {
                        removeSuccess = false;
                        break;
                    }
                }
            }
            else
            {
                removeSuccess = false;
            }

            _repository.UnitOfWork.SaveChanges();

            return removeSuccess;
        }

        public bool AddServersToGroup(List<Guid> serverIds, Guid serverGroupId)
        {
            var activateSuccess = true;
            var serverGroup = _repository.GetByKey<ServerGroup>(serverGroupId);

            if (serverGroup != null && serverIds != null && serverIds.Count != 0)
            {
                foreach (var serverId in serverIds)
                {
                    var server = _repository.GetByKey<Server>(serverId);

                    if (server != null)
                    {
                        //Make sure it doesn't already exist
                        var existingId = serverGroup.Servers.Where(x => x.Id.Equals(server.Id)).FirstOrDefault();

                        if (existingId == null)
                        {
                            serverGroup.Servers.Add(server);
                            _repository.Update(serverGroup);
                        }
                    }
                    else
                    {
                        activateSuccess = false;
                        break;
                    }
                }
            }
            else
            {
                activateSuccess = false;
            }

            _repository.UnitOfWork.SaveChanges();

            return activateSuccess;
        }

        public bool AuthenticateUser(string userName, string password)
        {
            var authenticated = false;
            var user = _repository.FindOne(new Specification<User>(u => u.EmailAddress == userName));

            user.Roles.Add(_repository.Find<Role>(x => x.Name == "DeltaAdmin").First());
            _repository.Update(user);
            _repository.UnitOfWork.SaveChanges();

            if (user != null && user.HasPassword(password))
            {
                authenticated = true;
            }

            return authenticated;
        }

        public User AddUser(string userName, string firstName, string lastName, string password)
        {
            var user = User.NewUser(userName, firstName, lastName, password);

            _repository.Add(user);
            _repository.UnitOfWork.SaveChanges();

            return user;
        }
        #endregion

        #region Update Methods

        /// <summary> 
        /// Updates a <see cref="T:Datavail.Delta.Domain.Server"/> object's LastCheckIn timestamp. If the specified Server does not exist, it is created.
        /// </summary> 
        /// <param name="tenantId">A <see cref="T:System.Guid"/> representing the <see cref="T:Datavail.Delta.Domain.Tenant"/> that the server belongs to.</param>
        /// <param name="serverId">A <see cref="T:System.Guid"/> representing the <see cref="T:Datavail.Delta.Domain.Server"/>.</param>
        /// <param name="hostname">A <see cref="T:System.String"/> containing the hostname of the Server.</param>
        /// <param name="ipAddress">A <see cref="T:System.String"/> containing the IP Address of the server.</param>
        /// <param name="agentVersion">A <see cref="T:System.String"/> containing the version number of the agent software running on the server.</param> 
        /// <param name="customerId">A <see cref="T:System.Guid"/> representing the <see cref="T:Datavail.Delta.Domain.Customer"/> that the server belongs to.</param>
        public void CheckIn(Guid tenantId, Guid serverId, string hostname, string ipAddress, string agentVersion, Guid? customerId = null)
        {
            Datavail.Delta.Infrastructure.Util.Guard.IsNotNull(tenantId);
            Datavail.Delta.Infrastructure.Util.Guard.IsNotNull(serverId);
            Datavail.Delta.Infrastructure.Util.Guard.IsNotNull(hostname);
            Datavail.Delta.Infrastructure.Util.Guard.IsNotNull(ipAddress);

            var server = _repository.GetByKey<Server>(serverId);
            if (server == null)
            {
                var tenant = _repository.GetByKey<Tenant>(tenantId);
                Datavail.Delta.Infrastructure.Util.Guard.IsNotNull(tenant, "Invalid TenantId Specified");

                server = Server.NewServer(tenant, serverId, hostname, ipAddress);

                ServerGroup serverGroup = null;
                if (customerId != null)
                {
                    var customer = _repository.GetByKey<Customer>(customerId);

                    //Guard against the user providing an invalid customer id during agent install
                    if (customer != null)
                    {
                        server.Customer = customer;
                        server.Status = Status.Active;

                        serverGroup = customer.ServerGroups.Where(s => s.Name == "Default").FirstOrDefault();
                        if (serverGroup != null)
                        {
                            serverGroup.Servers.Add(server);
                        }
                    }
                    else
                    {
                        server.Status = Status.Unknown;
                    }
                }
                else
                {
                    server.Status = Status.Unknown;
                }

                server.AgentVersion = agentVersion;
                server.LastCheckIn = DateTime.UtcNow;

                _repository.Add(server);

                if (serverGroup != null)
                {
                    _repository.Update(serverGroup);
                }
                _repository.UnitOfWork.SaveChanges();

                //Add default server metrics
                AddDefaultServerMetrics(serverId);
            }
            else
            {
                server.AgentVersion = agentVersion;
                server.Hostname = hostname;
                server.IpAddress = ipAddress;
                server.LastCheckIn = DateTime.UtcNow;

                _repository.Update(server);
                _repository.UnitOfWork.SaveChanges();
            }
        }

        public void UpdateCustomer(Guid serverId, Guid customerId)
        {
            if (serverId == Guid.Empty)
            {
                throw new InvalidOperationException(ApplicationErrors.InvalidServerId);
            }

            if (customerId == Guid.Empty)
            {
                throw new InvalidOperationException(ApplicationErrors.InvalidCustomerId);
            }

            var server = GetServerById(serverId);
            var customer = GetCustomerById(customerId);

            server.Customer = customer;
            server.Status = Status.Inactive;

            _repository.Update(server);
            _repository.UnitOfWork.SaveChanges();
        }

        public bool AddDefaultServerMetrics(Guid serverId)
        {
            var success = true;
            var server = _repository.GetByKey<Server>(serverId);


            if (server != null)
            {
                //CheckinPlugin
                if (!server.MetricInstances.Any(x => x.Metric.AdapterClass == "CheckInPlugin" && x.Status.Value != (int)Status.Deleted))
                {
                    if (!server.IsVirtual)
                    {
                        var checkInMetric =
                            _repository.Find<Metric>(x => x.AdapterClass == "CheckInPlugin").OrderBy(
                                x => x.AdapterVersion).LastOrDefault();
                        SaveMetricInstance(Guid.Empty, checkInMetric.Id, serverId,
                                           GetMetricData(checkInMetric.Id, serverId), Status.Active,
                                           MetricInstanceParentType.Server);
                    }
                }

                //DiskInventoryPlugin
                if (!server.MetricInstances.Any(x => x.Metric.AdapterClass == "DiskInventoryPlugin" && x.Status.Value != (int)Status.Deleted))
                {
                    var diskInventoryMetric = _repository.Find<Metric>(x => x.AdapterClass == "DiskInventoryPlugin").OrderBy(x => x.AdapterVersion).LastOrDefault();
                    SaveMetricInstance(Guid.Empty, diskInventoryMetric.Id, serverId, GetMetricData(diskInventoryMetric.Id, serverId), Status.Active, MetricInstanceParentType.Server);
                }

                //DiskPlugin
                if (!server.MetricInstances.Any(x => x.Metric.AdapterClass == "DiskPlugin" && x.Status.Value != (int)Status.Deleted))
                {
                    var serverDisks = _repository.Find<ServerDisk>(x => x.Server.Id.Equals(server.Id)).ToList();
                    var diskMetric = _repository.Find<Metric>(x => x.AdapterClass == "DiskPlugin").OrderBy(x => x.AdapterVersion).LastOrDefault();

                    foreach (var disk in serverDisks)
                    {
                        var metricData = GetMetricData(diskMetric.Id, serverId);
                        var pathData = metricData.Data.Where(x => x.TagName == "Path").FirstOrDefault();

                        pathData.Value = disk.Path;

                        SaveMetricInstance(Guid.Empty, diskMetric.Id, serverId, metricData, Status.Active, MetricInstanceParentType.Server);
                    }
                }
            }
            else
            {
                success = false;
            }

            return success;
        }

        public bool AddDefaultVirtualServerMetrics(Guid virtualServerId)
        {
            var success = true;

            AddDefaultServerMetrics(virtualServerId);

            return success;
        }

        public bool AddDefaultDatabaseInstanceMetrics(Guid databaseInstanceId)
        {
            var success = true;
            var databaseInstance = _repository.GetByKey<DatabaseInstance>(databaseInstanceId);

            if (databaseInstance != null)
            {

                //DatabaseInventoryPlugin
                if (!databaseInstance.Server.MetricInstances.Any(x => x.Metric.AdapterClass == "DatabaseInventoryPlugin" &&
                                                                        x.Status.Value != (int)Status.Deleted &&
                                                                        x.Metric.DatabaseVersion.Value == databaseInstance.DatabaseVersion.Value &&
                                                                        x.DatabaseInstance.Id.Equals(databaseInstance.Id)))
                {
                    var databaseInventoryMetric = _repository.Find<Metric>(x => x.AdapterClass == "DatabaseInventoryPlugin" &&
                                                                                    x.DatabaseVersion.Value == databaseInstance.DatabaseVersion.Value)
                                                                                    .OrderBy(x => x.AdapterVersion)
                                                                                    .LastOrDefault();

                    SaveMetricInstance(Guid.Empty, databaseInventoryMetric.Id, databaseInstance.Id, GetMetricData(databaseInventoryMetric.Id, databaseInstance.Id), Status.Active, MetricInstanceParentType.Instance);
                }

                //JobsInventoryPlugin
                if (!databaseInstance.Server.MetricInstances.Any(x => x.Metric.AdapterClass == "SqlAgentJobInventoryPlugin" &&
                                                                        x.Status.Value != (int)Status.Deleted &&
                                                                        x.Metric.DatabaseVersion.Value == databaseInstance.DatabaseVersion.Value &&
                                                                        x.DatabaseInstance.Id.Equals(databaseInstance.Id)))
                {
                    var databaseInventoryMetric = _repository.Find<Metric>(x => x.AdapterClass == "SqlAgentJobInventoryPlugin" &&
                                                                                    x.DatabaseVersion.Value == databaseInstance.DatabaseVersion.Value)
                                                                                    .OrderBy(x => x.AdapterVersion)
                                                                                    .LastOrDefault();

                    SaveMetricInstance(Guid.Empty, databaseInventoryMetric.Id, databaseInstance.Id, GetMetricData(databaseInventoryMetric.Id, databaseInstance.Id), Status.Active, MetricInstanceParentType.Instance);
                }

                //DatabaseServerJobsPlugin
                if (databaseInstance.Jobs != null)
                {
                    var databaseServerJobsMetric = _repository.Find<Metric>(x => x.AdapterClass == "DatabaseServerJobsPlugin" &&
                                                                               x.DatabaseVersion.Value == databaseInstance.DatabaseVersion.Value)
                                                                               .OrderBy(x => x.AdapterVersion)
                                                                               .LastOrDefault();

                    var existingJobsMetrics = databaseInstance.Server.MetricInstances.Where(x => x.Metric.AdapterClass == "DatabaseServerJobsPlugin" &&
                                                                                                x.Status.Value != (int)Status.Deleted &&
                                                                                                x.Metric.DatabaseVersion.Value == databaseInstance.DatabaseVersion.Value &&
                                                                                                x.DatabaseInstance.Id.Equals(databaseInstance.Id)).ToList();

                    foreach (var job in databaseInstance.Jobs)
                    {
                        //Check existing job metrics to see if it exists already
                        if (!existingJobsMetrics.Any(x => XElement.Parse(x.Data).Attribute("JobName").Value.Equals(job.Name)))
                        {
                            var metricData = GetMetricData(databaseServerJobsMetric.Id, databaseInstance.Id);

                            var jobData = metricData.Data.Where(x => x.TagName == "JobName").FirstOrDefault();
                            jobData.Value = job.Name;

                            SaveMetricInstance(Guid.Empty, databaseServerJobsMetric.Id, databaseInstance.Id, metricData, Status.Active, MetricInstanceParentType.Instance);
                        }
                    }
                }

                //SqlAgentStatusPlugin
                if (!databaseInstance.Server.MetricInstances.Any(x => x.Metric.AdapterClass == "SqlAgentStatusPlugin" &&
                                                                        x.Metric.DatabaseVersion.Value == databaseInstance.DatabaseVersion.Value &&
                                                                        x.Status.Value != (int)Status.Deleted &&
                                                                        x.DatabaseInstance.Id.Equals(databaseInstance.Id)))
                {
                    var sqlAgentStatusMetric = _repository.Find<Metric>(x => x.AdapterClass == "SqlAgentStatusPlugin" &&
                                                                                    x.DatabaseVersion.Value == databaseInstance.DatabaseVersion.Value)
                                                                                    .OrderBy(x => x.AdapterVersion)
                                                                                    .LastOrDefault();

                    SaveMetricInstance(Guid.Empty, sqlAgentStatusMetric.Id, databaseInstance.Id, GetMetricData(sqlAgentStatusMetric.Id, databaseInstance.Id), Status.Active, MetricInstanceParentType.Instance);
                }
            }
            else
            {
                success = false;
            }

            return success;
        }

        public bool AddDefaultDatabaseMetrics(Guid databaseId)
        {
            var success = true;
            var database = _repository.GetByKey<Database>(databaseId);

            if (database != null)
            {
                //DatabaseBackupStatusPlugin
                if (database.Name != "tempdb" && !database.Instance.Server.MetricInstances.Any(x => x.Metric.AdapterClass == "DatabaseBackupStatusPlugin" &&

                                                                        x.Metric.DatabaseVersion.Value == database.Instance.DatabaseVersion.Value &&
                                                                        x.Status.Value != (int)Status.Deleted &&
                                                                        x.Database.Id.Equals(database.Id)))
                {
                    var databaseInventoryMetric = _repository.Find<Metric>(x => x.AdapterClass == "DatabaseBackupStatusPlugin" &&
                                                                                    x.DatabaseVersion.Value == database.Instance.DatabaseVersion.Value)
                                                                                    .OrderBy(x => x.AdapterVersion)
                                                                                    .LastOrDefault();

                    SaveMetricInstance(Guid.Empty, databaseInventoryMetric.Id, database.Id, GetMetricData(databaseInventoryMetric.Id, database.Id), Status.Active, MetricInstanceParentType.Database);
                }

                //DatabaseStatusPlugin
                if (!database.Instance.Server.MetricInstances.Any(x => x.Metric.AdapterClass == "DatabaseStatusPlugin" &&
                                                                        x.Metric.DatabaseVersion.Value == database.Instance.DatabaseVersion.Value &&
                                                                        x.Status.Value != (int)Status.Deleted &&
                                                                        x.Database.Id.Equals(database.Id)))
                {
                    var databaseInventoryMetric = _repository.Find<Metric>(x => x.AdapterClass == "DatabaseStatusPlugin" &&
                                                                                    x.DatabaseVersion.Value == database.Instance.DatabaseVersion.Value)
                                                                                    .OrderBy(x => x.AdapterVersion)
                                                                                    .LastOrDefault();

                    SaveMetricInstance(Guid.Empty, databaseInventoryMetric.Id, database.Id, GetMetricData(databaseInventoryMetric.Id, database.Id), Status.Active, MetricInstanceParentType.Database);
                }
            }
            else
            {
                success = false;
            }

            return success;
        }

        public void UpdateStatus(Guid serverId, Status status)
        {

            if (serverId == Guid.Empty)
            {
                throw new InvalidOperationException(ApplicationErrors.InvalidServerId);
            }

            if (status.GetType() != typeof(Status))
            {

            }


            var server = GetServerById(serverId);
            server.Status = status;

            _repository.Update(server);
            _repository.UnitOfWork.SaveChanges();
        }
        #endregion

        #region Helper Methods
        private Server GetServerById(Guid serverId)
        {
            var server = _repository.GetByKey<Server>(serverId);
            if (server == null) throw new InvalidOperationException(ApplicationErrors.InvalidServerId);

            return server;
        }

        private Customer GetCustomerById(Guid customerId)
        {
            var customer = _repository.GetByKey<Customer>(customerId);
            if (customer == null) throw new InvalidOperationException(ApplicationErrors.InvalidCustomerId);

            return customer;
        }

        private static IEnumerable<XElement> CreateMetricInstanceNodes(MetricInstance metricInstance, MetricConfiguration metricConfiguration)
        {
            var nodes = new List<XElement>();

            foreach (var schedule in metricConfiguration.Schedules)
            {
                var miXml = new XElement("MetricInstance");
                miXml.Add(new XAttribute("Id", metricInstance.Id.ToString()));
                miXml.Add(new XAttribute("AdapterAssembly", metricInstance.Metric.AdapterAssembly));
                miXml.Add(new XAttribute("AdapterClass", metricInstance.Metric.AdapterClass));
                miXml.Add(new XAttribute("AdapterVersion", metricInstance.Metric.AdapterVersion));
                miXml.Add(new XAttribute("Label", metricInstance.Label));
                miXml.Add(new XAttribute("Data", metricInstance.Data));
                miXml.Add(new XAttribute("ScheduleType", schedule.ScheduleType.Value));
                miXml.Add(new XAttribute("ScheduleInterval", schedule.Interval));
                miXml.Add((new XAttribute("MetricConfigurationId", metricConfiguration.Id)));

                switch (schedule.ScheduleType.Enum)
                {
                    case ScheduleType.Seconds:
                        break;
                    case ScheduleType.Minutes:
                        break;
                    case ScheduleType.Hours:
                        miXml.Add(new XAttribute("ScheduleMinute", schedule.Minute));
                        break;
                    case ScheduleType.Days:
                        miXml.Add(new XAttribute("ScheduleMinute", schedule.Minute));
                        miXml.Add(new XAttribute("ScheduleHour", schedule.Hour));
                        break;
                    case ScheduleType.Weeks:
                        miXml.Add(new XAttribute("ScheduleMinute", schedule.Minute));
                        miXml.Add(new XAttribute("ScheduleHour", schedule.Hour));
                        miXml.Add(new XAttribute("ScheduleDay", schedule.DayOfWeek));
                        break;
                    case ScheduleType.Months:
                        miXml.Add(new XAttribute("ScheduleMinute", schedule.Minute));
                        miXml.Add(new XAttribute("ScheduleHour", schedule.Hour));
                        miXml.Add(new XAttribute("ScheduleDay", schedule.Day));
                        break;
                    case ScheduleType.Year:
                        miXml.Add(new XAttribute("ScheduleMinute", schedule.Minute));
                        miXml.Add(new XAttribute("ScheduleHour", schedule.Hour));
                        miXml.Add(new XAttribute("ScheduleDay", schedule.Day));
                        break;
                }
                nodes.Add(miXml);
            }
            return nodes;
        }
        #endregion

        #region Validation
        public bool ValidateCustomer(Customer customer, out List<string> errors)
        {
            errors = new List<string>();
            var customers = _repository.Find<Customer>(x => x.Name.Equals(customer.Name) && x.Id != customer.Id);

            if (customers.Any())
            {
                errors.Add("The Customer name is already in use");
            }

            _repository.UnitOfWork.SaveChanges();

            return !errors.Any();
        }

        public bool ValidateMaintenanceWindow(MaintenanceWindow maintenanceWindow, out List<string> errors)
        {
            errors = new List<string>();
            //TODO add validation rules if/when necessary
            return true;
        }

        public bool ValidateMetricThreshold(MetricThreshold metricThreshold, out List<string> errors)
        {
            errors = new List<string>();
            //TODO add validation rules if/when necessary
            return true;
        }

        public bool ValidateSchedule(Schedule schedule, out List<string> errors)
        {
            errors = new List<string>();
            //TODO add validation rules if/when necessary
            return true;
        }

        public bool ValidateMetricConfiguration(MetricConfiguration metricConfiguration, out List<string> errors)
        {
            errors = new List<string>();
            //TODO add validation rules if/when neccessary
            return true;
        }

        public bool ValidateDbInstance(DatabaseInstance databaseInstance, out List<string> errors)
        {
            errors = new List<string>();

            if (!databaseInstance.UseIntegratedSecurity && (string.IsNullOrEmpty(databaseInstance.Username) || string.IsNullOrEmpty(databaseInstance.Password)))
            {
                errors.Add("Username and Password are required when integrated security is not selected");
            }

            return !errors.Any();
        }

        public bool ValidateUser(User user, out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrEmpty(user.EmailAddress) || string.IsNullOrEmpty(user.FirstName) || string.IsNullOrEmpty(user.LastName))
            {
                errors.Add("The user is missing the required fields: Email Address, First Name, or Last Name");
            }

            if (user.Id.Equals(Guid.Empty) && (string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(user.PasswordSalt)))
            {
                errors.Add("The user password is a required field");
            }

            if (user.Id.Equals(Guid.Empty) && _repository.GetQuery<User>().Any(x => x.EmailAddress.Equals(user.EmailAddress)))
            {
                errors.Add("The supplied email address is already in use");
            }

            return !errors.Any();
        }

        public bool ValidateServerGroup(ServerGroup serverGroup, Guid parentId, out List<string> errors)
        {
            errors = new List<string>();
            Specification<ServerGroup> criteria = new ServerGroupsByParentSpecification(parentId);
            criteria = criteria.And(x => !x.Id.Equals(serverGroup.Id));
            criteria = criteria.And(x => x.Priority.Equals(serverGroup.Priority));
            criteria = criteria.And(x => x.Status.Value != (int)Status.Deleted);

            var serverGroups = _repository.Find<ServerGroup>(criteria);

            if (serverGroups.Any())
            {
                errors.Add("The Server Group priority is already in use");
            }

            //Fix to avoid attaching existing entities
            _repository.UnitOfWork.SaveChanges();
            return !errors.Any();
        }
        #endregion

        #region ServerGroup
        public bool DeleteDefaultServerGroup(Guid customerId)
        {
            var deleteSuccess = true;
            var serverGroup = _repository.Find<ServerGroup>(x => x.ParentCustomer.Id.Equals(customerId) && x.Name.Equals("Default")).FirstOrDefault();

            if (serverGroup != null)
            {
                serverGroup.Status = Status.Deleted;

                //Remove existing servers from the group
                for (int i = 0; i < serverGroup.Servers.Count; i++)
                {
                    serverGroup.Servers.Remove(serverGroup.Servers[i]);
                }

                _repository.Update(serverGroup);

                //Delete the associated metric configs and maintenance windows
                var metricConfigIds = serverGroup.MetricConfigurations.Select(x => x.Id).ToList();
                DeleteMetricConfigurations(metricConfigIds);

                var maintenanceWindowIds = serverGroup.MaintenanceWindows.Select(x => x.Id).ToList();
                DeleteMaintenanceWindows(maintenanceWindowIds);
            }
            else
            {
                deleteSuccess = false;
            }

            return deleteSuccess;
        }

        public bool DeleteServerGroups(List<Guid> serverGroupIds)
        {
            var deleteSuccess = true;
            var entities = _repository.Find<ServerGroup>(x => serverGroupIds.Contains(x.Id)).ToList();

            foreach (var serverGroup in entities)
            {
                if (serverGroup.Name != "Default")
                {
                    serverGroup.Status = Status.Deleted;

                    //Remove existing servers from the group
                    for (int i = 0; i < serverGroup.Servers.Count; i++)
                    {
                        serverGroup.Servers.Remove(serverGroup.Servers[i]);
                    }

                    _repository.Update(serverGroup);

                    //Delete the associated metric configs and maintenance windows
                    var metricConfigIds = serverGroup.MetricConfigurations.Select(x => x.Id).ToList();
                    DeleteMetricConfigurations(metricConfigIds);

                    var maintenanceWindowIds = serverGroup.MaintenanceWindows.Select(x => x.Id).ToList();
                    DeleteMaintenanceWindows(maintenanceWindowIds);
                }
                else
                {
                    deleteSuccess = false;
                }
            }

            _repository.UnitOfWork.SaveChanges();

            return deleteSuccess;
        }
        #endregion

        public IEnumerable<object> GetCustomerNames(string searchTerm)
        {
            var customerNames = _repository.Find<Customer>(x => x.Name.StartsWith(searchTerm))
                                            .Where(x => x.Status.Value != (int)Status.Deleted)
                                            .Select(x => new { name = x.Name, id = x.Id }).OrderBy(x => x.name).ToList();

            return customerNames;
        }

        public IEnumerable<object> GetServerNames(Guid customerId, string searchTerm)
        {
            var serverNames = _repository.Find<Server>(x => x.Customer.Id.Equals(customerId) && x.Hostname.StartsWith(searchTerm))
                                        .Select(x => new { name = x.Hostname, id = x.Id }).OrderBy(x => x.name).ToList();

            return serverNames;
        }

        public bool DeleteCustomers(List<Guid> customerIds)
        {
            var deleteSuccess = true;
            var entities = _repository.Find<Customer>(x => customerIds.Contains(x.Id)).ToList();

            foreach (var customer in entities)
            {
                customer.Status = Status.Deleted;
                _repository.Update(customer);

                //Delete the associated servers, servergroups, clusters, configs, and maint windows
                var serverGroupIds = customer.ServerGroups.Select(x => x.Id).ToList();
                DeleteServerGroups(serverGroupIds);

                //Explicitly delete the default Server Group
                DeleteDefaultServerGroup(customer.Id);

                //Delete servers
                var serverIds = customer.Servers.Select(x => x.Id).ToList();
                DeleteServers(serverIds);

                //Delete clusters
                var clusterIds = customer.Clusters.Select(x => x.Id).ToList();
                DeleteClusters(clusterIds);

                //Delete configs
                var metricConfigurationIds = customer.MetricConfigurations.Select(x => x.Id).ToList();
                DeleteMetricConfigurations(metricConfigurationIds);

                //Delete maintenance windows
                var maintenanceWindowIds = customer.MaintenanceWindows.Select(x => x.Id).ToList();
                DeleteMaintenanceWindows(maintenanceWindowIds);
            }

            _repository.UnitOfWork.SaveChanges();



            return deleteSuccess;
        }

        public bool DeleteMaintenanceWindows(List<Guid> maintenanceWindowIds)
        {
            var deleteSuccess = true;
            var entities = _repository.Find<MaintenanceWindow>(x => maintenanceWindowIds.Contains(x.Id));

            foreach (var maintenanceWindow in entities)
            {
                if (DateTime.Now >= maintenanceWindow.BeginDate && DateTime.Now <= maintenanceWindow.EndDate)
                {
                    deleteSuccess = false;
                }
                else
                {
                    _repository.Delete(maintenanceWindow);

                }
            }

            if (deleteSuccess)
            {
                _repository.UnitOfWork.SaveChanges();
            }

            return deleteSuccess;
        }

        public bool DeleteMetricThresholds(List<Guid> metricThresholdIds)
        {
            var deleteSuccess = true;
            var entities = _repository.Find<MetricThreshold>(x => metricThresholdIds.Contains(x.Id));

            foreach (var metricThreshold in entities)
            {
                _repository.Delete(metricThreshold);
            }

            _repository.UnitOfWork.SaveChanges();

            return deleteSuccess;
        }

        public bool DeleteMetricConfigurations(List<Guid> metricConfigurationIds)
        {
            var deleteSuccess = true;
            var entities = _repository.Find<MetricConfiguration>(x => metricConfigurationIds.Contains(x.Id)).ToList();

            foreach (var id in metricConfigurationIds)
            {
                var metricConfig = entities.Where(x => x.Id.Equals(id)).FirstOrDefault();

                if (metricConfig != null)
                {
                    foreach (var threshold in metricConfig.MetricThresholds.ToList())
                    {
                        _repository.Delete(threshold);
                    }

                    foreach (var schedule in metricConfig.Schedules.ToList())
                    {
                        _repository.Delete(schedule);
                    }

                    _repository.Delete(metricConfig);
                }
            }

            _repository.UnitOfWork.SaveChanges();

            return deleteSuccess;
        }

        public bool DeleteSchedules(List<Guid> scheduleIds)
        {
            var deleteSuccess = true;
            var entities = _repository.Find<Schedule>(x => scheduleIds.Contains(x.Id));

            foreach (var schedule in entities)
            {
                _repository.Delete(schedule);
            }

            _repository.UnitOfWork.SaveChanges();

            return deleteSuccess;
        }

        public bool DeleteServers(List<Guid> serverIds)
        {
            var deleteSuccess = true;
            var entities = _repository.Find<Server>(x => serverIds.Contains(x.Id)).ToList();

            foreach (var server in entities)
            {
                server.Status = Status.Deleted;
                _repository.Update(server);

                //Delete the associated Instances, Metric Instances, Configs, and Mainenance Windows
                var instanceIds = server.Instances.Select(x => x.Id).ToList();
                DeleteInstances(instanceIds);

                var metricInstanceIds = server.MetricInstances.Where(x => ((x.Metric.MetricType.Value & (int)MetricType.Server) == (int)MetricType.Server &&
                                                                                                x.Server.Id.Equals(server.Id) &&
                                                                                                x.Status.Value != (int)Status.Deleted))
                                                                                        .Select(x => x.Id).ToList();
                DeleteMetricInstances(metricInstanceIds);

                var metricConfigurationIds = server.MetricConfigurations.Select(x => x.Id).ToList();
                DeleteMetricConfigurations(metricConfigurationIds);

                var maintenanceWindowIds = server.MaintenanceWindows.Select(x => x.Id).ToList();
                DeleteMaintenanceWindows(maintenanceWindowIds);
            }

            _repository.UnitOfWork.SaveChanges();

            return deleteSuccess;
        }

        public bool DeleteUsers(List<Guid> userIds)
        {
            var deleteSuccess = true;
            var entities = _repository.Find<User>(x => userIds.Contains(x.Id));

            foreach (var user in entities)
            {
                _repository.Delete(user);
            }

            _repository.UnitOfWork.SaveChanges();

            return deleteSuccess;
        }

        public bool DeleteClusters(List<Guid> clusterIds)
        {
            var deleteSuccess = true;
            var entities = _repository.Find<Cluster>(x => clusterIds.Contains(x.Id)).ToList();

            foreach (var cluster in entities)
            {
                cluster.Status = Status.Deleted;

                //Remove Nodes
                foreach (var node in _repository.Find<Server>(x => x.Cluster.Id.Equals(cluster.Id)).ToList())
                {
                    cluster.Nodes.Remove(node);
                }

                //Remove virtual servers
                foreach (var virtualServer in _repository.Find<Server>(x => x.VirtualServerParent.Id.Equals(cluster.Id)).ToList())
                {
                    cluster.VirtualServers.Remove(virtualServer);
                }

                _repository.Update(cluster);
            }

            _repository.UnitOfWork.SaveChanges();

            return deleteSuccess;

        }

        public bool DeleteInstances(List<Guid> instanceIds)
        {
            var deleteSuccess = true;
            var entities = _repository.Find<DatabaseInstance>(x => instanceIds.Contains(x.Id)).ToList();

            foreach (var instance in entities)
            {
                instance.Status = Status.Deleted;
                _repository.Update(instance);

                //Delete the associated metric instances, databases and jobs
                var databaseIds = instance.Databases.Select(x => x.Id).ToList();
                DeleteDatabases(databaseIds);

                var metricInstanceIds = instance.Server.MetricInstances.Where(x => ((x.Metric.MetricType.Value & (int)MetricType.Instance) == (int)MetricType.Instance &&
                                                                                                x.DatabaseInstance.Id.Equals(instance.Id) &&
                                                                                                x.Status.Value != (int)Status.Deleted))
                                                                                        .Select(x => x.Id).ToList();
                DeleteMetricInstances(metricInstanceIds);

                var jobIds = instance.Jobs.Select(x => x.Id).ToList();
                DeleteJobs(jobIds);
            }

            _repository.UnitOfWork.SaveChanges();

            return deleteSuccess;
        }

        public bool DeleteDatabases(List<Guid> datbaseIds)
        {
            var deleteSuccess = true;
            var entities = _repository.Find<Database>(x => datbaseIds.Contains(x.Id)).ToList();

            foreach (var database in entities)
            {
                database.Status = Status.Deleted;
                _repository.Update(database);

                //Delete the associated metric instances
                var metricInstanceIds = database.Instance.Server.MetricInstances.Where(x => ((x.Metric.MetricType.Value & (int)MetricType.Database) == (int)MetricType.Database &&
                                                                                                x.Database.Id.Equals(database.Id) &&
                                                                                                x.Status.Value != (int)Status.Deleted))
                                                                                        .Select(x => x.Id).ToList();
                DeleteMetricInstances(metricInstanceIds);
            }

            _repository.UnitOfWork.SaveChanges();

            return deleteSuccess;
        }

        public bool DeleteJobs(List<Guid> jobIds)
        {
            var deleteSuccess = true;
            var entities = _repository.Find<SqlAgentJob>(x => jobIds.Contains(x.Id)).ToList();

            foreach (var job in entities)
            {
                job.Status = Status.Deleted;
                _repository.Update(job);

                //Delete the associated metric instances
                //var metricInstanceIds = job.Instance.Server.MetricInstances.Where(x => ((x.Metric.MetricType.Value & (int)MetricType.Instance) == (int)MetricType.Instance &&
                //                                                                        x.Label == string.Format("Job Status for '{0}' on Instance '{1}'", job.Name, job.Instance.Name) &&
                //                                                                        x.Status.Value != (int)Status.Deleted))
                //                                                                        .Select(x => x.Id).ToList();

                var label = string.Format("Job Status for '{0}' on Instance '{1}'", job.Name, job.Instance.Name);
                var metricInstanceIds =_repository.GetQuery<MetricInstance>(mi => mi.Server.Id == job.Instance.Server.Id &&
                                                               (mi.Metric.MetricType.Value & (int)MetricType.Instance) == (int)MetricType.Instance &&
                                                               mi.Label == label &&
                                                               mi.Status.Value != (int)Status.Deleted).Select(x => x.Id).ToList();

                DeleteMetricInstances(metricInstanceIds);
            }

            _repository.UnitOfWork.SaveChanges();

            return deleteSuccess;
        }

        public bool DeleteMetricInstances(List<Guid> metricInstanceIds)
        {
            var deleteSuccess = true;
            var entities = _repository.Find<MetricInstance>(x => metricInstanceIds.Contains(x.Id)).ToList();

            foreach (var metricInstance in entities)
            {
                metricInstance.Status = Status.Deleted;
                _repository.Update(metricInstance);

                //Delete the associated configs & maintenance windows
                var metricConfigIds = metricInstance.MetricConfigurations.Select(x => x.Id).ToList();
                DeleteMetricConfigurations(metricConfigIds);

                var maintenanceWindowsIds = metricInstance.MaintenanceWindows.Select(x => x.Id).ToList();
                DeleteMaintenanceWindows(maintenanceWindowsIds);
            }

            _repository.UnitOfWork.SaveChanges();

            return deleteSuccess;
        }

        public bool ServerExists(Guid serverId)
        {
            var server = _repository.GetByKey<Server>(serverId);
            return server != null;
        }

        public string GetServerInfo(Guid serverId)
        {
            var server = _repository.GetByKey<Server>(serverId);
            var xml = new XElement("ServerInfo", new XAttribute("serverId", server.Id.ToString()), new XAttribute("hostName", server.Hostname), new XAttribute("ipAddress", server.IpAddress));
            return xml.ToString();
        }

        public string GetServerInfoFromMetricInstanceId(Guid metricInstanceId)
        {
            var metricInstance = _repository.GetByKey<MetricInstance>(metricInstanceId);
            var server = metricInstance.Server;

            var xml = new XElement("ServerInfo", new XAttribute("serverId", server.Id.ToString()), new XAttribute("hostName", server.Hostname), new XAttribute("ipAddress", server.IpAddress));
            return xml.ToString();
        }

        public Dictionary<String, String> GetAssembliesForServer(Guid serverId)
        {
            var assemblies = new Dictionary<String, String>();
            
            //Get the server
            var server = _repository.GetByKey<Server>(serverId);
            if (server == null)
                return assemblies;

            assemblies.Add("Datavail.Delta.Agent.SharedCode", "4.1.3061.0");

            var metricInstanceList = new List<MetricInstance>();

            //Add all of the server's direct metric instances
            metricInstanceList.AddRange(server.MetricInstances.Where(mi => mi.Status != Status.Deleted));

            //Add metric instances from each VirtualServer of the cluster that this server belongs to (if any)
            if (server.Cluster != null)
            {
                foreach (var virtualServer in server.Cluster.VirtualServers)
                {
                    metricInstanceList.AddRange(virtualServer.MetricInstances.Where(mi => mi.Status != Status.Deleted));
                }
            }

            //Loop through the MetricInstances
            foreach (var metricInstance in metricInstanceList)
            {
                //For each MetricInstance, Get the Metric
                var metric = metricInstance.Metric;

                //Determine if adapter already added
                if (!assemblies.ContainsKey(metric.AdapterAssembly))
                {
                    assemblies.Add(metric.AdapterAssembly, metric.AdapterVersion);
                }
                else
                {
                    if (assemblies[metric.AdapterAssembly] != metric.AdapterVersion)
                    {
                        assemblies.Add(metric.AdapterAssembly, metric.AdapterVersion);
                    }
                }
            }

            //Return an IEnumerable of AssemblyVersion objects
            return assemblies;
        }

        #region Inventory Update Methods
        public void UpdateServerDiskInventory(Guid serverId, string drivePath, string label, long totalBytes)
        {
            var disk = _repository.FindOne(new Specification<ServerDisk>(s => s.Server.Id == serverId && s.Path == drivePath));

            if (disk == null)
            {
                var server = _repository.GetByKey<Server>(serverId);
                disk = ServerDisk.NewServerDisk(drivePath, server, totalBytes, label);

                _repository.Add(disk);
                _repository.UnitOfWork.SaveChanges();
            }

            var diskStatusMetric = _repository.Find<MetricInstance>(x => x.Server.Id.Equals(serverId)
                                                                         && x.Status.Value != (int)Status.Deleted
                                                                         && x.Metric.AdapterClass.Equals("DiskPlugin")
                                                                         && x.Data.Contains(drivePath)).FirstOrDefault();

            //Create the disk metric if it doesn't exist
            if (diskStatusMetric == null)
            {
                var metric = _repository.Find<Metric>(x => x.AdapterClass.Equals("DiskPlugin")).FirstOrDefault();
                //SaveMetricInstance(Guid.Empty, metric.Id, virtualServer.Id, GetMetricData(metric.Id, virtualServer.Id), Status.Active, MetricInstanceParentType.Server);

                var metricData = GetMetricData(metric.Id, serverId);
                var pathData = metricData.Data.Where(x => x.TagName == "Path").FirstOrDefault();

                pathData.Value = disk.Path;

                SaveMetricInstance(Guid.Empty, metric.Id, serverId, metricData, Status.Active, MetricInstanceParentType.Server);
            }
        }

        public void UpdateClusterDiskInventory(Guid serverId, string clusterName, string resourceGroupName, string drivePath, string label, long totalBytes)
        {
            var server = _repository.GetByKey<Server>(serverId);
            var disk = _repository.FindOne(new Specification<ServerDisk>(s => s.Server.Id == server.Id && s.Path == drivePath));

            if (disk == null)
            {
                disk = ServerDisk.NewServerDisk(drivePath, server, totalBytes, label);

                _repository.Add(disk);
                _repository.UnitOfWork.SaveChanges();
            }

            var diskStatusMetric = _repository.Find<MetricInstance>(x => x.Server.Id.Equals(server.Id)
                                                                         && x.Status.Value != (int)Status.Deleted
                                                                         && x.Metric.AdapterClass.Equals("DiskPlugin")
                                                                         && x.Data.Contains(drivePath)).FirstOrDefault();

            //Create the disk metric if it doesn't exist
            if (diskStatusMetric == null)
            {
                var metric = _repository.Find<Metric>(x => x.AdapterClass.Equals("DiskPlugin")).FirstOrDefault();
                //SaveMetricInstance(Guid.Empty, metric.Id, virtualServer.Id, GetMetricData(metric.Id, virtualServer.Id), Status.Active, MetricInstanceParentType.Server);

                var metricData = GetMetricData(metric.Id, serverId);
                var pathData = metricData.Data.Where(x => x.TagName == "Path").FirstOrDefault();

                pathData.Value = disk.Path;

                SaveMetricInstance(Guid.Empty, metric.Id, serverId, metricData, Status.Active, MetricInstanceParentType.Server);
            }
        }

        public void UpdateInstanceDatabaseInventory(Guid instanceId, ICollection<String> databaseNames)
        {
            var databaseInstance = _repository.GetByKey<DatabaseInstance>(instanceId);
            Guard.IsNotNull(databaseInstance, "Invalid Database Instance Id Specified");

            //Mark any physically deleted databases as deleted
            var databasesToDelete = databaseInstance.Databases.Where(database => !databaseNames.Contains(database.Name)).Select(database => database.Id).ToList();
            DeleteDatabases(databasesToDelete);

            //Mark any recreated (previously deleted) databases back to Active
            foreach (var database in databaseInstance.Databases.Where(database => databaseNames.Contains(database.Name)))
            {
                if (database.Status.Enum == Status.Deleted)
                {
                    database.Status = Status.Active;
                    _repository.Update(database);
                    _repository.UnitOfWork.SaveChanges();

                    AddDefaultDatabaseMetrics(database.Id);
                }

                databaseNames.Remove(database.Name);
            }

            //Add newly discovered databases
            foreach (var database in databaseNames.Select(databaseName => Database.NewDatabase(databaseName, databaseInstance)))
            {
                _repository.Add(database);
                try
                {
                    _repository.UnitOfWork.SaveChanges();
                    AddDefaultDatabaseMetrics(database.Id);
                }
                catch (Exception ex)
                {
                    _repository.Delete(database);

                    //Ignore the UNIQUE violation exception
                    if (!ex.Message.Contains("Violation of UNIQUE KEY constraint") && ex.InnerException != null && !ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint"))
                    {
                        throw;
                    }
                }
            }
        }

        public void UpdateInstanceSqlAgentJobInventory(Guid instanceId, ICollection<String> jobNames)
        {
            var hasUpdates = false;
            var databaseInstance = _repository.GetByKey<DatabaseInstance>(instanceId);
            Guard.IsNotNull(databaseInstance, "Invalid Database Instance Id Specified");

            //Mark any physically deleted jobs as deleted
            var jobsToDelete = databaseInstance.Jobs.Where(job => !jobNames.Contains(job.Name)).Select(job => job.Id).ToList();
            DeleteJobs(jobsToDelete);

            //Mark any recreated (previously deleted) jobs back to Active
            foreach (var job in databaseInstance.Jobs.Where(job => job.Status.Enum == Status.Deleted && jobNames.Contains(job.Name)))
            {
                hasUpdates = true;
                job.Status = Status.Active;
                _repository.Update(job);
                _repository.UnitOfWork.SaveChanges();

                jobNames.Remove(job.Name);
            }

            //Remove any existing jobs from the list
            foreach (var job in databaseInstance.Jobs.Where(job => jobNames.Contains(job.Name)))
            {
                jobNames.Remove(job.Name);
            }

            //Add newly discovered jobs

            foreach (var job in jobNames.Select(job => SqlAgentJob.NewSqlAgentJob(job, databaseInstance)))
            {
                hasUpdates = true;
                _repository.Add(job);

                try
                {
                    _repository.UnitOfWork.SaveChanges();
                }
                catch (Exception ex)
                {
                    _repository.Delete(job);

                    //Ignore the UNIQUE violation exception
                    if (!ex.Message.Contains("Violation of UNIQUE KEY constraint") && ex.InnerException != null && !ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint"))
                    {
                        throw;
                    }
                    else
                    {
                        Trace.WriteLine("ServerService::UpdateInstanceSqlJobInventory::Violation of UNIQUE KEY constraint");
                    }
                }
            }

            if (hasUpdates)
                AddDefaultDatabaseInstanceMetrics(instanceId);
        }
        #endregion

        public bool VirtualServerActiveNodeFailoverOccurred(Guid physicalServerId, Guid virtualServerId, string activeNodeName, out string previousNodeName)
        {
            var physicalserver = _repository.GetByKey<Server>(physicalServerId);
            var activeNode = _repository.FindOne(new Specification<Server>(s => s.Status.Value == (int)Status.Active && s.IsVirtual == false && s.Customer.Id == physicalserver.Customer.Id && s.Hostname.ToLower() == activeNodeName.ToLower()));

            Guard.IsNotNull(activeNode, string.Format("Active Node {0} Is Not Under Management", activeNodeName));

            var virtualServer = _repository.GetByKey<Server>(virtualServerId);

            previousNodeName = virtualServer.ActiveNode == null ? "Unknown" : virtualServer.ActiveNode.Hostname.ToUpper();

            if (virtualServer.ActiveNode == null)
            {
                virtualServer.ActiveNode = activeNode;
                _repository.Update(virtualServer);
                _repository.UnitOfWork.SaveChanges();
                return false;
            }

            if (virtualServer.ActiveNode != activeNode)
            {
                virtualServer.ActiveNode = activeNode;
                _repository.Update(virtualServer);
                _repository.UnitOfWork.SaveChanges();
                return true;
            }
            return false;
        }

        #region "Private Methods"
        private MetricData GetMetricData(string adapterClass, string metricXMLData, Guid parentId)
        {
            var metricData = new MetricData();
            var existingData = false;
            XElement xmlData = null;

            if (!string.IsNullOrEmpty(metricXMLData))
            {
                xmlData = XElement.Parse(metricXMLData);
                existingData = true;
            }

            switch (adapterClass)
            {
                case "CheckInPlugin":
                case "CpuPlugin":
                case "DiskInventoryPlugin":
                case "RamPlugin":
                case "DatabaseInventoryPlugin":
                case "SqlAgentJobInventoryPlugin":
                case "DatabaseServerBlockingPlugin":
                case "DatabaseServerPerformanceCountersPlugin":
                case "DatabaseBackupStatusPlugin":
                case "DatabaseStatusPlugin":
                case "MsClusterGroupStatusPlugin":
                case "MsClusterNodeStatusPlugin":
                case "DatabaseFileSizePlugin":
                    break;
                case "DatabaseServerJobsPlugin":
                    var jobs = _repository.Find<SqlAgentJob>(x => x.Instance.Id.Equals(parentId));

                    metricData.Data.Add(new MetricDataItem
                    {
                        DisplayName = "Job Name",
                        TagName = "JobName",
                        SelectedValueOption = existingData ? xmlData.Attribute("JobName").Value : string.Empty,
                        MultipleValues = false,
                        IsRequired = true,
                        ValueOptions = jobs.ToDictionary(x => x.Name, x => x.Name)
                    });
                    break;
                case "DatabaseServerLongRunningProcessPlugin":
                    metricData.Data.Add(new MetricDataItem
                    {
                        DisplayName = "Threshold",
                        TagName = "Threshold",
                        Value = existingData ? xmlData.Attribute("Threshold").Value : string.Empty,
                        MultipleValues = false,
                        IsRequired = true
                    });
                    break;
                case "DatabaseServerMergeReplicationPlugin":
                    metricData.Data.Add(new MetricDataItem
                    {
                        DisplayName = "Distribution Database Name",
                        TagName = "DistributionDatabaseName",
                        Value = existingData ? xmlData.Attribute("DistributionDatabaseName").Value : string.Empty,
                        MultipleValues = false,
                        IsRequired = true
                    });
                    break;
                case "DatabaseServerTransactionalReplicationPlugin":
                    metricData.Data.Add(new MetricDataItem
                    {
                        DisplayName = "Distribution Database Name",
                        TagName = "DistributionDatabaseName",
                        Value = existingData ? xmlData.Attribute("DistributionDatabaseName").Value : string.Empty,
                        MultipleValues = false,
                        IsRequired = true
                    });
                    break;
                case "DiskPlugin":
                    var disks = _repository.Find<ServerDisk>(x => x.Server.Id.Equals(parentId));

                    metricData.Data.Add(new MetricDataItem
                    {
                        DisplayName = "Disk Path",
                        TagName = "Path",
                        SelectedValueOption = existingData ? xmlData.Attribute("Path").Value : string.Empty,
                        MultipleValues = false,
                        IsRequired = true,
                        ValueOptions = disks.Distinct().ToDictionary(x => x.Path, x => x.Path)
                    });
                    break;
                case "LogWatcherPlugin":
                    metricData.Data.Add(new MetricDataItem
                    {
                        DisplayName = "File Name",
                        TagName = "FileNameToWatch",
                        Value = existingData ? xmlData.Attribute("FileNameToWatch").Value : string.Empty,
                        MultipleValues = false,
                        IsRequired = true
                    });
                    //Match Expressions
                    metricData.Data.Add(new MetricDataItem
                    {
                        DisplayName = "Match Expressions",
                        TagName = "MatchExpressions",
                        Value = string.Empty,
                        MultipleValues = true,
                        IsRequired = true
                    });
                    var matchExpressions = metricData.Data.Where(x => x.TagName == "MatchExpressions").FirstOrDefault();

                    if (existingData)
                    {
                        matchExpressions.Children = new List<MetricDataItem>();

                        foreach (var element in xmlData.Elements("MatchExpressions").Elements("MatchExpression"))
                        {
                            var metricDataItem = new MetricDataItem
                            {
                                DisplayName = "Match Expression",
                                TagName = "MatchExpression",
                                Value = element.Attribute("expression").Value,
                                MultipleValues = false
                            };
                            matchExpressions.Children.Add(metricDataItem);
                        }
                    }
                    else
                    {
                        //Add the default match expressions
                        matchExpressions.Children.Add(
                            new MetricDataItem
                            {
                                DisplayName = "Match Expression",
                                TagName = "MatchExpression",
                                Value = "is full|appears to be hung|system shutdown",
                                MultipleValues = false
                            });

                        matchExpressions.Children.Add(
                            new MetricDataItem
                            {
                                DisplayName = "Match Expression",
                                TagName = "MatchExpression",
                                Value = "is terminating|insufficient system memory|BEGIN STACK DUMP",
                                MultipleValues = false
                            });

                        matchExpressions.Children.Add(
                            new MetricDataItem
                            {
                                DisplayName = "Match Expression",
                                TagName = "MatchExpression",
                                Value = "The semaphore timeout period has expired",
                                MultipleValues = false
                            });

                        matchExpressions.Children.Add(
                            new MetricDataItem
                            {
                                DisplayName = "Match Expression",
                                TagName = "MatchExpression",
                                Value = "ready for client connections|replication conflicts",
                                MultipleValues = false
                            });

                        matchExpressions.Children.Add(
                            new MetricDataItem
                            {
                                DisplayName = "Match Expression",
                                TagName = "MatchExpression",
                                Value = "Database Settings Changed",
                                MultipleValues = false
                            });
                    }

                    //Exclude Expressions
                    metricData.Data.Add(new MetricDataItem
                    {
                        DisplayName = "Exclude Expressions",
                        TagName = "ExcludeExpressions",
                        Value = string.Empty,
                        MultipleValues = true,
                        IsRequired = false
                    });

                    if (existingData)
                    {
                        var excludeExpressions = metricData.Data.Where(x => x.TagName == "ExcludeExpressions").FirstOrDefault();
                        excludeExpressions.Children = new List<MetricDataItem>();

                        foreach (var element in xmlData.Elements("ExcludeExpressions").Elements("ExcludeExpression"))
                        {
                            var metricDataItem = new MetricDataItem
                            {
                                DisplayName = "Exclude Expression",
                                TagName = "ExcludeExpression",
                                Value = element.Attribute("expression").Value,
                                MultipleValues = false
                            };
                            excludeExpressions.Children.Add(metricDataItem);
                        }
                    }
                    break;
                case "ServiceStatusPlugin":
                    metricData.Data.Add(new MetricDataItem
                    {
                        DisplayName = "Service Name",
                        TagName = "ServiceName",
                        Value = existingData ? xmlData.Attribute("ServiceName").Value : string.Empty,
                        MultipleValues = false,
                        IsRequired = true
                    });
                    break;
                case "SqlAgentStatusPlugin":
                    metricData.Data.Add(new MetricDataItem
                    {
                        DisplayName = "Program Names",
                        TagName = "ProgramNames",
                        Value = string.Empty,
                        MultipleValues = true,
                        IsRequired = false
                    });

                    //Program Names
                    if (existingData)
                    {
                        var programNames = metricData.Data.Where(x => x.TagName == "ProgramNames").FirstOrDefault();
                        programNames.Children = new List<MetricDataItem>();

                        foreach (var element in xmlData.Elements("ProgramNames").Elements("ProgramName"))
                        {
                            var metricDataItem = new MetricDataItem
                            {
                                DisplayName = "Program Name",
                                TagName = "ProgramName",
                                Value = element.Attribute("Value").Value,
                                MultipleValues = false
                            };
                            programNames.Children.Add(metricDataItem);
                        }
                    }
                    break;
                default:
                    break;
            }

            return metricData;
        }

        private bool GetMetricInstanceDataAndLabel(Guid metricId, Guid metricParentId, MetricData metricData, out string xmlData, out string label, out Server server, out Metric metric)
        {
            var success = true;
            DatabaseInstance instance = null;
            Database database = null;
            server = null;
            metric = null;
            var connectionString = "";

            //out params
            label = "";
            xmlData = "";
            XElement xml = null;

            metric = _repository.GetByKey<Metric>(metricId);

            if (metric == null)
            {
                success = false;
            }
            else
            {
                switch (metric.MetricType.Enum)
                {
                    case MetricType.Server:
                    case MetricType.VirtualServer:
                    case MetricType.Server | MetricType.VirtualServer:
                        server = _repository.GetByKey<Server>(metricParentId);
                        break;
                    case MetricType.Instance:
                        instance = _repository.GetByKey<DatabaseInstance>(metricParentId);
                        server = instance.Server;
                        break;
                    case MetricType.Database:
                        database = _repository.GetByKey<Database>(metricParentId);
                        instance = database.Instance;
                        server = instance.Server;
                        break;
                    default:
                        success = false;
                        break;
                }

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
                    case "RamPlugin":
                        label = "RAM Status";
                        xml = new XElement("RamPluginInput");
                        break;
                    case "DatabaseInventoryPlugin":
                        label = "Database Inventory on Instance '" + instance.Name + "'";
                        connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "master",
                                                instance.UseIntegratedSecurity, instance.Username, instance.Password);

                        xml = new XElement("DatabaseInventoryPluginInput");
                        xml.Add(new XAttribute("ConnectionString", connectionString));
                        xml.Add(new XAttribute("InstanceName", instance.Name));
                        xml.Add(new XAttribute("InstanceId", instance.Id));
                        break;
                    case "SqlAgentJobInventoryPlugin":
                        label = "SQL Agent Job Inventory on Instance '" + instance.Name + "'";
                        connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "msdb",
                                                instance.UseIntegratedSecurity, instance.Username, instance.Password);

                        xml = new XElement("SqlAgentJobInventoryPluginInput");
                        xml.Add(new XAttribute("ConnectionString", connectionString));
                        xml.Add(new XAttribute("InstanceName", instance.Name));
                        xml.Add(new XAttribute("InstanceId", instance.Id));
                        break;
                    case "DatabaseServerBlockingPlugin":
                        label = "Blocking Status on Instance '" + instance.Name + "'";
                        connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "master",
                                                instance.UseIntegratedSecurity, instance.Username, instance.Password);

                        xml = new XElement("DatabaseServerBlockingPluginInput");
                        xml.Add(new XAttribute("ConnectionString", connectionString));
                        xml.Add(new XAttribute("InstanceName", instance.Name));
                        break;
                    case "DatabaseServerPerformanceCountersPlugin":
                        label = "Performance Counters for Instance '" + instance.Name + "'";
                        connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "master",
                                                instance.UseIntegratedSecurity, instance.Username, instance.Password);

                        xml = new XElement("DatabaseServerPerformanceCountersPluginInput");
                        xml.Add(new XAttribute("ConnectionString", connectionString));
                        xml.Add(new XAttribute("InstanceName", instance.Name));
                        break;
                    case "DatabaseBackupStatusPlugin":
                        label = "Backup Status for Database '" + database.Name + "' on Instance '" + instance.Name + "'";
                        connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, database.Name,
                                                instance.UseIntegratedSecurity, instance.Username, instance.Password);

                        xml = new XElement("DatabaseBackupStatusPluginInput");
                        xml.Add(new XAttribute("DatabaseName", database.Name));
                        xml.Add(new XAttribute("ConnectionString", connectionString));
                        xml.Add(new XAttribute("InstanceName", instance.Name));
                        break;
                    case "DatabaseStatusPlugin":
                        label = "Database Status for for Database '" + database.Name + "' on Instance '" + instance.Name + "'";
                        connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "master",
                                                instance.UseIntegratedSecurity, instance.Username, instance.Password);

                        xml = new XElement("DatabaseStatusPluginInput");
                        xml.Add(new XAttribute("DatabaseName", database.Name));
                        xml.Add(new XAttribute("ConnectionString", connectionString));
                        xml.Add(new XAttribute("InstanceName", instance.Name));
                        break;
                    case "MsClusterGroupStatusPlugin":
                        label = "Cluster Group Status for '" + server.ClusterGroupName + "'";
                        xml = new XElement("MsClusterGroupStatus");
                        break;
                    case "MsClusterNodeStatusPlugin":
                        label = "Cluster Node Status for '" + server.ClusterGroupName + "'";
                        xml = new XElement("MsClusterNodeStatus");
                        break;
                    case "DatabaseFileSizePlugin":
                        label = "File Size for Database '" + database.Name + "' on Instance '" + instance.Name + "'";
                        connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, database.Name,
                                                instance.UseIntegratedSecurity, instance.Username, instance.Password);

                        xml = new XElement("DatabaseFileSizePluginInput");
                        xml.Add(new XAttribute("DatabaseName", database.Name));
                        xml.Add(new XAttribute("ConnectionString", connectionString));
                        xml.Add(new XAttribute("InstanceName", instance.Name));
                        break;
                    case "DatabaseServerJobsPlugin":
                        var jobnameMetricData = metricData.Data.Where(x => x.TagName == "JobName").FirstOrDefault();
                        label = "Job Status for '" + jobnameMetricData.Value + "' on Instance '" + instance.Name + "'";
                        connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "msdb",
                                                instance.UseIntegratedSecurity, instance.Username, instance.Password);

                        xml = new XElement("DatabaseServerJobsPluginInput");
                        xml.Add(new XAttribute("JobName", jobnameMetricData.Value));
                        xml.Add(new XAttribute("ConnectionString", connectionString));
                        xml.Add(new XAttribute("InstanceName", instance.Name));
                        break;
                    case "DatabaseServerLongRunningProcessPlugin":
                        label = "Long Running Process Status on Instance '" + instance.Name + "'";
                        var thresholdMetricData = metricData.Data.Where(x => x.TagName == "Threshold").FirstOrDefault();
                        connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "master",
                                                instance.UseIntegratedSecurity, instance.Username, instance.Password);

                        xml = new XElement("DatabaseServerLongRunningProcessPluginInput");
                        xml.Add(new XAttribute("Threshold", thresholdMetricData.Value));
                        xml.Add(new XAttribute("ConnectionString", connectionString));
                        xml.Add(new XAttribute("InstanceName", instance.Name));
                        break;
                    case "DatabaseServerMergeReplicationPlugin":
                        var distDBMetricData = metricData.Data.Where(x => x.TagName == "DistributionDatabaseName").FirstOrDefault();
                        label = "Merge Replication Status for Distribution Database '" + distDBMetricData.Value + "' on Instance '" + instance.Name + "'";
                        connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "master",
                                                instance.UseIntegratedSecurity, instance.Username, instance.Password);

                        xml = new XElement("DatabaseServerMergeReplicationPlugin");
                        xml.Add(new XAttribute("DistributionDatabaseName", distDBMetricData.Value));
                        xml.Add(new XAttribute("ConnectionString", connectionString));
                        xml.Add(new XAttribute("InstanceName", instance.Name));
                        break;
                    case "DatabaseServerTransactionalReplicationPlugin":
                        var distribDBRepMetricData = metricData.Data.Where(x => x.TagName == "DistributionDatabaseName").FirstOrDefault();
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
                    case "LogWatcherPlugin":
                        var fileName = metricData.Data.Where(x => x.TagName == "FileNameToWatch").FirstOrDefault().Value;
                        var matchExprData = metricData.Data.Where(x => x.TagName == "MatchExpressions").FirstOrDefault();
                        var excludeExprData = metricData.Data.Where(x => x.TagName == "ExcludeExpressions").FirstOrDefault();
                        label = "Log Watcher for '" + fileName + "'";
                        xml = new XElement("LogWatcherPluginInput");
                        xml.Add(new XAttribute("FileNameToWatch", fileName));
                        var matchExpressionElem = new XElement("MatchExpressions");

                        if (matchExprData != null && matchExprData.Children != null && matchExprData.Children.Count() > 0)
                        {
                            foreach (var item in matchExprData.Children)
                            {
                                var matchExpr = new XElement("MatchExpression");
                                matchExpr.Add(new XAttribute("expression", item.Value));
                                matchExpressionElem.Add(matchExpr);
                            }
                        }

                        var excludeExpressionElem = new XElement("ExcludeExpressions");

                        if (excludeExprData != null && excludeExprData.Children != null && excludeExprData.Children.Count() > 0)
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
                        var serviceName = metricData.Data.Where(x => x.TagName == "ServiceName").FirstOrDefault().Value;
                        label = "Service Status for Service '" + serviceName + "'";
                        xml = new XElement("ServiceStatusPluginInput");
                        xml.Add(new XAttribute("ServiceName", serviceName));
                        break;
                    case "SqlAgentStatusPlugin":
                        label = "SQL Agent Status for Instance '" + instance.Name + "'";
                        connectionString = BuildEncryptedConnectionString(server.Hostname, instance.Name, "master",
                                                instance.UseIntegratedSecurity, instance.Username, instance.Password);
                        var programNameMetricData = metricData.Data.Where(x => x.TagName == "ProgramNames").FirstOrDefault();

                        xml = new XElement("SqlAgentStatusPluginInput");
                        xml.Add(new XAttribute("ConnectionString", connectionString));
                        xml.Add(new XAttribute("InstanceName", instance.Name));

                        var programNamesElem = new XElement("ProgramNames");

                        if (programNameMetricData != null && programNameMetricData.Children != null && programNameMetricData.Children.Count() > 0)
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

                xml.Add(new XAttribute("Label", label));

                if (server.IsVirtual)
                {
                    xml.Add(new XAttribute("ClusterGroupName", server.ClusterGroupName));
                    xml.Add(new XAttribute("VirtualServerId", server.Id));
                }

                xmlData = xml.ToString();
            }

            return success;
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

            var crypto = new Encryption();
            return crypto.EncryptToString(connectionString);
        }

        private IMaintenanceWindowParent GetMaintenanceWindowParent(Guid maintenanceWindowParentId, MaintenanceWindowParentType parentType)
        {
            if (maintenanceWindowParentId.Equals(Guid.Empty))
            {
                return null;
            }

            IMaintenanceWindowParent parent = null;

            switch (parentType)
            {
                case MaintenanceWindowParentType.Tenant:
                    parent = _repository.GetByKey<Tenant>(maintenanceWindowParentId);
                    break;
                case MaintenanceWindowParentType.Customer:
                    parent = _repository.GetByKey<Customer>(maintenanceWindowParentId);
                    break;
                case MaintenanceWindowParentType.ServerGroup:
                    parent = _repository.GetByKey<ServerGroup>(maintenanceWindowParentId);
                    break;
                case MaintenanceWindowParentType.Server:
                    parent = _repository.GetByKey<Server>(maintenanceWindowParentId);
                    break;
                case MaintenanceWindowParentType.MetricInstance:
                    parent = _repository.GetByKey<MetricInstance>(maintenanceWindowParentId);
                    break;
                case MaintenanceWindowParentType.Metric:
                    parent = _repository.GetByKey<Metric>(maintenanceWindowParentId);
                    break;
                default:
                    break;
            }

            return parent;
        }

        private IMetricConfigurationParent GetMetricConfigurationParent(Guid metricConfigParentId, MetricConfigurationParentType parentType)
        {
            if (metricConfigParentId.Equals(Guid.Empty))
            {
                return null;
            }

            IMetricConfigurationParent parent = null;

            switch (parentType)
            {
                case MetricConfigurationParentType.Tenant:
                    parent = _repository.GetByKey<Tenant>(metricConfigParentId);
                    break;
                case MetricConfigurationParentType.Customer:
                    parent = _repository.GetByKey<Customer>(metricConfigParentId);
                    break;
                case MetricConfigurationParentType.ServerGroup:
                    parent = _repository.GetByKey<ServerGroup>(metricConfigParentId);
                    break;
                case MetricConfigurationParentType.Server:
                    parent = _repository.GetByKey<Server>(metricConfigParentId);
                    break;
                case MetricConfigurationParentType.MetricInstance:
                    parent = _repository.GetByKey<MetricInstance>(metricConfigParentId);
                    break;
                case MetricConfigurationParentType.Metric:
                    parent = _repository.GetByKey<Metric>(metricConfigParentId);
                    break;
                default:
                    break;
            }

            return parent;
        }
        #endregion
    }
}
