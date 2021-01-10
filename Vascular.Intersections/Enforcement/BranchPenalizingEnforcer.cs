using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Intersections.Enforcement
{
    public abstract class BranchPenalizingEnforcer<TIntersection, TRecorder>
        : Enforcer<TIntersection, Branch, TRecorder>
        where TRecorder : Recorder<TIntersection, Branch>, new()
    {
        public BranchPenalizingEnforcer(Network[] n) : base(n)
        {

        }

        protected override void AddToCull(ICollection<Terminal> toCull, Branch branch)
        {
            if (branch.Start is Source)
            {
                if (this.ThrowIfSourceCulled)
                {
                    throw new TopologyException("Root branch has been requested for culling");
                }
                return;
            }
            Terminal.ForDownstream(branch, term =>
            {
                if (term.Partners != null)
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
    }
}
