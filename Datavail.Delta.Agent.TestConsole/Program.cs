using System;
using Datavail.Delta.Agent.Plugin.Host;
using Datavail.Delta.Agent.Plugin.Host.Cluster;
using Datavail.Delta.Agent.Plugin.LogWatcher;
using Datavail.Delta.Agent.Plugin.SqlServer2005;
using Datavail.Delta.Infrastructure.Agent.Cluster;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using Datavail.Delta.Infrastructure.Agent.ServerInfo;
using Datavail.Delta.Infrastructure.Agent.SqlRunner;
using Moq;
using RestSharp;

namespace Datavail.Delta.Agent.TestConsole
{
    class Program
    {
        static void Main(string[] args) { TransReplicationPluginTest(); }

        static void LogWatcherTest()
        {
            var dataQueuer = new Mock<IDataQueuer>();
            var logger = new Mock<IDeltaLogger>();
            var clusterInfo = new Mock<IClusterInfo>();

            var plugin = new LogWatcherPlugin(dataQueuer.Object, logger.Object, clusterInfo.Object);

            var inputData = "<LogWatcherPluginInput FileNameToWatch=\"C:\\Program Files\\Microsoft SQL Server\\MSSQL10.SQLEXPRESS\\MSSQL\\Log\\ERRORLOG\"><MatchExpressions><MatchExpression expression=\"Login\"/></MatchExpressions><ExcludeExpressions/></LogWatcherPluginInput>";

            plugin.Execute(Guid.NewGuid(), "Label", inputData);
        }

        static void DiskInventoryPluginTest()
        {
            var clusterInfo = new ClusterInfo();
            var systemInfo = new SystemInfo();
            var dataQueuer = new Mock<IDataQueuer>();
            var logger = new Mock<IDeltaLogger>();

            var plugin = new DiskInventoryPlugin(clusterInfo, systemInfo, dataQueuer.Object, logger.Object);

            var inputData = string.Empty;
            plugin.Execute(Guid.NewGuid(), "Label", inputData);
        }

        static void DatabaseFileSizePluginTest()
        {
            var clusterInfo = new ClusterInfo();
            var dataQueuer = new Mock<IDataQueuer>();
            var logger = new Mock<IDeltaLogger>();
            var sqlRunner = new SqlServerRunner();
            var databaseServerInfo = new SqlServerInfo("");

            const string inputData = "<DatabaseFileSizePluginInput DatabaseName=\"EZHNI\" ConnectionString=\"185026238254136051009003236182073159227201212006029086171120129110173103099250024055200014194251252146072227200254190106094047038062171176205164143027040111212242159207176234086059108050109078139177149025067011076168047169169225220084210076128099103066055154153101080103088254117233238137\" InstanceName=\"185026238254136051009003236182073159227201212006029086171120129110173103099250024055200014194251252146072227200254190106094047038062171176205164143027040111212242159207176234086059108050109078139177149025067011076168047169169225220084210076128099103066055154153101080103088254117233238137\" Label=\"File Size for Database 'EZHNI' on Instance 'LABSQLDBA88'\" ClusterGroupName=\"sql2k8r2\" VirtualServerId=\"69c28bcf-b2fb-44fb-99c2-fad4f50f3a43\" />";
            var plugin = new DatabaseFileSizePlugin(clusterInfo, dataQueuer.Object, logger.Object, sqlRunner, databaseServerInfo);

            plugin.Execute(Guid.NewGuid(), "Label", inputData);
        }

