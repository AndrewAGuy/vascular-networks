using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Optimization.Topological;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Geometric
{
    /// <summary>
    /// Uses a linear and angular spring combination to smooth branches.
    /// </summary>
    public class Smoother
    {
        /// <summary>
        /// 
        /// </summary>
        public Func<Terminal, Vector3> TerminalDirection { get; set; } = GroupTerminalDirection();

        /// <summary>
        /// 
        /// </summary>
        public Func<Source, Vector3> SourceDirection { get; set; } = InletSourceDirection();

        /// <summary>
        /// 
        /// </summary>
        public Func<Segment, double> LinearSpringConstant { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Func<Segment, double> AngularSpringConstant { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Func<Branch, double> TargetBranchLength { get; set; } = b => b.DirectLength;

        /// <summary>
        /// 
        /// </summary>
        public double NormalTolerance { get; set; } = 1.0e-9;

        /// <summary>
        /// Multiplicative factor: set to -1 for forces -> gradient descent.
        /// </summary>
        public double Scaling { get; set; } = 1.0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="k0"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Func<Segment, double> ThinShellLinearSpring(double k0, Func<Segment, double> t)
        {
            return s => k0 * s.Radius * t(s);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="k0"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Func<Segment, double> ThinShellAngularSpring(double k0, Func<Segment, double> t)
        {
            return s => k0 * Math.Pow(s.Radius, 3) * t(s);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        public static Func<Segment, double> LinearShellThickness(double k)
        {
            return s => k * s.Radius;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t2"></param>
        /// <returns></returns>
        public static Func<Terminal, Vector3> GroupTerminalDirection(double t2 = 1e-12)
        {
            return t =>
            {
                if (t.Partners is not null && t.Partners.Length > 1)
                {
                    var gn = t.Partners
                        .Select(T => T.Upstream.Direction.NormalizeSafe(t2) ?? Vector3.ZERO)
                        .Sum()
                        .NormalizeSafe(t2);
                    var ud = t.Upstream.Direction.NormalizeSafe(t2);
                    if (gn == null || ud == null)
                    {
                        return null;
                    }
                    var ip = LinearAlgebra.RemoveComponent(ud, gn);
                    return ip.NormalizeSafe(t2);
                }
                return null;
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Func<Source, Vector3> InletSourceDirection()
        {
            return s => s.Network.InletDirection;
        }

        /// <summary>
        /// Sets target length for a branch to be scaled as the volume of its crown ^1/3.
        /// Assumes uniform terminal flow rates and spacing, so volume is proportional to flow rate.
        /// </summary>
        /// <param name="L0">The length of a branch that carries 1 unit of flow.</param>
        /// <returns></returns>
        public static Func<Branch, double> FlowLength(double L0)
        {
            return b => L0 * Math.Cbrt(b.Flow);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="network"></param>
        /// <returns></returns>
        public IDictionary<IMobileNode, Vector3> Forces(Network network)
        {
            var forces = new Dictionary<IMobileNode, Vector3>(network.Nodes.Count());
            foreach (var branch in network.Branches)
            {
                AddLinearForces(branch, forces);
            }
            foreach (var node in network.Nodes)
            {
                AddAngularForces(node, forces);
            }
            return forces;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="forces"></param>
        public void AddAngularForces(INode node, IDictionary<IMobileNode, Vector3> forces)
        {
            switch (node)
            {
                case Source source:
                    AddAngularForces(source, forces);
                    break;
                case Terminal terminal:
                    AddAngularForces(terminal, forces);
                    break;
                default:
                    foreach (var child in node.Children)
                    {
                        AddAngularForces(node.Parent, child, forces);
                    }
                    break;
            }
        }

        private void AddAngularForces(Segment parent, Segment child, IDictionary<IMobileNode, Vector3> forces)
        {
            var pDir = parent.Direction.Normalize();
            var cDir = child.Direction.Normalize();
            var kParent = this.AngularSpringConstant(parent);
            var kChild = this.AngularSpringConstant(child);
            if (kParent == 0 || kChild == 0)
            {
                return;
            }
            var kSpring = 1.0 / (1.0 / kParent + 1.0 / kChild);
            var normal = pDir ^ cDir;
            var normalLength = normal.Length;
            if (normalLength <= this.NormalTolerance)
            {
                return;
            }
            var angle = Math.Abs(Math.Asin(normalLength));
            normal /= normalLength;
            var moment = kSpring * angle * this.Scaling;
            var pCouple = moment / parent.Length;
            var cCouple = moment / child.Length;
            var pForce = pCouple * (pDir ^ normal);
            var cForce = cCouple * (normal ^ cDir);
            TryAddForce(forces, parent.Start, pForce);
            TryAddForce(forces, parent.End, -pForce);
            TryAddForce(forces, child.Start, cForce);
            TryAddForce(forces, child.End, -cForce);
        }

        private void AddAngularForces(Terminal terminal, IDictionary<IMobileNode, Vector3> forces)
        {
            var pDir = terminal.Parent.Direction.Normalize();
            var cDir = this.TerminalDirection?.Invoke(terminal);
            if (cDir == null)
            {
                return;
            }
            var kSpring = this.AngularSpringConstant(terminal.Parent);
            var normal = pDir ^ cDir;
            var normalLength = normal.Length;
            if (normalLength <= this.NormalTolerance)
            {
                return;
            }
            var angle = Math.Abs(Math.Asin(normalLength));
            normal /= normalLength;
            var moment = kSpring * angle * this.Scaling;
            var pCouple = moment / terminal.Parent.Length;
            var pForce = pCouple * (pDir ^ normal);
            TryAddForce(forces, terminal.Parent.Start, pForce);
        }

        private void AddAngularForces(Source source, IDictionary<IMobileNode, Vector3> forces)
        {
            var cDir = source.Child.Direction.Normalize();
            var pDir = this.SourceDirection?.Invoke(source);
            if (pDir == null)
            {
                return;
            }
            var kSpring = this.AngularSpringConstant(source.Child);
            var normal = pDir ^ cDir;
            var normalLength = normal.Length;
            if (normalLength <= this.NormalTolerance)
            {
                return;
            }
            var angle = Math.Abs(Math.Asin(normalLength));
            normal /= normalLength;
            var moment = kSpring * angle * this.Scaling;
            var cCouple = moment / source.Child.Length;
            var cForce = cCouple * (normal ^ cDir);
            TryAddForce(forces, source.Child.End, -cForce);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="forces"></param>
        public void AddLinearForces(Branch branch, IDictionary<IMobileNode, Vector3> forces)
        {
            var naturalLength = this.TargetBranchLength(branch) / branch.Segments.Count;
            foreach (var segment in branch.Segments)
            {
                AddLinearForces(segment, naturalLength, forces);
            }
        }

        private void AddLinearForces(Segment segment, double naturalLength, IDictionary<IMobileNode, Vector3> forces)
        {
            var length = segment.Length;
            var magnitude = (length - naturalLength) * this.LinearSpringConstant(segment);
            var direction = segment.Direction.Normalize();
            var force = magnitude * direction * this.Scaling;
            TryAddForce(forces, segment.Start, force);
            TryAddForce(forces, segment.End, -force);
        }

        private void TryAddForce(IDictionary<IMobileNode, Vector3> forces, INode node, Vector3 force)
        {
            if (node is IMobileNode mobile && this.RecordPredicate(mobile))
            {
                forces[mobile] = forces.TryGetValue(mobile, out var value) ? value + force : force;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Func<IMobileNode, bool> RecordPredicate { get; set; } = n => true;
    }
}
