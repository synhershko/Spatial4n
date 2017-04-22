#if !NETSTANDARD1_6
using System;

namespace Spatial4n.Core
{
    internal static class TypeExtensions
    {
        public static Type GetTypeInfo(this Type type)
        {
            return type;
        }
    }
}
#endif