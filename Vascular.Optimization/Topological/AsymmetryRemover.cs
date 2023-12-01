using System;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Topological
{
    /// <summary>
    /// When bifurcations have high radius or flow imbalance, this class removes
    /// the low-flow side so that it can be rebuilt later from a more suitable point.
    /// </summary>
    public class AsymmetryRemover
    {
        private double rRatio = double.PositiveInfinity;
        private double rqRatio = double.PositiveInfinity;
        private double qRatio = double.PositiveInfinity;

        /// <summary>
        ///
        /// </summary>
        public double RadiusRatio
        {
            get => rRatio;
            set
            {
                if (value < 1)
                {
                    value = 1.0 / value;
                }

                if (value > 1)
                {
                    rRatio = value;
                    rqRatio = Math.Pow(value, 4);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        public double FlowRatio
        {
            get => qRatio;
            set
            {
                if (value < 1)
                {
                    value = 1.0 / value;
                }

                if (value > 1)
                {
                    qRatio = value;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        public bool ActDownwards { get; set; } = true;

        /// <summary>
        ///
        /// </summary>
        public bool ActUpwards { get; set; } = true;

        /// <summary>
        ///
        /// </summary>
        public Action<Terminal>? OnCull { get; set; }

        /// <summary>
        /// Visits the network in a down-up fashion, attempting to remove the low-flow side
        /// of asymmetric bifurcations.
        /// </summary>
        /// <param name="root"></param>
        public void Act(Branch root)
        {
            if (this.ActDownwards)
            {
                TryLoop(root);
            }
            else if (root.End is not Terminal)
            {
                Act(root.End);
            }
        }

        // private void Act(Bifurcation bifurc)
        // {
        //     if (this.ActDownwards)
        //     {
        //         // We get to this bifurcation knowing that it is safe from downwards removal
        //         // Hop to the next safe one to establish the next recursion point
        //         foreach (var downstream in bifurc.Downstream)
        //         {
        //             TryLoop(downstream);
        //         }
        //     }
        //     else
        //     {
        //         // All bifurcations are safe on the way downstream, touch them all
        //         foreach (var down in bifurc.Downstream)
        //         {
        //             if (down.End is Bifurcation downBifurc)
        //             {
        //                 Act(downBifurc);
        //             }
        //         }
        //     }

        //     if (this.ActUpwards)
        //     {
        //         // No need to worry in the upstream pass, just remove as needed
        //         TryRemove(bifurc);
        //     }
        // }

        private void Act(BranchNode bn)
        {
            if (this.ActDownwards)
            {
                // We get to this node knowing that it is safe from downwards removal
                // Hop to the next safe one to establish the next recursion point
                foreach (var downstream in bn.Downstream)
                {
                    TryLoop(downstream);
                }
            }
            else
            {
                // All nodes are safe on the way downstream, touch them all
                foreach (var down in bn.Downstream)
                {
                    if (down.End is not Terminal)
                    {
                        Act(down.End);
                    }
                }
            }

            if (this.ActUpwards)
            {
                // No need to worry in the upstream pass, just remove as needed
                TryRemove(bn);
            }
        }

        private void TryLoop(Branch br)
        {
        // If something gets removed this branch will now point to a new ending node.
        // So we recur directly from this.
        BEGIN:
            if (br.End is not Terminal)
            {
                if (TryRemove(br.End) is Transient)
                {
                    goto BEGIN;
                }
                else
                {
                    Act(br.End);
                }
            }
        }

        //         private void TryLoop(Branch branch)
        //         {
        // // If something gets removed this branch will now point to a new ending node.
        // // So we recur directly from this.
        // BEGIN:
        //             if (branch.End is Bifurcation bifurc)
        //             {
        //                 if (TryRemove(bifurc))
        //                 {
        //                     goto BEGIN;
        //                 }
        //                 else
        //                 {
        //                     Act(bifurc);
        //                 }
        //             }
        //         }

        //         private bool TryRemove(Bifurcation bf)
        //         {
        //             var (c0, c1) = (bf.Downstream[0], bf.Downstream[1]);
        //             var (q0, q1) = (c0.Flow, c1.Flow);
        //             var (qHi, qLo, cLo) = q0 > q1
        //                 ? (q0, q1, c1)
        //                 : (q1, q0, c0);
        //             if (qHi > qLo * qRatio)
        //             {
        //                 goto REMOVE;
        //             }
        //             // If radius on low flow side much larger than high flow,
        //             // indicates excess length and probable suboptimality.
        //             // So always penalize radius asymmetry by removing the low flow side.
        //             var rq0 = q0 * c0.ReducedResistance;
        //             var rq1 = q1 * c1.ReducedResistance;
        //             var (rqHi, rqLo) = rq0 > rq1 ? (rq0, rq1) : (rq1, rq0);
        //             if (rqHi > rqLo * rqRatio)
        //             {
        //                 goto REMOVE;
        //             }
        //             return false;

        // REMOVE:
        //             var action = new RemoveBranch(cLo)
        //             {
        //                 OnCull = this.OnCull
        //             };
        //             action.Execute(true, true);
        //             return true;
        //         }

        private INode? TryRemove(BranchNode bn)
        {
            var QMax = bn.Downstream[0].Flow;
            var iMax = 0;
            for (var i = 1; i < bn.Downstream.Length; ++i)
            {
                var q = bn.Downstream[i].Flow;
                if (q > QMax)
                {
                    (QMax, iMax) = (q, i);
                }
            }

            Span<int> culling = stackalloc int[bn.Downstream.Length - 1];
            var iCull = 0;
            var bMax = bn.Downstream[iMax];
            for (var i = 0; i < bn.Downstream.Length; ++i)
            {
                if (i == iMax)
                {
                    continue;
                }
                if (TestRemove(bMax, bn.Downstream[i]))
                {
                    culling[iCull] = i;
                    ++iCull;
                }
            }

            if (iCull > 0)
            {
                return Topology.CullBranches(bn, culling[..iCull], this.OnCull, null);
            }
            return null;
        }

        private bool TestRemove(Branch cMax, Branch cTest)
        {
            var (qMax, qTest) = (cMax.Flow, cTest.Flow);
            if (qMax > qTest * qRatio)
            {
                return true;
            }
            // If radius on low flow side much larger than high flow,
            // indicates excess length and probable suboptimality.
            // So always penalize radius asymmetry by removing the low flow side.
            var rqMax = qMax * cMax.ReducedResistance;
            var rqTest = qTest * cTest.ReducedResistance;
            var (rqHi, rqLo) = rqMax > rqTest ? (rqMax, rqTest) : (rqTest, rqMax);
            return rqHi > rqLo * rqRatio;
        }
    }
}
