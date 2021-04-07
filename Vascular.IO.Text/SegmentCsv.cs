using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.IO.Text
{
    public static class SegmentCsv
    {
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

        public static void Write(TextWriter writer, IEnumerable<Segment> segments,
            char sepChar = ',', Func<double, string> convert = null)
        {
            convert ??= d => d.ToString();
            foreach (var segment in segments)
            {
                var start = segment.Start.Position;
                var end = segment.Start.Position;
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

        public static IEnumerable<Segment> Read(TextReader reader, char sepChar = ',')
        {
            var line = reader.ReadLine();
            while (line != null)
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
