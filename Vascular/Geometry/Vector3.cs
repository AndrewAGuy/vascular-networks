using System;
using System.Linq;
using System.Runtime.Serialization;
using Vascular.Geometry.Bounds;

namespace Vascular.Geometry
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class Vector3 : IEquatable<Vector3>, IComparable<Vector3>, IFormattable, IAxialBoundable
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public double x = 0.0, y = 0.0, z = 0.0;

        /// <summary>
        /// Initializes the zero vector. See <see cref="ZERO"/>.
        /// </summary>
        public Vector3()
        {
        }

        /// <summary>
        /// A static all-zero vector. Please do not modify.
        /// </summary>
        public static readonly Vector3 ZERO = new Vector3();

        /// <summary>
        /// A static unit vector. Please do not modify.
        /// </summary>
        public static readonly Vector3 UNIT_X = new Vector3(1, 0, 0);

        /// <summary>
        /// A static unit vector. Please do not modify.
        /// </summary>
        public static readonly Vector3 UNIT_Y = new Vector3(0, 1, 0);

        /// <summary>
        /// A static unit vector. Please do not modify.
        /// </summary>
        public static readonly Vector3 UNIT_Z = new Vector3(0, 0, 1);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Vector3(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        /// <summary>
        /// Sets all elements to <paramref name="xyz"/>.
        /// </summary>
        /// <param name="xyz"></param>
        public Vector3(double xyz) : this(xyz, xyz, xyz)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        public Vector3(Vector3 v) : this(v.x, v.y, v.z)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3 operator -(Vector3 v)
        {
            return new Vector3(-v.x, -v.y, -v.z);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Vector3 operator +(Vector3 l, Vector3 r)
        {
            return new Vector3(l.x + r.x, l.y + r.y, l.z + r.z);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Vector3 operator -(Vector3 l, Vector3 r)
        {
            return new Vector3(l.x - r.x, l.y - r.y, l.z - r.z);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Vector3 operator *(Vector3 l, double r)
        {
            return new Vector3(l.x * r, l.y * r, l.z * r);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Vector3 operator *(double l, Vector3 r)
        {
            return new Vector3(r.x * l, r.y * l, r.z * l);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Vector3 operator /(Vector3 l, double r)
        {
            return new Vector3(l.x / r, l.y / r, l.z / r);
        }

        /// <summary>
        /// Dot product.
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static double operator *(Vector3 l, Vector3 r)
        {
            return l.x * r.x + l.y * r.y + l.z * r.z;
        }

        /// <summary>
        /// Cross product.
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Vector3 operator ^(Vector3 l, Vector3 r)
        {
            return new Vector3(
                l.y * r.z - l.z * r.y,
                l.z * r.x - l.x * r.z,
                l.x * r.y - l.y * r.x
                );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{x} {y} {z}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(x, y, z);
        }

        /// <summary>
        /// As strict as possible, if tolerance is desired then make a comparer class.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is Vector3 v && Equals(v);
        }

        /// <summary>
        /// Compares by x, then y, then z. Will be weird with <see cref="double.NaN"/> due to comparison rules.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public int CompareTo(Vector3 v)
        {
            return x > v.x ? +1 : x < v.x ? -1 : y > v.y ? +1 : y < v.y ? -1 : z > v.z ? +1 : z < v.z ? -1 : 0;
        }

        /// <summary>
        /// Strict equality check. Will be weird with <see cref="double.NaN"/> due to comparison rules.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool Equals(Vector3 v)
        {
            return v.x == x && v.y == y && v.z == z;
        }

        private static readonly string[] DEFAULT_FORMAT = new string[] { "", " ", "", null };

        /// <summary>
        /// See <paramref name="format"/> for format string specification.
        /// </summary>
        /// <param name="format">
        /// Format string separated by colons as:
        /// <list type="number">
        ///     <item> <description> Left enclosing string </description> </item>
        ///     <item> <description> Separating string </description> </item>
        ///     <item> <description> Right enclosing string </description> </item>
        ///     <item> <description> Numeric format string </description> </item>
        /// </list>
        /// </param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            var t = format?.Split(':') ?? DEFAULT_FORMAT;
            if (t.Length < 4)
            {
                t = DEFAULT_FORMAT;
            }
            return t[3] == null
                ? $"{t[0]}{x}{t[1]}{y}{t[1]}{z}{t[2]}"
                : $"{t[0]}{x.ToString(t[3], formatProvider)}{t[1]}{y.ToString(t[3], formatProvider)}{t[1]}{z.ToString(t[3], formatProvider)}{t[2]}";
        }

        /// <summary>
        /// Splits <paramref name="s"/> by <paramref name="c"/>, then tries to parse the non-empty strings to <paramref name="v"/>.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="v"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool TryParse(string s, out Vector3 v, char c = ' ')
        {
            var t = s.Split(c).Where(w => !string.IsNullOrWhiteSpace(w)).ToList();
            if (t.Count != 3 ||
                !double.TryParse(t[0], out var x) ||
                !double.TryParse(t[1], out var y) ||
                !double.TryParse(t[2], out var z))
            {
                v = null;
                return false;
            }
            v = new Vector3(x, y, z);
            return true;
        }

        /// <summary>
        /// An <see cref="AxialBounds"/> with <see cref="AxialBounds.Upper"/> = <see cref="AxialBounds.Lower"/> = this.
        /// </summary>
        /// <returns></returns>
        public AxialBounds GetAxialBounds()
        {
            return new AxialBounds(this);
        }

        /// <summary>
        /// 
        /// </summary>
        public (int i, int j, int k) Ceiling => ((int)Math.Ceiling(x), (int)Math.Ceiling(y), (int)Math.Ceiling(z));

        /// <summary>
        /// 
        /// </summary>
        public (int i, int j, int k) Floor => ((int)Math.Floor(x), (int)Math.Floor(y), (int)Math.Floor(z));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="midpointRounding"></param>
        /// <returns></returns>
        public (int i, int j, int k) Round(MidpointRounding midpointRounding)
        {
            return ((int)Math.Round(x, midpointRounding), (int)Math.Round(y, midpointRounding), (int)Math.Round(z, midpointRounding));
        }

        /// <summary>
        /// A legacy from before <see cref="MidpointRounding.ToPositiveInfinity"/> was added.
        /// </summary>
        /// <returns></returns>
        public Vector3 NearestIntegral()
        {
            var f = Math.Floor(x);
            var X = x - f >= 0.5 ? f + 1.0 : f;
            f = Math.Floor(y);
            var Y = y - f >= 0.5 ? f + 1.0 : f;
            f = Math.Floor(z);
            var Z = z - f >= 0.5 ? f + 1.0 : f;
            return new Vector3(X, Y, Z);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="decimals"></param>
        /// <param name="midpointRounding"></param>
        /// <returns></returns>
        public Vector3 Round(int decimals = 0, MidpointRounding midpointRounding = MidpointRounding.ToPositiveInfinity)
        {
            return new Vector3(Math.Round(x, decimals, midpointRounding), Math.Round(y, decimals, midpointRounding), Math.Round(z, decimals, midpointRounding));
        }

        /// <summary>
        /// 
        /// </summary>
        public double Max => Math.Max(Math.Max(x, y), z);

        /// <summary>
        /// 
        /// </summary>
        public double Min => Math.Min(Math.Min(x, y), z);

        /// <summary>
        /// The vector of element-wise absolutes.
        /// </summary>
        public Vector3 Abs => new Vector3(Math.Abs(x), Math.Abs(y), Math.Abs(z));

        /// <summary>
        /// Sum of elements.
        /// </summary>
        public double Sum => x + y + z;

        /// <summary>
        /// L2 norm.
        /// </summary>
        public double LengthSquared => this * this;

        /// <summary>
        /// L2 norm.
        /// </summary>
        public double Length => Math.Sqrt(this.LengthSquared);

        /// <summary>
        /// L2 norm.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double DistanceSquared(Vector3 a, Vector3 b)
        {
            var x = a.x - b.x;
            var y = a.y - b.y;
            var z = a.z - b.z;
            return x * x + y * y + z * z;
        }

        /// <summary>
        /// L2 norm.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double Distance(Vector3 a, Vector3 b)
        {
            return Math.Sqrt(DistanceSquared(a, b));
        }

        /// <summary>
        /// L2 norm.
        /// </summary>
        /// <param name="t">Tolerance before vector considered zero.</param>
        /// <returns>The unit vector.</returns>
        public Vector3 Normalize(double t = 1.0e-12)
        {
            var m = this.Length;
            return m > t ? this / m : throw new PhysicalValueException("Attempting to normalize zero vector.");
        }

        /// <summary>
        /// L2 norm. Same as <see cref="Normalize(double)"/> but returns null rather than throwing.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Vector3 NormalizeSafe(double t = 1.0e-12)
        {
            var m = this.Length;
            return m > t ? this / m : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static Vector3 FromArray(double[] array)
        {
            return array.Length != 3
                ? throw new ArgumentException($"Cannot create Vector3 from array with {array.Length} elements.")
                : new Vector3(array[0], array[1], array[2]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double[] ToArray()
        {
            return new double[3] { x, y, z };
        }

        /// <summary>
        /// Tests if any component is <see cref="double.NaN"/>.
        /// </summary>
        public bool IsNaN => double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(z);
    }
}
