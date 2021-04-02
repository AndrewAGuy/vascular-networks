namespace Vascular.Optimization
{
    /// <summary>
    /// Flow always has the trivial gradient 1, but reduced resistance is more complex.
    /// </summary>
    public class BranchGradients
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="RR"></param>
        /// <param name="RQ"></param>
        public BranchGradients(double RR, double RQ)
        {
            this.dRe_dR = RR;
            this.dRe_dQ = RQ;
        }

#pragma warning disable IDE1006 // Naming Styles
        /// <summary>
        /// 
        /// </summary>
        public double dRe_dR { get; }

        /// <summary>
        /// 
        /// </summary>
        public double dRe_dQ { get; }
#pragma warning restore IDE1006 // Naming Styles
    }
}
