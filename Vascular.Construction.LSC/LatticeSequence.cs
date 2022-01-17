using System;
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
        /// Instead of registering at the <see cref="LatticeState"/> level, can register callbacks here.
        /// </summary>
        public Action AfterIteration { get; set; }

        /// <summary>
        /// The network already contains some branches.
        /// </summary>
        public void Initialize()
        {
            state.Initialize();
            generations = 0;
            maxGenerations = state.GenerationsDown;
            state.OnEntry?.Invoke();
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

            var result = true;
            if (state.Exterior.Count == 0)
            {
                result = Refine();
            }
            else if (maxGenerations > 0 && generations == maxGenerations)
            {
                Coarsen();
            }

            this.AfterIteration?.Invoke();
            return result;
        }

        /// <summary>
        /// Advances until complete.
        /// </summary>
        public void Complete()
        {
            while (Advance())
            {

            }
            state.OnExit?.Invoke();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Reset()
        {
            var list = currentNode.List;
            foreach (var state in list)
            {
                state.Clear();
            }
            this.Current = list.First;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        public void Apply(Action<LatticeState> action)
        {
            foreach (var state in this.Elements)
            {
                action(state);
            }
        }

        private bool Refine()
        {
            if (currentNode.Next is not LinkedListNode<LatticeState> next)
            {
                return false;
            }

            if (next.Value.IsInitialized)
            {
                state.OnExit?.Invoke();
                state.BeforeReRefineAction?.Invoke();
                next.Value.Refine(next.Value.ReintroduceDown);
                next.Value.OnEntry?.Invoke();
                next.Value.AfterReRefineAction?.Invoke();
            }
            else
            {
                state.OnExit?.Invoke();
                state.BeforeRefineAction?.Invoke();
                next.Value.Initialize();
                next.Value.OnEntry?.Invoke();
                next.Value.AfterRefineAction?.Invoke();
            }

            this.Current = next;
            generations = 0;
            maxGenerations = state.GenerationsDown;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool CanCoarsen { get; set; } = true;

        private void Coarsen()
        {
            if (!this.CanCoarsen ||
                currentNode.Previous is not LinkedListNode<LatticeState> previous)
            {
                return;
            }

            state.OnExit?.Invoke();
            state.BeforeCoarsenAction?.Invoke();
            previous.Value.Coarsen(state, previous.Value.ReintroduceUp);
            previous.Value.OnEntry?.Invoke();
            previous.Value.AfterCoarsenAction?.Invoke();

            this.Current = previous;
            generations = 0;
            maxGenerations = state.GenerationsUp;
        }
    }
}
