using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Intersections
{
    public enum BranchRelationship
    {
        None,
        Internal,
        Upstream,
        Downstream,
        Parent,
        Child,
        Sibling,
        Disjoint,
        Matched
    }
}
