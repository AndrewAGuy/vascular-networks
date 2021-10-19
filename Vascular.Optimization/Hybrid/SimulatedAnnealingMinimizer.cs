using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Structure;

namespace Vascular.Optimization.Hybrid
{
    /// <summary>
    /// Implementation of simulated annealing with restarts. 
    /// </summary>
    public class SimulatedAnnealingMinimizer
    {
        /// <summary>
        /// Given initial temperature and iteration (0-indexed), returns the block temperature.
        /// </summary>
        public Func<double, int, double> Temperature { get; set; } = 
            (t0, k) => t0 * Math.Min(1.0, 1.0 / Math.Log(k + 1));

        /// <summary>
        /// 
        /// </summary>
        public int IterationsPerBlock { get; set; } = 1000;

        /// <summary>
        /// If no improvement is made in this many iterations, return to the best found so far.
        /// </summary>
        public int IterationsUntilReset { get; set; } = 100;

        /// <summary>
        /// Determines whether positive changes should be accepted given cost change and current temperature.
        /// </summary>
        public Func<double, double, Random, bool> Accept { get; set; } =
            (dC, T, r) => r.NextDouble() < Math.Exp(-dC / T);

        /// <summary>
        /// Acts on a clone of the current network, 
        /// </summary>
        public Action<Network> Perturb { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Func<Network, bool> Admissible { get; set; } = n => true;

        /// <summary>
        /// 
        /// </summary>
        public Random Random { get; set; } = new();

        /// <summary>
        /// 
        /// </summary>
        public Func<Network, double> Cost { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double InitialTemperature { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="network"></param>
        /// <param name="p0"></param>
        /// <param name="maxIterations"></param>
        /// <returns></returns>
        public Network SetInitialTemperatureFromMean(Network network, double p0, int maxIterations = -1)
        {
            var (bestNetwork, bestCost) = (network, this.Cost(network));
            var (currentNetwork, currentCost) = (bestNetwork.Clone(), bestCost);
            var initialCost = currentCost;
            var increases = new List<double>(this.IterationsPerBlock);
            maxIterations = Math.Max(maxIterations, this.IterationsPerBlock);
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
                    if (increases.Count == this.IterationsPerBlock)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="initial"></param>
        /// <param name="blocks"></param>
        /// <returns></returns>
        public Network Optimize(Network initial, int blocks)
        {
            var bestNetwork = initial;
            var bestCost = this.Cost(initial);
            var (currentCost, currentNetwork) = (bestCost, bestNetwork);

            var iterationsSinceImprovement = 0;

            for (var i = 0; i < blocks; ++i)
            {
                var temperature = this.Temperature(this.InitialTemperature, i);
                for (var j = 0; j < this.IterationsPerBlock; ++j)
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
                    if (iterationsSinceImprovement == this.IterationsUntilReset)
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
