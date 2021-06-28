﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;

namespace Vascular.Optimization
{
    public static class FiniteDifferenceGradients
    {
        public static double Gradient(Branch branch, double probeFlow, Func<double> cost)
        {
            var oldFlow = branch.Flow;
            branch.SetFlow(oldFlow + probeFlow);
            branch.PropagateLogicalUpstream();
            branch.PropagatePhysicalUpstream();
            var qp = cost();
            branch.SetFlow(oldFlow - probeFlow);
            branch.PropagateLogicalUpstream();
            branch.PropagatePhysicalUpstream();
            var qn = cost();
            branch.SetFlow(oldFlow);

            branch.PropagateLogicalUpstream();
            branch.PropagatePhysicalUpstream();
            var scale = 0.5 / probeFlow;
            return (qp - qn) * scale;
        }

        public static Vector3 Gradient(IMobileNode node, double probeLength, Func<double> cost)
        {
            var oldPos = node.Position.Copy();

            node.Position.x = oldPos.x + probeLength;
            node.UpdatePhysicalAndPropagate();
            var xp = cost();
            node.Position.x = oldPos.x - probeLength;
            node.UpdatePhysicalAndPropagate();
            var xn = cost();
            node.Position.x = oldPos.x;

            node.Position.y = oldPos.y + probeLength;
            node.UpdatePhysicalAndPropagate();
            var yp = cost();
            node.Position.y = oldPos.y - probeLength;
            node.UpdatePhysicalAndPropagate();
            var yn = cost();
            node.Position.y = oldPos.y;

            node.Position.z = oldPos.z + probeLength;
            node.UpdatePhysicalAndPropagate();
            var zp = cost();
            node.Position.z = oldPos.z - probeLength;
            node.UpdatePhysicalAndPropagate();
            var zn = cost();
            node.Position.z = oldPos.z;

            node.UpdatePhysicalAndPropagate();
            var scale = 0.5 / probeLength;
            return new Vector3(xp - xn, yp - yn, zp - zn) * scale;
        }

#if !NoEffectiveLength
        public static Vector3 Volume(IMobileNode node, double probeLength)
        {
            var source = node.Parent.Branch.Network.Source;
            return Gradient(node, probeLength, () => source.Volume);
        }

        public static double Volume(Branch branch, double probeFlow)
        {
            var source = branch.Network.Source;
            return Gradient(branch, probeFlow, () => source.Volume);
        }
#endif

        public static Vector3 EffectiveLength(EffectiveLengths el, IMobileNode node, double probeLength)
        {
            var grad = Gradient(node, probeLength, () =>
            {
                el.Propagate(node);
                return el.Value;
            });
            el.Propagate(node);
            return grad;
        }

        public static double EffectiveLength(EffectiveLengths el, Branch branch, double probeFlow)
        {
            var grad = Gradient(branch, probeFlow, () =>
            {
                el.Propagate(branch.Parent);
                return el.Value;
            });
            el.Propagate(branch.Parent);
            return grad;
        }

        public static Vector3 Work(IMobileNode node, double probeLength)
        {
            var source = node.Parent.Branch.Network.Source;
            return Gradient(node, probeLength, () => source.Work);
        }

        public static double Work(Branch branch, double probeFlow)
        {
            var source = branch.Network.Source;
            return Gradient(branch, probeFlow, () => source.Work);
        }

        public static Vector3 Resistance(IMobileNode node, double probeLength)
        {
            var source = node.Parent.Branch.Network.Source;
            return Gradient(node, probeLength, () => source.Resistance);
        }

        public static double Resistance(Branch branch, double probeFlow)
        {
            var source = branch.Network.Source;
            return Gradient(branch, probeFlow, () => source.Resistance);
        }
    }
}
