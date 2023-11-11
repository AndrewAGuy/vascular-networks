using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.IO.Text
{
    /// <summary>
    /// Reads and writes segments to CSV format, with each segment being stored as start(x,y,z); end(x,y,z); radius.
    /// </summary>
    public static class SegmentCsv
    {
        /// <summary>
        /// Creates a header row for a list of segments.
        /// </summary>
        /// <param name="sepChar"></param>
        /// <returns></returns>
        public static string Header(char sepChar = ',')
        {
            return new StringBuilder()
                .Append("xStart").Append(sepChar)
                .Append("yStart").Append(sepChar)
                .Append("zStart").Append(sepChar)
                .Append("xEnd").Append(sepChar)
                .Append("yEnd").Append(sepChar)
                .Append("zEnd").Append(sepChar)
                .Append("radius").ToString();
        }

        /// <summary>
        /// Writes a sequence of segments using the specified conversion function and separator.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="segments"></param>
        /// <param name="sepChar"></param>
        /// <param name="convert"></param>
        public static void Write(TextWriter writer, IEnumerable<Segment> segments,
            char sepChar = ',', Func<double, string>? convert = null)
        {
            convert ??= Serialization.WriteDouble;
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
        /// Reads a sequeuence of segments using the specified separator and conversion function.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="sepChar"></param>
        /// <param name="convert"></param>
        /// <returns></returns>
        public static IEnumerable<Segment> Read(TextReader reader, char sepChar = ',',
            Func<string, double>? convert = null)
        {
            convert ??= Serialization.ParseDouble;
            for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                var values = line.Split(sepChar)
                    .Select(convert)
                    .Where(d => !double.IsNaN(d)).ToList();
                if (values.Count == 7)
                {
                    var start = new Vector3(values[0], values[1], values[2]);
                    var end = new Vector3(values[3], values[4], values[5]);
                    yield return new Segment(new Dummy() { Position = start }, new Dummy() { Position = end })
                    {
                        Radius = values[6]
                    };
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="segments"></param>
        /// <param name="props"></param>
        /// <param name="sepChar"></param>
        public static void Write(TextWriter writer, IEnumerable<Segment> segments,
            IEnumerable<Func<Segment, string>> props, char sepChar = ',')
        {
            foreach (var segment in segments)
            {
                var line = string.Join(sepChar, props.Select(p => p(segment)));
                writer.WriteLine(line);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="props"></param>
        /// <returns></returns>
        public static List<Func<Segment, string>> DefaultProperties(List<Func<Segment, string>>? props = null)
        {
            props ??= new();
            props.AddRange(new Func<Segment, string>[]
            {
                s => Serialization.WriteDouble(s.Start.Position.x),
                s => Serialization.WriteDouble(s.Start.Position.y),
                s => Serialization.WriteDouble(s.Start.Position.z),
                s => Serialization.WriteDouble(s.End.Position.x),
                s => Serialization.WriteDouble(s.End.Position.y),
                s => Serialization.WriteDouble(s.End.Position.z),
                s => Serialization.WriteDouble(s.Radius)
            });
            return props;
        }
    }
}
