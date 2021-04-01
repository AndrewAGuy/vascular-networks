using System.Collections.Generic;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Intersections.Enforcement
{
    /// <summary>
    /// Base type for enforcers that remove branches and all downstream.
    /// </summary>
    /// <typeparam name="TIntersection"></typeparam>
    /// <typeparam name="TRecorder"></typeparam>
    public abstract class BranchEnforcer<TIntersection, TRecorder>
        : Enforcer<TIntersection, Branch, TRecorder>
        where TRecorder : Recorder<TIntersection, Branch>, new()
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        public BranchEnforcer(Network[] n) : base(n)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toCull"></param>
        /// <param name="branch"></param>
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
    }
}
