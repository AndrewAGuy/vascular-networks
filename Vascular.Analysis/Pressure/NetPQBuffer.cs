using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular;
using System.IO;
using System.Threading;

namespace Vascular.Analysis.Pressure
{
    public class NetPQBuffer
    {
        private struct BranchEntry
        {
            public BranchEntry(int i, int j, double r)
            {
                this.i = i;
                this.j = j;
                this.r = r;
            }
            public int i, j;
            public double r;
        }

        private readonly Dictionary<Vector3, int> nodes = new Dictionary<Vector3, int>();
        private readonly List<BranchEntry> branches = new List<BranchEntry>();
        private readonly Dictionary<int, double> pressures = new Dictionary<int, double>();
        private readonly Dictionary<int, double> flows = new Dictionary<int, double>();

        private double[] solution;
        private byte[] commands;

        public double GetPressure(INode node)
        {
            return nodes.TryGetValue(node.Position, out var idx)
                ? solution[idx] : double.NaN;
        }

        public Func<IReadOnlyList<Segment>, double> Resistance { get; set; } =
            S => S.Sum(s => s.Length / Math.Pow(s.Radius, 4));

        private int Id(INode node)
        {
            return nodes.ExistingOrNew(node.Position, () => nodes.Count);
        }

        public NetPQBuffer Add(Branch branch)
        {
            Add(branch.Segments);
            return this;
        }

        public NetPQBuffer Add(IEnumerable<Branch> branches)
        {
            foreach (var b in branches)
            {
                Add(b);
            }
            return this;
        }

        public NetPQBuffer Add(IReadOnlyList<Segment> segments)
        {
            var i = Id(segments[0].Start);
            var j = Id(segments[^1].End);
            var r = this.Resistance(segments);
            branches.Add(new BranchEntry(i, j, r));
            return this;
        }

        public NetPQBuffer SetPressure(INode node, double pressure)
        {
            pressures[Id(node)] = pressure;
            return this;
        }

        public NetPQBuffer SetFlow(INode node, double flow)
        {
            flows[Id(node)] = flow;
            return this;
        }

        public NetPQBuffer Prepare()
        {
            var total =
                3 * sizeof(int)
                + branches.Count * (4 * sizeof(int) + sizeof(double))
                + (pressures.Count + flows.Count) * (2 * sizeof(int) + sizeof(double))
                + 3 * sizeof(int);
            commands = new byte[total];
            var writer = new BinaryWriter(new MemoryStream(commands));
            writer.Write((int)Commands.New);
            writer.Write(nodes.Count);
            writer.Write(branches.Count);
            for (var k = 0; k < branches.Count; ++k)
            {
                var b = branches[k];
                writer.Write((int)Commands.Branch);
                writer.Write(b.i);
                writer.Write(b.j);
                writer.Write(k);
                writer.Write(b.r);
            }
            foreach (var kv in pressures)
            {
                writer.Write((int)Commands.Pressure);
                writer.Write(kv.Key);
                writer.Write(kv.Value);
            }
            foreach (var kv in flows)
            {
                writer.Write((int)Commands.Flow);
                writer.Write(kv.Key);
                writer.Write(kv.Value);
            }
            writer.Write((int)Commands.Default);
            writer.Write((int)Commands.Solve);
            writer.Write((int)Commands.Clear);
            return this;
        }

        public async Task<bool> Run(NetPQ netPQ, CancellationToken token = default)
        {
            async Task send(Stream stream)
            {
                await stream.WriteAsync(commands, token);
                await stream.FlushAsync(token);
            }

            async Task<bool> receive(Stream stream)
            {
                var bytes = new byte[sizeof(int)];
                var count = await stream.ReadBufferAsync(bytes, token);
                var reader = new BinaryReader(new MemoryStream(bytes));
                if (count != bytes.Length && reader.ReadInt32() != 1)
                {
                    return false;
                }
                bytes = new byte[nodes.Count * sizeof(double)];
                count = await stream.ReadBufferAsync(bytes, token);
                if (count != bytes.Length)
                {
                    return false;
                }
                solution = new double[nodes.Count];
                reader = new BinaryReader(new MemoryStream(bytes));
                for (var index = 0; index < nodes.Count; ++index)
                {
                    solution[index] = reader.ReadDouble();
                }
                return true;
            }

            var write = send(netPQ.Input);
            var read = receive(netPQ.Output);
            await write;
            return await read;
        }
    }
}
