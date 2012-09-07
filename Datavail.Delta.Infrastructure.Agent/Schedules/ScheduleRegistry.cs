using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;
using Datavail.Delta.Agent.Scheduler;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using FluentScheduler;

namespace Datavail.Delta.Infrastructure.Agent.Schedules
{
    public class ScheduleRegistry : Registry
    {
        private readonly IConfigLoader _loader;
        private readonly ICommon _common;
        private string _currentPlugin;
        private readonly IDeltaLogger _logger;

        public ScheduleRegistry(IConfigLoader loader, ICommon common)
        {
            _common = common;
            _loader = loader;
            _logger = new DeltaLogger();

            BuildSchedules();
        }

        private void BuildSchedules()
        {
            try
            {
                if (!File.Exists(_common.GetConfigPath()))
                    return;

                var config = _loader.LoadConfig(_common.GetConfigPath());

                var doc = new XmlDocument();
                doc.LoadXml(config);

                var nav = doc.CreateNavigator();
                var expr = nav.Compile("//MetricInstance");

                foreach (XPathNavigator node in nav.Select(expr))
                {
                    try
                    {
                        //Dynamically Load the Adapter Assembly
                        var assemblyName = node.GetAttribute("AdapterAssembly", "");
                        var assemblyVersion = node.GetAttribute("AdapterVersion", "");
                        var className = node.GetAttribute("AdapterClass", "");
                        _currentPlugin = assemblyName + "." + className;

                        var assemblyPath = Path.Combine(_common.GetPluginPath(), assemblyName + "." + assemblyVersion + ".dll");
                        var fullClassName = assemblyName + "." + className;
                        var assembly = Assembly.LoadFrom(assemblyPath).GetType(fullClassName);

                        //Get the reference for the method
                        var methodInfo = assembly.GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance);
                        var constructorInfo = assembly.GetConstructor(Type.EmptyTypes);
                        var responder = constructorInfo.Invoke(new object[] { });

                        //new[] {typeof(Guid), typeof(string), typeof(string)}
                        //new object[]{metricInstanceId, label, data}

                        var metricInstanceId = Guid.Parse(node.GetAttribute("Id", ""));
                        var label = node.GetAttribute("Label", "");
                        var data = node.GetAttribute("Data", "");

                        //Create input parameters to be passed to the method
                        var parameter = new object[3];
                        parameter[0] = metricInstanceId;
                        parameter[1] = label;
                        parameter[2] = data;

                        //Get schedule attributes from the XML config file
                        var scheduleType = Int32.Parse(node.GetAttribute("ScheduleType", ""));
                        var scheduleInterval = Int32.Parse(node.GetAttribute("ScheduleInterval", ""));

                        //Create the schedule based on the runInterval
                        switch (scheduleType)
                        {
                            case -1: //RunOnce
                                {
                                    var message = "Schedule Created: " + assembly.FullName + " to run now (once) (Data: " + data + ")";
                                    _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, message);

                                    Schedule(() => methodInfo.Invoke(responder, parameter)).ToRunNow();
                                    break;
                                }
                            case 0: //Seconds
                                {
                                    var message = "Schedule Created: " + assembly.FullName + " every " + scheduleInterval + " seconds (Data: " + data + ")";
                                    _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, message);

                                    Schedule(() => methodInfo.Invoke(responder, parameter)).ToRunEvery(scheduleInterval)
                                        .Seconds();
                                    break;
                                }
                            case 1: //Minute
                                {
                                    var message = "Schedule Created: " + assembly.FullName + " every " + scheduleInterval + " minutes (Data: " + data + ")";
                                    _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, message);

                                    Schedule(() => methodInfo.Invoke(responder, parameter)).ToRunEvery(scheduleInterval)
                                        .Minutes();
                                    break;
                                }
                            case 2: //Hours
                                {
                                    var runMinute = node.GetAttribute("ScheduleMinute", "");
                                    if (runMinute == string.Empty) runMinute = "0";

                                    Schedule(() => methodInfo.Invoke(responder, parameter))
                                        .ToRunEvery(scheduleInterval).Hours()
                                        .At(Int32.Parse(runMinute));

                                    var message = "Schedule Created: " + assembly.FullName + " every " + scheduleInterval + " hours at " + runMinute + " past the hour (Data: " + data + ")";
                                    _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, message);

                                    break;
                                }
                            case 3: //Days
                                {
                                    var runHour = node.GetAttribute("ScheduleHour", "");
                                    if (runHour == string.Empty) runHour = "0";

                                    var runMinute = node.GetAttribute("ScheduleMinute", "");
                                    if (runMinute == string.Empty) runMinute = "0";

                                    Schedule(() => methodInfo.Invoke(responder, parameter))
                                        .ToRunEvery(scheduleInterval).Days()
                                        .At(Int32.Parse(runHour), Int32.Parse(runMinute));
                                    break;
                                }
                            case 4: //Weeks
                                {
                                    var runDay = node.GetAttribute("ScheduleDay", "");
                                    if (runDay == string.Empty) runDay = "0";

                                    var runHour = node.GetAttribute("ScheduleHour", "");
                                    if (runHour == string.Empty) runHour = "0";

                                    var runMinute = node.GetAttribute("ScheduleMinute", "");
                                    if (runMinute == string.Empty) runMinute = "0";

                                    Schedule(() => methodInfo.Invoke(responder, parameter))
                                        .ToRunEvery(scheduleInterval).Weeks()
                                        .On((DayOfWeek)Int32.Parse(runDay))
                                        .At(Int32.Parse(runHour), Int32.Parse(runMinute));
                                    break;
                                }
                            case 5: //Month
                                {
                                    var runDay = node.GetAttribute("ScheduleDay", "");
                                    if (runDay == string.Empty) runDay = "0";

                                    var runHour = node.GetAttribute("ScheduleHour", "");
                                    if (runHour == string.Empty) runHour = "0";

                                    var runMinute = node.GetAttribute("ScheduleMinute", "");
                                    if (runMinute == string.Empty) runMinute = "0";

                                    Schedule(() => methodInfo.Invoke(responder, parameter))
                                        .ToRunEvery(scheduleInterval).Months()
                                        .On(Int32.Parse(runDay))
                                        .At(Int32.Parse(runHour), Int32.Parse(runMinute));
                                    break;
                                }
                            case 6: //Year
                                {
                                    var runDay = node.GetAttribute("ScheduleDay", "");
                                    if (runDay == string.Empty) runDay = "0";

                                    var runHour = node.GetAttribute("ScheduleHour", "");
                                    if (runHour == string.Empty) runHour = "0";

                                    var runMinute = node.GetAttribute("ScheduleMinute", "");
                                    if (runMinute == string.Empty) runMinute = "0";

                                    Schedule(() => methodInfo.Invoke(responder, parameter))
                                        .ToRunEvery(scheduleInterval).Years()
                                        .On(Int32.Parse(runDay))
                                        .At(Int32.Parse(runHour), Int32.Parse(runMinute));
                                    break;
                                }
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        _logger.LogSpecificError(WellKnownAgentMesage.UnhandledException, "Error occurred while creating plugin " + _currentPlugin + ". The specified plugin was not found.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogUnhandledException("Error occurred while creating plugin " + _currentPlugin, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception", ex);
            }
        }
    }
}
