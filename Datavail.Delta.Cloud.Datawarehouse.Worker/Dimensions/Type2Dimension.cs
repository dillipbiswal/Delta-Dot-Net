using System;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Dimensions
{
    public abstract class Type2Dimension
    {
        public DateTime RowStart { get; set; }
        public DateTime? RowEnd { get; set; }
        public bool IsRowCurrent { get; set; }
    }
}
