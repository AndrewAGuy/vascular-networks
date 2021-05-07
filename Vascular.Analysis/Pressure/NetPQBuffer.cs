using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;
using System.IO;
using System.Threading;

namespace Vascular.Analysis.Pressure
{
    /// <summary>
    /// Build a command buffer before running on a <see cref="NetPQ"/> instance.
    /// </summary>
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

        /// <summary>
        /// Once solved, query the pressure at a node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public double GetPressure(INode node)
        {
            return nodes.TryGetValue(node.Position, out var idx)
                ? solution[idx] : double.NaN;
        }

        /// <summary>
        /// Compute the resistance of a segment chain. Defaults to HP flow.
        /// </summary>
        public Func<IReadOnlyList<Segment>, double> Resistance { get; set; } =
            S => S.Sum(s => s.Length / Math.Pow(s.Radius, 4));

        private int Id(INode node)
        {
            return nodes.ExistingOrNew(node.Position, () => nodes.Count);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="branch"></param>
        /// <returns></returns>
        public NetPQBuffer Add(Branch branch)
        {
            Add(branch.Segments);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="branches"></param>
        /// <returns></returns>
        public NetPQBuffer Add(IEnumerable<Branch> branches)
        {
            foreach (var b in branches)
            {
                Add(b);
            }
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segments"></param>
        /// <returns></returns>
        public NetPQBuffer Add(IReadOnlyList<Segment> segments)
        {
            var i = Id(segments[0].Start);
            var j = Id(segments[^1].End);
            var r = this.Resistance(segments);
            branches.Add(new BranchEntry(i, j, r));
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="pressure"></param>
        /// <returns></returns>
        public NetPQBuffer SetPressure(INode node, double pressure)
        {
            pressures[Id(node)] = pressure;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="flow"></param>
        /// <returns></returns>
        public NetPQBuffer SetFlow(INode node, double flow)
        {
            flows[Id(node)] = flow;
            return this;
        }

        /// <summary>
        /// Builds the buffer. Must be called before <see cref="Run(NetPQ, CancellationToken)"/>.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Solve for pressures.
        /// </summary>
        /// <param name="netPQ"></param>
        /// <param name="token"></param>
        /// <returns></returns>
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
