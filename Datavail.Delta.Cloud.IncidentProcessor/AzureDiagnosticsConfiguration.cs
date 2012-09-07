using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Datavail.Framework.Azure.Logging;
using Microsoft.WindowsAzure.Diagnostics;
using log4net.Config;

namespace Datavail.Delta.Cloud.IncidentProcessor
{
    public class AzureDiagnoticsConfiguration
    {
        private readonly DiagnosticMonitorConfiguration _diagConfig;

        #region Public Properties

        public string StorageConnectionStringKey { get { return "Datavail.Framework.StorageConnectionString"; } }
        public List<CounterTransferConfig> CountersToTransfer { get; set; }
        public TimeSpan EventLogTransferTime { get; set; }
        public TimeSpan AzureLogTransferTime { get; set; }
        public TimeSpan PerformanceCountersTransferTime { get; set; }
        public bool SetupLog4Net { get; set; }
        public bool TransferApplicationEventLog { get; set; }
        public bool TransferSystemEventLog { get; set; }

        #endregion

        public AzureDiagnoticsConfiguration(DiagnosticMonitorConfiguration configuration)
        {
            _diagConfig = configuration;
            AzureLogTransferTime = TimeSpan.FromMinutes(1);
            SetupLog4Net = true;
            TransferApplicationEventLog = true;
            TransferSystemEventLog = true;
            EventLogTransferTime = TimeSpan.FromMinutes(1);
            PerformanceCountersTransferTime = TimeSpan.FromMinutes(1);
            CountersToTransfer = new List<CounterTransferConfig>();
        }

        public AzureDiagnoticsConfiguration(DiagnosticMonitorConfiguration configuration, TimeSpan azureLogTransferTime, bool transferSystemEventLog, bool transferApplicationEventLog, TimeSpan eventLogTransferTime, bool setupLog4Net)
        {
            _diagConfig = configuration;
            AzureLogTransferTime = azureLogTransferTime;
            SetupLog4Net = setupLog4Net;
            TransferApplicationEventLog = transferApplicationEventLog;
            TransferSystemEventLog = transferSystemEventLog;
            EventLogTransferTime = eventLogTransferTime;
            CountersToTransfer = new List<CounterTransferConfig>();
        }

        public DiagnosticMonitorConfiguration Configure()
        {
            ConfigEventLogs();
            ScheduleCounterTransfers();
            SetupLogging();

            return _diagConfig;
        }

        public void AddPerformanceCounterCategory(string name, string description, IList<CounterCreationData> counterCreationDatas, PerformanceCounterCategoryType categoryType = PerformanceCounterCategoryType.SingleInstance)
        {
            if (!PerformanceCounterCategory.Exists(name))
            {
                var counterCollection = new CounterCreationDataCollection();
                counterCollection.AddRange(counterCreationDatas.ToArray());
                PerformanceCounterCategory.Create(name, description, categoryType, counterCollection);
            }
        }

        private void ScheduleCounterTransfers()
        {
            foreach (var counterTransferConfig in CountersToTransfer)
            {
                var config = new PerformanceCounterConfiguration
                                 {
                                     CounterSpecifier = counterTransferConfig.Name,
                                     SampleRate = counterTransferConfig.SampleRate
                                 };
                _diagConfig.PerformanceCounters.DataSources.Add(config);
            }

            _diagConfig.PerformanceCounters.ScheduledTransferPeriod = PerformanceCountersTransferTime;
        }

        private void ConfigEventLogs()
        {
            if (TransferSystemEventLog)
                _diagConfig.WindowsEventLog.DataSources.Add("System!*");

            if (TransferApplicationEventLog)
                _diagConfig.WindowsEventLog.DataSources.Add("Application!*");

            _diagConfig.WindowsEventLog.ScheduledTransferPeriod = EventLogTransferTime;
        }

        private void SetupLogging()
        {
            _diagConfig.Logs.ScheduledTransferPeriod = AzureLogTransferTime;

            if (SetupLog4Net)
            {
                var appender = new Log4NetAzureTableAppender { TableStorageConnectionStringName = StorageConnectionStringKey };
                appender.ActivateOptions();
                BasicConfigurator.Configure(appender);
            }
        }
    }

    public class CounterTransferConfig
    {
        public string Name { get; set; }
        public TimeSpan SampleRate { get; set; }

        public CounterTransferConfig(string name, TimeSpan sampleInterval)
        {
            Name = name;
            SampleRate = sampleInterval;
        }
    }
}