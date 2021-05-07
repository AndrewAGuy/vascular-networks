using System.Collections.Generic;
using System.Linq;
using Vascular.Structure.Actions;

namespace Vascular.Intersections.Enforcement
{
    /// <summary>
    /// The base type for recording intersections and penalizing something.
    /// </summary>
    /// <typeparam name="TIntersection"></typeparam>
    /// <typeparam name="TPenalizing"></typeparam>
    public abstract class Recorder<TIntersection, TPenalizing>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        protected abstract void RecordSingle(TIntersection t);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        public virtual void Record(TIntersection t)
        {
            RecordSingle(t);
            total++;
        }

        /// <summary>
        /// 
        /// </summary>
        public abstract void Finish();

        private int total = 0;

        /// <summary>
        /// The number of intersections recorded.
        /// </summary>
        public virtual int Total => total;

        /// <summary>
        /// Not necessarily the number of unique objects, but if 0, definitely nothing detected.
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// Trying to resolve things with moving only.
        /// </summary>
        public virtual IEnumerable<GeometryAction> GeometryActions { get; protected set; }

        /// <summary>
        /// Trying to resolve things with topological changes such as end swaps and bifurcation moves.
        /// </summary>
        public virtual IEnumerable<BranchAction> BranchActions { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual IEnumerable<TPenalizing> Intersecting => intersecting;

        /// <summary>
        /// These have been flagged as hard or not worthwhile to resolve, so remove immediately.
        /// </summary>
        public virtual IEnumerable<TPenalizing> Culling => culling;

        /// <summary>
        /// 
        /// </summary>
        protected HashSet<TPenalizing> intersecting = new HashSet<TPenalizing>();

        /// <summary>
        /// 
        /// </summary>
        protected HashSet<TPenalizing> culling = new HashSet<TPenalizing>();

        /// <summary>
        /// 
        /// </summary>
        public virtual void Reset()
        {
            this.GeometryActions = null;
            intersecting = new HashSet<TPenalizing>();
            culling = new HashSet<TPenalizing>();
            total = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ts"></param>
        public virtual void Record(IEnumerable<TIntersection> ts)
        {
            foreach (var t in ts)
            {
                RecordSingle(t);
            }
            total += ts.Count();
        }
    }
}
