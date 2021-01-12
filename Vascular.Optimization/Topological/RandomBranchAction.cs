using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Structure;
using Vascular.Structure.Actions;

namespace Vascular.Optimization.Topological
{
    public class RandomBranchAction : BranchAction
    {
        public RandomBranchAction(BranchAction i, Predicate<BranchAction> p) : base(i.A, i.B)
        {
            inner = i;
            predicate = p;
        }
        private readonly BranchAction inner;
        private readonly Predicate<BranchAction> predicate;

        public override void Execute(bool propagateLogical = true, bool propagatePhysical = false)
        {
            inner.Execute(propagateLogical, propagatePhysical);
        }

        public override bool Intersects(BranchAction other)
        {
            return inner.Intersects(other);
        }

        public override bool IsPermissable()
        {
            return inner.IsPermissable() && predicate(inner);
        }

        public override bool IsValid()
        {
            return inner.IsValid() && predicate(inner);
        }

        public override bool Update()
        {
            return inner.Update() && predicate(inner);
        }
    }
}
