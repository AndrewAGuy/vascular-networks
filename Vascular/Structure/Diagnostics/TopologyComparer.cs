using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Diagnostics
{
    /// <summary>
    /// Compares network topologies of canonicalized networks.
    /// </summary>
    public class TopologyComparer : IEqualityComparer<Network>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(Network x, Network y)
        {
            return CompareCanonicalized(x, y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(Network obj)
        {
            return GetHashCode(obj.Root.End);
        }

        private int GetHashCode(BranchNode node)
        {
            if (node is Terminal terminal)
            {
                return this.TerminalEqualityComparer.GetHashCode(terminal);
            }
            else if (node is Bifurcation bifurcation)
            {
                return HashCode.Combine(
                    GetHashCode(bifurcation.Downstream[0].End),
                    GetHashCode(bifurcation.Downstream[1].End));
            }
            else
            {
                var hashCode = new HashCode();
                for (var i = 0; i < node.Downstream.Length; ++i)
                {
                    hashCode.Add(GetHashCode(node.Downstream[i].End));
                }
                return hashCode.ToHashCode();
            }
        }

        private bool CompareCanonicalized(Network x, Network y)
        {
            var Bx = enumeratorX.Downstream(x.Root).GetEnumerator();
            var By = enumeratorY.Downstream(y.Root).GetEnumerator();
            while (true)
            {
                var mx = Bx.MoveNext();
                var my = By.MoveNext();
                switch (mx, my)
                {
                    case (false, false):
                        return true;

                    case (false, true):
                    case (true, false):
                        return false;

                    case (true, true):
                        break;
                }
                var nx = Bx.Current.End;
                var ny = By.Current.End;
                if (nx.GetType() != ny.GetType())
                {
                    return false;
                }
                if (nx is Terminal tx && ny is Terminal ty)
                {
                    if (!this.TerminalEqualityComparer.Equals(tx, ty))
                    {
                        return false;
                    }
                }
            }
        }

        private readonly BranchEnumerator enumeratorX = new(), enumeratorY = new();

        /// <summary>
        /// 
        /// </summary>
        public IEqualityComparer<Terminal> TerminalEqualityComparer { get; set; } = new TerminalCanonicalPositionComparer();
    }
}
