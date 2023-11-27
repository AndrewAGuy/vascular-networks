using System;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Splitting;

/// <summary>
/// Represents the splitting rule at nodes.
/// In the higher degree framework, the nodes (or gradient caches) are responsible for preallocating the target arrays.
/// Special mode for bifurcations as we expect these more than any other type, and legacy mode is preserved for action
/// estimates (where nothing could be overriding the fractions and no other context may be used to determine fractions).
/// </summary>
public interface ISplittingFunction
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <param name="fracs"></param>
    public void Fractions(BranchNode node, double[] fracs);

    public void Fractions(ReadOnlySpan<double> R, ReadOnlySpan<double> Q, Span<double> f);

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public (double f1, double f2) Fractions(Bifurcation node);

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
    /// <param name="node"></param>
    /// <param name="dfi_dRj"></param>
    public void ReducedResistanceGradient(BranchNode node, double[,] dfi_dRj);

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <param name="dfi_dQj"></param>
    public void FlowGradient(BranchNode node, double[,] dfi_dQj);

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <param name="dfi_dRj"></param>
    /// <param name="dfi_dQj"></param>
    public void Gradient(BranchNode node, double[,] dfi_dRj, double[,] dfi_dQj);

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public (double df1_dq1, double df1_dq2, double df2_dq1, double df2_dq2) FlowGradient(Bifurcation node);

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public (double df1_drs1, double df1_drs2, double df2_drs1, double df2_drs2) ReducedResistanceGradient(Bifurcation node);



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
