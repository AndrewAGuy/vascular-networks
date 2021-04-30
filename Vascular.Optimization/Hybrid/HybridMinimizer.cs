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

        public void Iterate(int iterations)
        {
            for (var i = 0; i < iterations; ++i)
            {
                IterateGeometry();
                GetTopology();
                ActTopology();
            }
            IterateGeometry();
        }

        private void ActTopology()
        {
            foreach (var prepare in prepareEstimators)
            {
                prepare();
            }

            var executor = new TopologyExecutor()
            {
                PropagateLogical = false,
                PropagatePhysical = false,
                TryUpdate = true
            };

            var ranked = actions
                .Select(a =>
                {
                    var dC = 0.0;
                    foreach (var estimator in topologyEstimators)
                    {
                        dC += estimator(a);
                    }
                    return (a, dC);
                })
                .Where(a => a.dC < this.CostChangeThreshold)
                .OrderBy(a => a.dC)
                .Select(a => a.a);

            var taken = executor.IterateOrdered(ranked);
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

        private void GetTopology()
        {
            actions.Clear();
            SetGeometryAndTopology();
            GetRegrouping();
            GetRebalancing();
            GetPromotions();
            GetTerminalActions();
            GetExtraTopology();
        }

        public ClosestBasisFunction ToIntegral { get; set; }
        public Vector3[] Connections { get; set; }
        private Dictionary<Vector3, ICollection<Terminal>> interior;
        private bool interiorInvalid;

        private void SetInterior()
        {
            if (interiorInvalid)
            {
                interior = LatticeActions.GetMultipleInterior<List<Terminal>>(this.Network.Root, this.ToIntegral);
                interiorInvalid = false;
            }
        }

        public bool TryRegroup { get; set; }
        public double UnitLength { get; set; }
        public double MinLengthRatio { get; set; }
        public double MaxLengthRatio { get; set; }
        public bool OnlyRegroupEndpoints { get; set; } = false;
        public bool RegroupMultipleCandidates { get; set; } = false;
        public Func<BranchNode, BranchNode, BranchNode, Vector3> EvaluationPlacement { get; set; }
        public double CostChangeThreshold { get; set; }

        private void GetRegrouping()
        {
            if (this.TryRegroup)
            {
                foreach (var (parent, endpoints) in Grouping.LengthFlowRatioDownstream(
                    this.Network.Root, this.UnitLength, this.MinLengthRatio))
                {
                    var permissible = Grouping.PermissibleActions(endpoints, parent, this.OnlyRegroupEndpoints);
                    if (this.RegroupMultipleCandidates)
                    {
                        foreach (var action in permissible)
                        {
                            actions.Add(action);
                        }
                    }
                    else
                    {
                        if (permissible.ArgMin(a =>
                            {
                                var dC = 0.0;
                                foreach (var estimator in topologyEstimators)
                                {
                                    dC += estimator(a);
                                }
                                return dC;
                            }, out var optimal, out var dC) &&
                            dC < this.CostChangeThreshold)
                        {
                            actions.Add(optimal);
                        }
                    }
                }
            }
        }

        public bool TryRebalance { get; set; }
        public double FlowRatio { get; set; }
        public double RadiusRatio { get; set; }
        public bool AllowRebalanceRemoval { get; set; } = false;
        public bool OffloadRemovedTerminals { get; set; } = true;

        public Func<Branch, int> TerminalCountEstimate { get; set; } = b => (int)b.Flow;

        private void GetRebalancing()
        {
            if (this.TryRebalance)
            {
                if (this.AllowRebalanceRemoval)
                {
                    SetInterior();
                }
                var rqRatio = Math.Pow(this.RadiusRatio, 4);
                foreach (var b in this.Branches)
                {
                    var rebalance = Balancing.BifurcationRatio(b, this.FlowRatio, rqRatio);
                    if (rebalance == null &&
                        Balancing.LengthFlowRatio(b, this.UnitLength, this.MaxLengthRatio))
                    {
                        rebalance = new RemoveBranch(b);
                    }

                    if (rebalance is RemoveBranch remove && this.AllowRebalanceRemoval)
                    {
                        ProcessRemoval(remove);
                    }
                    else if (rebalance != null)
                    {
                        actions.Add(rebalance);
                    }
                }
            }
        }

        private void ProcessRemoval(RemoveBranch remove)
        {
            if (this.OffloadRemovedTerminals)
            {
                var offload = Balancing.OffloadTerminals(
                    this.Network.Root, remove.A, interior, this.ToIntegral, this.Connections,
                    br => Terminal.GetDownstream(br, this.TerminalCountEstimate(br)),
                    this.TryLocalTerminalActions);
                foreach (var action in offload.Where(o => o.IsPermissible()))
                {
                    actions.Add(action);
                }
            }
            else
            {
                actions.Add(remove);
                remove.OnCull = onCull;
            }
        }

        public bool TryTerminals { get; set; }
        public double MinTerminalLength { get; set; }
        public bool TryLocalTerminalActions { get; set; } = false;
        public Func<Terminal, IEnumerable<Branch>> TerminalActionExpansion { get; set; }

        private void GetTerminalActions()
        {
            if (this.TryTerminals)
            {
                SetInterior();
                var terminalActions = Balancing.TerminalActions(
                    this.Network.Root, interior, this.ToIntegral, this.Connections,
                    this.TryLocalTerminalActions, this.TerminalActionExpansion);
                foreach (var action in terminalActions.Where(a => a.IsPermissible()))
                {
                    actions.Add(action);
                }
            }
        }

        private void TrimTerminals()
        {
            if (this.MinTerminalLength != 0)
            {
                Balancing.RemoveShortTerminals(this.Network, this.MinTerminalLength, onCull);
            }
        }

        public bool TryDefragment { get; set; }
        public bool RemoveTransients { get; set; }
        public double DeviationRatio { get; set; }
        public double CaptureFraction { get; set; }

        private void ModifyTransients()
        {
            this.Network.Source.PropagateRadiiDownstream();
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
            else if (this.TryDefragment)
            {
                var defrag = Fragmentation.DeviationOrTouching(this.DeviationRatio, this.CaptureFraction);
                var newRadius = Fragmentation.BranchRadius;
                foreach (var b in this.Branches)
                {
                    geometryInvalid |= Fragmentation.Defragment(b, defrag, newRadius);
                }
            }
        }

        public bool TryPromote { get; set; }

        private void GetPromotions()
        {
            foreach (var p in Grouping.Promotions(this.Branches))
            {
                actions.Add(p);
            }
        }

        public int GeometryIterations { get; set; }
        public bool ResetStrideOnTopologyChange { get; set; } = true;
        public bool ResetStrideAlways { get; set; } = false;

        private void IterateGeometry()
        {
            if (this.ResetStrideAlways)
            {
                this.Minimizer.Stride = 0;
            }
            UpdateSoftTopology();
            for (var i = 0; i < this.GeometryIterations; ++i)
            {
                ModifyGeomtry();
                SetGeometryAndTopology();
                this.Minimizer.Iterate();
                RecordCost();
                UpdateSoftTopology();
            }
        }

        public Func<Branch, Branch, bool> Priority { get; set; }

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
        private Action<Terminal> DefaultCullAction => t => interiorInvalid = true;

        private readonly List<Func<Network, IEnumerable<BranchAction>>> topologySource = new();

        private readonly List<Func<BranchAction, double>> topologyEstimators = new();

        public void AddTopologySource(Func<Network, IEnumerable<BranchAction>> source)
        {
            topologySource.Add(source);
        }

        public void AddTopologyEstimator(Func<BranchAction, double> estimator)
        {
            topologyEstimators.Add(estimator);
        }

        private void GetExtraTopology()
        {
            foreach (var topology in topologySource)
            {
                foreach (var action in topology(this.Network))
                {
                    actions.Add(action);
                }
            }
        }

        private readonly List<Func<IMobileNode, Vector3>> geometrySource = new();

        public void AddGeometryModifier(Func<IMobileNode, Vector3> modifier)
        {
            geometrySource.Add(modifier);
        }

        private void ModifyGeomtry()
        {
            if (geometrySource.Count == 0)
            {
                return;
            }
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

        public Network Network { get; }
        public GradientDescentMinimizer Minimizer { get; set; }

        private readonly List<Action> prepareEstimators = new();

        private readonly List<Func<Network, double>> costs = new();

        public void AddCost(Func<Network, double> cost)
        {
            costs.Add(cost);
        }

        public void AddEstimatorPrepare(Action prepare)
        {
            prepareEstimators.Add(prepare);
        }

        public void AddHierarchicalCosts(HierarchicalCosts costs)
        {
            this.Minimizer.Add(n => costs.Evaluate());
            topologyEstimators.Add(t => Grouping.EstimateCostChange(t, costs, this.EvaluationPlacement));
            AddEstimatorPrepare(() => costs.SetCache());
        }

        public Action<double> LogCost { get; set; }

        private void RecordCost()
        {
            if (this.LogCost != null)
            {
                var cost = this.Minimizer.Cost;
                foreach (var extra in costs)
                {
                    cost += extra(this.Network);
                }
                this.LogCost(cost);
            }
        }

        private readonly BranchEnumerator enumerator = new();

        private IEnumerable<Branch> Branches => enumerator.Downstream(this.Network.Root, true);

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
            }
            if (geometryInvalid)
            {
                this.Network.Source.CalculatePhysical();
                geometryInvalid = false;
            }
        }
    }
}
