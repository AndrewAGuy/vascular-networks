using System;
using System.Collections.Generic;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO
{
    /// <summary>
    /// Represents the sequence of <see cref="Terminal"/> to be built.
    /// </summary>
    public class TerminalCollection
    {
        private List<Terminal> waiting = new List<Terminal>();
        private List<Terminal> rejected = new List<Terminal>();
        private List<Terminal> culled = new List<Terminal>();
        private readonly LinkedList<Terminal> built = new LinkedList<Terminal>();

        private Random random = new Random();
        private Network network = null;
        private int rejections = 0;

        /// <summary>
        /// 
        /// </summary>
        public TerminalCollection()
        {

        }

        /// <summary>
        /// Creates a copy of the reference sequence of terminals, with the given offset and flow multiplier.
        /// </summary>
        /// <param name="terminals"></param>
        /// <param name="offset"></param>
        /// <param name="flowFactor"></param>
        public TerminalCollection(IEnumerable<Terminal> terminals, Vector3 offset, double flowFactor)
        {
            foreach (var t in terminals)
            {
                Add(new Terminal(t.Position + offset, t.Flow * flowFactor));
            }
        }

        /// <summary>
        /// The next terminal that is to be built.
        /// </summary>
        public Terminal Current => waiting[^1];

        /// <summary>
        /// The number of terminals waiting to be built.
        /// </summary>
        public int Remaining => waiting.Count;

        /// <summary>
        /// A copy of the waiting list, for peeking.
        /// </summary>
        public IReadOnlyList<Terminal> Waiting => waiting;

        /// <summary>
        /// The terminals that have been built. Note that some of these may be flagged as culled and invalid, waiting removal.
        /// See <see cref="RemoveCulled(bool)"/>.
        /// </summary>
        public IEnumerable<Terminal> Built => built;

        /// <summary>
        /// The number of built terminals so far.
        /// </summary>
        public int Index => built.Count;

        /// <summary>
        /// The number of culled terminals.
        /// </summary>
        public int Culled => culled.Count;

        /// <summary>
        /// The number of rejected terminals.
        /// </summary>
        public int Rejected => rejected.Count;

        /// <summary>
        /// The number of terminals that are in the system.
        /// </summary>
        public int Total => this.Remaining + this.Processed;

        /// <summary>
        /// The number of terminals not in <see cref="Waiting"/>.
        /// </summary>
        public int Processed => rejected.Count + built.Count + culled.Count;

        /// <summary>
        /// The random number generator for reordering (see <see cref="Reorder()"/> and overloads).
        /// </summary>
        public Random Random
        {
            set => random = value ?? random;
        }

        /// <summary>
        /// The network associated with this collection.
        /// </summary>
        public Network Network
        {
            get => network;
            set
            {
                network = value;
                foreach (var t in waiting)
                {
                    t.Network = network;
                }
                foreach (var t in built)
                {
                    t.Network = network;
                }
                foreach (var t in rejected)
                {
                    t.Network = network;
                }
                foreach (var t in culled)
                {
                    t.Network = network;
                }
            }
        }

        /// <summary>
        /// Add a terminal to the waiting list.
        /// </summary>
        /// <param name="t"></param>
        public void Add(Terminal t)
        {
            waiting.Add(t);
            t.Network = network;
        }

        /// <summary>
        /// Add a range of terminals to the waiting list, in order.
        /// </summary>
        /// <param name="tt"></param>
        public void Add(IEnumerable<Terminal> tt)
        {
            waiting.AddRange(tt);
            foreach (var t in tt)
            {
                t.Network = network;
            }
        }

        /// <summary>
        /// Uses <see cref="Random"/>.
        /// </summary>
        public void Reorder()
        {
            Reorder(random);
        }

        /// <summary>
        /// Permutes <see cref="Waiting"/> using the given <see cref="System.Random"/>.
        /// </summary>
        /// <param name="r"></param>
        public void Reorder(Random r)
        {
            for (var i = waiting.Count - 1; i > 0; --i)
            {
                var swap = r.Next(i + 1);
                var temp = waiting[i];
                waiting[i] = waiting[swap];
                waiting[swap] = temp;
            }
        }

        /// <summary>
        /// Permutes <see cref="Waiting"/> using the given permutation <paramref name="order"/>.
        /// </summary>
        /// <param name="order">Must be a valid permutation: this is not checked.</param>
        public void Reorder(int[] order)
        {
            var wait = new List<Terminal>(waiting.Count);
            for (var i = 0; i < order.Length; ++i)
            {
                wait.Add(waiting[order[i]]);
            }
            waiting = wait;
        }

        /// <summary>
        /// Moves all rejected terminals to culled, including all matched partner terminals.
        /// </summary>
        public void CullRejected()
        {
            foreach (var t in rejected)
            {
                Cull(t);
            }
            culled.AddRange(rejected);
            rejected.Clear();
        }

        /// <summary>
        /// Adds all culled terminals to <see cref="Waiting"/>.
        /// </summary>
        public void RestoreCulled()
        {
            foreach (var t in culled)
            {
                t.Culled = false;
            }
            waiting.AddRange(culled);
            culled.Clear();
        }

        /// <summary>
        /// Adds all culled terminals satisfying <paramref name="predicate"/> to <see cref="Waiting"/>.
        /// </summary>
        /// <param name="predicate"></param>
        public void RestoreCulled(Predicate<Terminal> predicate)
        {
            var cull = new List<Terminal>(culled.Count);
            for (var i = culled.Count - 1; i >= 0; --i)
            {
                if (predicate(culled[i]))
                {
                    culled[i].Culled = false;
                    waiting.Add(culled[i]);
                }
                else
                {
                    cull.Add(culled[i]);
                }
            }
            culled = cull;
        }

        /// <summary>
        /// If a terminal has been culled, either directly or through a matching group, it may be present in any of the other collections.
        /// Always removes from <see cref="Built"/>. If <paramref name="all"/> specified, prevents them from showing up again 
        /// by removing from the waiting and rejected lists.
        /// </summary>
        /// <param name="all"></param>
        public void RemoveCulled(bool all = true)
        {
            var n = built.First;
            while (n != null)
            {
                var nn = n.Next;
                if (n.Value.Culled)
                {
                    culled.Add(n.Value);
                    built.Remove(n);
                }
                n = nn;
            }
            if (all)
            {
                var wait = new List<Terminal>(waiting.Count);
                for (var i = waiting.Count - 1; i >= 0; --i)
                {
                    if (waiting[i].Culled)
                    {
                        culled.Add(waiting[i]);
                    }
                    else
                    {
                        wait.Add(waiting[i]);
                    }
                }
                waiting = wait;
                var reject = new List<Terminal>(rejected.Count);
                for (var i = rejected.Count - 1; i >= 0; --i)
                {
                    if (rejected[i].Culled)
                    {
                        culled.Add(rejected[i]);
                    }
                    else
                    {
                        reject.Add(rejected[i]);
                    }
                }
                rejected = reject;
            }
        }

        /// <summary>
        /// Clears the culled list, never to be restored.
        /// </summary>
        public void ForgetCulled()
        {
            culled.Clear();
        }

        /// <summary>
        /// Resets the rejection counter, moves <see cref="Current"/> to <see cref="Built"/>.
        /// </summary>
        public void Accept()
        {
            rejections = 0;
            built.AddLast(this.Current);
            Advance();
        }

        /// <summary>
        /// Increments the rejection counter, moves <see cref="Current"/> to rejected.
        /// </summary>
        public void Reject()
        {
            rejections++;
            rejected.Add(this.Current);
            Advance();
        }

        /// <summary>
        /// If the number of sequential rejections is equal to the number of total rejections, we have no viable candidates.
        /// Otherwise, we can restore the rejected terminals to <see cref="Waiting"/> and possibly reorder.
        /// </summary>
        /// <param name="reorder"></param>
        /// <returns>
        ///     <c>True</c> if there is a possibility of finding a candidate.
        ///     <c>False</c> if construction must stop.
        /// </returns>
        public bool TryRestoreRejected(bool reorder = false)
        {
            if (rejected.Count == rejections)
            {
                return false;
            }
            if (reorder)
            {
                waiting.AddRange(rejected);
                Reorder();
            }
            else
            {
                for (var i = rejected.Count - 1; i > 0; --i)
                {
                    // Waiting is used as stack, so add most recent rejections first to cycle
                    waiting.Add(rejected[i]);
                }
            }
            rejected.Clear();
            rejections = 0;

            return true;
        }

        /// <summary>
        /// Search the <see cref="Network"/> for terminals.
        /// </summary>
        public void UpdateBuilt()
        {
            built.Clear();
            UpdateBuilt(network.Source.Child.Branch);
        }

        /// <summary>
        /// If any member of a <see cref="Terminal.Partners"/> has not been built, remove all of them.
        /// </summary>
        public void CullIncomplete()
        {
            foreach (var t in built)
            {
                foreach (var p in t.Partners)
                {
                    if (p.Parent == null)
                    {
                        Cull(t);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Filters the waiting list by removing those which satisfy the given <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate"></param>
        public void RemoveWaiting(Predicate<Terminal> predicate)
        {
            waiting.RemoveAll(predicate);
        }

        /// <summary>
        /// See <see cref="CullIncomplete"/>, <see cref="CullRejected"/>.
        /// </summary>
        public void CullIncompleteAndRejected()
        {
            CullIncomplete();
            CullRejected();
        }

        /// <summary>
        /// Peeks into the terminal waiting list.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public Terminal Peek(int offset)
        {
            return waiting[^offset];
        }

        /// <summary>
        /// Deconstructs all terminals and then adds everything back into <see cref="Waiting"/>.
        /// </summary>
        public void Reset()
        {
            // Take all culled and rejected terminals and add into build order
            RestoreCulled();
            TryRestoreRejected();
            // Take all built terminals and wipe them, place back in waiting
            foreach (var b in built)
            {
                b.Parent = null;
            }
            waiting.AddRange(built);
            built.Clear();
        }

        private void UpdateBuilt(Branch b)
        {
            if (b.End is Terminal t)
            {
                built.AddLast(t);
            }
            else
            {
                foreach (var c in b.Children)
                {
                    UpdateBuilt(c);
                }
            }
        }

        private void Advance()
        {
            waiting.RemoveAt(waiting.Count - 1);
        }

        private static void Cull(Terminal t)
        {
            if (t.Partners != null)
            {
                foreach (var p in t.Partners)
                {
                    var trans = Topology.CullTerminal(p);
                    if (trans != null)
                    {
                        trans.Parent.Branch.PropagateLogicalUpstream();
                        trans.UpdatePhysicalAndPropagate();
                    }
                }
            }
            else
            {
                var trans = Topology.CullTerminal(t);
                if (trans != null)
                {
                    trans.Parent.Branch.PropagateLogicalUpstream();
                    trans.UpdatePhysicalAndPropagate();
                }
            }
        }

        /// <summary>
        /// For a number of <see cref="TerminalCollection"/> <paramref name="collections"/>, creates a set <see cref="Terminal.Partners"/>
        /// by taking each element of <see cref="Waiting"/> in turn. Optionally shuffles each one afterwards.
        /// </summary>
        /// <param name="collections"></param>
        /// <param name="reorder"></param>
        public static void Match(TerminalCollection[] collections, bool reorder)
        {
            var n = collections[0].Waiting.Count;
            for (var i = 0; i < n; ++i)
            {
                var T = new Terminal[collections.Length];
                for (var j = 0; j < collections.Length; ++j)
                {
                    T[j] = collections[j].Waiting[i];
                    collections[j].Waiting[i].Partners = T;
                }
            }
            if (reorder)
            {
                foreach (var c in collections)
                {
                    c.Reorder();
                }
            }
        }
    }
}
