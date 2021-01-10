using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;

namespace Vascular.Intersections.Enforcement
{
    internal class SingleEntry
    {
        public SingleEntry(Vector3 _v)
        {
            v = _v;
            n = 1;
        }

        private Vector3 v;
        private int n;

        public void Add(Vector3 _v)
        {
            v += _v;
            n++;
        }

        public Vector3 Value
        {
            get
            {
                return v;
            }
        }

        public Vector3 Mean
        {
            get
            {
                return v / n;
            }
        }
    }
}
