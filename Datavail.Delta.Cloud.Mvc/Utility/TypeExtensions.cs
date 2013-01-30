using System;

namespace Datavail.Delta.Cloud.Mvc.Utility
{
    public static class TypeExtensions
    {
        public static bool CanBeCastTo<TType>(this Type t)
        {
            return typeof(TType).IsAssignableFrom(t);
        }
    }
}