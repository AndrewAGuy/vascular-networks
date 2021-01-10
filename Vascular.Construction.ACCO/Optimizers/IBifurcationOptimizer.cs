using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Optimizers
{
    public interface IBifurcationOptimizer
    {
        void Optimize(Bifurcation b);
    }
}
