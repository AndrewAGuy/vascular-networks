using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Geometric
{
    public class Smoother
    {
        public Func<Terminal, Vector3> TerminalDirection { get; set; }
        public Func<Source, Vector3> SourceDirection { get; set; }

        public Func<Segment, double> LinearSpringConstant { get; set; }
        public Func<Segment, double> AngularSpringConstant { get; set; }

        public double NormalTolerance { get; set; } = 1.0e-9;
        public double Scaling { get; set; } = 1.0;

        public static Func<Segment, double> ThinShellLinearSpring(double k0, Func<Segment, double> t)
        {
            return s => k0 * s.Radius * t(s);
        }

        public static Func<Segment, double> ThinShellAngularSpring(double k0, Func<Segment, double> t)
        {
            return s => k0 * Math.Pow(s.Radius, 3) * t(s);
        }

        public static Func<Segment, double> LinearShellThickness(double k)
        {
            return s => k * s.Radius;
        }

        public static Func<Terminal, Vector3> GroupTerminalDirection()
        {
            return t =>
            {
                if (t.Partners is not null && t.Partners.Length > 1)
                {
                    var gn = t.Partners.Select(T => T.Upstream.NormalizedDirection).Sum().Normalize();
                    var ip = LinearAlgebra.RemoveComponent(t.Upstream.Direction, gn);
                    return ip.NormalizeSafe();
                }
                return null;
            };
        }

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

        public void AddLinearForces(Branch branch, IDictionary<IMobileNode, Vector3> forces)
        {
            var naturalLength = branch.DirectLength / branch.Segments.Count;
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

        private static void TryAddForce(IDictionary<IMobileNode, Vector3> forces, INode node, Vector3 force)
        {
            if (node is IMobileNode mobile)
            {
                forces[mobile] = forces.TryGetValue(mobile, out var value) ? value + force : force;
            }
        }
    }
}
