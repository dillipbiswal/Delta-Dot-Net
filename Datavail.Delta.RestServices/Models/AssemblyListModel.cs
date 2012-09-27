﻿using System;
using System.Collections.Generic;

namespace Datavail.Delta.RestServices.Models
{
    public class AssemblyModel
    {
        public string AssemblyName { get; set; }
        public string Version { get; set; }
    }
    public class AssemblyListModel
    {
        public AssemblyListModel()
        {
            Assemblies = new List<AssemblyModel>();
        }

        public List<AssemblyModel> Assemblies { get; set; }
        public string GeneratingServer { get; set; }
        public DateTime Timestamp { get; set; }
    }
}