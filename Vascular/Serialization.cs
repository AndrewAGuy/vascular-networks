using System;
using System.Collections.Generic;
using System.Linq;

namespace Vascular
{
    /// <summary>
    /// For making serializers aware of types.
    /// </summary>
    public static class Serialization
    {
        /// <summary>
        /// Get all concrete types that can be assigned to <paramref name="root"/>.
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static IEnumerable<Type> Types(Type root)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsAbstract && root.IsAssignableFrom(type));
        }

        /// <summary>
        /// Adds to <paramref name="types"/> all concrete types that can be assigned to <paramref name="root"/>.
        /// </summary>
        /// <param name="types"></param>
        /// <param name="root"></param>
        public static void AddTypes(this ICollection<Type> types, Type root)
        {
            foreach (var type in Types(root))
            {
                types.Add(type);
            }
        }
    }
}
