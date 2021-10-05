using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                if (predicate(f))
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
                this.Recorder.Record(violations);
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
    }
}
