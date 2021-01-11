using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Geometry.Lattices;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.LSC
{
    using SingleMap = Dictionary<Vector3, Terminal>;
    using MultipleMap = Dictionary<Vector3, ICollection<Terminal>>;

    public class LatticeState
    {
        public LatticeState(Network network, Lattice lattice)
        {
            this.Network = network;
            this.Lattice = lattice;
            this.Connections = lattice.VoronoiCell.Connections;
            this.ClosestBasisFunction = v => this.Lattice.ClosestVectorBasis(v);
        }

        public Network Network { get; private set; }
        public Lattice Lattice { get; private set; }
        public SingleMap SingleInterior { get; private set; }
        public MultipleMap MultipleInterior { get; private set; }
        public MultipleMap Exterior { get; private set; }
        public Vector3[] Connections { get; private set; }

        public int GenerationsDown { get; set; }
        public int GenerationsUp { get; set; }
        public bool ReintroduceDown { get; set; }
        public bool ReintroduceUp { get; set; }

        public ExteriorOrderingGenerator ExteriorOrderingGenerator { get; set; }
        public ExteriorPredicate ExteriorPredicate { get; set; }
        public TerminalFlowFunction TerminalFlowFunction { get; set; }
        public TerminalPairPredicate TerminalPairPredicate { get; set; }
        public TerminalPairCostFunction TerminalPairCostFunction { get; set; }
        public BifurcationPositionFunction BifurcationPositionFunction { get; set; }
        public TerminalPairBuildAction TerminalPairBuildAction { get; set; }

        public IntermediateAction BeforeSpreadAction { get; set; }
        public IntermediateAction BeforeRefineAction { get; set; }
        public IntermediateAction BeforeCoarsenAction { get; set; }
        public IntermediateAction BeforeReRefineAction { get; set; }
        public IntermediateAction AfterSpreadAction { get; set; }
        public IntermediateAction AfterRefineAction { get; set; }
        public IntermediateAction AfterCoarsenAction { get; set; }
        public IntermediateAction AfterReRefineAction { get; set; }

        public ClosestBasisFunction ClosestBasisFunction { get; set; }

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
                    var t = new Terminal(xT, TF(zT, xT)) { Network = N };
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

        public void Initialize()
        {
            this.SingleInterior = LatticeActions.GetSingleInterior(this.Network.Root, this.ClosestBasisFunction);
            this.Exterior = LatticeActions.GetExterior<List<Terminal>>(this.SingleInterior, this.Connections);
        }

        public void Propagate()
        {
            this.Exterior = this.MultipleInterior != null
                ? LatticeActions.PropagateExterior<List<Terminal>>(this.MultipleInterior, this.Connections, this.Exterior.Keys)
                : LatticeActions.PropagateExterior<List<Terminal>>(this.SingleInterior, this.Connections, this.Exterior.Keys);
        }

        public void Coarsen(LatticeState fine, bool reintroduce)
        {
            var newInterior = LatticeActions.GetMultipleInterior<List<Terminal>>(this.Network.Root, this.ClosestBasisFunction);
            this.Exterior = new MultipleMap(newInterior.Count * this.Connections.Length);
            foreach (var zf in fine.Exterior.Keys)
            {
                var x = fine.Lattice.ToSpace(zf);
                var z = this.ClosestBasisFunction(x);
                LatticeActions.AddExterior<List<Terminal>>(this.Exterior, newInterior, z, this.Connections);
            }
            if (reintroduce)
            {
                if (this.MultipleInterior != null)
                {
                    foreach (var i in this.MultipleInterior.Keys)
                    {
                        if (!newInterior.ContainsKey(i))
                        {
                            LatticeActions.AddExterior<List<Terminal>>(this.Exterior, newInterior, i, this.Connections);
                        }
                    }
                }
                else
                {
                    foreach (var i in this.SingleInterior.Keys)
                    {
                        if (!newInterior.ContainsKey(i))
                        {
                            LatticeActions.AddExterior<List<Terminal>>(this.Exterior, newInterior, i, this.Connections);
                        }
                    }
                }
            }
            this.MultipleInterior = newInterior;
            this.SingleInterior = null;
        }

        public void Refine(bool reintroduce)
        {
            if (this.MultipleInterior != null)
            {
                var oldInterior = this.MultipleInterior.Keys;
                this.MultipleInterior = LatticeActions.GetMultipleInterior<List<Terminal>>(this.Network.Root, this.ClosestBasisFunction);
                LatticeActions.GetDifference<List<Vector3>>(oldInterior, this.MultipleInterior.Keys, out var gained, out var lost);
                this.Exterior = new MultipleMap((gained.Count + lost.Count) * this.Connections.Length);
                foreach (var g in gained)
                {
                    LatticeActions.AddExterior<List<Terminal>>(this.Exterior, this.MultipleInterior, g, this.Connections);
                }
                if (reintroduce)
                {
                    foreach (var l in lost)
                    {
                        LatticeActions.AddExterior<List<Terminal>>(this.Exterior, this.MultipleInterior, l, this.Connections);
                    }
                }
            }
            else
            {
                var oldInterior = this.SingleInterior.Keys;
                this.SingleInterior = LatticeActions.GetSingleInterior(this.Network.Root, this.ClosestBasisFunction);
                LatticeActions.GetDifference<List<Vector3>>(oldInterior, this.SingleInterior.Keys, out var gained, out var lost);
                this.Exterior = new MultipleMap((gained.Count + lost.Count) * this.Connections.Length);
                foreach (var g in gained)
                {
                    LatticeActions.AddExterior<List<Terminal>>(this.Exterior, this.SingleInterior, g, this.Connections);
                }
                if (reintroduce)
                {
                    foreach (var l in lost)
                    {
                        LatticeActions.AddExterior<List<Terminal>>(this.Exterior, this.SingleInterior, l, this.Connections);
                    }
                }
            }
        }

        public bool Remove(Terminal terminal)
        {
            var index = this.ClosestBasisFunction(terminal.Position);
            if (this.MultipleInterior != null)
            {
                return LatticeActions.Remove(this.MultipleInterior, this.Exterior, terminal, index, this.Connections);
            }
            else if (this.SingleInterior != null)
            {
                return LatticeActions.Remove(this.SingleInterior, this.Exterior, terminal, index, this.Connections);
            }
            return false;
        }

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
                var t = new Terminal(x, this.TerminalFlowFunction(z, x))
                {
                    Network = this.Network
                };

                bool p(Terminal T) => this.TerminalPairPredicate(T, t);
                double f(Terminal T) => this.TerminalPairCostFunction(T, t);

                if (!kv.Value.MinSuitable(f, p, out var tt, out var V))
                {
                    continue;
                }

                var bf = Topology.CreateBifurcation(tt.Parent, t);
                bf.Position = this.BifurcationPositionFunction(bf);

                this.TerminalPairBuildAction?.Invoke(tt, t);
                add(z, t);
            }
            Propagate();
            this.AfterSpreadAction?.Invoke();
        }
    }
}
