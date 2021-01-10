using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
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

        sealed public override double Pressure
        {
            get
            {
                return pressure;
            }
        }

        sealed public override double RootRadius
        {
            get
            {
                return Math.Pow(this.ReducedResistance * this.Flow * pressureInverted, 0.25);
            }
        }

        sealed public override double Volume
        {
            get
            {
                return Math.PI * Math.Sqrt(this.ReducedResistance * this.Flow * pressureInverted) * this.EffectiveLength;
            }
        }

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
