using System;
using System.Runtime.Serialization;
using Vascular.Geometry;

namespace Vascular.Structure.Nodes
{
    /// <summary>
    /// A <see cref="Source"/> where pressure drop from source to terminals is specified, and radius varies.
    /// Used in the old implementations of CCO (see Schreiner DOI: 10.1109/10.243413, Karch DOI 10.1016/S0010-4825(98)00045-6)
    /// but engineering approaches may prefer to use <see cref="RadiusSource"/>.
    /// </summary>
    [DataContract]
    public class PressureSource : Source
    {
        [DataMember]
        private double pressure, pressureInverted;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="p"></param>
        public PressureSource(Vector3 x, double p) : base(x)
        {
            SetPressure(p);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        public void SetPressure(double p)
        {
            if (p <= 0.0)
            {
                throw new PhysicalValueException($"Pressure at source must be greater than 0: p = {p}");
            }
            pressure = p;
            pressureInverted = 1.0 / pressure;
        }

#if !NoPressure
        /// <inheritdoc/>
        sealed public override double Pressure => pressure;
#else
        /// <summary>
        /// The source node pressure.
        /// </summary>
        public double Pressure => pressure;
#endif

        /// <inheritdoc/>
        sealed public override double RootRadius =>
            Math.Pow(this.ReducedResistance * this.Network.ScaledViscosity * this.Flow * pressureInverted, 0.25);

#if !NoEffectiveLength
        /// <inheritdoc/>
        sealed public override double Volume => Math.PI * this.EffectiveLength *
            Math.Sqrt(this.ReducedResistance * this.Network.ScaledViscosity * this.Flow * pressureInverted);
#endif

        /// <inheritdoc/>
        public override double Work => pressure * this.Flow;

        /// <inheritdoc/>
        public override double Resistance => pressure / this.Flow;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public RadiusSource ConvertToRadiusSource()
        {
            return new RadiusSource(new Vector3(this.Position), this.RootRadius);
        }

        /// <inheritdoc/>
        public sealed override void SetTargetRadius(double target, double current)
        {
            var ratio = target / current;
            SetPressure(pressure / Math.Pow(ratio, 4));
        }

        /// <inheritdoc/>
        public override Source Clone()
        {
            return new PressureSource(this.Position.Copy(), pressure);
        }
    }
}
