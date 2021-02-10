using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vascular.Optimization
{
    public class BranchGradients
    {
        public BranchGradients(double RR, double RQ)
        {
            this.dRe_dR = RR;
            this.dRe_dQ = RQ;
        }

#pragma warning disable IDE1006 // Naming Styles
        public double dRe_dR { get; }
        public double dRe_dQ { get; }
#pragma warning restore IDE1006 // Naming Styles
    }
}
