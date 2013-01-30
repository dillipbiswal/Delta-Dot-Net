using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Datavail.Delta.Application;
using Datavail.Delta.Application.IncidentProcessor.Rules.Inventory;
using Datavail.Delta.Application.IncidentProcessor.Rules.SqlServerPlugin;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Datavail.Delta.Repository.EfWithMigrations;
using Moq;


namespace Datavail.Delta.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //Decrypt();
            TestDiskInventoryRule();
        }

        private static void Decrypt()
        {
            const string encrypted = "142134139055012143191112055042004065098242171030171139177131189063226215243157218149194179043151255124046180085092095164191251179200047117222180044027084043090156139138169069008185176084016084030232145037060209052179004102174171112100153183";
            var enc = new Encryption();
            var decrypted = enc.DecryptString(encrypted);
        }

        private static void TestAddDefaultDatabaseInstanceMetrics()
        {
            var logger = new Mock<IDeltaLogger>();
            var context = new DeltaDbContext();
            var repository = new ServerRepository(context, logger.Object);
            var serverService = new ServerService(logger.Object, repository);

            serverService.AddDefaultDatabaseInstanceMetrics(new Guid("0ED58722-FE13-4E77-8B9B-26CD7DD90159"));
        }

        private static void FixInstanceDefaultMetrics()
        {
            var logger = new Mock<IDeltaLogger>();
            var context = new DeltaDbContext();
            var repository = new ServerRepository(context, logger.Object);
            var serverService = new ServerService(logger.Object,repository);

            var instances = repository.GetQuery<DatabaseInstance>(i => i.Status != Status.Deleted).ToList();

            foreach (var databaseInstance in instances)
            {
                Console.WriteLine(string.Format("{0}\tUpdaating Instance: {1} on Server: {2}", DateTime.Now, databaseInstance.Name, databaseInstance.Server.Hostname));
                serverService.AddDefaultDatabaseInstanceMetrics(databaseInstance.Id);
            }
        }

        private static void TestSqlAgentJobsInventory()
        {
            var context = new DeltaDbContext();
            var logger = new Mock<IDeltaLogger>();
            var repository = new ServerRepository(context, logger.Object);
            var serverService = new ServerService(logger.Object, repository);
            var jobs = new List<string>()
                           {
                               "UpdateStats",
                               "Packaging_UpdateWorkOrderExpectedEndDates",
                               "Packaging_GetWorkOrderBottleCounts_4hour",
                               "syspolicy_purge_history",
                               "Email_Customer_Letters",
                               "collection_set_3_collection",
                               "collection_set_3_upload",
                               "collection_set_2_upload",
                               "collection_set_1_noncached_collect_and_upload",
                               "Wholesale Integration: Orders",
                               "collection_set_2_collection",
                               "All T-Log Backup.Subplan_1",
                               "Packaging_GetWorkOrderBottleCounts",
                               "Transaction Server DB Backup",
                               "ASPState_Job_DeleteExpiredSessions",
                               "Formulas_Get_QC_List",
                               "Wholesale Integration: Customer Ship To",
                               "Wholesale Integration: Orders Retry Send to As400",
                               "Test Job",
                               "Formulas_Get_R&amp;D",
                               "Wholesale Clean Log Tables",
                               "Packaging_Get Run times",
                               "PettyCash_Extract",
                               "DeTuinen File Imports",
                               "RegenerateAudiences",
                               "All System DB Backup.Subplan_1",
                               "Start Incremental Catalog Population on rmrv4_uat.RMR_Catalog_Detuinen",
                               "Packaging_AddNewWorkOrders",
                               "Daily Stored procedure for Codex",
                               "Wholesale Integration: Promotions",
                               "DeTuinen User Imports",
                               "All User DB Backup.Subplan_1",
                               "Check T-Log Backup Status",
                               "DV_IndexMaint",
                               "DV Healthcheck",
                               "Wholesale Integration: Deal Sheet",
                               "Packaging_Get Run Rates",
                               "Wholesale Integration: Product",
                               "WholesaleBI Get Sales By Brand Summary Data"
                           };

            serverService.UpdateInstanceSqlAgentJobInventory(new Guid("8b84de83-da6a-460b-8c0d-4c9336809ed8"), jobs);
        }

        private static void TestSqlAgentJob()
        {
            var context = new DeltaDbContext();
            var logger = new Mock<IDeltaLogger>();
            var repository = new ServerRepository(context, logger.Object);
            var incidentRepository = new IncidentRepository(context, logger.Object);
            var serverService = new ServerService(logger.Object, repository);
            var serviceDesk = new Mock<IServiceDesk>();
            var incidentService = new IncidentService(serviceDesk.Object, incidentRepository);
            const string data = @"<DatabaseJobStatusPluginOutput timestamp=""2012-03-01T22:51:16.5525572Z"" product=""SQL Server"" productVersion=""8.00.2055"" productLevel=""SP4"" productEdition=""Standard Edition"" metricInstanceId=""447bafbc-9c32-45af-872a-43a5eacb0d10"" label=""Job Status for 'DV_monitoringTEST' on Instance 'localhost'""><JobStatus resultCode=""0"" resultMessage=""DV_monitoringTEST status successfully returned."" jobName=""DV_monitoringTEST"" instanceName=""localhost"" jobId=""96e75022-d2dd-4d53-824c-96f837715cfa"" jobStatus=""Failed"" message=""The job failed.  The Job was invoked by User EMPIRE\\adm-datavail2.  The last step to run was step 1 (IsTheMonitoringToolPickingUpFailedJobs)."" retriesAttempted=""0"" runDate=""20120301"" runDuration=""0"" runTime=""142336"" stepId=""0"" stepName=""(Job outcome)"" /></DatabaseJobStatusPluginOutput>";
            var xdoc = XDocument.Parse(data);
            var rule = new JobStatusRule(incidentService, xdoc, serverService);

            var match = rule.IsMatch();
        }

        private static void TestDiskInventoryRule()
        {
            var context = new DeltaDbContext();
            var logger = new Mock<IDeltaLogger>();
            var repository = new ServerRepository(context, logger.Object);
            var incidentRepository = new IncidentRepository(context, logger.Object);
            var serverService = new ServerService(logger.Object, repository);
            var serviceDesk = new Mock<IServiceDesk>();
            var incidentService = new IncidentService(serviceDesk.Object, incidentRepository);
            const string data = @"<DiskInventoryPluginOutput timestamp=""2012-05-16T15:46:11.5879494Z"" metricInstanceId=""983ed91c-b1a3-4751-8e3d-adc150004a83"" label=""Disk Inventory"" resultCode=""0"" resultMessage="""" product=""Win32NT"" productVersion=""6.1.7601.65536"" productLevel=""Service Pack 1"" productEdition="""">\r\n  <Disk clusterName=""OESQLCLUSTER"" resourceGroupName=""OE_Citrix"" driveFormat=""NTFS"" isClusterDisk=""true"" label=""SQLSYSDB-INT2"" path=""F:\\"" totalBytes=""42945478656"" />\r\n  <Disk clusterName=""OESQLCLUSTER"" resourceGroupName=""OE_Citrix"" driveFormat=""NTFS"" isClusterDisk=""true"" label=""SQLBACKUP-INT2"" path=""H:\\"" totalBytes=""268431261696"" />\r\n  <Disk clusterName=""OESQLCLUSTER"" resourceGroupName=""OEInstance1"" driveFormat=""NTFS"" isClusterDisk=""true"" label=""SQLBACKUP-INT1"" path=""G:\\"" totalBytes=""268431261696"" />\r\n  <Disk clusterName=""OESQLCLUSTER"" resourceGroupName=""OE_Citrix"" driveFormat=""NTFS"" isClusterDisk=""true"" label=""SQLUSERDB-INT1"" path=""J:\\"" totalBytes=""536866717696"" />\r\n  <Disk clusterName=""OESQLCLUSTER"" resourceGroupName=""OE_Citrix"" driveFormat=""NTFS"" isClusterDisk=""true"" label=""SQLUSERLOG-INT2"" path=""L:\\"" totalBytes=""107369988096"" />\r\n  <Disk clusterName=""OESQLCLUSTER"" resourceGroupName=""OE_Citrix"" driveFormat=""NTFS"" isClusterDisk=""true"" label=""SQLSYSDB-INT1"" path=""E:\\"" totalBytes=""42945478656"" />\r\n  <Disk clusterName=""OESQLCLUSTER"" resourceGroupName=""OEInstance1"" driveFormat=""NTFS"" isClusterDisk=""true"" label=""SQLUSERLOG-INT1"" path=""K:\\"" totalBytes=""107369988096"" />\r\n  <Disk clusterName=""OESQLCLUSTER"" resourceGroupName=""OEInstance1"" driveFormat=""NTFS"" isClusterDisk=""true"" label=""SQLTEMPDB-INT-2"" path=""T:\\"" totalBytes=""42945478656"" />\r\n  <Disk clusterName=""OESQLCLUSTER"" resourceGroupName=""OEInstance1"" driveFormat=""NTFS"" isClusterDisk=""true"" label=""SQLUSERDB-INT1"" path=""I:\\"" totalBytes=""536866717696"" />\r\n  <Disk clusterName=""OESQLCLUSTER"" resourceGroupName=""Cluster Group"" driveFormat=""NTFS"" isClusterDisk=""true"" label=""QUORUM"" path=""Q:\\"" totalBytes=""42945478656"" />\r\n  <Disk clusterName=""OESQLCLUSTER"" resourceGroupName=""OEInstance1"" driveFormat=""NTFS"" isClusterDisk=""true"" label=""SQL SYSTEM-INT1"" path=""S:\\"" totalBytes=""42945478656"" />\r\n  <Disk driveFormat=""NTFS"" isClusterDisk=""false"" label="""" path=""C:\\"" totalBytes=""85791338496"" />\r\n  <Disk driveFormat=""NTFS"" isClusterDisk=""false"" label=""SQL Apps"" path=""D:\\"" totalBytes=""85896196096"" />\r\n</DiskInventoryPluginOutput>";
            var xdoc = XDocument.Parse(data);
            var rule = new DiskInventoryUpdateRule(incidentService, xdoc, serverService);

            var match = rule.IsMatch();
        }
    }
}
