using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Specification;

namespace Datavail.Delta.Application.Interface
{
    public interface IServerService
    {
        void CheckIn(Guid tenantId, Guid serverId, string hostname, string ipAddress, string agentVersion, Guid? customerId);
        string GetConfig(Guid serverId);
        string GetServerInfo(Guid serverId);
        string GetServerInfoFromMetricInstanceId(Guid metricInstanceId);
        MetricInstance GetMetricInstance(Guid metricInstanceId);
        MetricInstance GetMetricInstance(Guid metricInstanceId, Guid parentId, out MetricData metricData);

        MetricConfiguration GetActiveConfiguration(Server server, MetricInstance metricInstance);
        Dictionary<String, String> GetAssembliesForServer(Guid serverId);

        IEnumerable<Role> GetRoles();
        IEnumerable<object> GetCustomerNames(string searchTerm);
        IEnumerable<object> GetServerNames(Guid customerId, string searchTerm);
        IEnumerable<Server> GetServerListForTenant(Guid tenantId);
        IEnumerable<Server> GetServerListForCustomer(Guid customerId);
        IEnumerable<Server> GetActiveServersForTenant(Guid tenantId);
        IEnumerable<Server> GetServersNotCheckedInForTenant(Guid tenantId, int minutes);
        IEnumerable<Server> GetServersNotCheckedInForCustomer(Guid tenantId, int minutes);
        IEnumerable<Server> GetUnknownServers(Guid tenantId);
        IEnumerable<Server> GetClusterNodes(Guid clusterId);
        IEnumerable<MetricThreshold> GetThresholds(Guid metricInstanceId);
        IEnumerable<Metric> GetMetrics(MetricType metricType, Guid parentId);
        IEnumerable<Metric> GetMetrics(MetricConfigurationParentType parentType, Guid parentId);
        IEnumerable<Metric> GetMetrics();
        MetricData GetMetricData(Guid metricId, Guid parentId);
        MetricData GetMetricInstanceData(Guid metricInstanceId, Guid parentId, out MetricInstance metricInstance);
        MetricConfiguration GetMetricConfiguration(Guid metricId, Guid metricConfigParentId);

        int GetPagedEntities<TEntity>(int page, int pageSize, Specification<TEntity> criteria, Expression<Func<TEntity, string>> orderBy, out IEnumerable<TEntity> entities) where TEntity : class, IDomainObject;
        TEntity GetByKey<TEntity>(Guid id) where TEntity : class;
        IEnumerable<TEntity> Find<TEntity>(Specification<TEntity> criteria) where TEntity : class;
        bool AuthenticateUser(string userName, string password);
        User AddUser(string userName, string firstName, string lastName, string password);
        bool AddVirtualServer(Guid clusterId, ref Server virtualServer);
        bool AddMetricConfiguration(Guid metricId, Guid configParentId, MetricConfigurationParentType parentType, ref MetricConfiguration metricConfiguration);
        bool SaveCustomer(ref Customer customer, Guid tenantId);
        bool SaveServer(ref Server sever);
        bool SaveCustomerServerGroup(ref ServerGroup serverGroup, Guid parentId);
        bool SaveCluster(ref Cluster cluster, Guid customerId);
        bool SaveUser(ref User user, List<Guid> roleIds);
        bool SaveDatabaseInstance(ref DatabaseInstance databaseInstance, Guid serverId);
        bool SaveDatabase(ref Database database, Guid instanceId);
        bool SaveMetricInstance(Guid metricInstanceId, Guid metricId, Guid metricParentId, MetricData data, Status status, MetricInstanceParentType parentType);
        bool SaveMetricConfiguration(ref MetricConfiguration metricConfiguration);
        bool SaveMetricThreshold(ref MetricThreshold metricThreshold, Guid metricConfigurationId);
        bool SaveMaintenanceWindow(ref MaintenanceWindow maintenanceWindow, Guid maintenanceWindowParentId, MaintenanceWindowParentType parentType);
        bool SaveSchedule(ref Schedule schedule, Guid metricConfigurationId);
        bool ActivateServers(List<Guid> serverIds, Guid customerId);
        bool SetActiveServers(List<Guid> serverIds, Guid customerId);
        bool SetClusterNodes(List<Guid> serverIds, Guid clusterId);
        bool SetServerGroupServers(List<Guid> serverIds, Guid serverGroupId);
        bool AddNodesToCluster(List<Guid> serverIds, Guid clusterId);
        bool AddServersToGroup(List<Guid> serverIds, Guid serverGroupId);
        bool ValidateCustomer(Customer customer, out List<string> errors);
        bool ValidateUser(User user, out List<string> errors);
        bool ValidateServerGroup(ServerGroup serverGroup, Guid parentId, out List<string> errors);
        bool ValidateDbInstance(DatabaseInstance databaseInstance, out List<string> errors);
        bool ValidateMetricThreshold(MetricThreshold metricThreshold, out List<string> errors);
        bool ValidateMaintenanceWindow(MaintenanceWindow maintenanceWindow, out List<string> errors);
        bool ValidateMetricConfiguration(MetricConfiguration metricConfiguration, out List<string> errors);
        bool ValidateSchedule(Schedule schedule, out List<string> errors);
        bool DeleteServerGroups(List<Guid> serverGroupIds);
        bool DeleteCustomers(List<Guid> customerIds);
        bool DeleteServers(List<Guid> serverIds);
        bool DeleteClusters(List<Guid> clusterIds);
        bool DeleteInstances(List<Guid> instanceIds);
        bool DeleteDatabases(List<Guid> databaseIds);
        bool DeleteUsers(List<Guid> userIds);
        bool DeleteMetricInstances(List<Guid> metricInstanceIds);
        bool DeleteMetricThresholds(List<Guid> metricThresholdIds);
        bool DeleteMaintenanceWindows(List<Guid> maintenanceWindowIds);
        bool DeleteMetricConfigurations(List<Guid> metricConfigurationIds);
        bool DeleteJobs(List<Guid> jobIds);
        bool DeleteSchedules(List<Guid> scheduleIds);
        bool RemoveServersFromServerGroup(List<Guid> serverIds, Guid serverGroupId);
        bool RemoveNodesFromCluster(List<Guid> nodeIds, Guid clusterId);
        bool RemoveServersFromCustomer(List<Guid> serverIds, Guid customerId);
        bool AddDefaultServerMetrics(Guid serverId);
        bool AddDefaultVirtualServerMetrics(Guid virtualServerId);
        bool AddDefaultDatabaseInstanceMetrics(Guid databaseInstanceId);
        bool AddDefaultDatabaseMetrics(Guid databaseId);
        bool ServerExists(Guid serverId);
        

        void UpdateServerDiskInventory(Guid serverId, string drivePath, string label, long totalBytes);
        void UpdateClusterDiskInventory(Guid serverId, string clusterName, string resourceGroupName, string drivePath, string label, long totalBytes);
        void UpdateInstanceDatabaseInventory(Guid instanceId, ICollection<String> databaseNames);
        void UpdateInstanceSqlAgentJobInventory(Guid instanceId, ICollection<String> jobNames);

        bool VirtualServerActiveNodeFailoverOccurred(Guid physicalServerId, Guid virtualServerId, string activeNodeName, out string previousNodeName);

        void UpdateCustomer(Guid serverId, Guid customerId);
        void UpdateStatus(Guid serverId, Status status);
    }
}
