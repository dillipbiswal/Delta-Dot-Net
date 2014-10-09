using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Datavail.Delta.Agent.Plugin.LogWatcher.Cluster;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Cluster;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;


namespace Datavail.Delta.Agent.Plugin.LogWatcher
{
    public class LogWatcherPlugin : IPlugin
    {
        private readonly IClusterInfo _clusterInfo;
        private readonly IDataQueuer _dataQueuer;
        private readonly IDeltaLogger _logger;
        private bool _isRunning;
        private FileSystemWatcher _fileWatcher;
        private bool _watchedFileDeleted;

        private Guid _metricInstance;
        private string _label;

        private string _clusterGroupName;
        private bool _runningOnCluster = false;

        //Specific
        private string _fileToWatch;
        private readonly List<string> _matchExpressions = new List<string>();
        private readonly List<string> _excludeExpressions = new List<string>();

        public LogWatcherPlugin()
        {
            _clusterInfo = new ClusterInfo();
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

        public LogWatcherPlugin(IDataQueuer dataQueuer, IDeltaLogger logger, IClusterInfo clusterInfo)
        {
            _clusterInfo = clusterInfo;
            _dataQueuer = dataQueuer;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("LogWatcher.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}", metricInstance, label, data));
            try
            {
                Guard.GuidArgumentNotEmpty(metricInstance, "metricInstance");
                Guard.ArgumentNotNullOrEmptyString(label, "label");
                Guard.ArgumentNotNullOrEmptyString(data, "data");

                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);

                if (!_runningOnCluster || (_runningOnCluster && _clusterInfo.IsActiveClusterNodeForGroup(_clusterGroupName)))
                {
                    SetupLogWatcher();
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception", ex);
            }

        }

        //<LogWatcherPluginInput FileNameToWatch="">
        //  <MatchExpressions>
        //      <MatchExpression expression=""/>
        //  </MatchExpressions>
        //  <ExcludeExpressions>
        //      <ExcludeExpression exresssion="" />
        //  </ExcludeExpressions>
        //</LogWatcherPluginInput>
        private void ParseData(string data)
        {
            var xmlData = XElement.Parse(data);

            _fileToWatch = xmlData.Attribute("FileNameToWatch").Value;

            var matchExpressions = xmlData.Elements("MatchExpressions").Elements("MatchExpression");
            foreach (var matchExpression in matchExpressions)
            {
                var xmlExpression = XElement.Parse(matchExpression.ToString());
                var expression = xmlExpression.Attribute("expression").Value;

                if (!_matchExpressions.Contains(expression))
                {
                    _matchExpressions.Add(expression);
                }
            }

            var excludeExpressions = xmlData.Elements("ExcludeExpressions").Elements("ExcludeExpression");
            foreach (var excludeExpression in excludeExpressions)
            {
                var xmlExpression = XElement.Parse(excludeExpression.ToString());
                var expression = xmlExpression.Attribute("expression").Value;

                if (!_excludeExpressions.Contains(expression))
                {
                    _excludeExpressions.Add(expression);
                }
            }

            if (xmlData.Attribute("ClusterGroupName") != null)
            {
                _runningOnCluster = true;
                _clusterGroupName = xmlData.Attribute("ClusterGroupName").Value;
            }
        }

        private void SetupLogWatcher()
        {
            _isRunning = true;
            _watchedFileDeleted = false;
            const int historyCount = 1000;

            StreamReader fileReader = null;
            try
            {
                if (_fileWatcher == null)
                {
                    _fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(_fileToWatch),
                                                         Path.GetFileName(_fileToWatch)) { EnableRaisingEvents = true };
                    _fileWatcher.Created += FilewatcherCreated;
                }

                var fileStream = new FileStream(_fileToWatch, FileMode.Open, FileAccess.Read,
                                                FileShare.ReadWrite | FileShare.Delete,1024, true);
                fileStream.Seek(Math.Max(0, fileStream.Length - historyCount), SeekOrigin.Begin);
                var encoding = GetFileEncoding(_fileToWatch);
                fileReader = new StreamReader(fileStream, encoding);

                while (_isRunning)
                {
                    try
                    {
                        Thread.Sleep(2000);
                        var results = fileReader.ReadToEnd();

                        if (results.Length > 0)
                            Debug.WriteLine(results);

                        if (results.Length > 0)
                            QueueEventForMatch(results);
                    }
                    catch (ThreadInterruptedException)
                    {
                        Thread.Sleep(50);
                    }
                    catch (Exception)
                    {
                        _isRunning = false;
                    }
                }

                if (_watchedFileDeleted)
                {
                    ReadFileContentsOnCreation();
                    SetupLogWatcher();
                }
            }
            catch (Exception ex)
            {
                if (_fileToWatch != null)
                    _logger.LogUnhandledException("Unhandled Exception in LogWatcher (" + _fileToWatch + ")", ex);
                else
                    _logger.LogUnhandledException("Unhandled Exception in LogWatcher", ex);
            }
            finally
            {
                if (fileReader != null)
                    fileReader.Close();
            }
        }

        void ReadFileContentsOnCreation()
        {
            using (var fileStream = new FileStream(_fileToWatch, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1024, true))
            {
                fileStream.Seek(0, SeekOrigin.Begin);
                var encoding = GetFileEncoding(_fileToWatch);
                var fileReader = new StreamReader(fileStream, encoding);
                var results = fileReader.ReadToEnd();

                QueueEventForMatch(results);
            }
        }

        void FilewatcherCreated(object sender, FileSystemEventArgs e)
        {
            _isRunning = false;
            _watchedFileDeleted = true;
        }

        private void QueueEventForMatch(string input)
        {
            var lines = input.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            var match = false;
            var exclude = false;

            foreach (var line in lines)
            {
                foreach (var matchExpression in _matchExpressions)
                {
                    if (Regex.IsMatch(line, matchExpression))
                    {
                        match = true;
                    }
                }

                foreach (var excludeExpression in _excludeExpressions)
                {
                    if (Regex.IsMatch(line, excludeExpression))
                    {
                        exclude = true;
                    }
                }

                if (match && !exclude)
                {
                    BuildExecuteOutput(line);
                }

                match = false;
                exclude = false;
            }
        }

        private void BuildExecuteOutput(string matchingLine)
        {
            var xml = new XElement("LogWatcherPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty),
                                   new XAttribute("matchingLine", matchingLine));

            var output = xml.ToString();
            _logger.LogDebug("Data Queued: " + output);
            _dataQueuer.Queue(output);
        }

        private static Encoding GetFileEncoding(string srcFile)
        {
            // *** Use Default of Encoding.Default (Ansi CodePage)
            var enc = Encoding.Default;

            // *** Detect byte order mark if any - otherwise assume default
            var buffer = new byte[5];
            var file = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            file.Read(buffer, 0, 5);
            file.Close();

            if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
                enc = Encoding.UTF8;
            else if (buffer[0] == 0xfe && buffer[1] == 0xff)
                enc = Encoding.Unicode;
            else if (buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xfe && buffer[3] == 0xff)
                enc = Encoding.UTF32;
            else if (buffer[0] == 0x2b && buffer[1] == 0x2f && buffer[2] == 0x76)
                enc = Encoding.UTF7;
            else if (buffer[0] == 0xff && buffer[1] == 0xfe && buffer[2] == 0x32)
                enc = Encoding.GetEncoding("UTF-16LE");

            return enc;
        }


    }
}
