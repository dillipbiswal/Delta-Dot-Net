using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Datavail.Delta.Datawarehouse.Processor.Dimensions
{
    public abstract class DimensionBase
    {
        public DateTime RowStart { get; set; }
        public DateTime RowEnd { get; set; }
        public bool IsRowCurrent { get; set; }
    }
}
