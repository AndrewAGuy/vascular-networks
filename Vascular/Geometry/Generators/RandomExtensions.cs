using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Geometry.Generators
{
    public static class RandomExtensions
    {
        public static Random NextRandom(this Random r)
        {
            return new Random(r.Next());
        }
    }
}
