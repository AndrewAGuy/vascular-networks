using System;
using System.Runtime.Serialization;
using Vascular.Structure;

namespace Vascular.Geometry.Bounds
{
    /// <summary>
    /// Represents an axially aligned bounding box.
    /// </summary>
    public class AxialBounds : IAxialBoundable
    {
        /// <summary>
        /// The empty bounds.
        /// </summary>
        public AxialBounds()
        {
            this.Upper = new Vector3(double.NegativeInfinity);
            this.Lower = new Vector3(double.PositiveInfinity);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="v"></param>
        public AxialBounds(Vector3 v)
        {
            this.Upper = new Vector3(v);
            this.Lower = new Vector3(v);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="l"></param>
        /// <param name="u"></param>
        public AxialBounds(Vector3 l, Vector3 u)
        {
            this.Lower = new Vector3(l);
            this.Upper = new Vector3(u);
        }

        /// <summary>
        /// Point plus padding.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="r"></param>
        public AxialBounds(Vector3 v, double r)
        {
            this.Upper = new Vector3(v.x + r, v.y + r, v.z + r);
            this.Lower = new Vector3(v.x - r, v.y - r, v.z - r);
        }

        /// <summary>
        /// Overload useful for segments.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="r"></param>
        public AxialBounds(Vector3 a, Vector3 b, double r)
        {
            this.Upper = new Vector3(Math.Max(a.x, b.x) + r, Math.Max(a.y, b.y) + r, Math.Max(a.z, b.z) + r);
            this.Lower = new Vector3(Math.Min(a.x, b.x) - r, Math.Min(a.y, b.y) - r, Math.Min(a.z, b.z) - r);
        }

        /// <summary>
        /// Copy.
        /// </summary>
        /// <param name="ab"></param>
        public AxialBounds(AxialBounds ab)
        {
            this.Upper = new Vector3(ab.Upper);
            this.Lower = new Vector3(ab.Lower);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="s"></param>
        public AxialBounds(Segment s) : this(s.Start.Position, s.End.Position, s.Radius)
        {

        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public AxialBounds Copy()
        {
            return new AxialBounds(this);
        }

        /// <summary>
        ///
        /// </summary>
        public static AxialBounds Infinite => new(new Vector3(double.NegativeInfinity), new Vector3(double.PositiveInfinity));

        /// <summary>
        ///
        /// </summary>
        public Vector3 Upper { get; private set; }

        /// <summary>
        ///
        /// </summary>
        public Vector3 Lower { get; private set; }

        /// <summary>
        ///
        /// </summary>
        public Vector3 Range => this.Upper - this.Lower;

        /// <summary>
        /// Modifies the bounds to also contain <paramref name="v"/> if required, then returns the same object.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Modifies the bounds to also contain <paramref name="b"/> if required, then returns the same object.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Strict inequality at the upper end, allows tiling by bounds that assigns each point
        /// to only one set.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool IntersectsOpenUpper(Vector3 v)
        {
            return
               v.x < this.Upper.x &&
               v.y < this.Upper.y &&
               v.z < this.Upper.z &&
               v.x >= this.Lower.x &&
               v.y >= this.Lower.y &&
               v.z >= this.Lower.z;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static AxialBounds operator +(AxialBounds l, AxialBounds r)
        {
            return new AxialBounds(l).Append(r);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="b"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static AxialBounds operator +(AxialBounds b, Vector3 v)
        {
            return new AxialBounds(b).Append(v);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="v"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static AxialBounds operator +(Vector3 v, AxialBounds b)
        {
            return new AxialBounds(b).Append(v);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public AxialBounds Translate(Vector3 t)
        {
            this.Upper += t;
            this.Lower += t;
            return this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public AxialBounds GetAxialBounds()
        {
            return this;
        }

        /// <summary>
        /// Returns intersection between bounds.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public AxialBounds Trim(AxialBounds other)
        {
            this.Lower.x = Math.Max(this.Lower.x, other.Lower.x);
            this.Lower.y = Math.Max(this.Lower.y, other.Lower.y);
            this.Lower.z = Math.Max(this.Lower.z, other.Lower.z);
            this.Upper.x = Math.Min(this.Upper.x, other.Upper.x);
            this.Upper.y = Math.Min(this.Upper.y, other.Upper.y);
            this.Upper.z = Math.Min(this.Upper.z, other.Upper.z);
            return this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Contains(AxialBounds other)
        {
            return this.Upper.x >= other.Upper.x
                && this.Upper.y >= other.Upper.y
                && this.Upper.z >= other.Upper.z
                && this.Lower.x <= other.Lower.x
                && this.Lower.y <= other.Lower.y
                && this.Lower.z <= other.Lower.z;
        }
    }
}
