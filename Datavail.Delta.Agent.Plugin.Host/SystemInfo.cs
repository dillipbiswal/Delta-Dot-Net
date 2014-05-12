using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;
using Datavail.Delta.Infrastructure.Agent.Common;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Datavail.Delta.Agent.Plugin.Host
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
        public MEMORYSTATUSEX()
        {
            this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    public class SystemInfo : ISystemInfo
    {
        #region Disk
        public void GetDiskFreeSpace(string path, out long totalBytes, out long totalFreeBytes)
        {
            const string queryString = "SELECT * FROM Win32_Volume";
            var query = new SelectQuery(queryString);

            var regex = new Regex(@"{(.*)}", RegexOptions.IgnoreCase);

            var correctedPath = string.Empty;
            if (regex.IsMatch(path))
            {
                correctedPath = regex.Matches(path)[0].ToString().ToLower();
            }

            using (var searcher = new ManagementObjectSearcher(query))
            {
                foreach (var disk in searcher.Get())
                {
                    if (disk["DeviceID"].ToString().ToLower().Contains(correctedPath) || String.Equals(disk["Name"].ToString(), path, StringComparison.CurrentCultureIgnoreCase))
                    {
                        totalBytes = long.Parse(disk["Capacity"].ToString());
                        totalFreeBytes = long.Parse(disk["FreeSpace"].ToString());
                        return;
                    }
                }
            }

            totalBytes = -1;
            totalFreeBytes = -1;
        }

        public string[] GetLogicalDrives()
        {
            var output = new List<String>();
            const string queryString = "SELECT * FROM Win32_Volume";
            var query = new SelectQuery(queryString);

            using (var searcher = new ManagementObjectSearcher(query))
            {
                foreach (var disk in searcher.Get())
                {
                    Console.WriteLine(disk["Name"] + " | " + disk["DeviceId"].ToString());
                    output.Add(disk["DeviceId"].ToString());
                }
            }

            return output.ToArray();
        }

        public void GetDriveInfo(string path, out DriveType driveType, out string driveFormat, out long totalSize,
            out string volumeLabel)
        {
            const string queryString = "SELECT * FROM Win32_Volume";
            var query = new SelectQuery(queryString);

            var regex = new Regex(@"{(.*)}", RegexOptions.IgnoreCase);

            var correctedPath = string.Empty;
            if (regex.IsMatch(path))
            {
                correctedPath = regex.Matches(path)[0].ToString().ToLower();
            }

            using (var searcher = new ManagementObjectSearcher(query))
            {
                foreach (var disk in searcher.Get())
                {
                    foreach (var property in disk.Properties)
                    {
                        Debug.WriteLine(property.Name + ": " + disk.Properties[property.Name].Value);
                    }

                    if (disk["DeviceID"].ToString().ToLower().Contains(correctedPath) ||
                        String.Equals(disk["Name"].ToString(), path, StringComparison.CurrentCultureIgnoreCase))
                    {
                        try
                        {
                            driveType = (DriveType)uint.Parse(disk["DriveType"].ToString());
                        }
                        catch (Exception)
                        {
                            driveType = DriveType.Unknown;
                        }

                        try
                        {
                            driveFormat = disk["FileSystem"].ToString();
                        }
                        catch (Exception)
                        {
                            driveFormat = "Unknown";
                        }

                        try
                        {
                            totalSize = long.Parse(disk["Capacity"].ToString());
                        }
                        catch (Exception)
                        {
                            totalSize = -1;
                        }

                        try
                        {
                            if (disk["Label"] != null)
                            {
                                volumeLabel = disk["Name"] + " (" + disk["Label"] + ")";
                            }
                            else
                            {
                                volumeLabel = disk["Name"].ToString();
                            }
                        }
                        catch (Exception)
                        {
                            volumeLabel = string.Empty;
                        }

                        return;
                    }
                }
            }

            driveType = DriveType.Unknown;
            driveFormat = "Unknown";
            totalSize = -1;
            volumeLabel = "Unknown";
        }

        #endregion

        #region RAM
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public void GetRamInfo(out ulong totalPhysicalMemory, out ulong totalVirtualMemory, out ulong availablePhysicalMemory, out ulong availableVirtualMemory)
        {
            if (OsInfo.IsRunningOnUnix())
            {
                var proc = new System.Diagnostics.Process
                               {
                                   EnableRaisingEvents = false,
                                   StartInfo =
                                       {
                                           FileName = "/bin/cat",
                                           Arguments = "/proc/meminfo",
                                           UseShellExecute = false,
                                           RedirectStandardOutput = true
                                       }
                               };
                proc.Start();
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                totalPhysicalMemory = 0;
                totalVirtualMemory = 0;
                availablePhysicalMemory = 0;
                availableVirtualMemory = 0;

                var outputArray = output.Split('\n');
                for (var i = 0; i < outputArray.Length - 1; i++)
                {
                    var valueArray = outputArray[i].Split(':');

                    if (valueArray[0].ToLower() == "memtotal")
                    {
                        totalPhysicalMemory = ulong.Parse(valueArray[1].Replace("kB", "").Trim()) * 1024;
                    }

                    if (valueArray[0].ToLower() == "memfree")
                    {
                        availablePhysicalMemory = ulong.Parse(valueArray[1].Replace("kB", "").Trim()) * 1024;
                    }

                    if (valueArray[0].ToLower() == "swaptotal")
                    {
                        totalVirtualMemory = ulong.Parse(valueArray[1].Replace("kB", "").Trim()) * 1024;
                    }

                    if (valueArray[0].ToLower() == "swapfree")
                    {
                        availableVirtualMemory = ulong.Parse(valueArray[1].Replace("kB", "").Trim()) * 1024;
                    }
                }
            }
            else
            {
                MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                var result = GlobalMemoryStatusEx(memStatus);

                totalPhysicalMemory = memStatus.ullTotalPhys;
                totalVirtualMemory = memStatus.ullTotalVirtual;
                availablePhysicalMemory = memStatus.ullAvailPhys;
                availableVirtualMemory = memStatus.ullAvailVirtual;
            }
        }

        #endregion

        public float GetCpuUtilization()
        {
            if (OsInfo.IsRunningOnUnix())
            {
                var proc = new System.Diagnostics.Process
                {
                    EnableRaisingEvents = false,
                    StartInfo =
                    {
                        FileName = "/bin/cat",
                        Arguments = "/proc/loadavg",
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    }
                };

                proc.Start();
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                var valueArray = output.Split(' ');
                var cpu = float.Parse(valueArray[0]);

                return cpu;
            }
            else
            {
                var cpuCounter = new System.Diagnostics.PerformanceCounter
                                     {
                                         CategoryName = "Processor",
                                         CounterName = "% Processor Time",
                                         InstanceName = "_Total"
                                     };

                //Need to give the Perf counter a chance to start and collect data
                cpuCounter.NextValue();
                Thread.Sleep(3000);

                return cpuCounter.NextValue();
            }
        }

        public string GetOsPlatform()
        {
            return Environment.OSVersion.Platform.ToString();
        }
    }
}
