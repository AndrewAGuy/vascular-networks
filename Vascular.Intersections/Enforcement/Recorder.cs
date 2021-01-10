using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Structure;
using Vascular.Structure.Actions;

namespace Vascular.Intersections.Enforcement
{
    public abstract class Recorder<TIntersection, TPenalizing>
    {
        protected double minimumNodePerturbation = 0.0;
        protected double radialCaptureFraction = 1.25;
        protected double aggressionFactor = 1.25;

        public virtual double MinimumNodePerturbation
        {
            get
            {
                return minimumNodePerturbation;
            }
            set
            {
                if (value >= 0)
                {
                    minimumNodePerturbation = value;
                }
            }
        }

        public virtual double RadialCaptureFraction
        {
            get
            {
                return radialCaptureFraction;
            }
            set
            {
                if (value >= 1)
                {
                    radialCaptureFraction = value;
                }
            }
        }

        public virtual double AggressionFactor
        {
            get
            {
                return aggressionFactor;
            }
            set
            {
                if (value >= 1)
                {
                    aggressionFactor = value;
                }
            }
        }

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

        public virtual void Record(IReadOnlyList<TIntersection> ts)
        {
            foreach (var t in ts)
            {
                RecordSingle(t);
            }
            total += ts.Count;
        }
    }
}
