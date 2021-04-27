using System.Collections.Generic;
using System.Linq;

namespace Vascular.Construction.LSC
{
    /// <summary>
    /// Represents a sequence of <see cref="LatticeState"/> and moves between them.
    /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        public InitialTerminalOrderingGenerator InitialTerminalOrderingGenerator { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public InitialTerminalPredicate InitialTerminalPredicate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public InitialTerminalCostFunction InitialTerminalCostFunction { get; set; }

        private int generations;
        private int maxGenerations;

        /// <summary>
        /// The network already contains some branches.
        /// </summary>
        public void Initialize()
        {
            state.Initialize();
            generations = 0;
            maxGenerations = state.GenerationsDown;
        }

        /// <summary>
        /// The network contains no branches. Makes the first.
        /// </summary>
        /// <param name="iterations"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Spread, then try to move to a different lattice if needed.
        /// </summary>
        /// <returns>False if no more iterations may be made.</returns>
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

        /// <summary>
        /// Advances until complete.
        /// </summary>
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
                this.Current.Value.OnExit?.Invoke();
                this.Current.Value.BeforeReRefineAction?.Invoke();
                next.Value.Refine(next.Value.ReintroduceDown);
                next.Value.OnEntry?.Invoke();
                next.Value.AfterReRefineAction?.Invoke();
            }
            else
            {
                this.Current.Value.OnExit?.Invoke();
                this.Current.Value.BeforeRefineAction?.Invoke();
                next.Value.Initialize();
                next.Value.OnEntry?.Invoke();
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

            this.Current.Value.OnExit?.Invoke();
            this.Current.Value.BeforeCoarsenAction?.Invoke();
            previous.Value.Coarsen(this.Current.Value, previous.Value.ReintroduceUp);
            previous.Value.OnEntry?.Invoke();
            previous.Value.AfterCoarsenAction?.Invoke();

            this.Current = previous;
            generations = 0;
            maxGenerations = this.Current.Value.GenerationsUp;
        }
    }
}
