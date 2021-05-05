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
            dRe_dR = RR;
            dRe_dQ = RQ;
        }

        /// <summary>
        /// 
        /// </summary>
        public readonly double dRe_dR;

        /// <summary>
        /// 
        /// </summary>
        public readonly double dRe_dQ;
    }
}
