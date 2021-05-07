namespace Vascular.Structure.Splitting
{
    /// <summary>
    /// Represents the splitting rule at bifurcations.
    /// </summary>
    public interface ISplittingFunction
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rs1"></param>
        /// <param name="q1"></param>
        /// <param name="rs2"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public (double f1, double f2) Fractions(double rs1, double q1, double rs2, double q2);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rs1"></param>
        /// <param name="q1"></param>
        /// <param name="rs2"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public (double df1_dq1, double df2_dq1) FlowGradient(double rs1, double q1, double rs2, double q2);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rs1"></param>
        /// <param name="q1"></param>
        /// <param name="rs2"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public (double df1_drs1, double df2_drs1) ReducedResistanceGradient(double rs1, double q1, double rs2, double q2);
    }
}
