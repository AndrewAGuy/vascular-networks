using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;
using Vascular.Structure.Diagnostics;
using Vascular.Structure.Nodes;

namespace Vascular.Intersections.Implicit
{
    using ImplicitFunction = Func<Vector3, (double f, Vector3 g)>;

    /// <summary>
    /// 
    /// </summary>
    public class ImplicitEnforcer : Enforcer<ImplicitViolation, INode, ImplicitRecorder>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="f"></param>
        public ImplicitEnforcer(Network[] n, IEnumerable<ImplicitFunction> f) : base(n)
        {
            this.Functions.AddRange(f);
        }

        /// <summary>
        /// 
        /// </summary>
        public List<ImplicitFunction> Functions { get; } = new();

        /// <summary>
        /// Modified version of <see cref="BranchEnforcer{TIntersection, TRecorder}.AddToCull(ICollection{Terminal}, Branch)"/>.
        /// </summary>
        /// <param name="toCull"></param>
        /// <param name="obj"></param>
        protected override void AddToCull(ICollection<Terminal> toCull, INode obj)
        {
            if (obj is Source)
            {
                if (this.ThrowIfSourceCulled)
                {
                    throw new TopologyException("Root branch has been requested for culling");
                }
                return;
            }
            Terminal.ForDownstream(obj.Parent.Branch, term =>
            {
                if (term.Partners != null && this.CullMatched)
                {
                    foreach (var t in term.Partners)
                    {
                        toCull.Add(t);
                    }
                }
                else
                {
                    toCull.Add(term);
                }
            });
        }

        private Func<double, bool> predicate = f => f >= 0;

        /// <summary>
        /// Allows a threshold per-node, in case radius needs to be taken into account.
        /// Should return negative values to cause movement when node position permissible.
        /// </summary>
        public Func<INode, double> Threshold { get; set; } = n => 0;

        /// <summary>
        /// If set to true, does not class f(x) = 0 as being a violation.
        /// </summary>
        public bool AllowMarginal
        {
            set
            {
                predicate = value
                    ? f => f > 0
                    : f => f >= 0;
            }
        }

        private async Task Evaluate(ImplicitFunction function, Network network)
        {
            var enumerator = new BranchEnumerator();
            var violations = new List<ImplicitViolation>(enumerator.CountNodes(network.Root));
            foreach (var node in enumerator.Nodes(network.Root))
            {
                var (f, g) = function(node.Position);
                var t = this.Threshold(node);
                if (predicate(f - t))
                {
                    violations.Add(new()
                    {
                        Node = node,
                        Value = f,
                        Gradient = g
                    });
                }
            }

            await recorderSemaphore.WaitAsync();
            try
            {
                this.Recorder.Record(violations, function);
            }
            finally
            {
                recorderSemaphore.Release();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override async Task Detect()
        {
            await this.Functions.SelectMany(f => networks, (f, n) => (f, n))
                .RunAsync(async o => await Evaluate(o.f, o.n));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="func"></param>
        /// <param name="dx"></param>
        /// <returns></returns>
        public static ImplicitFunction ForwardDifference(Func<Vector3, double> func, double dx)
        {
            return x =>
            {
                var f = func(x);
                var fx = func(x + new Vector3(dx, 0, 0));
                var fy = func(x + new Vector3(0, dx, 0));
                var fz = func(x + new Vector3(0, 0, dx));
                var d = 1.0 / dx;
                return (f, new(fx * d, fy * d, fz * d));
            };
        }
    }
}
