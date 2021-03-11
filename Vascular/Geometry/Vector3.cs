using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Vascular.Geometry.Bounds;

namespace Vascular.Geometry
{
    [DataContract]
    public class Vector3 : IEquatable<Vector3>, IComparable<Vector3>, IFormattable, IAxialBoundable
    {
        [DataMember]
        public double x = 0.0, y = 0.0, z = 0.0;

        public Vector3()
        {
        }

        public static readonly Vector3 ZERO = new Vector3();
        public static readonly Vector3 UNIT_X = new Vector3(1, 0, 0);
        public static readonly Vector3 UNIT_Y = new Vector3(0, 1, 0);
        public static readonly Vector3 UNIT_Z = new Vector3(0, 0, 1);

        public Vector3(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3(double xyz) : this(xyz, xyz, xyz)
        {
        }

        public Vector3(Vector3 v) : this(v.x, v.y, v.z)
        {
        }

        public static Vector3 operator -(Vector3 v)
        {
            return new Vector3(-v.x, -v.y, -v.z);
        }

        public static Vector3 operator +(Vector3 l, Vector3 r)
        {
            return new Vector3(l.x + r.x, l.y + r.y, l.z + r.z);
        }

        public static Vector3 operator -(Vector3 l, Vector3 r)
        {
            return new Vector3(l.x - r.x, l.y - r.y, l.z - r.z);
        }

        public static Vector3 operator *(Vector3 l, double r)
        {
            return new Vector3(l.x * r, l.y * r, l.z * r);
        }

        public static Vector3 operator *(double l, Vector3 r)
        {
            return new Vector3(r.x * l, r.y * l, r.z * l);
        }

        public static Vector3 operator /(Vector3 l, double r)
        {
            return new Vector3(l.x / r, l.y / r, l.z / r);
        }

        public static double operator *(Vector3 l, Vector3 r)
        {
            return l.x * r.x + l.y * r.y + l.z * r.z;
        }

        public static Vector3 operator ^(Vector3 l, Vector3 r)
        {
            return new Vector3(
                l.y * r.z - l.z * r.y,
                l.z * r.x - l.x * r.z,
                l.x * r.y - l.y * r.x
                );
        }

        public override string ToString()
        {
            return $"{x} {y} {z}";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y, z);
        }

        public override bool Equals(object obj)
        {
            // As strict as possible, if tolerance is desired then make a comparer class
            return obj is Vector3 v && Equals(v);
        }

        public int CompareTo(Vector3 v)
        {
            // Compare by x, then y, then z
            return x > v.x ? +1 : x < v.x ? -1 : y > v.y ? +1 : y < v.y ? -1 : z > v.z ? +1 : z < v.z ? -1 : 0;
        }

        public bool Equals(Vector3 v)
        {
            return v.x == x && v.y == y && v.z == z;
        }

        private static readonly string[] DEFAULT_FORMAT = new string[] { "", " ", "", null };

        public string ToString(string format, IFormatProvider formatProvider)
        {
            // Format as:
            //      Left enclosing string
            //      Separating string
            //      Right enclosing string
            //      Numeric format string
            // Separated by colon
            var t = format?.Split(':') ?? DEFAULT_FORMAT;
            if (t.Length < 4)
            {
                t = DEFAULT_FORMAT;
            }
            return t[3] == null
                ? $"{t[0]}{x}{t[1]}{y}{t[1]}{z}{t[2]}"
                : $"{t[0]}{x.ToString(t[3], formatProvider)}{t[1]}{y.ToString(t[3], formatProvider)}{t[1]}{z.ToString(t[3], formatProvider)}{t[2]}";
        }

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

        public AxialBounds GetAxialBounds()
        {
            return new AxialBounds(this);
        }

        public (int i, int j, int k) Ceiling => ((int)Math.Ceiling(x), (int)Math.Ceiling(y), (int)Math.Ceiling(z));

        public (int i, int j, int k) Floor => ((int)Math.Floor(x), (int)Math.Floor(y), (int)Math.Floor(z));

        public (int i, int j, int k) Round(MidpointRounding midpointRounding)
        {
            return ((int)Math.Round(x, midpointRounding), (int)Math.Round(y, midpointRounding), (int)Math.Round(z, midpointRounding));
        }

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

        public Vector3 Round(int decimals = 0, MidpointRounding midpointRounding = MidpointRounding.ToPositiveInfinity)
        {
            return new Vector3(Math.Round(x, decimals, midpointRounding), Math.Round(y, decimals, midpointRounding), Math.Round(z, decimals, midpointRounding));
        }

        public double Max => Math.Max(Math.Max(x, y), z);

        public double Min => Math.Min(Math.Min(x, y), z);

        public Vector3 Abs => new Vector3(Math.Abs(x), Math.Abs(y), Math.Abs(z));

        public double Sum => x + y + z;

        public double LengthSquared => this * this;

        public double Length => Math.Sqrt(this.LengthSquared);

        public static double DistanceSquared(Vector3 a, Vector3 b)
        {
            var x = a.x - b.x;
            var y = a.y - b.y;
            var z = a.z - b.z;
            return x * x + y * y + z * z;
        }

        public static double Distance(Vector3 a, Vector3 b)
        {
            return Math.Sqrt(DistanceSquared(a, b));
        }

        public Vector3 Normalize(double t2 = 1.0e-12)
        {
            var m = this.Length;
            return m > t2 ? this / m : throw new PhysicalValueException("Attempting to normalize zero vector.");
        }
    }
}
