using System;

namespace Datavail.Delta.Infrastructure.Util
{
    public static class DateFunctions
    {
        public static DateTime DateTimeDefault
        {
            get
            {
                return new DateTime(1970, 1, 1, 0, 0, 0);    
            }
        }
    }
}
