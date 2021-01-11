using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vascular.Construction.LSC
{
    public class LatticeSequence
    {
        private LinkedListNode<LatticeState> currentNode;
        private LatticeState state;
        private LinkedListNode<LatticeState> Current
        {
            get => currentNode;
            set
            {
                currentNode = value;
                state = currentNode.Value;
            }
        }
        public IEnumerable<LatticeState> Elements
        {
            get => this.Current.List;
            set
            {
                if (value != null && value.Any())
                {
                    this.Current = value is LinkedList<LatticeState> list
                        ? list.First : new LinkedList<LatticeState>(value).First;
                }
            }
        }

        public InitialTerminalOrderingGenerator InitialTerminalOrderingGenerator { get; set; }
        public InitialTerminalPredicate InitialTerminalPredicate { get; set; }
        public InitialTerminalCostFunction InitialTerminalCostFunction { get; set; }

        private int generations;
        private int maxGenerations;

        public void Initialize()
        {
            state.Initialize();
            generations = 0;
            maxGenerations = state.GenerationsDown;
        }

        public bool Begin(int iterations)
        {
            var b = state.Begin(iterations, this.InitialTerminalPredicate,
                this.InitialTerminalCostFunction, this.InitialTerminalOrderingGenerator);
            if (b)
            {
                Initialize();
            }
            return b;
        }

        public bool Advance()
        {
            state.Spread();
            ++generations;

            if (state.Exterior.Count == 0)
            {
                return Refine();
            }

            if (maxGenerations > 0 && generations == maxGenerations)
            {
                Coarsen();
            }
            return true;
        }

        public void Complete()
        {
            while (Advance())
            {

            }
        }

        private bool Refine()
        {
            if (this.Current.Next is not LinkedListNode<LatticeState> next)
            {
                return false;
            }

            if (next.Value.SingleInterior != null || next.Value.MultipleInterior != null)
            {
                this.Current.Value.BeforeReRefineAction?.Invoke();
                next.Value.Refine(next.Value.ReintroduceDown);
                next.Value.AfterReRefineAction?.Invoke();
            }
            else
            {
                this.Current.Value.BeforeRefineAction?.Invoke();
                next.Value.Initialize();
                next.Value.AfterRefineAction?.Invoke();
            }

            this.Current = next;
            generations = 0;
            maxGenerations = this.Current.Value.GenerationsDown;
            return true;
        }

        private void Coarsen()
        {
            if (this.Current.Previous is not LinkedListNode<LatticeState> previous)
            {
                return;
            }

            this.Current.Value.BeforeCoarsenAction?.Invoke();
            previous.Value.Coarsen(this.Current.Value, previous.Value.ReintroduceUp);
            previous.Value.AfterCoarsenAction?.Invoke();

            this.Current = previous;
            generations = 0;
            maxGenerations = this.Current.Value.GenerationsUp;
        }
    }
}
