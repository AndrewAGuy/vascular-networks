using System;
using System.Collections.Generic;
using System.Linq;

namespace Vascular
{
    public static class Serialization
    {
        public static IEnumerable<Type> Types(Type root)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsAbstract && root.IsAssignableFrom(type));
        }

        public static void AddTypes(this ICollection<Type> types, Type root)
        {
            foreach (var type in Types(root))
            {
                types.Add(type);
            }
        }
    }
}
