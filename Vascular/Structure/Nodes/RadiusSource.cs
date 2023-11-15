using System;
using Vascular.Geometry;

namespace Vascular.Structure.Nodes
{
    /// <summary>
    /// A fixed radius source, useful for engineering approaches where inlet connections matter.
    /// </summary>
    public class RadiusSource : Source
    {
        private double radius, radius2, radius4inv;

        /// <summary>
        ///
        /// </summary>
        /// <param name="x"></param>
        /// <param name="r"></param>
        public RadiusSource(Vector3 x, double r) : base(x)
        {
            SetRadius(r);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="r"></param>
        public void SetRadius(double r)
        {
            if (r <= 0.0)
            {
                throw new PhysicalValueException($"Source radius must be greater than 0: r = {r}");
            }
            radius = r;
            radius2 = r * r;
            radius4inv = 1.0 / (radius2 * radius2);
        }

        /// <inheritdoc/>
        sealed public override double RootRadius => radius;

#if !NoEffectiveLength
        /// <inheritdoc/>
        sealed public override double Volume => Math.PI * radius2 * this.EffectiveLength;
#endif

        /// <inheritdoc/>
        public sealed override double Resistance => this.ReducedResistance * this.Network.ScaledViscosity * radius4inv;

        /// <inheritdoc/>
        public override double Work => this.Resistance * Math.Pow(this.Flow, 2);

#if !NoPressure
        /// <inheritdoc/>
        sealed public override double Pressure => this.Resistance * this.Flow;
#else
        private double Pressure => this.Resistance * this.Flow;
#endif

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public PressureSource ConvertToPressureSource()
        {
            return new PressureSource(new Vector3(this.Position), this.Pressure);
        }

        /// <inheritdoc/>
        public sealed override void SetTargetRadius(double target, double current)
        {
            var ratio = target / current;
            SetRadius(radius * ratio);
        }

        /// <inheritdoc/>
        public override Source Clone()
        {
            return new RadiusSource(this.Position.Copy(), radius);
        }
    }
}
