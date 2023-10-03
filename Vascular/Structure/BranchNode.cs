using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Structure.Nodes;

namespace Vascular.Structure
{
    /// <summary>
    /// A specialization of <see cref="INode"/> that links <see cref="Branch"/> instances. 
    /// These are responsible for propagating data up and down the tree.
    /// </summary>
    [DataContract]
    [KnownType(typeof(RadiusSource))]
    [KnownType(typeof(PressureSource))]
    [KnownType(typeof(Bifurcation))]
    [KnownType(typeof(Terminal))]
    public abstract class BranchNode : INode
    {
#if !NoPressure
        /// <summary>
        /// The pressure at a node.
        /// </summary>
        public abstract double Pressure { get; }

        /// <summary>
        /// Calculates pressures in a downwards pass.
        /// </summary>
        public abstract void CalculatePressures();
#endif

#if !NoDepthPathLength
        /// <summary>
        /// The sum of all <see cref="Branch.Length"/> down to this node.
        /// </summary>
        public abstract double PathLength { get; }

        /// <summary>
        /// The number of <see cref="Branch"/> instances between this and a <see cref="Source"/> node.
        /// </summary>
        public abstract int Depth { get; }

        /// <summary>
        /// Updates in a downwards pass.
        /// </summary>
        public abstract void CalculatePathLengthsAndDepths();

        /// <summary>
        /// Sets <see cref="Depth"/> to Strahler order.
        /// </summary>
        public abstract void CalculatePathLengthsAndOrder();
#endif

        /// <inheritdoc/>
        public abstract Segment Parent { get; set; }

        /// <inheritdoc/>
        public abstract Segment[] Children { get; }

        /// <inheritdoc/>
        public abstract Vector3 Position { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public abstract Branch Upstream { get; }

        /// <summary>
        /// 
        /// </summary>
        public abstract Branch[] Downstream { get; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public Network Network { get; set; } = null;

        /// <summary>
        /// The sum of downstream flows.
        /// </summary>
        public abstract double Flow { get; }

#if !NoEffectiveLength
        /// <summary>
        /// See <see cref="Branch.EffectiveLength"/> and DOI: 10.1109/TBME.2019.2942313 for the update rules.
        /// </summary>
        public abstract double EffectiveLength { get; }
#endif

        /// <summary>
        /// Combined at splitting nodes as parallel resistances.
        /// </summary>
        public abstract double ReducedResistance { get; }

        /// <summary>
        /// Updates <see cref="Branch.Flow"/>.
        /// </summary>
        public abstract void PropagateLogicalUpstream();

        /// <summary>
        /// Updates reduced resistance and effective length.
        /// </summary>
        public abstract void PropagatePhysicalUpstream();

        /// <summary>
        /// Requires the parent radius to be set.
        /// </summary>
        public abstract void SetChildRadii();

        /// <summary>
        /// Set radii in a single pass downstream.
        /// </summary>
        public abstract void PropagateRadiiDownstream();

        /// <summary>
        /// Sets radii in a downwards pass, then pads by <paramref name="pad"/> on the way up.
        /// A specialization of <see cref="PropagateRadiiDownstream(Func{Branch, double})"/> for the simple case
        /// of <c>b.r = b.r + p</c>.
        /// </summary>
        /// <param name="pad"></param>
        public abstract void PropagateRadiiDownstream(double pad);

        /// <summary>
        /// Sets radii in a downwards pass using <see cref="SetChildRadii"/>, then on the way up modifies them
        /// by <c>b.r = f(b.r)</c>.
        /// </summary>
        /// <param name="postProcessing"></param>
        public abstract void PropagateRadiiDownstream(Func<Branch, double> postProcessing);

        /// <summary>
        /// Updates all lengths, reduced resitances and effective lengths in a down-up pass.
        /// Must be called after <see cref="Branch.SetLogical"/> if initializing.
        /// </summary>
        public abstract void CalculatePhysical();

        /// <summary>
        /// Generates bounds in a down-up pass. Defers to <see cref="Downstream"/>, calling <see cref="Branch.GenerateDownstreamBounds()"/>, then
        /// combines these on the way up.
        /// </summary>
        /// <returns></returns>
        public abstract AxialBounds GenerateDownstreamBounds();

        /// <summary>
        /// See <see cref="GenerateDownstreamBounds()"/>, <see cref="Branch.GenerateDownstreamBounds(double)"/>.
        /// </summary>
        /// <param name="pad"></param>
        /// <returns></returns>
        public abstract AxialBounds GenerateDownstreamBounds(double pad);

        /// <summary>
        /// Visits each branch downstream of here and executes <paramref name="action"/>.
        /// </summary>
        /// <param name="action"></param>
        public void ForEach(Action<Branch> action)
        {
            foreach (var c in this.Downstream)
            {
                action(c);
                c.End.ForEach(action);
            }
        }

        /// <summary>
        /// Enumerates the first <paramref name="n"/> branches upstream of this node,
        /// starting from <see cref="Upstream"/>.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public IEnumerable<Branch> EnumerateUpstream(int n)
        {
            var b = this.Upstream;
            while (n > 0 && b != null)
            {
                yield return b;
                b = b.Parent;
                n--;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="S"></param>
        public virtual void SetChildren(Segment[] S)
        {
            for (var i = 0; i < S.Length; ++i)
            {
                this.Children[i] = S[i];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void UpdateDownstream()
        {
            for (var i = 0; i < this.Children.Length; ++i)
            {
                this.Downstream[i] = this.Children[i].Branch;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void UpdateChildTopology()
        {
            for (var i = 0; i < this.Children.Length; ++i)
            {
                this.Children[i].Start = this;
                this.Downstream[i].Start = this;
            }
        }
    }
}
