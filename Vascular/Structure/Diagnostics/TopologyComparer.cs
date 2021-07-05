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
                return GetHashCode(bifurcation.Downstream[0].End) 
                    ^ GetHashCode(bifurcation.Downstream[1].End);
            }
            else
            {
                var hashCode = GetHashCode(node.Downstream[0].End);
                for (var i = 1; i < node.Downstream.Length; ++i)
                {
                    hashCode ^= GetHashCode(node.Downstream[i].End);
                }
                return hashCode;
            }
        }

        private bool CompareCanonicalized(Network x, Network y)
        {
            var Tx = enumeratorX.Terminals(x.Root);
            var Ty = enumeratorY.Terminals(y.Root);
            return Tx.SequenceEqual(Ty, this.TerminalEqualityComparer);
        }

        private readonly BranchEnumerator enumeratorX = new(), enumeratorY = new();

        /// <summary>
        /// 
        /// </summary>
        public IEqualityComparer<Terminal> TerminalEqualityComparer { get; set; } = new TerminalPositionComparer();
    }
}
