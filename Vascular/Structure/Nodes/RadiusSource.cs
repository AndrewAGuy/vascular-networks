using System;
using System.Runtime.Serialization;
using Vascular.Geometry;

namespace Vascular.Structure.Nodes
{
    [DataContract]
    public class RadiusSource : Source
    {
        [DataMember]
        private double radius, radius2, radius4inv;

        public RadiusSource(Vector3 x, double r) : base(x)
        {
            SetRadius(r);
        }

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

        sealed public override double RootRadius => radius;

#if !NoEffectiveLength
        sealed public override double Volume => Math.PI * radius2 * this.EffectiveLength;
#endif

        public sealed override double Resistance => this.ReducedResistance * this.Network.ScaledViscosity * radius4inv;

        public override double Work => this.Resistance * Math.Pow(this.Flow, 2);

#if !NoPressure
        sealed public override double Pressure => this.Resistance * this.Flow;
#else
        private double Pressure => this.Resistance * this.Flow;
#endif

        public PressureSource ConvertToPressureSource()
        {
            return new PressureSource(new Vector3(this.Position), this.Pressure);
        }

        public sealed override void SetTargetRadius(double target, double current)
        {
            var ratio = target / current;
            SetRadius(radius * ratio);
        }
    }
}
