using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;

namespace Vascular.Construction.LSC.Defaults
{
    public class ExteriorLimiter
    {
        public Action AfterBuild { get; }

        public Action OnEntry { get; }

        public ExteriorLimiter(LatticeState latticeState, int limit)
        {
            var visited = new Dictionary<Vector3, int>();
            this.OnEntry = () => visited.Clear();
            this.AfterBuild = () =>
            {
                foreach (var v in latticeState.Exterior.Keys)
                {
                    visited.AddOrUpdate(v, 1, i => i + 1);
                }
                foreach (var kv in visited)
                {
                    if (kv.Value >= limit)
                    {
                        latticeState.Exterior.Remove(kv.Key);
                    }
                }
            };
        }
    }
}
