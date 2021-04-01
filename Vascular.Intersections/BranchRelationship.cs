namespace Vascular.Intersections
{
    /// <summary>
    /// 
    /// </summary>
    public enum BranchRelationship
    {
        /// <summary>
        /// No relationship set. Is distinct from <see cref="Disjoint"/>.
        /// </summary>
        None,
        /// <summary>
        /// Branches are in the same network, but not comparable.
        /// </summary>
        Internal,
        /// <summary>
        /// 
        /// </summary>
        Upstream,
        /// <summary>
        /// 
        /// </summary>
        Downstream,
        /// <summary>
        /// 
        /// </summary>
        Parent,
        /// <summary>
        /// 
        /// </summary>
        Child,
        /// <summary>
        /// Branches are not comparable, but have a common parent.
        /// </summary>
        Sibling,
        /// <summary>
        /// Branches are confirmed to have no relation.
        /// </summary>
        Disjoint,
        /// <summary>
        /// Branches are a crossover point from one network to another.
        /// </summary>
        Matched
    }
}
