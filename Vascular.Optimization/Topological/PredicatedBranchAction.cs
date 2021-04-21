﻿using System;
using Vascular.Structure.Actions;

namespace Vascular.Optimization.Topological
{
    /// <summary>
    /// Modifies a <see cref="BranchAction"/> to only execute according to a predicate.
    /// </summary>
    public class PredicatedBranchAction : BranchAction
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <param name="p"></param>
        public PredicatedBranchAction(BranchAction i, Predicate<BranchAction> p) : base(i.A, i.B)
        {
            inner = i;
            predicate = p;
        }
        private readonly BranchAction inner;
        private readonly Predicate<BranchAction> predicate;

        /// <inheritdoc/>
        public override void Execute(bool propagateLogical = true, bool propagatePhysical = false)
        {
            inner.Execute(propagateLogical, propagatePhysical);
        }

        /// <inheritdoc/>
        public override bool Intersects(BranchAction other)
        {
            return inner.Intersects(other);
        }

        /// <inheritdoc/>
        public override bool IsPermissible()
        {
            return inner.IsPermissible() && predicate(inner);
        }

        /// <inheritdoc/>
        public override bool IsValid()
        {
            return inner.IsValid() && predicate(inner);
        }

        /// <inheritdoc/>
        public override bool Update()
        {
            return inner.Update() && predicate(inner);
        }
    }
}
