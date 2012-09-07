
namespace Datavail.Delta.Infrastructure.Azure
{
    public static class AzureConstants
    {
        public static class Queues
        {
            public const string CheckIns = "servercheckins";
            public const string Collections = "incidentprocessor";
            public const string CollectionArchives = "datawarehouse";
            public const string CollectionErrors = "incidentprocessorerrors";
            public const string CollectionTest = "incidentprocessortest";
        }

        public static class PerfCounters
        {
            public const string CountersCategory = "DatavailCounters";
            public const string CountersCategoryDesc = "Datavail Delta custom counters";

            public const string CollectionServiceMessagesQueued = "Collection Service Messages Queued/sec";
            public const string CollectionServiceMessagesQueuedDesc = "The number of messages queued by the collection service per second";
            public const string CollectionServiceMessagesIgnored = "Collection Service Messages Ignored/sec";
            public const string CollectionServiceMessagesIgnoredDesc = "The number of messages ignored by the collection service";
            public const string CollectionServiceMessagesForTest = "Collection Service Messages Queued For Test/sec";
            public const string CollectionServiceMessagesForTestDesc = "The number of messages diverted to the test queue by the collection service per second";

            public const string CheckInServiceCheckIns = "Check-In Service Check-Ins/sec";
            public const string CheckInServiceCheckInsDesc = "The number of server check-ins that have occurred per second";

            public const string UpdateServiceConfigurationDownloads = "Update Service Configuration Downloads/sec";
            public const string UpdateServiceConfigurationDownloadsDesc = "The number of configuration downloads that have occurred in the update service per second";

            public const string UpdateServiceAssemblyDownloads = "Update Service Assembly Downloads/sec";
            public const string UpdateServiceAssemblyDownloadsDesc = "The number of assembly downloads that have occurred in the update service per second";

            public const string IncidentProcessorMessagesProcessed = "Incident Processor Messages Processed/sec";
            public const string IncidentProcessorMessagesProcessedDesc = "The number of messages processed by the incident processor per second";

            public const string IncidentProcessorIncidentsOpened = "Incident Processor Incidents Opened/sec";
            public const string IncidentProcessorIncidentsOpenedDesc = "The number of incidents opened by the incident processor per second";

            public const string IncidentProcessorQueueDepth = "Incident Processor Queue Depth";
            public const string IncidentProcessorQueueDepthDesc = "The depth of the incident processor queue";
        }
    }
}
