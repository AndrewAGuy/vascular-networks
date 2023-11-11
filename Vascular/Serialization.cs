using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Vascular.Structure;

namespace Vascular
{
    /// <summary>
    /// For making serializers aware of types.
    /// </summary>
    public static class Serialization
    {
        /// <summary>
        /// Writes a sequence of segments using the specified conversion function and separator.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="segments"></param>
        /// <param name="sepChar"></param>
        /// <param name="convert"></param>
        public static void WriteCsv(TextWriter writer, IEnumerable<Segment> segments,
            char sepChar = ',', Func<double, string>? convert = null)
        {
            convert ??= WriteDouble;
            foreach (var segment in segments)
            {
                var start = segment.Start.Position;
                var end = segment.End.Position;
                var radius = segment.Radius;
                var line = new StringBuilder()
                    .Append(convert(start.x)).Append(sepChar)
                    .Append(convert(start.y)).Append(sepChar)
                    .Append(convert(start.z)).Append(sepChar)
                    .Append(convert(end.x)).Append(sepChar)
                    .Append(convert(end.y)).Append(sepChar)
                    .Append(convert(end.z)).Append(sepChar)
                    .Append(convert(radius));
                writer.WriteLine(line);
            }
        }

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

        /// <summary>
        ///
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static double ParseDouble(string text)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? number : double.NaN;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string WriteDouble(double number)
        {
            return number.ToString("G17", CultureInfo.InvariantCulture);
        }
    }
}