        static void DatabaseStatusPluginTest()
        {
            var clusterInfo = new ClusterInfo();
            var dataQueuer = new Mock<IDataQueuer>();
            var logger = new Mock<IDeltaLogger>();
            var sqlRunner = new SqlServerRunner();
            var databaseServerInfo = new SqlServerInfo("");

            const string inputData = "<DatabaseStatusPluginInput DatabaseName=\"distribution\" ConnectionString=\"185026238254136051009003236182073159227201212006193191058198249152071008159155173000115022192092240162127239168204133246164150155201134146017023038220003003148044143224153030095144238201190044101055039127238143018233193037026182238153195000\" InstanceName=\"185026238254136051009003236182073159227201212006029086171120129110173103099250024055200014194251252146072227200254190106094047038062171176205164239113189036005024150123181172219163173196140229148066001208038221100172066227122014088205055164202184165067102141093109211037227196149112184032\" Label=\"Database Status for for Databse 'EZHNI' on Instance 'LABSQLDBA88'\"  />";
            var plugin = new DatabaseStatusPlugin(clusterInfo, dataQueuer.Object, logger.Object, sqlRunner, databaseServerInfo);

            plugin.Execute(Guid.NewGuid(), "Label", inputData);
        }

        static void TransReplicationPluginTest()
        {
            var clusterInfo = new ClusterInfo();
            var dataQueuer = new Mock<IDataQueuer>();
            var logger = new Mock<IDeltaLogger>();
            var sqlRunner = new SqlServerRunner();
            var sqlRunnerPublishers = new SqlServerRunner();
            var databaseServerInfo = new SqlServerInfo("");



            databaseServerInfo.Product = "MS SQL";
            databaseServerInfo.ProductEdition = "Standard";
            databaseServerInfo.ProductLevel = "10.0";
            databaseServerInfo.ProductVersion = "";


            const string inputData =
                "<DatabaseServerTransactionalReplicationPlugin DistributionDatabaseName=\"distribution\" ConnectionString=\"185026238254136051009003236182073159227201212006193191058198249152071008159155173000115022192092240162127239168204133246164150155201134146017023038220003003148044143224153030095144238201190044101055039127238143018233193037026182238153195000\" InstanceName=\"labsqldba1\\sql2k5\" Label=\"Transactional Replication Status for Distribution Database 'distribution' on Instance 'LABSQLDBA1\\SQL2k5'\" />";
            var plugin = new DatabaseServerTransactionalReplicationPlugin(clusterInfo, dataQueuer.Object, logger.Object, sqlRunner, sqlRunnerPublishers, databaseServerInfo);

            plugin.Execute(Guid.NewGuid(), "Label", inputData);
        }


        static void DatabaseServerBlockingTest()
        {
            var clusterInfo = new ClusterInfo();
            var dataQueuer = new Mock<IDataQueuer>();
            var logger = new Mock<IDeltaLogger>();
            var sqlRunner = new SqlServerRunner();
            var sqlRunnerPublishers = new SqlServerRunner();
            var databaseServerInfo = new SqlServerInfo("185026238254136051009003236182073159227201212006193191058198249152071008159155173000115022192092240162127239168204133246164150155201134146017023038220003003148044143224153030095144238201190044101055039127238143018233193037026182238153195000");



            databaseServerInfo.Product = "MS SQL";
            databaseServerInfo.ProductEdition = "Standard";
            databaseServerInfo.ProductLevel = "10.0";
            databaseServerInfo.ProductVersion = "";


            const string inputData = "<DatabaseServerBlockingPluginInput ConnectionString=\"185026238254136051009003236182073159227201212006193191058198249152071008159155173000115022192092240162127239168204133246164150155201134146017023038220003003148044143224153030095144238201190044101055039127238143018233193037026182238153195000\" DatabaseName=\"master\" InstanceName=\"LABSQLDBA1\\SQL2K5\" Label=\"Blocking\" />";
            var plugin = new DatabaseServerBlockingPlugin(clusterInfo, dataQueuer.Object, logger.Object, sqlRunner, databaseServerInfo);

            plugin.Execute(Guid.NewGuid(), "Label", inputData);
        }


        static void RestDataUploadTest()
        {
            var client = new RestClient("http://localhost:25458/v41/");
            var request = new RestRequest("Server/PostData/{id}", Method.POST);

            request.AddUrlSegment("id", "07CDFB37-AACB-4CB5-BC6F-611782966EC7");
            request.AddParameter("Data", "Hi");
            request.AddParameter("IpAddress", "1.1.1.1");// adds to POST or URL querystring based on Method

            var response = client.Execute(request);
            var content = response.Content; // raw content as string
        }




    }
}
