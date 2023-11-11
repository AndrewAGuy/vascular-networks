using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Generators;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;
using Vascular.Structure.Actions;

namespace Vascular.Intersections.Segmental
{
    /// <summary>
    ///
    /// </summary>
    public class SegmentalRecorder : SegmentRecorder<Branch>
    {
        private double totalConsumptionRatio = 10.0;

        /// <summary>
        /// For large regions and small branches, don't bother going around. Disable by setting to 0.
        /// </summary>
        public double TotalConsumptionRatio
        {
            get => totalConsumptionRatio;
            set
            {
                if (value >= 0)
                {
                    totalConsumptionRatio = value;
                }
            }
        }

        /// <summary>
        /// If conflicting resolution paths given, the branch might get trapped. Prevent this by culling immediately.
        /// </summary>
        public bool CullIfSurrounded { get; set; } = true;

        /// <summary>
        ///
        /// </summary>
        public override int Count => intersecting.Count;

        private Dictionary<IMobileNode, SingleEntry> nodes = new();
        private Dictionary<Segment, DoubleEntry> segments = new();

        /// <summary>
        ///
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            nodes = new();
            segments = new();
        }

        /// <summary>
        ///
        /// </summary>
        public CubeGrayCode GrayCode { get; set; } = new CubeGrayCode();

        /// <summary>
        ///
        /// </summary>
        public override void Finish()
        {
            var actions = new List<GeometryAction>(nodes.Count + segments.Count);
            actions.AddRange(minimumNodePerturbation > 0.0
                ? nodes.Select(kv =>
                {
                    var mv = kv.Value.Mean;
                    var mp = mv.Length;
                    if (mp < minimumNodePerturbation)
                    {
                        mv = mp != 0.0
                            ? mv * minimumNodePerturbation / mp
                            : this.GrayCode.NextVector3().Normalize() * minimumNodePerturbation;
                    }
                    return new PerturbNode(kv.Key, mv);
                })
                : nodes.Select(kv => new PerturbNode(kv.Key, kv.Value.Mean)));
            actions.AddRange(segments.Select(kv => new InsertTransient(kv.Key, kv.Value.Mean)));
            this.GeometryActions = actions;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="i"></param>
        protected override void RecordSingle(SegmentIntersection i)
        {
            intersecting.Add(i.B.Branch);
            if (totalConsumptionRatio != 0.0)
            {
                if (i.Overlap > i.B.Radius * totalConsumptionRatio)
                {
                    culling.Add(i.B.Branch);
                    return;
                }
            }

            if (i.Indeterminate)
            {
                RecordIndefinite(i);
            }
            else
            {
                RecordDefinite(i);
            }
        }

        private void RecordDefinite(SegmentIntersection i)
        {
            var perturb = i.NormalAB * (aggressionFactor * i.Overlap);

            var tryPushS = i.FractionB * i.B.Slenderness < radialCaptureFraction;
            var tryPushE = (1 - i.FractionB) * i.B.Slenderness < radialCaptureFraction;
            var tryPush = tryPushE || tryPushS;

            if (!tryPush)
            {
                Request(i.B, i.ClosestB!, perturb);
            }
            else
            {
                if (tryPushE)
                {
                    Request(i.B.End, perturb);
                }
                if (tryPushS)
                {
                    Request(i.B.Start, perturb);
                }
            }
        }

        private void RecordIndefinite(SegmentIntersection i)
        {
            var perturb = i.NormalAB * (aggressionFactor * i.Overlap);

            // We now wish to do the following:
            //  - Move 1 node, make 1 transient
            //  - Move 2 nodes
            //  - Make 2 transients     (But this isn't allowed by the single action rule!)

            var moveS = i.StartB * i.B.Slenderness < radialCaptureFraction;
            var moveE = (1 - i.EndB) * i.B.Slenderness < radialCaptureFraction;

            if (moveS)
            {
                Request(i.B.Start, perturb);
                if (moveE)
                {
                    Request(i.B.End, perturb);
                }
                else
                {
                    Request(i.B, i.B.AtFraction(i.EndB), perturb);
                }
            }
            else
            {
                if (moveE)
                {
                    Request(i.B.End, perturb);
                    Request(i.B, i.B.AtFraction(i.StartB), perturb);
                }
                else
                {
                    // We have consumed the forbidden branch... oops
                    var mid = (i.StartB + i.EndB) * 0.5;
                    Request(i.B, i.B.AtFraction(mid), perturb);
                }
            }
        }

        private void Request(INode node, Vector3 pert)
        {
            if (node is IMobileNode mobile)
            {
                if (nodes.TryGetValue(mobile, out var cur))
                {
                    // Test if perturbation requested opposes current
                    if (cur.Value * pert < 0 && this.CullIfSurrounded)
                    {
                        culling.Add(mobile.Parent!.Branch);
                    }
                    else
                    {
                        cur.Add(pert);
                    }
                }
                else
                {
                    nodes[mobile] = new SingleEntry(pert);
                }
            }
            else
            {
                culling.Add(node.Parent!.Branch);
            }
        }

        private void Request(Segment segment, Vector3 from, Vector3 pert)
        {
            if (segments.TryGetValue(segment, out var cur))
            {
                if (cur.Direction * pert < 0 && this.CullIfSurrounded)
                {
                    culling.Add(segment.Branch);
                }
                else
                {
                    cur.Add(from, pert);
                }
            }
            else
            {
                segments[segment] = new DoubleEntry(from, pert);
            }
        }
    }
}
