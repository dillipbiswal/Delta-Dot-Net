using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;

using Datavail.Delta.Infrastructure.Util;

namespace Datavail.Delta.Application.IncidentProcessor.Rules.SharePointPlugin
{
    public sealed class WebsiteRule : IncidentProcessorRule
    {
        private string _webSiteStatus;
        private string _webSiteName;

        private const string ServiceDeskMatchMessage = "The Delta monitoring application has detected that the Web Site {0} is {1} (metricInstanceId: {2}).\n\nAgent Timestamp (UTC): {5}\nServer: {3}\nIp Address: {4}\n";
        private const string ServiceDeskMatchCountMessage = "The Delta monitoring application has detected that the Web Site {0} is {1} (metricInstanceId: {2}). This has occurred {3} times in the last {4} minutes.\n\nAgent Timestamp (UTC): {9}\nMatch Value: {5}\nMetric Threshold: {6}\nServer: {7}\nIp Address: {8}\n";
        private const string ServiceDeskSummary = "P{0}/{1}/Web Site {2} is {3}";

        public WebsiteRule(IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base(incidentService, dataCollection, serverService)
        {
            RuleName = "Web Site Match";
            XmlMatchString = "WebSitePluginOutput";

            SetupMatchParams();
        }

        public override bool IsMatch()
        {
            if (DataCollection.Root != null && DataCollection.Root.Name != XmlMatchString)
                return false;

            if (_webSiteStatus != "Started")
            {
                IncidentPriority = 1;
                IncidentMesage = FormatStandardServiceDeskMessage("Match Type", null);
                IncidentSummary = FormatSummaryServiceDeskMessage("Match Type");
                return true;
            }
            return false;
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();

            MatchTypeValue = _webSiteStatus;
        }
        protected override string FormatStandardServiceDeskMessage(string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMatchMessage, _webSiteName, _webSiteStatus, MetricInstanceId, Hostname, IpAddress, Timestamp);
            return message;
        }


        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var message = string.Format(ServiceDeskSummary, IncidentPriority, Hostname, _webSiteName, _webSiteStatus);
            return message;
        }

        protected override void ParseDataCollection(XDocument dataCollection)
        {
            Guard.IsNotNull(dataCollection.Root.Attribute("SiteName"), "SiteName");
            Guard.IsNotNull(dataCollection.Root.Attribute("SiteStatus"), "SiteStatus");

            _webSiteName = dataCollection.Root.Attribute("SiteName").Value;
            _webSiteStatus = dataCollection.Root.Attribute("SiteStatus").Value;
        }

    }
}