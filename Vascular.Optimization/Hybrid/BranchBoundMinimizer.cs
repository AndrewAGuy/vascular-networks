using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Optimization.Geometric;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Diagnostics;

namespace Vascular.Optimization.Hybrid
{
    public class BranchBoundMinimizer
    {
        private class Entry
        {
            public Entry(Network network, double cost)
            {
                this.MinCost = cost;
                this.Optimal = network;
            }

            public int Visits { get; private set; } = 1;
            public double MinCost { get; private set; }
            public Network Optimal { get; private set; }

            public void Add(Network network, double cost)
            {
                this.Visits++;
                if (cost < this.MinCost)
                {
                    this.Optimal = network;
                    this.MinCost = cost;
                }
            }
        }

        private readonly Dictionary<Network, Entry> topologies = new(new TopologyComparer());
        private TerminalPositionComparer terminalComparer = new();
        private readonly BranchEnumerator enumerator = new();

        public int MaxVisits { get; set; } = 5;
        public int MutationDepth { get; set; } = 10;
        public Func<PromoteNode, bool> Predicate { get; set; } = pn => true;

        public async Task Optimize(Network network)
        {
            Clear();

            var current = network.AsEnumerable();
            var nTerminals = enumerator.Terminals(network.Root).Count();
            for (var i = 0; i < this.MutationDepth; ++i)
            {
                await GenerateNetworks(current, nTerminals).RunAsync(async chunk => await ProcessChunk(chunk), this.MaxConcurrency);
            }
        }

        private void Clear()
        {
            topologies.Clear();
        }

        public int NetworksPerChunk { get; set; } = 4;
        public int MaxConcurrency { get; set; } = 8;
        private readonly SemaphoreSlim topologySemaphore = new(1);

        private HashSet<Network> generated;

        private IEnumerable<List<Network>> GenerateNetworks(IEnumerable<Network> current, int nTerminals)
        {
            var total = current.Count() * nTerminals;
            generated = new HashSet<Network>(total, new TopologyComparer());
            var list = new List<Network>(this.NetworksPerChunk);

            foreach (var network in current)
            {
                foreach (var modified in GeneratePromotions(network, this.Predicate))
                {
                    if (generated.Add(modified))
                    {
                        list.Add(modified);
                        if (list.Count == this.NetworksPerChunk)
                        {
                            yield return list;
                            list = new(this.NetworksPerChunk);
                        }
                    }
                }
            }
        }

        private async Task ProcessChunk(List<Network> chunk)
        {
            var costs = new List<double>(chunk.Count);
            foreach (var network in chunk)
            {
                var cost = this.CostGenerator(network);
                var hybridMinimizer = GetMinimizer(network, cost);
                hybridMinimizer.Iterate(0, false, false, false);
                costs.Add(hybridMinimizer.Minimizer.Cost);
            }

            await topologySemaphore.WaitAsync();
            for (var i = 0; i < chunk.Count; ++i)
            {
                var clone = chunk[i];
                var value = costs[i];
                if (topologies.TryGetValue(clone, out var entry))
                {
                    entry.Add(clone, value);
                }
                else
                {
                    topologies[clone] = new(clone, value);
                }
            }
            topologySemaphore.Release();
        }

        public Action<GradientDescentMinimizer> ConfigureGradientDescent { get; set; }
        public Action<HybridMinimizer> ConfigureHybridMinimizer { get; set; }
        public Func<Network, Func<Network, (double, IDictionary<IMobileNode, Vector3>)>> CostGenerator { get; set; }

        private HybridMinimizer GetMinimizer(Network network, Func<Network, (double, IDictionary<IMobileNode, Vector3>)> cost)
        {
            var gd = new GradientDescentMinimizer(network);
            gd.Add(cost);
            this.ConfigureGradientDescent?.Invoke(gd);
            var hm = new HybridMinimizer(network)
            {
                Minimizer = gd,
            };
            this.ConfigureHybridMinimizer?.Invoke(hm);
            return hm;
        }

        private void Optimize(Network clone, HybridMinimizer hybridMinimizer)
        {
            hybridMinimizer.Iterate(0, false, false, false);
            var cost = hybridMinimizer.Minimizer.Cost;
            if (topologies.TryGetValue(clone, out var entry))
            {
                entry.Add(clone, cost);
            }
            else
            {
                topologies[clone] = new(clone, cost);
            }
        }

        private IEnumerable<Network> GeneratePromotions(Network network, Func<PromoteNode, bool> predicate)
        {
            foreach (var branch in enumerator.Downstream(network.Root, false))
            {
                var candidate = new PromoteNode(branch.End);
                if (candidate.IsPermissible() && predicate(candidate))
                {
                    // Make move
                    var clone = network.Clone();
                    var address = Address.Get(branch);
                    var target = Address.Navigate(clone.Root, address);
                    var promotion = new PromoteNode(target.End);
                    promotion.Execute(true, true);

                    // Test if permissible
                    Topology.Canonicalize(clone.Root, terminalComparer);
                    if (topologies.TryGetValue(clone, out var entry))
                    {
                        if (entry.Visits >= this.MaxVisits)
                        {
                            continue;
                        }
                    }

                    yield return clone;
                }
            }
        }
    }
}
