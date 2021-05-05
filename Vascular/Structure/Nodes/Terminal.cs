using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;

namespace Vascular.Structure.Nodes
{
    /// <summary>
    /// The terminal node. Nomenclature comes from medical community, not the engineers who might consider terminals
    /// the points at which we plug it in. For faster calculation, terminals are assumed to be at 0 pressure. This
    /// is fine for engineering uses but may not be suitable for others.
    /// </summary>
    [DataContract]
    public class Terminal : BranchNode
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="Q"></param>
        public Terminal(Vector3 x, double Q)
        {
            SetPosition(x);
            SetFlow(Q);
        }

        [DataMember]
        private Vector3 position = null;
        [DataMember]
        private double flow;

        private static readonly Segment[] CHILDREN = Array.Empty<Segment>();
        private static readonly Branch[] DOWNSTREAM = Array.Empty<Branch>();

        /// <inheritdoc/>
        [DataMember]
        public override Segment Parent { get; set; } = null;

        /// <inheritdoc/>
        public override Segment[] Children => CHILDREN;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        public void SetPosition(Vector3 x)
        {
            position = x ?? throw new PhysicalValueException("Terminal position must not be null");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Q"></param>
        public void SetFlow(double Q)
        {
            flow = Q > 0.0 ? Q : throw new PhysicalValueException($"Terminal flow rate must be positive: Q = {Q}");
        }

        /// <inheritdoc/>
        public override Vector3 Position
        {
            get => position;
            set => throw new GeometryException("Terminal node position is fixed");
        }

#if !NoPressure
        /// <summary>
        /// Always 0.
        /// </summary>
        public override double Pressure => 0.0;

        /// <inheritdoc/>
        public override void CalculatePressures()
        {
            return;
        }
#endif

#if !NoEffectiveLength
        /// <summary>
        /// Always 0.
        /// </summary>
        public override double EffectiveLength => 0.0;
#endif

        /// <summary>
        /// Always 0.
        /// </summary>
        public override double ReducedResistance => 0.0;

#if !NoDepthPathLength
        [DataMember]
        private double pathLength = -1.0;
        [DataMember]
        private int depth = -1;

        /// <inheritdoc/>
        public override double PathLength => pathLength;

        /// <inheritdoc/>
        public override int Depth => depth;

        /// <inheritdoc/>
        public override void CalculatePathLengthsAndDepths()
        {
            depth = this.Upstream.Start.Depth + 1;
            pathLength = this.Upstream.Start.PathLength + this.Upstream.Length;
        }

        /// <inheritdoc/>
        public override void CalculatePathLengthsAndOrder()
        {
            depth = 1;
            pathLength = this.Upstream.Start.PathLength + this.Upstream.Length;
        }
#endif

        /// <summary>
        /// Terminals may matched together, and are granted exceptions to collision resolution.
        /// </summary>
        [DataMember]
        public Terminal[] Partners { get; set; } = null;

        /// <summary>
        /// Legacy flag for tidying up collections.
        /// </summary>
        [DataMember]
        public bool Culled { get; set; } = false;

        /// <summary>
        /// See <see cref="Branch.IsRooted"/>.
        /// </summary>
        public bool IsRooted => this.Upstream?.IsRooted ?? false;

        /// <summary>
        /// Specified at each terminal.
        /// </summary>
        public override double Flow => flow;

        /// <inheritdoc/>
        public override void CalculatePhysical()
        {
            return;
        }

        /// <inheritdoc/>
        public override void PropagateRadiiDownstream()
        {
            return;
        }

        /// <inheritdoc/>
        public override void PropagateRadiiDownstream(double pad)
        {
            return;
        }

        /// <inheritdoc/>
        public override void PropagateRadiiDownstream(Func<Branch, double> postProcessing)
        {
            return;
        }

        /// <inheritdoc/>
        public override AxialBounds GenerateDownstreamBounds()
        {
            return new AxialBounds();
        }

        /// <inheritdoc/>
        public override AxialBounds GenerateDownstreamBounds(double pad)
        {
            return new AxialBounds();
        }

        /// <inheritdoc/>
        public override void SetChildRadii()
        {
            return;
        }

        /// <inheritdoc/>
        public override void PropagateLogicalUpstream()
        {
            this.Upstream.PropagateLogicalUpstream();
        }

        /// <inheritdoc/>
        public override void PropagatePhysicalUpstream()
        {
            this.Upstream.PropagatePhysicalUpstream();
        }

        /// <inheritdoc/>
        public override Branch Upstream => this.Parent?.Branch;

        /// <inheritdoc/>
        public override Branch[] Downstream => DOWNSTREAM;

        /// <summary>
        /// Acts on each <see cref="Terminal"/> downstream of <paramref name="branch"/>. Is recursive.
        /// For stack-based visiting, see <see cref="Network.Terminals"/>.
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="action"></param>
        public static void ForDownstream(Branch branch, Action<Terminal> action)
        {
            if (branch.End is Terminal terminal)
            {
                action(terminal);
            }
            else
            {
                foreach (var child in branch.Children)
                {
                    ForDownstream(child, action);
                }
            }
        }

        /// <summary>
        /// Uses <see cref="ForDownstream(Branch, Action{Terminal})"/> to add to a list.
        /// Allocates <paramref name="capacity"/> beforehand, use <see cref="CountDownstream(Branch)"/>
        /// to avoid reallocation.
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public static List<Terminal> GetDownstream(Branch branch, int capacity = 0)
        {
            var list = capacity > 0 ? new List<Terminal>(capacity) : new List<Terminal>();
            ForDownstream(branch, t => list.Add(t));
            return list;
        }

        /// <summary>
        /// Counts terminals downstream of <paramref name="branch"/>.
        /// </summary>
        /// <param name="branch"></param>
        /// <returns></returns>
        public static int CountDownstream(Branch branch)
        {
            var count = 0;
            ForDownstream(branch, t => ++count);
            return count;
        }
    }
}
