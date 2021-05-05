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
            char sepChar = ',', Func<double, string> convert = null)
        {
            convert ??= d => d.ToString();
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
        /// Reads a sequeuence of segments using the specified separator.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="sepChar"></param>
        /// <returns></returns>
        public static IEnumerable<Segment> Read(TextReader reader, char sepChar = ',')
        {
            for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                var values = line.Split(sepChar)
                    .Select(t => double.TryParse(t, out var d) ? d : double.NaN)
                    .Where(d => !double.IsNaN(d)).ToList();
                if (values.Count == 7)
                {
                    var start = new Vector3(values[0], values[1], values[2]);
                    var end = new Vector3(values[3], values[4], values[5]);
                    yield return new Segment()
                    {
                        Start = new Dummy() { Position = start },
                        End = new Dummy() { Position = end },
                        Radius = values[6]
                    };
                }
            }
        }
    }
}
