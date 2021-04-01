using System;
using System.Linq;
using Vascular.Construction.ACCO.Optimizers;
using Vascular.Construction.ACCO.Selectors;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO
{
    /// <summary>
    /// Grows a network by attempting to bifurcate into a sequence of <see cref="Terminal"/> nodes, provided by a <see cref="TerminalCollection"/>.
    /// Implements the algorithm described in doi: 10.1109/TBME.2019.2942313
    /// <para>
    ///     For each terminal:
    ///     <list type="number">
    ///         <item> <description> Searches downwards from the root according to the selection policy implemented by <see cref="Selector"/> </description> </item>
    ///         <item> <description> If a suitable candidate is returned, places the bifurcation using a simple weighted mean using 
    ///             <see cref="Weighting"/> and <see cref="ParentMultiplier"/> </description> </item>
    ///         <item> <description> Runs the optimization policy implemented by <see cref="Optimizer"/> </description> </item>
    ///     </list>
    /// </para>
    /// <para>
    ///     Terminates if all remaining terminals fail to produce a candidate bifurcation or all terminals have been built.
    /// </para>
    /// </summary>
    public class SequentialBuilder
    {
        /// <summary>
        /// The selection policy. Recommended: <see cref="CountedSelector"/>.
        /// </summary>
        public Selector Selector { get; set; } = new CountedSelector();

        /// <summary>
        /// The optimization policy. Recommended: <see cref="PassOptimizer"/> (does nothing, use global optimizers in between building chunks instead).
        /// </summary>
        public IBifurcationOptimizer Optimizer { get; set; } = new PassOptimizer();

        /// <summary>
        /// The branch weighting. For a parent node <c>p</c>; children <c>c1, c2</c>; weighting function <c>w</c> and parent multiplier <c>m</c>, 
        /// the bifurcation initial position, <c>b.x</c>, is given by
        /// <code> b.x = (m * w(p) * p.x + w(c1) * c1.x + w(c2) * c2.x) / (m * w(p) + w(c1) + w(c2)) </code>
        /// where positions are the nodes at the opposite ends of the branches from the bifurcation.
        /// </summary>
        public Func<Branch, double> Weighting { get; set; } = b => b.Flow;

        /// <summary>
        /// See <see cref="Weighting"/>.
        /// </summary>
        public double ParentMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Attempts to build terminals from <paramref name="terminals"/> into <paramref name="network"/> until <paramref name="total"/> have been built.
        /// Terminates if <paramref name="total"/> are present, or no more progress can be made.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="terminals"></param>
        /// <param name="total"></param>
        /// <returns>
        ///     <c>True</c> if <paramref name="total"/> terminals have been built. 
        ///     <c>False</c> if the network could not be started or there are no more candidate bifurcations.
        /// </returns>
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

        /// <summary>
        /// Attempts to build the next <paramref name="steps"/> number of terminals from <paramref name="terminals"/> into <paramref name="network"/>.
        /// Terminates if no more progress can be made. Delegates to <see cref="Until(Network, TerminalCollection, int)"/>.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="terminals"></param>
        /// <param name="steps"></param>
        /// <returns>
        ///     <c>True</c> if the requested number of terminals were added. 
        ///     <c>False</c> if the network could not be started or there are no more candidate bifurcations.
        /// </returns>
        public bool Next(Network network, TerminalCollection terminals, int steps)
        {
            return Until(network, terminals, terminals.Built.Count() + steps);
        }

        /// <summary>
        /// Attempts to build from <paramref name="terminals"/> into <paramref name="network"/> until the number
        /// <paramref name="fraction"/> <c>*</c> <paramref name="terminals"/>.Total have been built. 
        /// Delegates to <see cref="Until(Network, TerminalCollection, int)"/>.
        /// Terminates if the given <paramref name="fraction"/> of terminals are built or no more progress can be made.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="terminals"></param>
        /// <param name="fraction"></param>
        /// <returns>
        /// <c>True</c> if the requested fraction of the terminals was built.
        /// <c>False</c> if the network could not be started or there are no more candidate bifurcations.
        /// </returns>
        public bool Until(Network network, TerminalCollection terminals, double fraction)
        {
            var total = terminals.Total * fraction;
            return Until(network, terminals, (int)total);
        }

        /// <summary>
        /// Attempts to build the next <paramref name="fraction"/> of terminals from <paramref name="terminals"/> into <paramref name="network"/>.
        /// Terminates if no more progress can be made. Delegates to <see cref="Next(Network, TerminalCollection, int)"/>.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="terminals"></param>
        /// <param name="fraction"></param>
        /// <returns>
        /// <c>True</c> if the requested fraction of the terminals was built.
        /// <c>False</c> if the network could not be started or there are no more candidate bifurcations.
        /// </returns>
        public bool Next(Network network, TerminalCollection terminals, double fraction)
        {
            var steps = terminals.Total * fraction;
            return Next(network, terminals, (int)steps);
        }

        /// <summary>
        /// Builds all terminals until no more remain or there are no candidate bifurcations.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="terminals"></param>
        /// <returns>
        /// <c>True</c> if all terminals are built.
        /// <c>False</c> if the network could not be started or there are no more candidate bifurcations.
        /// </returns>
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

        private static bool Begin(Network network, TerminalCollection terminals)
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
