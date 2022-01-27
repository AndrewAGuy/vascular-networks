using System;
using System.Collections.Generic;
using Vascular.Geometry;
using Vascular.Geometry.Lattices;
using Vascular.Geometry.Lattices.Manipulation;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.LSC
{
    using SingleMap = Dictionary<Vector3, Terminal>;
    using MultipleMap = Dictionary<Vector3, ICollection<Terminal>>;

    /// <summary>
    /// Wraps a lattice, interior and exterior maps, and a set of actions and delegates.
    /// </summary>
    public class LatticeState
    {
        /// <summary>
        /// 
        /// </summary>
        public LatticeState(Network network, Lattice lattice)
        {
            this.Network = network;
            this.Lattice = lattice;
            this.Connections = lattice.VoronoiCell.Connections;
            this.ClosestBasisFunction = this.Lattice.ClosestVectorBasis;
        }

        /// <summary>
        /// 
        /// </summary>
        public Network Network { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public Lattice Lattice { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public SingleMap SingleInterior { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public MultipleMap MultipleInterior { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public MultipleMap Exterior { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public Vector3[] Connections { get; private set; }

        /// <summary>
        /// How many iterations before coarsening, when refined into.
        /// </summary>
        public int GenerationsDown { get; set; }

        /// <summary>
        /// How many iterations before coarsening, when coarsened into.
        /// </summary>
        public int GenerationsUp { get; set; }

        /// <summary>
        /// Whether to reintroduce lost interior vectors on re-refining.
        /// </summary>
        public bool ReintroduceDown { get; set; }

        /// <summary>
        /// Whether to reintroduce lost interior vectors on coarsening.
        /// </summary>
        public bool ReintroduceUp { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ExteriorOrderingGenerator ExteriorOrderingGenerator { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ExteriorPredicate ExteriorPredicate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TerminalFlowFunction TerminalFlowFunction { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TerminalConstructor TerminalConstructor { get; set; } = (x, Q) => new Terminal(x, Q);

        /// <summary>
        /// 
        /// </summary>
        public TerminalPairPredicate TerminalPairPredicate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TerminalPairCostFunction TerminalPairCostFunction { get; set; }

        /// <summary>
        /// Defaults to taking the parent of the end node.
        /// </summary>
        public BifurcationSegmentSelector BifurcationSegmentSelector { get; set; } = (b, t) => b.End.Parent;

        /// <summary>
        /// 
        /// </summary>
        public BifurcationPositionFunction BifurcationPositionFunction { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TerminalPairBuildAction TerminalPairBuildAction { get; set; }

        /// <summary>
        /// Called before each iteration.
        /// </summary>
        public Action BeforeSpreadAction { get; set; }

        /// <summary>
        /// Called before leaving this lattice for a previously unvisited fine lattice.
        /// </summary>
        public Action BeforeRefineAction { get; set; }

        /// <summary>
        /// Called before leaving this lattice for a more coarse one.
        /// </summary>
        public Action BeforeCoarsenAction { get; set; }

        /// <summary>
        /// Called before leaving this lattice for a more fine one, which has already been visited.
        /// </summary>
        public Action BeforeReRefineAction { get; set; }

        /// <summary>
        /// Called after each iteration.
        /// </summary>
        public Action AfterSpreadAction { get; set; }

        /// <summary>
        /// Called when entered into from a more coarse lattice.
        /// </summary>
        public Action AfterRefineAction { get; set; }

        /// <summary>
        /// Called when entered into from a more fine lattice.
        /// </summary>
        public Action AfterCoarsenAction { get; set; }

        /// <summary>
        /// Called when entered into multiple times.
        /// </summary>
        public Action AfterReRefineAction { get; set; }

        /// <summary>
        /// Called before <see cref="AfterRefineAction"/>, <see cref="AfterReRefineAction"/>, <see cref="AfterCoarsenAction"/>.
        /// </summary>
        public Action OnEntry { get; set; }

        /// <summary>
        /// Called before <see cref="BeforeRefineAction"/>, <see cref="BeforeReRefineAction"/>, <see cref="BeforeCoarsenAction"/>.
        /// </summary>
        public Action OnExit { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ClosestBasisFunction ClosestBasisFunction { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public InteriorMode InteriorMode { get; set; } = InteriorMode.Default;

        /// <summary>
        /// If <see cref="InteriorMode"/> is <see cref="InteriorMode.Single"/>, used to convert multiple interiors to single.
        /// Defaults to closest terminal to lattice point.
        /// </summary>
        public InteriorReductionFunction InteriorReductionFunction { get; set; } =
            (z, x, T) => T.ArgMin(t => Vector3.DistanceSquared(t.Position, x));

        private Func<Vector3, ICollection<Terminal>, Terminal> ReductionFunction
            => (z, T) => this.InteriorReductionFunction(z, this.Lattice.ToSpace(z), T);

        /// <summary>
        /// If non-null and interior is multiple, filters elements after acquisition.
        /// </summary>
        public InteriorFilter InteriorFilter { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iterations"></param>
        /// <param name="predicate"></param>
        /// <param name="cost"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public bool Begin(int iterations, InitialTerminalPredicate predicate,
            InitialTerminalCostFunction cost, InitialTerminalOrderingGenerator order)
        {
            var N = this.Network;
            var L = this.Lattice;
            var EP = this.ExteriorPredicate;
            var TF = this.TerminalFlowFunction;
            var xS = N.Source.Position;
            var C = L.VoronoiCell.Connections;
            double f(Vector3 z) => cost(xS, L.ToSpace(z));
            bool p(Vector3 z)
            {
                var x = L.ToSpace(z);
                return EP(z, x) && predicate(xS, x);
            }

            var zS = this.ClosestBasisFunction(xS);
            var E = new HashSet<Vector3>() { zS };
            var U = new HashSet<Vector3>();
            for (var i = 0; i < iterations; ++i)
            {
                if (order(E).MinSuitable(f, p, out var zT, out var V))
                {
                    var xT = L.ToSpace(zT);
                    var Q = TF(zT, xT);
                    var t = this.TerminalConstructor(xT, Q);
                    t.Network = N;
                    Topology.MakeFirst(N.Source, t);
                    return true;
                }

                var EE = new HashSet<Vector3>();
                foreach (var e in E)
                {
                    U.Add(e);
                }
                foreach (var e in E)
                {
                    foreach (var c in C)
                    {
                        var v = e + c;
                        if (!U.Contains(v))
                        {
                            EE.Add(v);
                        }
                    }
                }
                E = EE;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Initialize()
        {
            switch (this.InteriorMode)
            {
                default:
                case InteriorMode.Default:
                    this.SingleInterior = LatticeActions.GetSingleInterior(this.Network.Root, this.ClosestBasisFunction);
                    this.MultipleInterior = null;
                    this.Exterior = LatticeActions.GetExterior<List<Terminal>>(this.SingleInterior, this.Connections);
                    break;

                case InteriorMode.Single:
                    this.SingleInterior = LatticeActions.Reduce(
                        LatticeActions.GetMultipleInterior<List<Terminal>>(this.Network.Root, this.ClosestBasisFunction),
                        this.ReductionFunction);
                    this.MultipleInterior = null;
                    this.Exterior = LatticeActions.GetExterior<List<Terminal>>(this.SingleInterior, this.Connections);
                    break;

                case InteriorMode.Multiple:
                    this.SingleInterior = null;
                    this.MultipleInterior = LatticeActions.GetMultipleInterior<List<Terminal>>(this.Network.Root, this.ClosestBasisFunction);
                    TryFilterInterior();
                    this.Exterior = LatticeActions.GetExterior<List<Terminal>>(this.MultipleInterior, this.Connections);
                    break;
            }
        }

        private void TryFilterInterior()
        {
            if (this.InteriorFilter != null)
            {
                foreach (var (z, T) in this.MultipleInterior)
                {
                    var x = this.Lattice.ToSpace(z);
                    this.InteriorFilter(z, x, T);
                }
            }
        }

        /// <summary>
        /// Tests whether either interior is currently set.
        /// </summary>
        public bool IsInitialized => this.SingleInterior != null || this.MultipleInterior != null;

        /// <summary>
        /// Allows re-use.
        /// </summary>
        public void Clear()
        {
            this.SingleInterior = null;
            this.MultipleInterior = null;
            this.Exterior = null;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Propagate()
        {
            this.Exterior = this.MultipleInterior != null
                ? LatticeActions.PropagateExterior<List<Terminal>>(this.MultipleInterior, this.Connections, this.Exterior.Keys)
                : LatticeActions.PropagateExterior<List<Terminal>>(this.SingleInterior, this.Connections, this.Exterior.Keys);
        }

        /// <summary>
        /// Enter into this lattice from a more refined lattice. 
        /// Results in a multiple map interior unless reduced by specifying <see cref="InteriorMode.Single"/>.
        /// Can be entered into without having been visited before, in which case maintains the exterior from
        /// the more refined lattice, but ignores the reintroduction step.
        /// </summary>
        /// <param name="fine"></param>
        /// <param name="reintroduce"></param>
        public void Coarsen(LatticeState fine, bool reintroduce)
        {
            var (oldSingle, oldMultiple) = (this.SingleInterior, this.MultipleInterior);
            var newMulitple = LatticeActions.GetMultipleInterior<List<Terminal>>(this.Network.Root, this.ClosestBasisFunction);
            this.Exterior = new MultipleMap(newMulitple.Count * this.Connections.Length);

            Action<Vector3> addExterior = null;
            Func<Vector3, bool> inInterior = null;
            IEnumerable<Vector3> oldInterior = null;
            if (this.InteriorMode == InteriorMode.Single)
            {
                this.SingleInterior = LatticeActions.Reduce(newMulitple, this.ReductionFunction);
                this.MultipleInterior = null;

                addExterior = z => LatticeActions.AddExterior<List<Terminal>>(this.Exterior, this.SingleInterior, z, this.Connections);
                inInterior = z => this.SingleInterior.ContainsKey(z);
                oldInterior = (IEnumerable<Vector3>)oldSingle?.Keys ?? oldMultiple?.Keys;
            }
            else
            {
                this.MultipleInterior = newMulitple;
                this.SingleInterior = null;
                TryFilterInterior();

                addExterior = z => LatticeActions.AddExterior<List<Terminal>>(this.Exterior, this.MultipleInterior, z, this.Connections);
                inInterior = z => this.MultipleInterior.ContainsKey(z);
                oldInterior = (IEnumerable<Vector3>)oldMultiple?.Keys ?? oldSingle?.Keys;
            }

            foreach (var zf in fine.Exterior.Keys)
            {
                var x = fine.Lattice.ToSpace(zf);
                var z = this.ClosestBasisFunction(x);
                addExterior(z);
            }
            if (reintroduce && oldInterior != null)
            {
                foreach (var i in oldInterior)
                {
                    if (!inInterior(i))
                    {
                        addExterior(i);
                    }
                }
            }
        }

        /// <summary>
        /// Enters into this lattice from a more coarse one. If <paramref name="reintroduce"/> is true, any interior vectors
        /// that were lost at the more coarse stage are added to the exterior, as we might be able to rebuild them.
        /// </summary>
        /// <param name="reintroduce"></param>
        public void Refine(bool reintroduce)
        {
            var (oldSingle, oldMultiple) = (this.SingleInterior, this.MultipleInterior);

            ICollection<Vector3> oldInterior = null;
            ICollection<Vector3> newInterior = null;
            Action<Vector3> addExterior = null;
            switch (this.InteriorMode)
            {
                default:
                case InteriorMode.Default:
                    if (oldMultiple != null)
                    {
                        goto case InteriorMode.Multiple;
                    }
                    else
                    {
                        this.SingleInterior = LatticeActions.GetSingleInterior(this.Network.Root, this.ClosestBasisFunction);
                        goto PREPARE_SINGLE;
                    }

                case InteriorMode.Single:
                    this.SingleInterior = LatticeActions.Reduce(
                        LatticeActions.GetMultipleInterior<List<Terminal>>(this.Network.Root, this.ClosestBasisFunction),
                        this.ReductionFunction);
                PREPARE_SINGLE:
                    this.MultipleInterior = null;
                    oldInterior = (ICollection<Vector3>)oldSingle?.Keys ?? oldMultiple?.Keys;
                    newInterior = this.SingleInterior.Keys;
                    addExterior = z => LatticeActions.AddExterior<List<Terminal>>(this.Exterior, this.SingleInterior, z, this.Connections);
                    break;

                case InteriorMode.Multiple:
                    this.MultipleInterior = LatticeActions.GetMultipleInterior<List<Terminal>>(this.Network.Root, this.ClosestBasisFunction);
                    this.SingleInterior = null;
                    TryFilterInterior();
                    oldInterior = (ICollection<Vector3>)oldMultiple?.Keys ?? oldSingle?.Keys;
                    newInterior = this.MultipleInterior.Keys;
                    addExterior = z => LatticeActions.AddExterior<List<Terminal>>(this.Exterior, this.MultipleInterior, z, this.Connections);
                    break;
            }

            LatticeActions.GetDifference<List<Vector3>>(oldInterior, newInterior, out var gained, out var lost);
            this.Exterior = new MultipleMap((gained.Count + lost.Count) * this.Connections.Length);
            foreach (var g in gained)
            {
                addExterior(g);
            }
            if (reintroduce)
            {
                foreach (var l in lost)
                {
                    addExterior(l);
                }
            }
        }

        /// <summary>
        /// Remove <paramref name="terminal"/> from the interior. If successful, <paramref name="tryAddExterior"/>
        /// is true and the slot occupied by <paramref name="terminal"/> is empty, add the index back to
        /// <see cref="Exterior"/>.
        /// </summary>
        /// <param name="terminal"></param>
        /// <param name="tryAddExterior"></param>
        /// <returns>True if the terminal was present in the network.</returns>
        public bool Remove(Terminal terminal, bool tryAddExterior = false)
        {
            var index = this.ClosestBasisFunction(terminal.Position);
            var result = false;

            if (this.MultipleInterior != null)
            {
                result = LatticeActions.Remove(this.MultipleInterior, this.Exterior, terminal, index, this.Connections);
            }
            else if (this.SingleInterior != null)
            {
                result = LatticeActions.Remove(this.SingleInterior, this.Exterior, terminal, index, this.Connections);
            }

            if (result && tryAddExterior)
            {
                AddExterior(index);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exterior"></param>
        public void AddExterior(Vector3 exterior)
        {
            if (this.MultipleInterior != null)
            {
                LatticeActions.AddExterior<List<Terminal>>(this.Exterior, this.MultipleInterior, exterior, this.Connections);
            }
            else
            {
                LatticeActions.AddExterior<List<Terminal>>(this.Exterior, this.SingleInterior, exterior, this.Connections);
            }
        }

        /// <summary>
        /// For each element in the exterior, attempts to add into the network.
        /// </summary>
        public void Spread()
        {
            this.BeforeSpreadAction?.Invoke();
            var add = this.MultipleInterior != null
                ? new Action<Vector3, Terminal>((z, t) => this.MultipleInterior[z] = new List<Terminal>(1) { t })
                : new Action<Vector3, Terminal>((z, t) => this.SingleInterior[z] = t);

            var exterior = this.ExteriorOrderingGenerator?.Invoke(this.Exterior) ?? this.Exterior;
            foreach (var kv in exterior)
            {
                var z = kv.Key;
                var x = this.Lattice.ToSpace(z);
                if (!this.ExteriorPredicate(z, x))
                {
                    continue;
                }
                var Q = this.TerminalFlowFunction(z, x);
                var t = this.TerminalConstructor(x, Q);
                t.Network = this.Network;

                bool p(Terminal T) => this.TerminalPairPredicate(T, t);
                double f(Terminal T) => this.TerminalPairCostFunction(T, t);

                if (!kv.Value.MinSuitable(f, p, out var tt, out var V))
                {
                    continue;
                }

                var s = this.BifurcationSegmentSelector(tt.Upstream, t);
                var bf = Topology.CreateBifurcation(s, t);
                bf.Position = this.BifurcationPositionFunction(bf);

                this.TerminalPairBuildAction?.Invoke(tt, t);
                add(z, t);
            }
            Propagate();
            this.AfterSpreadAction?.Invoke();
        }
    }
}
