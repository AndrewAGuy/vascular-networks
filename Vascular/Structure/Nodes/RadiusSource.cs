using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
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

        sealed public override double RootRadius
        {
            get
            {
                return radius;
            }
        }

        sealed public override double Volume
        {
            get
            {
                return Math.PI * radius2 * this.EffectiveLength;
            }
        }

        sealed public override double Pressure
        {
            get
            {
                return this.ReducedResistance * this.Flow * radius4inv;
            }
        }

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
