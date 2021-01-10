using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vascular.Construction.ACCO.Optimizers;
using Vascular.Construction.ACCO.Selectors;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO
{
    public class SequentialBuilder
    {
        public Selector Selector { get; set; } = new CountedSelector();

        public IBifurcationOptimizer Optimizer { get; set; } = new PassOptimizer();

        public Func<Branch, double> Weighting { get; set; } = b => b.Flow;

        public double ParentMultiplier { get; set; } = 2.0;

        public bool Until(Network network, TerminalCollection terminals, int total)
        {
            if (!Begin(network, terminals))
            {
                return false;
            }

            while (terminals.Built.Count() < total)
            {
                if (!Step(network, terminals))
                {
                    return false;
                }
            }
            return true;
        }

        public bool Next(Network network, TerminalCollection terminals, int steps)
        {
            return Until(network, terminals, terminals.Built.Count() + steps);
        }

        public bool Until(Network network, TerminalCollection terminals, double fraction)
        {
            var total = terminals.Total * fraction;
            return Until(network, terminals, (int)total);
        }

        public bool Next(Network network, TerminalCollection terminals, double fraction)
        {
            var steps = terminals.Total * fraction;
            return Next(network, terminals, (int)steps);
        }

        public bool All(Network network, TerminalCollection terminals)
        {
            return Until(network, terminals, terminals.Total);
        }

        private bool Step(Network network, TerminalCollection terminals)
        {
            while (!Add(network, terminals))
            {
                if (terminals.Remaining == 0)
                {
                    if (!terminals.TryRestoreRejected())
                    {
                        return false;
                    }
                }
            }

            if (terminals.Remaining == 0 && terminals.Rejected != 0)
            {
                if (!terminals.TryRestoreRejected())
                {
                    return false;
                }
            }
            return true;
        }

        private bool Begin(Network network, TerminalCollection terminals)
        {
            if (network.Source.Child == null)
            {
                if (terminals.Remaining == 0)
                {
                    return false;
                }

                var branch = Topology.MakeFirst(network.Source, terminals.Current);
                terminals.Accept();
                branch.UpdateLogical();
                branch.UpdateLengths();
                branch.UpdatePhysicalLocal();
                branch.UpdatePhysicalGlobal();
            }
            return true;
        }

        private bool Add(Network network, TerminalCollection terminals)
        {
            if (terminals.Remaining == 0)
            {
                return false;
            }
            var t = terminals.Current;

            network.Source.SetChildRadii();
            var evaluation = this.Selector.Select(network.Source.Child.Branch, t);
            if (evaluation.Cost < 0 || !evaluation.Suitable)
            {
                terminals.Reject();
                return false;
            }
            terminals.Accept();

            evaluation.Object.Reset();
            var bifurc = Topology.CreateBifurcation(evaluation.Object.Segments[0], t);
            bifurc.UpdateLogicalAndPropagate();
            bifurc.Position = WeightedCentre(bifurc);
            bifurc.UpdatePhysicalAndPropagate();

            this.Optimizer.Optimize(bifurc);
            return true;
        }

        private Vector3 WeightedCentre(Bifurcation bf)
        {
            var weight = this.Weighting(bf.Upstream) * this.ParentMultiplier;
            var sum = bf.Upstream.Start.Position * weight;
            var num = weight;
            foreach (var c in bf.Downstream)
            {
                weight = this.Weighting(c);
                sum += c.End.Position * weight;
                num += weight;
            }
            return sum / num;
        }
    }
}
