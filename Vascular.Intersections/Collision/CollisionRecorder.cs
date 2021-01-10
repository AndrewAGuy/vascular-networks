using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;
using Vascular.Structure.Actions;

namespace Vascular.Intersections.Collision
{
    public class CollisionRecorder : SegmentRecorder< INode>
    {
        private double immediateCullRatio = 0.0;
        public double ImmediateCullRatio
        {
            get
            {
                return immediateCullRatio;
            }
            set
            {
                if (value >= 0)
                {
                    immediateCullRatio = value;
                }
            }
        }
        public bool ResetStationaryFractions { get; set; } = true;

        public bool RecordTopology { get; set; } = false;

        public override int Count
        {
            get
            {
                // All immediate cull nodes are also present in the stationary set
                return segments.Count + nodes.Count + intersecting.Count;
            }
        }

        private Dictionary<Segment, SingleEntry> segments = new Dictionary<Segment, SingleEntry>();
        private Dictionary<IMobileNode, SingleEntry> nodes = new Dictionary<IMobileNode, SingleEntry>();

        private HashSet<BranchAction> branchActions = new HashSet<BranchAction>();

        public override void Reset()
        {
            base.Reset();
            segments = new Dictionary<Segment, SingleEntry>();
            nodes = new Dictionary<IMobileNode, SingleEntry>();
            branchActions = new HashSet<BranchAction>();
        }

        protected override void RecordSingle(SegmentIntersection i)
        {
            if (!TryRecordTopology(i))
            {
                if (i.Indeterminate)
                {
                    RecordIndefinite(i);
                }
                else
                {
                    RecordDefinite(i);
                }
            }
        }

        private void RecordDefinite(SegmentIntersection data)
        {
            // Does the nearest point lie within one radius of an end plus Fudge Factor (TM)?
            // For now, don't do anything clever. Repeated attempts will perhaps resolve everything
            var tryPushAS = data.FractionA * data.A.Slenderness < radialCaptureFraction;
            var tryPushAE = (1 - data.FractionA) * data.A.Slenderness < radialCaptureFraction;
            var tryPushBS = data.FractionB * data.B.Slenderness < radialCaptureFraction;
            var tryPushBE = (1 - data.FractionB) * data.B.Slenderness < radialCaptureFraction;
            var tryPushA = tryPushAS || tryPushAE;
            var tryPushB = tryPushBS || tryPushBE;
            // If we need to try pushing, check if we can't manage for any attempts. This informs us if we need to push 50:50 or all on one
            var notPushA = (tryPushAS && !(data.A.Start is IMobileNode)) || (tryPushAE && !(data.A.End is IMobileNode));
            var notPushB = (tryPushBS && !(data.B.Start is IMobileNode)) || (tryPushBE && !(data.B.End is IMobileNode));
            // New weighting, gives high flow branches priority
            var compA = data.A.Branch.Network.RelativeCompliance * data.B.Flow;
            var compB = data.B.Branch.Network.RelativeCompliance * data.A.Flow;
            var totalCompliance = compA + compB;
            var aCompFrac = compA / totalCompliance;
            var bCompFrac = compB / totalCompliance;
            if (tryPushA)
            {
                if (tryPushB)
                {
                    var facA = (notPushB && this.ResetStationaryFractions ? 1 : aCompFrac) * aggressionFactor * data.Overlap;
                    var facB = (notPushA && this.ResetStationaryFractions ? 1 : bCompFrac) * aggressionFactor * data.Overlap;
                    // Both need to be pushed
                    if (tryPushAS)
                    {
                        Record(data.A.Start, -data.NormalAB * facA, data.B);
                    }
                    if (tryPushAE)
                    {
                        Record(data.A.End, -data.NormalAB * facA, data.B);
                    }

                    if (tryPushBS)
                    {
                        Record(data.B.Start, data.NormalAB * facB, data.A);
                    }
                    if (tryPushBE)
                    {
                        Record(data.B.End, data.NormalAB * facB, data.A);
                    }
                }
                else
                {
                    var facA = aCompFrac * aggressionFactor * data.Overlap;
                    var facB = (notPushA && this.ResetStationaryFractions ? 1 : bCompFrac) * aggressionFactor * data.Overlap;
                    // Push A
                    if (tryPushAS)
                    {
                        Record(data.A.Start, -data.NormalAB * facA, data.B);
                    }
                    if (tryPushAE)
                    {
                        Record(data.A.End, -data.NormalAB * facA, data.B);
                    }
                    // Make transient node in B
                    Record(data.B, data.ClosestB + facB * data.NormalAB);
                }
            }
            else
            {
                if (tryPushB)
                {
                    var facA = (notPushB && this.ResetStationaryFractions ? 1 : aCompFrac) * aggressionFactor * data.Overlap;
                    var facB = bCompFrac * aggressionFactor * data.Overlap;
                    if (tryPushBS)
                    {
                        Record(data.B.Start, data.NormalAB * facB, data.A);
                    }
                    if (tryPushBE)
                    {
                        Record(data.B.End, data.NormalAB * facB, data.A);
                    }
                    Record(data.A, data.ClosestA - facA * data.NormalAB);
                }
                else
                {
                    var facA = aCompFrac * aggressionFactor * data.Overlap;
                    var facB = bCompFrac * aggressionFactor * data.Overlap;
                    Record(data.B, data.ClosestB + facB * data.NormalAB);
                    Record(data.A, data.ClosestA - facA * data.NormalAB);
                }
            }
        }

