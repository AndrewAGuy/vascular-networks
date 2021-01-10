using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Intersections.Collision
{
    public class CollisionEnforcer : Enforcer<SegmentIntersection, INode, CollisionRecorder>
    {
        public enum Mode
        {
            All,
            External,
            Internal
        }
        public Mode OperatingMode { get; set; } = Mode.External;
        private int internalTestStagger = 1;
        public int InternalTestStagger
        {
            get
            {
                return internalTestStagger;
            }
            set
            {
                if (value > 0)
                {
                    internalTestStagger = value;
                }
            }
        }

        private readonly List<Collider> externalColliders = new List<Collider>();
        private readonly List<InternalCollider> internalColliders = new List<InternalCollider>();
        private readonly List<MatchedCollider> matchedColliders = new List<MatchedCollider>();
        private readonly List<DisjointCollider> disjointColliders = new List<DisjointCollider>();
        public IReadOnlyList<Collider> ExternalColliders => externalColliders;
        public IReadOnlyList<InternalCollider> InternalColliders => internalColliders;
        public IReadOnlyList<MatchedCollider> MatchedColliders => matchedColliders;
        public IReadOnlyList<DisjointCollider> DisjointColliders => disjointColliders;
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

        public CollisionEnforcer(Network[] n) : base(n)
        {
            for (var i = 0; i < n.Length; ++i)
            {
                internalColliders.Add(new InternalCollider(n[i]));
                for (var j = i + 1; j < n.Length; ++j)
                {
                    if (n[i].Partners != null && n[i].Partners.Contains(n[j]))
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

        protected override async Task Detect()
        {
            var eTask = Task.CompletedTask;
            var iTask = Task.CompletedTask;
            async Task colliderTask (Collider c)
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
            if (this.OperatingMode != Mode.External)
            {
                iTask = internalColliders.RunAsync(colliderTask);
            }
            await eTask;
            await iTask;
        }

        protected override void AddToCull(ICollection<Terminal> toCull, INode obj)
        {
            if (obj is Terminal term)
            {
                if (term.Partners != null)
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
