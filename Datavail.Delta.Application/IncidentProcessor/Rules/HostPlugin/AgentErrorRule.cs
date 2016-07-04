using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;

using Datavail.Delta.Infrastructure.Util;

namespace Datavail.Delta.Application.IncidentProcessor.Rules.HostPlugin
{
    public sealed class AgentErrorRule : IncidentProcessorRule
    {
        private string _fileName;
        private string _matchingLine;
        private string _matchingExpression;

        //private const string ServiceDeskMatchMessage = "The Delta monitoring application has detected Agent Error {0} contains {1} (metricInstanceId: {2}).\n\nAgent Timestamp (UTC): {7}\nMatch Value: {3}\nMetric Threshold: {4}\nServer: {5}\nIp Address: {6}\n";
        //private const string ServiceDeskMatchCountMessage = "The Delta monitoring application has detected Agent Error {0} contains {1} (metricInstanceId: {2}). This has occurred {3} times in the last {4} minutes.\n\nAgent Timestamp (UTC): {9}\nMatch Value: {5}\nMetric Threshold: {6}\nServer: {7}\nIp Address: {8}\n";
        //private const string ServiceDeskSummary = "P{0}/{1}/Agent Error : {2}";
        private const string ServiceDeskMessage = "The Delta monitoring application has detected an Agent Error (metricInstanceId: {0}).\n\n\nServer: {1} \nIp Address: {2}\nError Message: {3}";
        private const string ServiceDeskSummary = "{2}/P{0}/{1}/Agent Error Detected"; //"P{0}/{1}/Agent Error Detected on Server {2}";

        public AgentErrorRule(IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base(incidentService, dataCollection, serverService)
        {
            RuleName = "Agent Error Rule";
            XmlMatchString = "AgentErrorOutput";

            SetupMatchParams();
        }

        public override bool IsMatch()
        {
            if (DataCollection.Root != null && DataCollection.Root.Name != XmlMatchString)
                return false;

            var matchFound = false;
            var incidentDetailMesages = new List<string>();

            //const string timeStampSpidRegEx = "[0-9]{1,4}-[0-9]{1,2}-[0-9]{1,2} [0-9]{1,2}:[0-9]{1,2}:[0-9]{1,2}.[0-9]{1,2} spid[0-9?]{0,5} {1,7}";
            //const string timeStampRegEx = "[0-9]{1,4}-[0-9]{1,2}-[0-9]{1,2} [0-9]{1,2}:[0-9]{1,2}:[0-9]{1,2}.[0-9]{1,2} [a-zA-Z0-9_-]{3,14} {1,7}";
            var data = string.Empty;

            data = _matchingLine.Trim().Substring(0, 200);

            AdditionalData = string.Format("<AdditionalData><ErrorMessage>{0}</ErrorMessage></AdditionalData>", data);
            IncidentPriority = 1;
            IncidentMesage = FormatStandardServiceDeskMessage("Match Type", null);
            IncidentSummary = FormatSummaryServiceDeskMessage("Match Type");
            return true;
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();

            MatchTypeValue = _matchingLine;
        }


        protected override string FormatStandardServiceDeskMessage(string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessage, MetricInstanceId, ServerId, IpAddress, _matchingLine);
            return message;
        }

        //protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        //{
        //    var message = string.Format(ServiceDeskSummary, IncidentPriority, Hostname, ServerId);
        //    return message;
        //}
        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var customerName = "None";
            if (MetricInstance.Server.Customer != null && MetricInstance.Server.Customer.Name != null)
            {
                customerName = MetricInstance.Server.Customer.Name;
            }

            var message = string.Format(ServiceDeskSummary, IncidentPriority, Hostname, customerName);
            return message;
        }

        protected override void ParseDataCollection(XDocument dataCollection)
        {
            Guard.IsNotNull(dataCollection.Root.Attribute("ErrorMessage"), "ErrorMessage");

            _matchingLine = dataCollection.Root.Attribute("ObjectName").Value + " :: " + dataCollection.Root.Attribute("MethodName").Value + " :: " + dataCollection.Root.Attribute("ErrorMessage").Value;

            //if (dataCollection.Root.Attribute("fileName") != null)
            //    _fileName = dataCollection.Root.Attribute("fileName").Value;
        }
    }
}