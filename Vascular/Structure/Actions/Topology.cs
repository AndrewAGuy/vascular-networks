using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Actions
{
    public static class Topology
    {
        public static Transient InsertTransient(Segment seg)
        {
            var end = seg.End;
            var child = new Segment();
            var tran = new Transient();
            // Child relationships first, to prevent wipe of segment end.
            child.Start = tran;
            child.End = end;
            tran.Child = child;
            end.Parent = child;
            // Parent relationships
            seg.End = tran;
            tran.Parent = seg;
            // Update branch
            seg.Branch.Reinitialise();
            return tran;
        }

        public static Segment RemoveTransient(Transient tran)
        {
            // Simple case of rewiring existing parent to existing child end.
            var seg = tran.Parent;
            var end = tran.Child.End;
            seg.End = end;
            end.Parent = seg;
            seg.Branch.Reinitialise();
            return seg;
        }

        public static Transient CullTerminal(Terminal term)
        {
            // Was terminal ever actually built?
            if (term.Parent == null)
            {
                term.Culled = true;
                return null;
            }
            // Will this kill the whole network?
            var branch = term.Upstream;
            if (!(branch.Start is Bifurcation bifurc))
            {
                throw new TopologyException("Branch to be culled does not start at bifurcation");
            }
            // Rewire sibling and parent into single branch, this turns 3 branches into 1.
            var parent = bifurc.Parent;
            var other = bifurc.Downstream[0] == branch ? bifurc.Children[1] : bifurc.Children[0];
            var tran = new Transient()
            {
                Position = bifurc.Position,
                Child = other,
                Parent = parent
            };
            other.Start = tran;
            parent.End = tran;
            parent.Branch.End = other.Branch.End;
            parent.Branch.Reinitialise();
            // Set as culled, cast branch into the void
            term.Parent = null;
            term.Culled = true;
            return tran;
        }

        public static Transient CullTerminalAndPropagate(Terminal term)
        {
            var tran = CullTerminal(term);
            if (tran != null)
            {
                tran.Parent.Branch.PropagateLogicalUpstream();
                tran.UpdatePhysicalAndPropagate();
            }
            return tran;
        }

        public static Branch MakeFirst(Source s, Terminal t)
        {
            // Must create branch beforehand, since setting source child segment will pull branch in
            var seg = new Segment() { Start = s, End = t };
            var br = new Branch(seg) { Start = s, End = t };
            s.Child = seg;
            t.Parent = seg;
            return br;
        }

        public static Bifurcation CreateBifurcation(Segment from, BranchNode to)
        {
            // Existing segment becomes parent of bifurcation
            // Two new child segments created. Both start at new bifurcation.
            //      Child 0 ends with the existing segment end
            //      Child 1 ends with the terminal
            var end = from.End;
            var branchEnd = from.Branch.End;
            var child0 = new Segment();
            var child1 = new Segment();
            var bifurc = new Bifurcation()
            {
                Network = from.Branch.Network
            };
            // Structural relationships of children and bifurcation.
            child0.Start = bifurc;
            child0.End = end;
            child1.Start = bifurc;
            child1.End = to;
            bifurc.Children[0] = child0;
            bifurc.Children[1] = child1;
            end.Parent = child0;
            to.Parent = child1;
            // Structural relationship of existing segment and bifurcation.
            from.End = bifurc;
            bifurc.Parent = from;
            // Two new branches created to deal with this, one for each new child.
            // Update bifurcation downstream to pull references into bifurcation.
            var branch0 = new Branch(child0) { Start = bifurc, End = branchEnd };
            var branch1 = new Branch(child1) { Start = bifurc, End = to };
            bifurc.UpdateDownstream();
            // Update the existing branch.
            from.Branch.End = bifurc;
            from.Branch.Reinitialise();
            return bifurc;
        }

        public static void SwapEnds(Branch a, Branch b)
        {
            var endA = a.End;
            var endB = b.End;
            a.End = endB;
            b.End = endA;
            a.Reset();
            b.Reset();
        }

        public static void SwapConnections(Segment a, Segment b)
        {
            // Update segment endings
            var endA = a.End;
            var endB = b.End;
            a.End = endB;
            b.End = endA;
            endB.Parent = a;
            endA.Parent = b;
            // Update branch endings and then branch as whole
            var branchEndA = a.Branch.End;
            var branchEndB = b.Branch.End;
            a.Branch.End = branchEndB;
            b.Branch.End = branchEndA;
            a.Branch.Reinitialise();
            b.Branch.Reinitialise();
        }

        public static (Transient, Bifurcation) MoveBifurcation(Branch moving, Branch from)
        {
            if (moving.Start is Bifurcation bifurc)
            {
                // Remove bifurcation, rewire parent and sibling into single branch
                var keptChild = 1 - bifurc.IndexOf(moving);
                var tran = new Transient()
                {
                    Position = bifurc.Position,
                    Child = bifurc.Children[keptChild],
                    Parent = bifurc.Parent
                };
                tran.Parent.End = tran;
                tran.Child.Start = tran;
                tran.Parent.Branch.End = tran.Child.Branch.End;
                tran.Parent.Branch.Reinitialise();
                // Create new bifurcation, reset branches to do this
                moving.Reset();
                from.Reset();
                return (tran, CreateBifurcation(from.Segments[0], moving.End));
            }
            return (null, null);
        }
    }
}
