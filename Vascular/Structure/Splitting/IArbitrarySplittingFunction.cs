namespace Vascular.Structure.Splitting;

/// <summary>
/// Represents the splitting rule at arbitrary degree nodes.
/// In this framework, the nodes (or gradient caches) are responsible for preallocating the target arrays.
/// </summary>
public interface IArbitrarySplittingFunction
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="node"></param>
    /// <param name="fracs"></param>
    public void Fractions(BranchNode node, double[] fracs);

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
}