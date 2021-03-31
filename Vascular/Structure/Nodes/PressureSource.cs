using System;
using System.Runtime.Serialization;
using Vascular.Geometry;

namespace Vascular.Structure.Nodes
{
    [DataContract]
    public class PressureSource : Source
    {
        [DataMember]
        private double pressure, pressureInverted;

        public PressureSource(Vector3 x, double p) : base(x)
        {
            SetPressure(p);
        }

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
        sealed public override double Pressure => pressure;
#endif

        sealed public override double RootRadius =>
            Math.Pow(this.ReducedResistance * this.Network.ScaledViscosity * this.Flow * pressureInverted, 0.25);

#if !NoEffectiveLength
        sealed public override double Volume => Math.PI * this.EffectiveLength *
            Math.Sqrt(this.ReducedResistance * this.Network.ScaledViscosity * this.Flow * pressureInverted);
#endif

        public override double Work => pressure * this.Flow;

        public override double Resistance => pressure / this.Flow;

        public RadiusSource ConvertToRadiusSource()
        {
            return new RadiusSource(new Vector3(this.Position), this.RootRadius);
        }

        public sealed override void SetTargetRadius(double target, double current)
        {
            var ratio = target / current;
            SetPressure(pressure / Math.Pow(ratio, 4));
        }
    }
}
