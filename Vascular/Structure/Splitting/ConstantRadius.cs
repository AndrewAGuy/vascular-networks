using System;
using System.Runtime.Serialization;

namespace Vascular.Structure.Splitting
{
    /// <summary>
    /// A constant radius splitting law, for systems made of one type of pipe. Will cause a deviation from expected flow rates.
    /// </summary>
    [DataContract]
    public class ConstantRadius : ISplittingFunction
    {        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rs1"></param>
        /// <param name="q1"></param>
        /// <param name="rs2"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public (double f1, double f2) Fractions(double rs1, double q1, double rs2, double q2)
        {           
            return (1, 1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rs1"></param>
        /// <param name="q1"></param>
        /// <param name="rs2"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public (double df1_dq1, double df2_dq1) FlowGradient(double rs1, double q1, double rs2, double q2)
        {
            return (0, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rs1"></param>
        /// <param name="q1"></param>
        /// <param name="rs2"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public (double df1_drs1, double df2_drs1) ReducedResistanceGradient(double rs1, double q1, double rs2, double q2)
        {
            return (0, 0);
        }
    }
}
