using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Geometry.Bounds
{
    [Serializable]
    public class AxialBounds : IAxialBoundable
    {
        public AxialBounds()
        {
            this.Upper = new Vector3(double.NegativeInfinity);
            this.Lower = new Vector3(double.PositiveInfinity);
        }

        public AxialBounds(Vector3 v)
        {
            this.Upper = new Vector3(v);
            this.Lower = new Vector3(v);
        }

        public AxialBounds(Vector3 l, Vector3 u)
        {
            this.Lower = new Vector3(l);
            this.Upper = new Vector3(u);
        }

        public AxialBounds(Vector3 v, double r)
        {
            this.Upper = new Vector3(v.x + r, v.y + r, v.z + r);
            this.Lower = new Vector3(v.x - r, v.y - r, v.z - r);
        }

        public AxialBounds(Vector3 a, Vector3 b, double r)
        {
            this.Upper = new Vector3(Math.Max(a.x, b.x) + r, Math.Max(a.y, b.y) + r, Math.Max(a.z, b.z) + r);
            this.Lower = new Vector3(Math.Min(a.x, b.x) - r, Math.Min(a.y, b.y) - r, Math.Min(a.z, b.z) - r);
        }

        public AxialBounds(AxialBounds ab)
        {
            this.Upper = new Vector3(ab.Upper);
            this.Lower = new Vector3(ab.Lower);
        }

        public AxialBounds Copy()
        {
            return new AxialBounds(this);
        }

        public static AxialBounds Infinite => new AxialBounds(new Vector3(double.NegativeInfinity), new Vector3(double.PositiveInfinity));

        public Vector3 Upper { get; private set; }

        public Vector3 Lower { get; private set; }

        public Vector3 Range => this.Upper - this.Lower;

        public AxialBounds Append(Vector3 v)
        {
            this.Upper.x = Math.Max(this.Upper.x, v.x);
            this.Upper.y = Math.Max(this.Upper.y, v.y);
            this.Upper.z = Math.Max(this.Upper.z, v.z);
            this.Lower.x = Math.Min(this.Lower.x, v.x);
            this.Lower.y = Math.Min(this.Lower.y, v.y);
            this.Lower.z = Math.Min(this.Lower.z, v.z);
            return this;
        }

        public AxialBounds Append(AxialBounds b)
        {
            this.Upper.x = Math.Max(this.Upper.x, b.Upper.x);
            this.Upper.y = Math.Max(this.Upper.y, b.Upper.y);
            this.Upper.z = Math.Max(this.Upper.z, b.Upper.z);
            this.Lower.x = Math.Min(this.Lower.x, b.Lower.x);
            this.Lower.y = Math.Min(this.Lower.y, b.Lower.y);
            this.Lower.z = Math.Min(this.Lower.z, b.Lower.z);
            return this;
        }

        public bool Intersects(Vector3 v)
        {
            return
                v.x <= this.Upper.x &&
                v.y <= this.Upper.y &&
                v.z <= this.Upper.z &&
                v.x >= this.Lower.x &&
                v.y >= this.Lower.y &&
                v.z >= this.Lower.z;
        }

        public bool Intersects(AxialBounds b)
        {
            return
                b.Upper.x >= this.Lower.x &&
                b.Upper.y >= this.Lower.y &&
                b.Upper.z >= this.Lower.z &&
                b.Lower.x <= this.Upper.x &&
                b.Lower.y <= this.Upper.y &&
                b.Lower.z <= this.Upper.z;
        }

        public static AxialBounds operator +(AxialBounds l, AxialBounds r)
        {
            return new AxialBounds(l).Append(r);
        }

        public static AxialBounds operator +(AxialBounds b, Vector3 v)
        {
            return new AxialBounds(b).Append(v);
        }

        public static AxialBounds operator +(Vector3 v, AxialBounds b)
        {
            return new AxialBounds(b).Append(v);
        }

        public AxialBounds Extend(double d)
        {
            this.Upper.x += d;
            this.Upper.y += d;
            this.Upper.z += d;
            this.Lower.x -= d;
            this.Lower.y -= d;
            this.Lower.z -= d;
            return this;
        }

        public AxialBounds Translate(Vector3 t)
        {
            this.Upper += t;
            this.Lower += t;
            return this;
        }

        public AxialBounds GetAxialBounds()
        {
            return this;
        }
    }
}
