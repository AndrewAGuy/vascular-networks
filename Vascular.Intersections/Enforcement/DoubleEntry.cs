using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;

namespace Vascular.Intersections.Enforcement
{
    internal class DoubleEntry
    {
        public DoubleEntry(Vector3 _v, Vector3 _d)
        {
            v = _v;
            d = _d;
            n = 1;
        }

        private Vector3 v;
        private Vector3 d;
        private int n;

        public void Add(Vector3 _v, Vector3 _d)
        {
            v += _v;
            d += _d;
            n++;
        }

        public Vector3 Direction
        {
            get
            {
                return d;
            }
        }

        public Vector3 Mean
        {
            get
            {
                return (v + d) / n;
            }
        }
    }
}
