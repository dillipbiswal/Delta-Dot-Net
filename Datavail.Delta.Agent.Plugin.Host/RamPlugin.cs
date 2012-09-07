using System;
using System.Xml.Linq;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;

namespace Datavail.Delta.Agent.Plugin.Host
{
    public class RamPlugin : IPlugin
    {
        private readonly ISystemInfo _systemInfo;
        private readonly IDataQueuer _dataQueuer;
        private readonly IDeltaLogger _logger;

        private Guid _metricInstance;
        private string _label;

        private ulong _totalPhysicalMemoryBytes;
        private string _totalPhysicalMemoryBytesFriendly;
        private string _availablePhysicalMemoryBytesFriendly;
        private ulong _totalVirtualMemoryBytes;
        private string _totalVirtualMemoryBytesFriendly;
        private string _availableVirtualMemoryBytesFriendly;
        private ulong _availablePhysicalMemoryBytes;
        private ulong _availableVirtualMemoryBytes;
        private double _percentagePhysicalFree;
        private double _percentageVirtualFree;

        private const double Mb = 1048576;
        private const double Gb = 1073741824;
        private const double Tb = 1099511627776;

        private string _output;

        public RamPlugin()
        {
            _systemInfo = new SystemInfo();
            var common = new Common();
            if (common.GetAgentVersion().Contains("4.0."))
            {
                _dataQueuer = new DataQueuer();
            }
            else
            {
                _dataQueuer = new DotNetDataQueuer();
            }
            _logger = new DeltaLogger();
        }

        public RamPlugin(ISystemInfo systemInfo, IDataQueuer dataqueuer, IDeltaLogger logger)
        {
            _systemInfo = systemInfo;
            _dataQueuer = dataqueuer;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("RamPlugIn.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}", metricInstance, label, data));
            
            try
            {
                _metricInstance = metricInstance;
                _label = label;

                GetRamInfo();
                BuildExecuteOutput();
                _dataQueuer.Queue(_output);
                _logger.LogDebug("Data Queued: " + _output);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException(string.Format("Unhandled Exception while running RamPlugin::Execute({0},{1},{2})", metricInstance, label, data), ex);
            }
        }



        private void GetRamInfo()
        {
            _systemInfo.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes);

            _percentagePhysicalFree = Math.Round(((double)_availablePhysicalMemoryBytes / (double)_totalPhysicalMemoryBytes) * 100, 2);
            _percentageVirtualFree = Math.Round(((double)_availableVirtualMemoryBytes / (double)_totalVirtualMemoryBytes) * 100, 2);

            //Total Physical Bytes
            if (_totalPhysicalMemoryBytes >= Tb)
            {
                _totalPhysicalMemoryBytesFriendly = Math.Round(_totalPhysicalMemoryBytes / Tb, 2) + " TB";
            }
            else if (_totalPhysicalMemoryBytes >= Gb)
            {
                _totalPhysicalMemoryBytesFriendly = Math.Round(_totalPhysicalMemoryBytes / Gb, 2) + " GB";
            }
            else if (_totalPhysicalMemoryBytes >= Mb)
            {
                _totalPhysicalMemoryBytesFriendly = Math.Round(_totalPhysicalMemoryBytes / Mb, 2) + " MB";
            }

            //Total Virtual Bytes
            if (_totalVirtualMemoryBytes >= Tb)
            {
                _totalVirtualMemoryBytesFriendly = Math.Round(_totalVirtualMemoryBytes / Tb, 2) + " TB";
            }
            else if (_totalVirtualMemoryBytes >= Gb)
            {
                _totalVirtualMemoryBytesFriendly = Math.Round(_totalVirtualMemoryBytes / Gb, 2) + " GB";
            }
            else if (_totalVirtualMemoryBytes >= Mb)
            {
                _totalVirtualMemoryBytesFriendly = Math.Round(_totalVirtualMemoryBytes / Mb, 2) + " MB";
            }

            //Physical Bytes Available
            if (_availablePhysicalMemoryBytes >= Tb)
            {
                _availablePhysicalMemoryBytesFriendly = Math.Round(_availablePhysicalMemoryBytes / Tb, 2) + " TB";
            }
            else if (_availablePhysicalMemoryBytes >= Gb)
            {
                _availablePhysicalMemoryBytesFriendly = Math.Round(_availablePhysicalMemoryBytes / Gb, 2) + " GB";
            }
            else if (_availablePhysicalMemoryBytes >= Mb)
            {
                _availablePhysicalMemoryBytesFriendly = Math.Round(_availablePhysicalMemoryBytes / Mb, 2) + " MB";
            }

            //Virtual Bytes Available
            if (_availableVirtualMemoryBytes >= Tb)
            {
                _availableVirtualMemoryBytesFriendly = Math.Round(_availableVirtualMemoryBytes / Tb, 2) + " TB";
            }
            else if (_availableVirtualMemoryBytes >= Gb)
            {
                _availableVirtualMemoryBytesFriendly = Math.Round(_availableVirtualMemoryBytes / Gb, 2) + " GB";
            }
            else if (_availableVirtualMemoryBytes >= Mb)
            {
                _availableVirtualMemoryBytesFriendly = Math.Round(_availableVirtualMemoryBytes / Mb, 2) + " MB";
            }
        }

        private void BuildExecuteOutput()
        {
            var xml = new XElement("RamPluginOutput",
                new XAttribute("timestamp", DateTime.UtcNow),
                new XAttribute("metricInstanceId", _metricInstance),
                new XAttribute("label", _label),
                new XAttribute("resultCode", 0),
                new XAttribute("resultMessage", string.Empty),
                new XAttribute("product", Environment.OSVersion.Platform),
                new XAttribute("productVersion", Environment.OSVersion.Version),
                new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                new XAttribute("productEdition", string.Empty),
                new XAttribute("totalPhysicalMemoryBytes", _totalPhysicalMemoryBytes),
                new XAttribute("totalPhysicalMemoryFriendly", _totalPhysicalMemoryBytesFriendly),
                new XAttribute("totalVirtualMemoryBytes", _totalVirtualMemoryBytes),
                new XAttribute("totalVirtualMemoryFriendly", _totalVirtualMemoryBytesFriendly),
                new XAttribute("availablePhysicalMemoryBytes", _availablePhysicalMemoryBytes),
                new XAttribute("availablePhysicalMemoryFriendly", _availablePhysicalMemoryBytesFriendly),
                new XAttribute("availableVirtualMemoryBytes", _availableVirtualMemoryBytes),
                new XAttribute("availableVirtualMemoryFriendly", _availableVirtualMemoryBytesFriendly),
                new XAttribute("percentagePhysicalMemoryAvailable", _percentagePhysicalFree),
                new XAttribute("percentageVirtualMemoryAvailable", _percentageVirtualFree));

            _output = xml.ToString();
        }
    }
}
