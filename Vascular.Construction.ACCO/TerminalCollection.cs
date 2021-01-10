using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO
{
    public class TerminalCollection
    {
        private List<Terminal> waiting = new List<Terminal>();
        private List<Terminal> rejected = new List<Terminal>();
        private List<Terminal> culled = new List<Terminal>();
        private readonly LinkedList<Terminal> built = new LinkedList<Terminal>();

        private Random random = new Random();
        private Network network = null;
        private int rejections = 0;

        public TerminalCollection()
        {

        }

        public TerminalCollection(IEnumerable<Terminal> terminals, Vector3 offset, double flowFactor)
        {
            foreach (var t in terminals)
            {
                Add(new Terminal(t.Position + offset, t.Flow * flowFactor));
            }
        }

        public Terminal Current
        {
            get
            {
                return waiting[^1];
            }
        }

        public int Remaining
        {
            get
            {
                return waiting.Count;
            }
        }

        public IReadOnlyList<Terminal> Waiting
        {
            get
            {
                return waiting;
            }
        }

        public IEnumerable<Terminal> Built
        {
            get
            {
                return built;
            }
        }

        public int Index
        {
            get
            {
                return built.Count;
            }
        }

        public int Culled
        {
            get
            {
                return culled.Count;
            }
        }

        public int Rejected
        {
            get
            {
                return rejected.Count;
            }
        }

        public int Total
        {
            get
            {
                return this.Remaining + this.Processed;
            }
        }

        public int Processed
        {
            get
            {
                return rejected.Count + built.Count + culled.Count;
            }
        }

        public Random Random
        {
            set
            {
                random = value ?? random;
            }
        }

        public Network Network
        {
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

        public void Add(Terminal t)
        {
            waiting.Add(t);
            t.Network = network;
        }

        public void Add(IEnumerable<Terminal> tt)
        {
            waiting.AddRange(tt);
            foreach (var t in tt)
            {
                t.Network = network;
            }
        }

        public void Reorder()
        {
            Reorder(random);
        }

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

        public void Reorder(int[] order)
        {
            var wait = new List<Terminal>(waiting.Count);
            for (var i = 0; i < order.Length; ++i)
            {
                wait.Add(waiting[order[i]]);
            }
            waiting = wait;
        }

        public void CullRejected()
        {
            foreach (var t in rejected)
            {
                Cull(t);
            }
            culled.AddRange(rejected);
            rejected.Clear();
        }

        public void RestoreCulled()
        {
            foreach (var t in culled)
            {
                t.Culled = false;
            }
            waiting.AddRange(culled);
            culled.Clear();
        }

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

        public void ForgetCulled()
        {
            culled.Clear();
        }

        public void Accept()
        {
            rejections = 0;
            built.AddLast(this.Current);
            Advance();
        }

        public void Reject()
        {
            rejections++;
            rejected.Add(this.Current);
            Advance();
        }

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

        public void UpdateBuilt()
        {
            built.Clear();
            UpdateBuilt(network.Source.Child.Branch);
        }

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

        public void RemoveWaiting(Predicate<Terminal> predicate)
        {
            waiting.RemoveAll(predicate);
        }

        public void CullIncompleteAndRejected()
        {
            CullIncomplete();
            CullRejected();
        }

        public Terminal Peek(int offset)
        {
            return waiting[^offset];
        }

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

        private void Cull(Terminal t)
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
    }
}
