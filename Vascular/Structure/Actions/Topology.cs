using Vascular.Structure.Nodes;

namespace Vascular.Structure.Actions
{
    /// <summary>
    /// Helper methods for changing the network topology.
    /// </summary>
    public static class Topology
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="seg"></param>
        /// <returns></returns>
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
            seg.Branch.Reinitialize();
            return tran;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tran"></param>
        /// <returns></returns>
        public static Segment RemoveTransient(Transient tran)
        {
            // Simple case of rewiring existing parent to existing child end.
            var seg = tran.Parent;
            var end = tran.Child.End;
            seg.End = end;
            end.Parent = seg;
            seg.Branch.Reinitialize();
            return seg;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="term"></param>
        /// <param name="nullParent"></param>
        /// <returns></returns>
        public static Transient CullTerminal(Terminal term, bool nullParent = true)
        {
            // Was terminal ever actually built?
            if (term.Parent == null)
            {
                term.Culled = true;
                return null;
            }
            // Will this kill the whole network?
            var branch = term.Upstream;
            if (branch.Start is not Bifurcation bifurc)
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
            parent.Branch.Reinitialize();
            // Set as culled, cast branch into the void
            if (nullParent)
            {
                term.Parent = null;
            }
            term.Culled = true;
            return tran;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="term"></param>
        /// <param name="nullParent"></param>
        /// <returns></returns>
        public static Transient CullTerminalAndPropagate(Terminal term, bool nullParent = true)
        {
            var tran = CullTerminal(term, nullParent);
            if (tran != null)
            {
                tran.Parent.Branch.PropagateLogicalUpstream();
                tran.UpdatePhysicalAndPropagate();
            }
            return tran;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Branch MakeFirst(Source s, Terminal t)
        {
            // Must create branch beforehand, since setting source child segment will pull branch in
            var seg = new Segment() { Start = s, End = t };
            var br = new Branch(seg) { Start = s, End = t };
            s.Child = seg;
            t.Parent = seg;
            t.Network = s.Network; // Forgetting this has made life so hard in the past
            return br;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
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
            from.Branch.Reinitialize();
            return bifurc;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bifurc"></param>
        /// <param name="keptChild"></param>
        /// <param name="markDownstream"></param>
        /// <param name="nullDownstream"></param>
        /// <param name="nullLost"></param>
        /// <returns></returns>
        public static Transient RemoveBifurcation(Bifurcation bifurc, int keptChild,
            bool markDownstream = true, bool nullDownstream = true, bool nullLost = false)
        {
            var lostChild = keptChild == 0 ? 1 : 0;
            // Rewire bifurcation into transient, same as in culling
            var tran = new Transient()
            {
                Position = bifurc.Position,
                Child = bifurc.Children[keptChild],
                Parent = bifurc.Parent
            };
            tran.Parent.End = tran;
            tran.Child.Start = tran;
            tran.Parent.Branch.End = tran.Child.Branch.End;
            tran.Parent.Branch.Reinitialize();
            // Now find all downstream terminals on the lost side, remove them
            if (markDownstream)
            {
                Terminal.ForDownstream(bifurc.Downstream[lostChild], t =>
                {
                    if (nullDownstream)
                    {
                        t.Parent = null;
                    }
                    t.Culled = true;
                });
            }
            if (nullLost)
            {
                bifurc.Parent = null;
                bifurc.Downstream[lostChild].End.Parent = null;
            }
            return tran;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="throwIfRoot"></param>
        /// <param name="markDownstream"></param>
        /// <param name="nullDownstream"></param>
        /// <param name="nullLost"></param>
        /// <returns></returns>
        public static Transient RemoveBranch(Branch branch, bool throwIfRoot = true,
            bool markDownstream = true, bool nullDownstream = true, bool nullLost = false)
        {
            switch (branch.Start)
            {
                case Bifurcation bifurc:
                    return RemoveBifurcation(bifurc, bifurc.IndexOf(branch), markDownstream, nullDownstream, nullLost);
                case Source source:
                    if (throwIfRoot)
                    {
                        throw new TopologyException("Branch to be removed is root vessel");
                    }
                    source.Child = null;
                    if (markDownstream)
                    {
                        Terminal.ForDownstream(branch, t =>
                        {
                            if (nullDownstream)
                            {
                                t.Parent = null;
                            }
                            t.Culled = true;
                        });
                    }
                    return null;
                default:
                    throw new TopologyException("Branch started with invalid node");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void SwapEnds(Branch a, Branch b)
        {
            var endA = a.End;
            var endB = b.End;
            a.End = endB;
            b.End = endA;
            a.Reset();
            b.Reset();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
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
            a.Branch.Reinitialize();
            b.Branch.Reinitialize();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="moving"></param>
        /// <param name="from"></param>
        /// <returns></returns>
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
                tran.Parent.Branch.Reinitialize();
                // Create new bifurcation, reset branches to do this
                moving.Reset();
                from.Reset();
                return (tran, CreateBifurcation(from.Segments[0], moving.End));
            }
            return (null, null);
        }
    }
}
