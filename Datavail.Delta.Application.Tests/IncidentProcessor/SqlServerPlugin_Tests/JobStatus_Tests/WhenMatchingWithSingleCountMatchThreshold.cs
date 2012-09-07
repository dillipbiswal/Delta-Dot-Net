using System;
using System.Xml.Linq;
using Datavail.Delta.Application.IncidentProcessor.Rules.SqlServerPlugin;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Application.Tests.IncidentProcessor.Rules.SqlServerPlugin_Tests.JobStatus_Tests
{
    [TestClass]
    public class WhenMatchingWithSingleCountPercentageThreshold
    {
        #region Variables and Helpers
        //Common
        private const string MatchType = "percentage available";
        private const string MetricInstanceId = "3572def9-9ee9-4ef8-b907-cb69de6eb13e";
        private const string Label = "some label";
        
        //ServerInfo
        private const string ServerId = "b7a7ec5f-5ea7-497c-8393-43ea1d47214a";
        private const string Hostname = "testhost";
        private const string IpAddress = "10.1.1.1";

        //DataCollection Specific
        private const string JobName = "Test Job";
        private const string Message = "The Job 'Test Job' has Failed";
        private const string JobStatus = "Failed";
        private const string InstanceName = "Test Instance";
        private const string JobId = "1";
        private const string retriesAttempted = "45";
        private const string runDate = "12/12/2011";
        private const string runTime = "1125";
        private const string runDuration = "49";
        private const string stepId = "0";
        private const string stepName = "Step 0";



        //Metric Threshold
        private Guid _metricThresholdId = new Guid("fa4846a3-ed6a-4e16-a556-d33b881f0d11");
        private Severity Severity = Severity.Informational;
        private ThresholdValueType ThresholdValueType = ThresholdValueType.Value;
        private ThresholdComparisonFunction ThresholdComparisonFunction = ThresholdComparisonFunction.Match;
        private const float Floor = 50;
        private const float Ceiling = 100;
        private const int MatchCount = 1;
        private const int TimePeriod = -1;
        
        private MetricThreshold _metricThreshold;

        private XDocument GetMatchingXml()
        {
            var xelement1 = new XElement("JobStatus",
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("instanceName", "Test Instance"),
                                   new XAttribute("jobName", JobName),
                                   new XAttribute("jobId", JobId),
                                   new XAttribute("jobStatus", JobStatus),
                                   new XAttribute("message", Message),
                                   new XAttribute("retriesAttempted", retriesAttempted),
                                   new XAttribute("runDate", runDate),
                                   new XAttribute("runDuration", runDuration),
                                   new XAttribute("runTime", runTime),
                                   new XAttribute("stepId", stepId),
                                   new XAttribute("stepName", stepName));

            var xelement2 = new XElement("JobStatus",
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("instanceName", "Test Instance"),
                                   new XAttribute("jobName", JobName),
                                   new XAttribute("jobId", JobId),
                                   new XAttribute("jobStatus", JobStatus),
                                   new XAttribute("message", "Message for Step 1 here...."),
                                   new XAttribute("retriesAttempted", retriesAttempted),
                                   new XAttribute("runDate", runDate),
                                   new XAttribute("runDuration", runDuration),
                                   new XAttribute("runTime", runTime),
                                   new XAttribute("stepId", "1"),
                                   new XAttribute("stepName", "Step 1"));



            var xmlMain = new XDocument(
                            new XElement("DatabaseJobStatusPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", MetricInstanceId),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty),
                                   new XAttribute("label", "some label")));




            xmlMain.Root.Add(xelement1);
            xmlMain.Root.Add(xelement2);

            return xmlMain;


        }

        private IServerService SetupServerService()
        {
            var serverService = new Mock<IServerService>();
            var metric = Metric.NewMetric("TestAssembly", "TestClass", "TestAdapter", "TestName");
            var metricInstance = MetricInstance.NewMetricInstance("Test Metric Instance", metric);
            serverService.Setup(s => s.GetMetricInstance(It.IsAny<Guid>())).Returns(metricInstance);
            var serverInfoXml = new XElement("ServerInfo", new XAttribute("serverId", ServerId), new XAttribute("hostName", Hostname), new XAttribute("ipAddress", IpAddress));
            serverService.Setup(s => s.GetServerInfoFromMetricInstanceId(It.IsAny<Guid>())).Returns(serverInfoXml.ToString);

            _metricThreshold = MetricThreshold.NewMetricThreshold(Ceiling, Floor, "Failed", MatchCount, Severity, ThresholdComparisonFunction, ThresholdValueType, TimePeriod, _metricThresholdId);
            serverService.Setup(s => s.GetThresholds(It.IsAny<Guid>())).Returns(new[] { _metricThreshold });

            return serverService.Object;
        }
        #endregion

        [TestMethod]
        public void ThenItIsAMatch()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetMatchingXml();

            var rule = new JobStatusRule(incidentService.Object, xml, serverService);

            Assert.AreEqual(true, rule.IsMatch());
        }

        [TestMethod]
        public void ThenAThresholdHistoryRecordIsLogged()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetMatchingXml();
            var rule = new JobStatusRule(incidentService.Object, xml, serverService);

            var match = rule.IsMatch();

            incidentService.Verify(s=>s.AddMetricThresholdHistory(It.IsAny<DateTime>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<float>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()));
        }

        [TestMethod]
        public void ThenServiceDeskMessageIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetMatchingXml();
            var rule = new JobStatusRule(incidentService.Object, xml, serverService);
            var expectedMessage = string.Format("The Delta monitoring application has detected that the job {0} is reporting a status of {1} (metricInstanceId: {2}).\nInstance Name: {3}\n\nStep Details\n\n\r\nStep Id: 0\nStep Name: Step 0\nRun Date:12/12/2011\nRun Time: 1125\nRun Duration: 49\nRetries Attempted: 45\nMessage: The Job 'Test Job' has Failed\n\n----------------------------------------------------------------------\r\n\r\nStep Id: 1\nStep Name: Step 1\nRun Date:12/12/2011\nRun Time: 1125\nRun Duration: 49\nRetries Attempted: 45\nMessage: Message for Step 1 here....\n\n----------------------------------------------------------------------\r\n\r\n", JobName, JobStatus, MetricInstanceId, InstanceName);

            var match = rule.IsMatch();

            Assert.AreEqual(expectedMessage, rule.IncidentMesage);
        }

        [TestMethod]
        public void ThenServiceDeskSummaryIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetMatchingXml();
            var rule = new JobStatusRule(incidentService.Object, xml, serverService);
            var expectedMessage = string.Format("P{0}/{1}/Job {4}/{2} is {3}", (int)Severity, Hostname, JobName, JobStatus, InstanceName);

            var match = rule.IsMatch();

            Assert.AreEqual(expectedMessage, rule.IncidentSummary);
        }

        [TestMethod]
        public void ThenServiceDeskPriorityIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetMatchingXml();
            var rule = new JobStatusRule(incidentService.Object, xml, serverService);
            var expectedPriority = (int) Severity;

            var match = rule.IsMatch();

            Assert.AreEqual(expectedPriority, rule.IncidentPriority);
        }
    }
}
