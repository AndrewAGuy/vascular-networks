using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Generators;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;
using Vascular.Structure.Actions;

namespace Vascular.Intersections.Implicit
{
    using ImplicitFunction = Func<Vector3, (double f, Vector3 g)>;

    /// <summary>
    /// 
    /// </summary>
    public class ImplicitRecorder : Recorder<ImplicitViolation, INode>
    {
        /// <summary>
        /// 
        /// </summary>
        public override int Count => intersecting.Count;

        /// <summary>
        /// 
        /// </summary>
        public Func<INode, double> MinimumPerturbation { get; set; } = null;

        /// <summary>
        /// For clamping to minimum perturbations.
        /// </summary>
        public IVector3Generator Generator { get; set; } = new CubeGrayCode();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="violations"></param>
        /// <param name="function"></param>
        public void Record(IEnumerable<ImplicitViolation> violations, ImplicitFunction function)
        {
            this.function = function;
            base.Record(violations);
        }

        /// <summary>
        /// 
        /// </summary>
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
            this.GeometryActions = actions;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="violation"></param>
        protected override void RecordSingle(ImplicitViolation violation)
        {
            if (TryTopology(violation))
            {
                return;
            }

            RecordGeometry(violation);
        }

        /// <summary>
        /// 
        /// </summary>
        public Func<ImplicitViolation, bool> ImmunityPredicate { get; set; } = v => false;

        /// <summary>
        /// 
        /// </summary>
        public double ImmediateCull { get; set; } = double.PositiveInfinity;

        /// <summary>
        /// 
        /// </summary>
        public bool CullIfSurrounded { get; set; } = true;

        /// <summary>
        /// 
        /// </summary>
        public Func<ImplicitViolation, ImplicitFunction, Vector3> Perturbation { get; set; } = (iv, f) => -iv.Gradient;

        private ImplicitFunction function;

        private void RecordGeometry(ImplicitViolation violation)
        {
            var pert = this.Perturbation(violation, function);
            Request(violation.Node, pert);
        }

        /// <summary>
        /// 
        /// </summary>
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
            if (this.ImmunityPredicate(violation))
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
