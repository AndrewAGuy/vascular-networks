using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.LSC.Defaults
{
    using MultipleMapEntry = KeyValuePair<Vector3, ICollection<Terminal>>;

    public static class RandomOrdering
    {
        public static ExteriorOrderingGenerator PermuteExterior(Random random)
        {
            return E =>
            {
                var kv = E.ToArray();
                kv.Permute(random);
                return kv;
            };
        }

        public static InitialTerminalOrderingGenerator PermuteInitial(Random random)
        {
            return V =>
            {
                var kv = V.ToArray();
                kv.Permute(random);
                return kv;
            };
        }
    }
}
