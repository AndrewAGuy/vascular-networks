using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Structure.Actions
{
    public abstract class BranchAction : TopologyAction
    {
        public BranchAction(Branch a, Branch b)
        {
            this.a = a;
            this.b = b;
        }
        protected Branch a;
        protected Branch b;
        public Branch A => a;
        public Branch B => b;  

        public bool Update()
        {
            a = a.CurrentTopologicallyValid;
            b = b.CurrentTopologicallyValid;
            return !ReferenceEquals(a, b);
        }

        public bool IsValid()
        {
            return a.IsTopologicallyValid
                && b.IsTopologicallyValid;
        }

        public bool Intersects(BranchAction other)
        {
            return ReferenceEquals(a, other.a)
                || ReferenceEquals(b, other.b)
                || ReferenceEquals(a, other.b)
                || ReferenceEquals(b, other.a);
        }
    }
}
