using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Xml.Linq;
using Datavail.Delta.Agent.Plugin.Host;

namespace TestConsoleApp
{
    class Program
    {
        private static void Main222(string[] args)
        {
            const string queryString = "SELECT * FROM Win32_Volume";
            var query = new SelectQuery(queryString);

            using (var searcher = new ManagementObjectSearcher(query))
            {
                foreach (var disk in searcher.Get())
                {
                    Console.WriteLine(disk["DeviceId"].ToString());
                    Console.WriteLine(disk["Name"].ToString());
                    if (disk["Label"] != null)
                        Console.WriteLine(disk["Label"].ToString());
                    Console.WriteLine("-------------------------------------------");
                }
            }
            Console.ReadLine();
        }

        private static void Main(string[] args)
        {
        }

        private static void Main921(string[] args)
        {
            var plugin = new DiskInventoryPlugin();
            plugin.Execute(Guid.NewGuid(), "Label", string.Empty);
        }

        private static void Main1(string[] args)
        {
            const string data = @"<DiskPlugin Path=""\\\\?\\VOLUME{38763f71-4e4d-11e3-bccb-806e6f6e6963}\"" Label=""Disk Status for '\\\\?\\VOLUME{F6D1A919-2C5B-11DF-B86E-00215AABE40E}\'"" />";
            var plugin = new DiskPlugin();
            plugin.Execute(Guid.NewGuid(), "Label", data);
        }

        private static void Main11(string[] args)
        {
            string scopeStr = string.Format(@"root\cimv2");

            var scope = new ManagementScope(scopeStr);
            scope.Connect();

            var queryString = "SELECT * FROM Win32_Volume";
            var query = new SelectQuery(queryString);

            using (var searcher = new ManagementObjectSearcher(query))
            {
                foreach (var disk in searcher.Get())
                {
                    Console.WriteLine("DeviceId: " + disk["DeviceID"]);
                    Console.WriteLine("Name: " + disk["Name"]);
                    Console.WriteLine("DriveType: " + disk["DriveType"]);
                    Console.WriteLine("DriveFormat: " + disk["FileSystem"]);
                    Console.WriteLine("TotalSize: " + disk["Capacity"]);
                    Console.WriteLine("Label: " + disk["Label"]);
                    Console.WriteLine("----------------------------------------");
                    //if (disk["DeviceID"].ToString().ToLower() == args[0].ToLower() || disk["Name"].ToString().ToLower() == args[0].ToLower())
                    //{
                    //    Console.WriteLine("DriveType: " + disk["DriveType"]);
                    //   Console.WriteLine("DriveFormat: " + disk["FileSystem"]);
                    //  Console.WriteLine("TotalSize: " + disk["Capacity"]);
                    // Console.WriteLine("Label: " + disk["Label"]);
                    // }
                }
            }
        }

        private static void Main2(string[] args)
        {
            string scopeStr = string.Format(@"root\cimv2");

            var scope = new ManagementScope(scopeStr);
            scope.Connect();

            var queryString = "SELECT * FROM Win32_Volume";
            var query = new SelectQuery(queryString);

            using (var searcher = new ManagementObjectSearcher(query))
            {
                foreach (var disk in searcher.Get())
                {

                    //Console.WriteLine("Free: " + disk["Name"]);
                    if (disk["DeviceID"].ToString().ToLower() == args[0].ToLower() || disk["Name"].ToString().ToLower() == args[0].ToLower())
                    {
                        Console.WriteLine("DriveType: " + disk["DriveType"]);
                        Console.WriteLine("DriveFormat: " + disk["FileSystem"]);
                        Console.WriteLine("TotalSize: " + disk["Capacity"]);
                        Console.WriteLine("Label: " + disk["Label"]);
                    }
                }
            }
        }
    }
}
