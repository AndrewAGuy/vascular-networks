using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Vascular.Intersections.Enforcement
{
    public interface IEnforcer
    {
        bool CullingPermitted { get; set; }
        bool ChangeTopology { get; set; }
        bool ChangeGeometry { get; set; }
        bool PropagateTopology { get; set; }
        bool PropagateGeometry { get; set; }
        
        bool ClearOnSuccess { get; set; }
        bool ThrowIfSourceCulled { get; set; }

        Task<int> Advance(int steps);
        Task Resolve();
        int Iterations { get; }
    }
}
