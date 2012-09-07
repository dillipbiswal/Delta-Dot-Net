using System;

namespace Datavail.Delta.Infrastructure.Agent.Common
{
    public static class Guard
    {
        public static void ArgumentNotNullOrEmptyString(string value, string paramName, string message=null)
        {
            if (String.IsNullOrEmpty(value))
                if (string.IsNullOrEmpty(message))
                    throw new ArgumentNullException(paramName);
                else
                    throw new ArgumentNullException(paramName,message);

        }

        public static void ArgumentNotNull(object value, string paramName, string message=null)
        {
            if (value == null)
                if (string.IsNullOrEmpty(message))
                    throw new ArgumentNullException(paramName);
                else
                    throw new ArgumentNullException(paramName,message);
        }

        public static void GuidArgumentNotEmpty(Guid value, string paramName, string message = null)
        {
            if (value == Guid.Empty)
                if (string.IsNullOrEmpty(message))
                    throw new ArgumentNullException(paramName);
                else
                    throw new ArgumentNullException(paramName, message);
        }
    }
}
