using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Datavail.Delta.Datawarehouse.Processor.Dimensions;

namespace Datavail.Delta.Datawarehouse.Processor
{
    class Program
    {
        static void Main(string[] args)
        {
            var date = Date.GetFromDateTime(DateTime.Now);
            Console.WriteLine("ok");
        }
    }
}