        private void RecordIndefinite(SegmentIntersection data)
        {
            var push = data.NormalAB * data.Overlap * aggressionFactor;
            var compA = data.A.Branch.Network.RelativeCompliance * data.B.Flow;
            var compB = data.B.Branch.Network.RelativeCompliance * data.A.Flow;
            var totalCompliance = compA + compB;
            var aCompFrac = compA / totalCompliance;
            var bCompFrac = compB / totalCompliance;
            var pushA = aCompFrac * push;
            var pushB = bCompFrac * push;
            // This is the least likely scenario, so the performance hit of using LINQ _should_ be negligible
            var tryMoveA = new List<INode>(2);
            if (data.StartA * data.A.Slenderness < radialCaptureFraction)
            {
                tryMoveA.Add(data.A.Start);
            }
            if ((1 - data.EndA) * data.A.Slenderness < radialCaptureFraction)
            {
                tryMoveA.Add(data.A.End);
            }
            var tryMoveB = new List<INode>(2);
            if (data.StartB * data.B.Slenderness < radialCaptureFraction)
            {
                tryMoveB.Add(data.B.Start);
            }
            if ((1 - data.EndB) * data.B.Slenderness < radialCaptureFraction)
            {
                tryMoveB.Add(data.B.End);
            }

            foreach (var n in tryMoveA)
            {
                Record(n, -pushA, data.B);
            }
            foreach (var n in tryMoveB)
            {
                Record(n, pushB, data.A);
            }

            if (!tryMoveA.All(n => n is IMobileNode))
            {
                var midB = (data.StartB + data.EndB) * 0.5;
                if (midB * data.B.Slenderness >= radialCaptureFraction &&
                    (1 - midB) * data.B.Slenderness >= radialCaptureFraction)
                {
                    Record(data.B, data.B.AtFraction(midB) + pushB);
                }
            }
            if (!tryMoveB.All(n => n is IMobileNode))
            {
                var midA = (data.StartA + data.EndA) * 0.5;
                if (midA * data.A.Slenderness >= radialCaptureFraction &&
                    (1 - midA) * data.A.Slenderness >= radialCaptureFraction)
                {
                    Record(data.A, data.A.AtFraction(midA) - pushA);
                }
            }
        }

        private void Record(Segment s, Vector3 v)
        {
            if (segments.TryGetValue(s, out var p))
            {
                p.Add(v);
            }
            else
            {
                segments.Add(s, new SingleEntry(v));
            }
        }

        private void Record(INode n, Vector3 v, Segment s)
        {
            if (n is IMobileNode m)
            {
                if (nodes.TryGetValue(m, out var p))
                {
                    p.Add(v);
                }
                else
                {
                    nodes.Add(m, new SingleEntry(v));
                }
            }
            else
            {
                intersecting.Add(n);
                if (immediateCullRatio != 0.0 && n.Parent != null) // Ensure that this isn't a source node
                {
                    var flowscaled = n.Parent.Flow * immediateCullRatio;
                    if (s.Flow > flowscaled)
                    {
                        culling.Add(n);
                    }
                }
            }
        }

        public Func<Branch, Branch, bool> BranchActionPredicate { get; set; } =
            (a, b) => Math.Min(a.Flow, b.Flow) > 4;

        private bool TryRecordTopology(SegmentIntersection i)
        {
            var A = i.A.Branch;
            var B = i.B.Branch;
            if (!this.RecordTopology || 
                A.Network != B.Network || 
                !this.BranchActionPredicate(A, B))
            {
                return false;
            }
            var sA = A.Start.Position;
            var eA = A.End.Position;
            var sB = B.Start.Position;
            var eB = B.End.Position;
            var lA = Vector3.DistanceSquared(sA, eA);
            var lB = Vector3.DistanceSquared(sB, eB);
            var dA = Vector3.DistanceSquared(sB, eA);
            var dB = Vector3.DistanceSquared(sA, eB);
            if (dA < lA)
            {
                if (dB < lB)
                {
                    branchActions.Add(new SwapEnds(A, B));
                }
                else
                {
                    branchActions.Add(new MoveBifurcation(A, B));
                }
            }
            else
            {
                if (dA < lA)
                {
                    branchActions.Add(new MoveBifurcation(B, A));
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public override void Finish()
        {
            FinishGeometry();
            FinishTopology();
        }

        private void FinishGeometry()
        {
            var actions = new List<GeometryAction>(nodes.Count + segments.Count);
            actions.AddRange(minimumNodePerturbation == 0
                ? nodes.Select(n => new PerturbNode(n.Key, n.Value.Mean))
                : nodes.Select(n =>
                {
                    var delta = n.Value.Mean;
                    var d = Math.Sqrt(delta.LengthSquared);
                    if (d < minimumNodePerturbation)
                    {
                        if (d == 0.0)
                        {
                            delta = new Vector3(0.0, 0.0, minimumNodePerturbation);
                        }
                        else
                        {
                            delta *= minimumNodePerturbation / d;
                        }
                    }
                    return new PerturbNode(n.Key, delta);
                }));
            actions.AddRange(segments.Select(s => new InsertTransient(s.Key, s.Value.Mean)));
            this.GeometryActions = actions;
        }

        private void FinishTopology()
        {
            this.BranchActions = branchActions;
        }
    }
}