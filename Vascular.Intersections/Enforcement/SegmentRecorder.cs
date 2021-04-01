namespace Vascular.Intersections.Enforcement
{
    /// <summary>
    /// The base type for segment based intersection recording.
    /// </summary>
    /// <typeparam name="TPenalizing"></typeparam>
    public abstract class SegmentRecorder<TPenalizing> : Recorder<SegmentIntersection, TPenalizing>
    {
        /// <summary>
        /// 
        /// </summary>
        protected double minimumNodePerturbation = 0.0;

        /// <summary>
        /// 
        /// </summary>
        protected double radialCaptureFraction = 1.25;

        /// <summary>
        /// 
        /// </summary>
        protected double aggressionFactor = 1.25;

        /// <summary>
        /// The minimum distance a node must be moved. Prevents long sequences of tiny movements or requests that when added to positions fall below precision.
        /// </summary>
        public virtual double MinimumNodePerturbation
        {
            get => minimumNodePerturbation;
            set
            {
                if (value >= 0)
                {
                    minimumNodePerturbation = value;
                }
            }
        }

        /// <summary>
        /// How many multiples of the radius along the branch do we go before inserting transients rather than moving nodes.
        /// </summary>
        public virtual double RadialCaptureFraction
        {
            get => radialCaptureFraction;
            set
            {
                if (value >= 1)
                {
                    radialCaptureFraction = value;
                }
            }
        }

        /// <summary>
        /// Safety tolerance - move further than the actual overlap in case the vessels expand a bit.
        /// </summary>
        public virtual double AggressionFactor
        {
            get => aggressionFactor;
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
