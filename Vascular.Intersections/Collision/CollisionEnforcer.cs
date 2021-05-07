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

        private readonly List<Collider> externalColliders = new List<Collider>();
        private readonly List<InternalCollider> internalColliders = new List<InternalCollider>();
        private readonly List<MatchedCollider> matchedColliders = new List<MatchedCollider>();
        private readonly List<DisjointCollider> disjointColliders = new List<DisjointCollider>();

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
