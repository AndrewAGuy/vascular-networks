using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Intersections.Collision
{
    /// <summary>
    /// Resolves collisions between networks, as described in doi: 10.1109/TBME.2019.2942313
    /// </summary>
    public class CollisionEnforcer : Enforcer<SegmentIntersection, INode, CollisionRecorder>
    {
        /// <summary>
        /// 
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// 
            /// </summary>
            All,
            /// <summary>
            /// 
            /// </summary>
            External,
            /// <summary>
            /// 
            /// </summary>
            Internal
        }

        /// <summary>
        /// 
        /// </summary>
        public Mode OperatingMode { get; set; } = Mode.External;

        private int internalTestStagger = 1;

        /// <summary>
        /// The number of iterations between each internal test, when <see cref="OperatingMode"/> is <see cref="Mode.All"/>.
        /// </summary>
        public int InternalTestStagger
        {
            get => internalTestStagger;
            set
            {
                if (value > 0)
                {
                    internalTestStagger = value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pad"></param>
        /// <returns></returns>
        public static Func<Branch, double> Padding(double pad)
        {
            return b => b.Radius + pad;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minRadius"></param>
        /// <param name="pad"></param>
        /// <returns></returns>
        public static Func<Branch, double> ClampedPadding(double minRadius, double pad)
        {
            return b => Math.Max(b.Radius, minRadius) + pad;
        }

        /// <summary>
        /// Given a maximum stress, use the highest pressure in the vessel alongside the inner radius
        /// to compute the wall thickness such that the maximum hoop stress (assumed uniform) is 
        /// equal to this critical value, then makes the vessel behave as though it is this large.
        /// <para/>
        /// Uses the classical thin-walled cylinder theory. Make sure that units are correctly scaled!
        /// The units of pressure in the vessels must match the units of critical stress, and the rest
        /// can be computed as the required ratio of outer to inner.
        /// </summary>
        /// <param name="sMax"></param>
        /// <returns></returns>
        public static Func<Branch, double> ThinWalledStressPadding(double sMax)
        {
            return b =>
            {
                // Uses s = p r / t
                var pMax = b.Start.Pressure + b.Network.PressureOffset;
                var k = pMax / sMax;
                return b.Radius * (1 + k);
            };
        }

        /// <summary>
        /// Given a maximum stress, use the highest pressure in the vessel alongside the inner radius
        /// to compute the outer radius such that the maximum hoop stress (at the inner surface) is 
        /// equal to this critical value, then makes the vessel behave as though it is this large.
        /// <para/>
        /// Uses the classical thick-walled cylinder theory. Make sure that units are correctly scaled!
        /// The units of pressure in the vessels must match the units of critical stress, and the rest
        /// can be computed as the required ratio of outer to inner.
        /// </summary>
        /// <param name="sMax"></param>
        /// <returns></returns>
        public static Func<Branch, double> ThickWalledStressPadding(double sMax)
        {
            return b =>
            {
                // Uses s = p ri^2 / (ro^2 - ri^2) (1 + ro^2 / r^2), evaluated at r = ri
                var pMax = b.Start.Pressure + b.Network.PressureOffset;
                var k = sMax / pMax;
                return b.Radius * Math.Sqrt((1 + k) / (k - 1));
            };
        }

        /// <summary>
        /// Computes the pressure at each node, possibly recomputing required properties.
        /// </summary>
        /// <param name="setLogical"></param>
        /// <param name="setPhysical"></param>
        /// <returns></returns>
        public static Action<Network> SetPressures(bool setLogical = false, bool setPhysical = false)
        {
            return n =>
            {
                switch ((setLogical, setPhysical))
                {
                    case (true, _):
                        n.Root.SetLogical();
                        goto PHYSICAL;

                    case (false, true):
                    PHYSICAL:
                        n.Source.CalculatePhysical();
                        break;
                }

                n.Source.PropagateRadiiDownstream();
                n.Source.CalculatePressures();
            };
        }

        private readonly List<Collider> externalColliders = new();
        private readonly List<InternalCollider> internalColliders = new();
        private readonly List<MatchedCollider> matchedColliders = new();
        private readonly List<DisjointCollider> disjointColliders = new();

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<Collider> ExternalColliders => externalColliders;

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<InternalCollider> InternalColliders => internalColliders;

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<MatchedCollider> MatchedColliders => matchedColliders;

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<DisjointCollider> DisjointColliders => disjointColliders;

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<Collider> Colliders
        {
            get
            {
                foreach (var c in externalColliders)
                {
                    yield return c;
                }
                foreach (var c in internalColliders)
                {
                    yield return c;
                }
            }
        }

        /// <summary>
        /// Creates an <see cref="InternalCollider"/> for each network in <paramref name="n"/>. 
        /// For each pair of distinct networks, tests if <see cref="Network.Partners"/> contains each other.
        /// Creates either a <see cref="MatchedCollider"/> or <see cref="DisjointCollider"/> based on this.
        /// Can disable this and force all to be treated as matched using <paramref name="noDisjoint"/>.
        /// </summary>
        /// <param name="n"></param>
        /// <param name="noDisjoint">Defaults to false to preserve legacy behaviour.</param>
        public CollisionEnforcer(Network[] n, bool noDisjoint = false) : base(n)
        {
            for (var i = 0; i < n.Length; ++i)
            {
                internalColliders.Add(new InternalCollider(n[i]));
                for (var j = i + 1; j < n.Length; ++j)
                {
                    if (noDisjoint ||
                        n[i].Partners != null && n[i].Partners.Contains(n[j]))
                    {
                        var c = new MatchedCollider(n[i], n[j]);
                        externalColliders.Add(c);
                        matchedColliders.Add(c);
                    }
                    else
                    {
                        var c = new DisjointCollider(n[i], n[j]);
                        externalColliders.Add(c);
                        disjointColliders.Add(c);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override async Task Detect()
        {
            var eTask = Task.CompletedTask;
            var iTask = Task.CompletedTask;
            async Task colliderTask(Collider c)
            {
                var i = c.Evaluate();
                await recorderSemaphore.WaitAsync();
                try
                {
                    this.Recorder.Record(i);
                }
                finally
                {
                    recorderSemaphore.Release();
                }
            }
            if (this.OperatingMode != Mode.Internal)
            {
                eTask = externalColliders.RunAsync(colliderTask);
            }
            if (this.OperatingMode == Mode.Internal ||
                this.OperatingMode == Mode.All && this.Iterations % this.InternalTestStagger == 0)
            {
                iTask = internalColliders.RunAsync(colliderTask);
            }
            await eTask;
            await iTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toCull"></param>
        /// <param name="obj"></param>
        protected override void AddToCull(ICollection<Terminal> toCull, INode obj)
        {
            if (obj is Terminal term)
            {
                if (term.Partners != null && this.CullMatched)
                {
                    foreach (var t in term.Partners)
                    {
                        toCull.Add(t);
                    }
                }
                else
                {
                    toCull.Add(term);
                }
            }
            else if (obj is Source && this.ThrowIfSourceCulled)
            {
                throw new TopologyException("Source node has hit cull limit");
            }
        }
    }
}
