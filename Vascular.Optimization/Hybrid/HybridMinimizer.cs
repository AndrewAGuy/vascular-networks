using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Geometry.Lattices.Manipulation;
using Vascular.Optimization.Geometric;
using Vascular.Optimization.Topological;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Diagnostics;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Hybrid
{
    /// <summary>
    /// Uses a variety of topological methods in association with a <see cref="GradientDescentMinimizer"/>
    /// to try to find the optimal network structure.
    /// </summary>
    public class HybridMinimizer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="network"></param>
        public HybridMinimizer(Network network)
        {
            this.Network = network;
            this.Minimizer = new GradientDescentMinimizer(this.Network)
            {
                UpdateStrideToTarget = true,
                BlockLength = 10,
                BlockRatio = 0.75
            };
        }

        private readonly HashSet<BranchAction> actions = new();

        /// <summary>
        /// Executes the specified number of iterations of geometric optimization followed by topological.
        /// A final set of geometric optimization is performed afterwards. By default, marks everything as
        /// needing recalculating.
        /// </summary>
        /// <param name="iterations"></param>
        /// <param name="invalidateInterior"></param>
        /// <param name="invalidateGeometry"></param>
        /// <param name="invalidateTopology"></param>
        public void Iterate(int iterations, bool invalidateInterior = true,
            bool invalidateGeometry = true, bool invalidateTopology = true)
        {
            topologyInvalid |= invalidateTopology;
            geometryInvalid |= invalidateGeometry;
            interiorInvalid |= invalidateInterior;
            for (var i = 0; i < iterations; ++i)
            {
                IterateGeometry();
                GetTopology();
                ActTopology();
            }
            IterateGeometry();
        }

        /// <summary>
        /// If true, recalculates the cost at each step of topological optimization using 
        /// <see cref="TopologyExecutor.Iterate(IEnumerable{BranchAction})"/>. For large networks,
        /// can be very expensive.
        /// If false, calculates the cost once and relies on most actions being invalidated before
        /// they get a change to be made, executing using 
        /// <see cref="TopologyExecutor.IterateOrdered(IEnumerable{BranchAction})"/>.
        /// </summary>
        public bool Recost { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        public Func<BranchAction, bool> ActionPredicate { get; set; }

        /// <summary>
        /// Can attach pre and post action hooks with this, as well as combine or overwrite costs and predicates.
        /// </summary>
        public Action<TopologyExecutor> ConfigureExector { get; set; }

        private void ActTopology()
        {
            var executor = new TopologyExecutor()
            {
                PropagateLogical = this.Recost,
                PropagatePhysical = this.Recost,
                TryUpdate = true
            };

            var taken = 0;

            if (this.Recost)
            {
                executor.Predicate = this.ActionPredicate != null
                    ? ba => EstimateChange(ba) < this.CostChangeThreshold && this.ActionPredicate(ba)
                    : ba => EstimateChange(ba) < this.CostChangeThreshold;
                executor.Cost = ba => EstimateChange(ba);
                executor.ContinuationPredicate = () =>
                {
                    foreach (var prepare in prepareEstimators)
                    {
                        prepare();
                    }
                    return true;
                };

                this.ConfigureExector?.Invoke(executor);
                taken = executor.Iterate(actions);
            }
            else
            {
                var ranked = (IEnumerable<BranchAction>)actions;
                if (this.ActionPredicate != null)
                {
                    ranked = ranked.Where(this.ActionPredicate);
                }
                ranked = ranked
                    .Select(a =>
                    {
                        var dC = EstimateChange(a);
                        return (a, dC);
                    })
                    .Where(a => a.dC < this.CostChangeThreshold)
                    .OrderBy(a => a.dC)
                    .Select(a => a.a);

                this.ConfigureExector?.Invoke(executor);
                taken = executor.IterateOrdered(ranked);
            }

            if (taken != 0)
            {
                topologyInvalid = true;
                geometryInvalid = true;
                if (this.ResetStrideOnTopologyChange)
                {
                    this.Minimizer.Stride = 0;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public double EstimateChange(BranchAction action)
        {
            var dC = 0.0;
            foreach (var estimator in topologyEstimators)
            {
                dC += estimator(action);
            }
            return dC;
        }

        private void GetTopology()
        {
            actions.Clear();
            foreach (var prepare in prepareEstimators)
            {
                prepare();
            }
            SetGeometryAndTopology();
            EvaluateTopology();

            if (this.Placement != null)
            {
                foreach (var action in actions)
                {
                    if (action is MoveBifurcation move)
                    {
                        move.Position ??= this.Placement;
                    }
                }
            }
        }

        /// <summary>
        /// Used for <see cref="TryTerminals"/> and <see cref="TryRebalance"/> if <see cref="OffloadRemovedTerminals"/>
        /// is specified.
        /// </summary>
        public ClosestBasisFunction ToIntegral { get; set; }

        /// <summary>
        /// See <see cref="ToIntegral"/>.
        /// </summary>
        public Vector3[] Connections { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Vector3, ICollection<Terminal>> Interior { get; private set; }

        private bool interiorInvalid = true;

        /// <summary>
        /// 
        /// </summary>
        public void SetInterior()
        {
            if (interiorInvalid)
            {
                this.Interior = LatticeActions.GetMultipleInterior<List<Terminal>>(this.Network.Root, this.ToIntegral);
                interiorInvalid = false;
            }
        }

        /// <summary>
        /// The placement function to use for evaluating <see cref="MoveBifurcation"/> actions.
        /// </summary>
        public Func<BranchNode, BranchNode, BranchNode, Vector3> EvaluationPlacement { get; set; }

        /// <summary>
        /// The placement function to give to <see cref="MoveBifurcation"/> actions.
        /// </summary>
        public Func<Bifurcation, Vector3> Placement { get; set; }

        /// <summary>
        /// Candidate actions are thresholded by this. Set to a small negative number to prevent spurious
        /// actions from being made, or a small positive number to allow actions that are beneficial but
        /// not considered so by the estimators to be made.
        /// </summary>
        public double CostChangeThreshold { get; set; }

        /// <summary>
        /// Used for preallocating memory.
        /// </summary>
        public Func<Branch, int> TerminalCountEstimate { get; set; } = b => (int)b.Flow;

        /// <summary>
        /// If non-zero, try to trim short terminals using 
        /// <see cref="Balancing.RemoveShortTerminals(Branch, BranchEnumerator, double, Action{Terminal})"/>.
        /// When terminals are removed through this, they are processed using <see cref="OnTrim"/>, as this
        /// is considered a marginal case of topology change - the optimizer has most likely pulled the other
        /// pathway directly over it and it is almost as though the terminal doesn't need to exist any more,
        /// in comparison to explicity culled terminals.
        /// </summary>
        public double MinTerminalLength { get; set; }

        private void TrimTerminals()
        {
            if (this.MinTerminalLength != 0)
            {
                Balancing.RemoveShortTerminals(this.Network, this.MinTerminalLength, onTrim);
            }
        }

        /// <summary>
        /// More than just defragmenting - completely wipe all transient nodes. Good for performance
        /// and early stage optimizations.
        /// </summary>
        public bool RemoveTransients { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Func<Transient, bool> DefragmentationPredicate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Func<Transient, double> DefragmentationRadius { get; set; }

        private void ModifyTransients()
        {
            if (this.RemoveTransients)
            {
                foreach (var b in this.Branches)
                {
                    if (b.Segments.Count > 1)
                    {
                        b.Reset();
                        geometryInvalid = true;
                    }
                }
            }
            else if (this.DefragmentationPredicate != null)
            {
                this.Network.Source.PropagateRadiiDownstream();
                foreach (var b in this.Branches)
                {
                    geometryInvalid |= Fragmentation.Defragment(b,
                        this.DefragmentationPredicate,
                        this.DefragmentationRadius ?? Fragmentation.MeanRadius);
                }
            }
        }

        /// <summary>
        /// The maximum number of geometry iterations that can be made. Can iterate fewer times
        /// if <see cref="GradientDescentMinimizer.Iterate"/> returns false, see
        /// <see cref="GradientDescentMinimizer.TerminationPredicate"/> and
        /// <see cref="GradientDescentMinimizer.UseConvergenceTest(double, int, int)"/>.
        /// </summary>
        public int GeometryIterations { get; set; }

        /// <summary>
        /// If a topology change was made, reset the stride. Does not count for trimming,
        /// see <see cref="MinTerminalLength"/>.
        /// </summary>
        public bool ResetStrideOnTopologyChange { get; set; } = true;

        /// <summary>
        /// If true, always reset the stride at the start of the geometry iteration block.
        /// </summary>
        public bool ResetStrideAlways { get; set; } = false;

        private void IterateGeometry()
        {
            if (this.ResetStrideAlways)
            {
                this.Minimizer.Stride = 0;
            }
            this.Minimizer.ResetIteration();
            SetGeometryAndTopology();
            UpdateSoftTopology();
            for (var i = 0; i < this.GeometryIterations; ++i)
            {
                ModifyGeomtry();
                SetGeometryAndTopology();
                UpdateSoftTopology();
                var done = this.Minimizer.Iterate();
                TryRecordCost();
                UpdateSoftTopology();
                if (done)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Executed whenever a terminal is explicitly culled. For branches removed
        /// through <see cref="MinTerminalLength"/>, process using <see cref="OnTrim"/>.
        /// For explicit cull, attempting to rebuild may be reasonable.
        /// </summary>
        public Action<Terminal> OnCull
        {
            get => onCull;
            set
            {
                if (value != null)
                {
                    var defaultAction = this.DefaultCullAction;
                    onCull = t =>
                    {
                        value(t);
                        defaultAction(t);
                    };
                }
                else
                {
                    onCull = this.DefaultCullAction;
                }
            }
        }

        private Action<Terminal> onCull;

        /// <summary>
        /// Executed whenever a branch is removed through 
        /// <see cref="Balancing.RemoveShortTerminals(Branch, BranchEnumerator, double, Action{Terminal})"/>.
        /// Attempting a rebuild is likely to lead to the same scenario again, hence the different callbacks offered.
        /// </summary>
        public Action<Terminal> OnTrim
        {
            get => onTrim;
            set
            {
                if (value != null)
                {
                    var defaultAction = this.DefaultCullAction;
                    onTrim = t =>
                    {
                        value(t);
                        defaultAction(t);
                    };
                }
                else
                {
                    onTrim = this.DefaultCullAction;
                }
            }
        }

        private Action<Terminal> onTrim;

        private Action<Terminal> DefaultCullAction => t => interiorInvalid = true;

        private readonly List<Func<IEnumerable<BranchAction>>> topologySource = new();

        private readonly List<Func<BranchAction, double>> topologyEstimators = new();

        /// <summary>
        /// Adds an extra source of topology actions. Common sources might come from <see cref="Global"/>,
        /// or redundancy removal in intersection resolution.
        /// </summary>
        /// <param name="source"></param>
        public HybridMinimizer AddTopologySource(Func<IEnumerable<BranchAction>> source)
        {
            topologySource.Add(source);
            return this;
        }

        /// <summary>
        /// Adds an additional cost change estimator for topology actions.
        /// </summary>
        /// <param name="estimator"></param>
        public HybridMinimizer AddTopologyEstimator(Func<BranchAction, double> estimator)
        {
            topologyEstimators.Add(estimator);
            return this;
        }

        private void EvaluateTopology()
        {
            foreach (var topology in topologySource)
            {
                foreach (var action in topology())
                {
                    actions.Add(action);
                }
            }
        }

        private readonly List<Func<IMobileNode, Vector3>> geometrySource = new();

        /// <summary>
        /// Adds a source of geometry modification. Common sources might come from <see cref="Perturbation"/>.
        /// </summary>
        /// <param name="modifier"></param>
        public HybridMinimizer AddGeometryModifier(Func<IMobileNode, Vector3> modifier)
        {
            geometrySource.Add(modifier);
            return this;
        }

        private void ModifyGeomtry()
        {
            if (geometrySource.Count == 0)
            {
                return;
            }
            //var branches = enumerator.Downstream(this.Network.Root).ToList();
            //var nodes = enumerator.MobileNodes(this.Network.Root);
            foreach (var node in enumerator.MobileNodes(this.Network.Root)
                .Where(n => this.Minimizer.MovingPredicate(n)))
            {
                foreach (var geometry in geometrySource)
                {
                    node.Position += geometry(node);
                }
            }
            geometryInvalid = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public Network Network { get; }

        /// <summary>
        /// 
        /// </summary>
        public GradientDescentMinimizer Minimizer { get; set; }

        private readonly List<Action> prepareEstimators = new();

        private readonly List<Func<Network, double>> costs = new();

        /// <summary>
        /// Adds an additional cost that is not considered by <see cref="Minimizer"/>.
        /// </summary>
        /// <param name="cost"></param>
        public HybridMinimizer AddCost(Func<Network, double> cost)
        {
            costs.Add(cost);
            return this;
        }

        /// <summary>
        /// Before estimators are used, these actions are executed to ensure that they will
        /// work as expected.
        /// </summary>
        /// <param name="prepare"></param>
        public HybridMinimizer AddEstimatorPrepare(Action prepare)
        {
            prepareEstimators.Add(prepare);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public Action<double> RecordCost { get; set; }

        private void TryRecordCost()
        {
            if (this.RecordCost != null)
            {
                var cost = this.Minimizer.Cost;
                foreach (var extra in costs)
                {
                    cost += extra(this.Network);
                }
                this.RecordCost(cost);
            }
        }

        private readonly BranchEnumerator enumerator = new();

        /// <summary>
        /// Uses <see cref="BranchEnumerator.Downstream(Branch, bool)"/> - don't call in nested loops.
        /// </summary>
        public IEnumerable<Branch> Branches => enumerator.Downstream(this.Network.Root, true);

        private bool geometryInvalid;
        private bool topologyInvalid;

        private void UpdateSoftTopology()
        {
            TrimTerminals();
            ModifyTransients();
        }

        private void SetGeometryAndTopology()
        {
            if (topologyInvalid)
            {
                this.Network.Root.SetLogical();
                topologyInvalid = false;
                geometryInvalid = true;
            }
            if (geometryInvalid)
            {
                this.Network.Source.CalculatePhysical();
                geometryInvalid = false;
            }
        }
    }
}
