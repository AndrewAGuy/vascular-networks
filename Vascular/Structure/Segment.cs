using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;

namespace Vascular.Structure
{
    [Serializable]
    public class Segment : IAxialBoundable
    {
        private double radius = 0.0;

        public Segment()
        {

        }

        public Segment(INode start, INode end)
        {
            this.Start = start;
            this.End = end;
        }

        public Branch Branch { get; set; } = null;

        public INode Start { get; set; }

        public INode End { get; set; }

        public double Length { get; private set; } = 0.0;

        public AxialBounds Bounds { get; private set; } = new AxialBounds();

        public double Radius
        {
            get
            {
                return radius;
            }
            set
            {
                if (value >= 0.0)
                {
                    radius = value;
                }
            }
        }

        public void UpdateRadius()
        {
            radius = this.Branch.Radius;
        }

        public double Flow
        {
            get
            {
                return this.Branch.Flow;
            }
        }

        public void UpdateLength()
        {
            this.Length = Vector3.Distance(this.Start.Position, this.End.Position);
        }

        public AxialBounds GenerateBounds()
        {
            return this.Bounds = new AxialBounds(this);
        }

        public AxialBounds GenerateBounds(double pad)
        {
            return this.Bounds = new AxialBounds(this).Extend(pad);
        }

        public double Slenderness
        {
            get
            {
                return this.Length / this.Radius;
            }
        }

        public Vector3 AtFraction(double f)
        {
            return ((1 - f) * this.Start.Position) + (f * this.End.Position);
        }

        public Vector3 Direction
        {
            get
            {
                return this.End.Position - this.Start.Position;
            }
        }

        public double DistanceToSurface(Vector3 v)
        {
            var bS = this.Start.Position;
            var bE = this.End.Position;
            var dir = bE - bS;
            // Get projection of v onto branch line
            var f = LinearAlgebra.LineFactor(bS, dir, v);
            if (f >= 1.0)
            {
                // Outside far end
                return Vector3.Distance(v, bE) - radius;
            }
            else if (f <= 0.0)
            {
                // Outside back end
                return Vector3.Distance(v, bS) - radius;
            }
            else
            {
                // Test cylindrically
                return Vector3.Distance(v, bS + (f * dir)) - radius;
            }
        }

        public AxialBounds GetAxialBounds()
        {
            return this.Bounds;
        }
    }
}
