using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Geometry.Generators;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;
using Vascular.Structure.Actions;

namespace Vascular.Intersections.Implicit
{
    public class ImplicitRecorder : Recorder<ImplicitViolation, INode>
    {
        public override int Count => intersecting.Count;

        public Func<INode, double> MinimumPerturbation { get; set; } = null;

        public IVector3Generator Generator { get; set; } = new CubeGrayCode();

        public override void Finish()
        {
            var actions = this.MinimumPerturbation != null
                ? nodes.Select(kv =>
                {
                    var (node, pert) = kv;
                    var minPert = this.MinimumPerturbation(node);
                    var dx = pert.Mean.ClampOutsideBall(Vector3.ZERO, minPert, generator: this.Generator);
                    return new PerturbNode(node, dx);
                })
                : nodes.Select(kv => new PerturbNode(kv.Key, kv.Value.Mean));
        }

        protected override void RecordSingle(ImplicitViolation violation)
        {
            if (TryTopology(violation))
            {
                return;
            }

            RecordGeometry(violation);
        }

        public Func<INode, bool> ImmunityPredicate { get; set; }

        public double ImmediateCull { get; set; } = double.PositiveInfinity;

        public bool CullIfSurrounded { get; set; } = true;

        public Func<ImplicitViolation, Vector3> Perturbation { get; set; }

        private void RecordGeometry(ImplicitViolation violation)
        {
            var pert = this.Perturbation(violation);
            Request(violation.Node, pert);
        }

        public override void Reset()
        {
            base.Reset();
            nodes.Clear();
        }

        private readonly Dictionary<IMobileNode, SingleEntry> nodes = new();

        private void Request(INode node, Vector3 pert)
        {
            if (node is IMobileNode mobile)
            {
                if (nodes.TryGetValue(mobile, out var cur))
                {
                    // Test if perturbation requested opposes current
                    if (cur.Value * pert < 0 && this.CullIfSurrounded)
                    {
                        culling.Add(mobile);
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
                culling.Add(node);
            }
        }

        private bool TryTopology(ImplicitViolation violation)
        {
            if (this.ImmunityPredicate != null && this.ImmunityPredicate(violation.Node))
            {
                return true;
            }
            intersecting.Add(violation.Node);

            if (violation.Value < this.ImmediateCull)
            {
                return false;
            }

            culling.Add(violation.Node);
            return true;
        }
    }
}
