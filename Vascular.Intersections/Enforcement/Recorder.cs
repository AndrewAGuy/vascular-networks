using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vascular.Structure;
using Vascular.Structure.Actions;

namespace Vascular.Intersections.Enforcement
{
    public abstract class Recorder<TIntersection, TPenalizing>
    {
        protected abstract void RecordSingle(TIntersection t);
        public virtual void Record(TIntersection t)
        {
            RecordSingle(t);
            total++;
        }
        public abstract void Finish();

        private int total = 0;
        public virtual int Total
        {
            get
            {
                return total;
            }
        }
        public abstract int Count { get; }
        public virtual IEnumerable<GeometryAction> GeometryActions { get; protected set; }
        public virtual IEnumerable<BranchAction> BranchActions { get; protected set; }

        public virtual IEnumerable<TPenalizing> Intersecting
        {
            get
            {
                return intersecting;
            }
        }
        public virtual IEnumerable<TPenalizing> Culling
        {
            get
            {
                return culling;
            }
        }
        protected HashSet<TPenalizing> intersecting = new HashSet<TPenalizing>();
        protected HashSet<TPenalizing> culling = new HashSet<TPenalizing>();

        public virtual void Reset()
        {
            this.GeometryActions = null;
            intersecting = new HashSet<TPenalizing>();
            culling = new HashSet<TPenalizing>();
            total = 0;
        }

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
