using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Structure;

namespace Vascular.Optimization.Hybrid
{
    public class SimulatedAnnealingMinimizer
    {
        public Func<double, int, double> Temperature { get; set; } = (t0, k) => t0 / Math.Log(k + 1);

        public int Iterations { get; set; } = 1000;

        public int Reset { get; set; } = 100;

        public Func<double, double, Random, bool> Accept { get; set; } =
            (dC, T, r) => r.NextDouble() < Math.Exp(-dC / T);

        public Action<Network> Perturb { get; set; }

        public Func<Network, bool> Admissible { get; set; } = n => true;

        public Random Random { get; set; } = new();

        public Func<Network, double> Cost { get; set; }

        public double InitialTemperature { get; set; }

        public Network SetInitialTemperatureFromMean(Network network, double p0, int maxIterations = -1)
        {
            var (bestNetwork, bestCost) = (network, this.Cost(network));
            var (currentNetwork, currentCost) = (bestNetwork.Clone(), bestCost);
            var initialCost = currentCost;
            var increases = new List<double>(this.Iterations);
            maxIterations = Math.Max(maxIterations, this.Iterations);
            for (var i = 0; i < maxIterations; ++i)
            {
                this.Perturb(currentNetwork);
                var cost = this.Cost(currentNetwork);
                if (cost < bestCost)
                {
                    (bestNetwork, bestCost) = (currentNetwork, cost);
                    currentNetwork = currentNetwork.Clone();
                }
                else if (cost > currentCost)
                {
                    increases.Add(cost - currentCost);
                    if (increases.Count == this.Iterations)
                    {
                        break;
                    }
                }
                currentCost = cost;
            }

            if (increases.Count == 0)
            {
                throw new VascularException("Cannot find an increase in cost");
            }

            this.InitialTemperature = -increases.Sum() / (increases.Count * Math.Log(p0));

            return bestNetwork;
        }

        public Network Optimize(Network initial, int blocks)
        {
            var bestNetwork = initial;
            var bestCost = this.Cost(initial);
            var (currentCost, currentNetwork) = (bestCost, bestNetwork);

            var iterationsSinceImprovement = 0;

            for (var i = 0; i < blocks; ++i)
            {
                var temperature = this.Temperature(this.InitialTemperature, i);
                for (var j = 0; j < this.Iterations; ++j)
                {
                    var clone = currentNetwork.Clone();
                    this.Perturb(clone);
                    if (this.Admissible(clone))
                    {
                        var cost = this.Cost(clone);
                        if (cost <= bestCost)
                        {
                            bestCost = currentCost = cost;
                            bestNetwork = currentNetwork = clone;
                            iterationsSinceImprovement = 0;
                            continue;
                        }
                        var dC = cost - currentCost;
                        if (dC <= 0 || this.Accept(dC, temperature, this.Random))
                        {
                            (currentCost, currentNetwork) = (cost, clone);
                        }
                    }
                    iterationsSinceImprovement++;
                    if (iterationsSinceImprovement == this.Reset)
                    {
                        iterationsSinceImprovement = 0;
                        (currentCost, currentNetwork) = (bestCost, bestNetwork);
                    }
                }
            }

            return bestNetwork;
        }
    }
}
