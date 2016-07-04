﻿using Datavail.Delta.Agent.Scheduler;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace Datavail.Delta.Agent.SharedCode.Queues
{
    public class DotNetQueueRunner
    {
        private readonly ICommon _common;
        private readonly IDeltaLogger _logger;
        private readonly BlockingCollection<QueueMessage> _queue;
        private readonly IConfigLoader _loader;

        private WaitHandle _waitHandle;
        private readonly string _path;

        public DotNetQueueRunner()
        {
            try
            {
                _common = new Common();
                _logger = new DeltaLogger();
                _queue = DotNetDataQueuerFactory.Current;
                _path = Path.Combine(_common.GetCachePath(), "QueueData.xml");
                _loader = new ConfigFileLoader();
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception in QueueRunner()", ex);
            }
        }


        public DotNetQueueRunner(ICommon common, IDeltaLogger deltaLogger)
        {
            try
            {
                _common = common;
                _logger = deltaLogger;
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception in QueueRunner(ICommon common, IDeltaLogger deltaLogger)", ex);
            }
        }


        public string GetPluginUriAddress(string infopluginname)
        {

            if (!File.Exists(_common.GetConfigPath()))
                return "";

            var config = _loader.LoadConfig(_common.GetConfigPath());

            var doc = new XmlDocument();
            doc.LoadXml(config);

            var nav = doc.CreateNavigator();
            var expr = nav.Compile("//APIURI");
            string strURIAddress = "";
            try
            {
                foreach (XPathNavigator node in nav.Select(expr))
                {
                    try
                    {

                        string pluginname = (node.GetAttribute("PlugInName", ""));
                        string uriAddress = (node.GetAttribute("URIAddress", ""));

                        if (infopluginname.ToLower().Contains(pluginname.ToLower()))
                        {
                            strURIAddress = uriAddress;
                            break;
                        }
                    }
                    catch
                    {

                    }
                }

            }
            catch
            { }

            return strURIAddress;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>

        public bool IsAgentErrorEnabled()
        {
            bool strAgentErrorStatus = false;

            if (!File.Exists(_common.GetConfigPath()))
                strAgentErrorStatus = false;

            var config = _loader.LoadConfig(_common.GetConfigPath());

            var doc = new XmlDocument();
            doc.LoadXml(config);

            var nav = doc.CreateNavigator();
            var expr = nav.Compile("//AgentError");

            try
            {
                foreach (XPathNavigator node in nav.Select(expr))
                {
                    try
                    {
                        string agenterrorstatus = (node.GetAttribute("AgentErrorStatus", ""));
                        if (agenterrorstatus == "Enabled")
                        {
                            strAgentErrorStatus = true;
                            break;
                        }
                    }
                    catch
                    {

                    }
                }

            }
            catch
            { }

            return strAgentErrorStatus;
        }

        public void Execute(WaitHandle waitHandle)
        {
            try
            {
                _waitHandle = waitHandle;

                var tenantId = _common.GetTenantId();
                var serverId = _common.GetServerId();
                var hostname = _common.GetHostname();
                var ipaddress = _common.GetIpAddress();



                //Push the serialized messages into the queue
                if (File.Exists(_path))
                {
                    try
                    {
                        var deserializer = new XmlSerializer(typeof(List<QueueMessage>));
                        using (var tr = new StreamReader(_path))
                        {
                            var tempQueue = (List<QueueMessage>)deserializer.Deserialize(tr);
                            tr.Close();

                            foreach (var queueMessage in tempQueue)
                            {
                                _queue.Add(queueMessage);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogUnhandledException("Unhandled Exception", ex); //Added for debug

                    }
                    finally
                    {
                        if (File.Exists(_path))
                            File.Delete(_path);
                    }
                }

                var client = new RestClient(ConfigurationManager.AppSettings["DeltaApiUrl"]);

                while (true)
                {
                    try
                    {
                        _logger.LogDebug("QueueRunner Run Begin");

                        QueueMessage msg;
                        _queue.TryTake(out msg, TimeSpan.FromSeconds(3));

                        while (msg != null)
                        {
                            IRestResponse response = null;

                            try
                            {


                                var request = new RestRequest("Server/PostData/{id}", Method.POST) { RequestFormat = DataFormat.Json };

                                var Inventoryrequest = new RestRequest("Server/PostInventoryData/{id}", Method.POST) { RequestFormat = DataFormat.Json };

                                //Add ServerId to the URL
                                request.AddUrlSegment("id", serverId.ToString());

                                Inventoryrequest.AddUrlSegment("id", serverId.ToString());

                                //Create JSON body
                                request.AddBody(new { Data = msg.Data, Hostname = hostname, IpAddress = ipaddress, Timestamp = msg.Timestamp, TenantId = tenantId.ToString() });

                                Inventoryrequest.AddBody(new { Data = msg.Data, Hostname = hostname, IpAddress = ipaddress, Timestamp = msg.Timestamp, TenantId = tenantId.ToString() });
                                //request.AddParameter("Data", msg.Data);
                                //request.AddParameter("Hostname", hostname);
                                //request.AddParameter("IpAddress", ipaddress);
                                //request.AddParameter("Timestamp", msg.Timestamp);
                                //request.AddParameter("TenantId", tenantId.ToString());

                                //response = client.Execute(request);

                                string pluginuriaddress = GetPluginUriAddress(msg.Data);

                                bool responceStatus = true;

                                //if ((msg.Data).ToString().ToLower().Contains("agenterroroutput") && Convert.ToBoolean(ConfigurationManager.AppSettings["AgentErrorFlag"]))
                                if ((msg.Data).ToString().ToLower().Contains("agenterroroutput"))
                                {
                                    if (IsAgentErrorEnabled())
                                    {
                                        var ErrorAPIclient = new RestClient(ConfigurationManager.AppSettings["DeltaErrApiUrl"]);
                                        response = ErrorAPIclient.Execute(request);
                                        if (response.StatusCode == HttpStatusCode.OK)
                                        {
                                            _common.SetBackoffTimer(0);
                                            _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, "Posting " + msg.Data);
                                        }
                                        else
                                        {
                                            responceStatus = false;
                                            _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, "Posting " + msg.Data);
                                            var errorMessage = string.Format("Error while posting data. {0}: {1}", response.StatusCode, response.ErrorMessage);
                                            _logger.LogSpecificError(WellKnownAgentMesage.UnhandledException, errorMessage);

                                            DoBackOff();

                                        }
                                    }
                                    else
                                    {
                                        responceStatus = false;
                                    }
                                }
                                else
                                {
                                    if (pluginuriaddress == "")
                                    {
                                        response = client.Execute(request);
                                        if (response.StatusCode == HttpStatusCode.OK)
                                        {
                                            _common.SetBackoffTimer(0);
                                            _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, "Posting " + msg.Data);
                                        }
                                        else
                                        {
                                            responceStatus = false;
                                            _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, "Posting " + msg.Data);
                                            var errorMessage = string.Format("Error while posting data. {0}: {1}", response.StatusCode, response.ErrorMessage);
                                            _logger.LogSpecificError(WellKnownAgentMesage.UnhandledException, errorMessage);

                                            DoBackOff();

                                        }
                                    }
                                    else
                                    {
                                        var APIclient = new RestClient(pluginuriaddress);
                                        response = APIclient.Execute(Inventoryrequest);
                                        if (response.StatusCode == HttpStatusCode.OK)
                                        {
                                            _common.SetBackoffTimer(0);
                                            _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, "Posting " + msg.Data);
                                        }
                                        else
                                        {
                                            responceStatus = false;
                                            _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, "Posting " + msg.Data);
                                            var errorMessage = string.Format("Error while posting data. {0}: {1}", response.StatusCode, response.ErrorMessage);
                                            _logger.LogSpecificError(WellKnownAgentMesage.UnhandledException, errorMessage);

                                            DoBackOff();

                                        }

                                    }
                                }

                                if (responceStatus)
                                {
                                    if (response.StatusCode == HttpStatusCode.OK)
                                    {
                                        _common.SetBackoffTimer(0);
                                        _logger.LogDebug("Posted sucessfully");
                                    }

                                }
                                else
                                {
                                }
                            }
                            catch (ThreadAbortException)
                            {
                                //Don't log anything if we're shutting down
                            }
                            catch (Exception ex)
                            {
                                _logger.LogUnhandledException("Unhandled Exception in QueueRunner.Execute()", ex);
                                DoBackOff();
                            }

                            if (response != null && response.StatusCode != HttpStatusCode.OK && _queue.Count < 5000)
                            {
                                _queue.Add(msg);
                            }

                            //Only save top N messages
                            while (_queue.Count > 5000)
                            {
                                QueueMessage throwAwayMessage;
                                _queue.TryTake(out throwAwayMessage, TimeSpan.FromSeconds(1));
                            }

                            _queue.TryTake(out msg, TimeSpan.FromSeconds(3));

                            //Wait to see if we're cancelled, if not loop again   
                            var cancelled = _waitHandle.WaitOne(1000);
                            if (cancelled)
                            {
                                SerializeQueue();
                                break;
                            }
                        }
                    }
                    catch (ThreadAbortException)
                    {
                    }
                    catch (Exception ex)
                    {
                        _logger.LogUnhandledException("Unhandled Exception in Scheduler.Execute(WaitHandle waitHandle)", ex);
                    }

                }
            }
            catch (ThreadAbortException)
            {
            }
        }

        private void DoBackOff()
        {
            //If there is an error communicating with the service, don't fill the error logs.
            //backoff 30 seconds for each loop until we've reached 10 minutes. Then reset to 1 minute. 
            var backoffTimer = _common.GetBackoffTimer();

            if (backoffTimer >= 600)
            {
                backoffTimer = 60;
                _common.SetBackoffTimer(backoffTimer);
            }
            else
            {
                backoffTimer = backoffTimer + 30;
                _common.SetBackoffTimer(backoffTimer);
            }

            _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, "Waiting " + backoffTimer + " seconds before retrying posting queued data");

            var cancelled = _waitHandle.WaitOne(TimeSpan.FromSeconds(backoffTimer));
            if (cancelled)
                SerializeQueue();
        }

        private void SerializeQueue()
        {
            try
            {
                if (_queue.Count <= 0) return;

                var serializableList = new List<QueueMessage>(_queue);
                var serializer = new XmlSerializer(typeof(List<QueueMessage>));
                var tw = new StreamWriter(_path);
                serializer.Serialize(tw, serializableList);
                tw.Close();
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Could not serialize queue", ex);
            }
        }

        public void PostAgentStartStop(string msg)
        {
            try
            {
                var tenantId = _common.GetTenantId();
                string serverId = _common.GetServerId().ToString();
                var hostname = _common.GetHostname();
                var ipaddress = _common.GetIpAddress();

                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["DeltaApiUrl"]);

                var request = new RestRequest("Server/PostAgentStartStopStatus/{id}/{msg}", Method.POST) { RequestFormat = DataFormat.Json };
                request.AddUrlSegment("id", serverId);
                request.AddUrlSegment("msg", msg);
                response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _common.SetBackoffTimer(0);
                    _logger.LogDebug("Agent Start and Stop status posted sucessfully");
                }
                else
                {
                    var errorMessage = string.Format("Error while posting Agent Start and Stop status. {0}: {1}", response.StatusCode, response.ErrorMessage);
                    _logger.LogSpecificError(WellKnownAgentMesage.UnhandledException, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception in QueueRunner.Execute()", ex);
            }

        }
    }
}