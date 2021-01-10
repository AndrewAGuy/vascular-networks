using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Actions;

namespace Vascular.Intersections.Enforcement
{
    public abstract class SegmentRecorder<TPenalizing> : Recorder<SegmentIntersection, TPenalizing>
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
    }
}
