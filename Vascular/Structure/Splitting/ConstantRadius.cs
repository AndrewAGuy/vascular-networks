using System;
using System.Runtime.Serialization;
using Vascular.Structure.Nodes;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="fracs"></param>
        public void Fractions(BranchNode node, double[] fracs)
        {
            for (var i = 0; i < fracs.Length; ++i)
            {
                fracs[i] = 1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="dfi_dRj"></param>
        public void ReducedResistanceGradient(BranchNode node, double[,] dfi_dRj)
        {
            for (var i = 0; i < node.Children.Length; ++i)
            {
                for (var j = 0; j < node.Children.Length; ++j)
                {
                    dfi_dRj[i, j] = 0;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="dfi_dQj"></param>
        public void FlowGradient(BranchNode node, double[,] dfi_dQj)
        {
            for (var i = 0; i < node.Children.Length; ++i)
            {
                for (var j = 0; j < node.Children.Length; ++j)
                {
                    dfi_dQj[i, j] = 0;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public (double f1, double f2) Fractions(Bifurcation node)
        {
            return (1, 1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public (double df1_dq1, double df1_dq2, double df2_dq1, double df2_dq2) FlowGradient(Bifurcation node)
        {
            return (0, 0, 0, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public (double df1_drs1, double df1_drs2, double df2_drs1, double df2_drs2) ReducedResistanceGradient(Bifurcation node)
        {
            return (0, 0, 0, 0);
        }
    }
}
