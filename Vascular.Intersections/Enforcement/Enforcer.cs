using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Nodes;

namespace Vascular.Intersections.Enforcement
{
    public abstract class Enforcer<TIntersection, TPenalizing, TRecorder> : IEnforcer
        where TRecorder : Recorder<TIntersection, TPenalizing>, new()
    {
        public virtual bool CullingPermitted { get; set; } = true;
        public virtual bool ChangeGeometry { get; set; } = true;
        public virtual bool ChangeTopology { get; set; } = true;
        public virtual int Iterations { get; private set; } = 0;
        public virtual bool ClearOnSuccess { get; set; } = true;
        public virtual bool ThrowIfSourceCulled { get; set; } = false;
        public virtual bool PropagateGeometry { get; set; } = true;
        public virtual bool PropagateTopology { get; set; } = true;

        public virtual Func<Branch, double> RadiusModification { get; set; } = null;
        public virtual double BoundsPadding { get; set; } = 0.0;

        public virtual Penalizer<TPenalizing> Penalizer { get; set; } = new Penalizer<TPenalizing>();
        public virtual TRecorder Recorder { get; set; } = new TRecorder();

        protected SemaphoreSlim recorderSemaphore = new SemaphoreSlim(1);

        protected readonly Network[] networks;
        public Enforcer(Network[] n)
        {
            if (n == null || n.Length == 0)
            {
                throw new InputException("Enforcer cannot be created with zero networks");
            }
            networks = n;
        }

        public delegate void TerminalCullAction(Terminal t);
        public event TerminalCullAction TerminalCulled;

        public virtual async Task<int> Advance(int steps)
        {
            this.Iterations = 0;
            while (this.Iterations < steps)
            {
                var prepare = Prepare();
                this.Recorder.Reset();
                await prepare;
                await Detect();
                if (this.Recorder.Count == 0)
                {
                    TryClear();
                    return 0;
                }

                this.Recorder.Finish();
                var culling = GetCulling();
                await TryChangeGeometry();
                await TryChangeTopology();
                await TryCull(await culling);
                this.Iterations++;
            }
            return this.Recorder.Count;
        }

        protected virtual void TryClear()
        {
            if (this.ClearOnSuccess)
            {
                this.Penalizer.Clear();
            }
        }

        protected virtual Task TryChangeGeometry()
        {
            if (this.ChangeGeometry && this.Recorder.GeometryActions != null)
            {
                foreach (var action in this.Recorder.GeometryActions)
                {
                    action.Execute(this.PropagateGeometry);
                }

                if (!this.PropagateGeometry)
                {
                    return networks.RunAsync(n => n.Source.CalculatePhysical());
                }
            }
            return Task.CompletedTask;
        }

        protected virtual Task TryChangeTopology()
        {
            if (this.ChangeTopology && this.Recorder.BranchActions != null)
            {
                var executor = new TopologyExecutor()
                {
                    PropagateLogical = this.PropagateTopology,
                    PropagatePhysical = this.PropagateGeometry,
                    TryUpdate = true,
                    Priority = (a, b) => Math.Max(a.Flow, b.Flow)
                };
                executor.Iterate(this.Recorder.BranchActions);

                return Recalculate();
            }
            return Task.CompletedTask;
        }

        protected virtual Task<List<Terminal>> GetCulling()
        {
            return this.CullingPermitted
                ? Task.Run(() =>
                {
                    var culling = new List<Terminal>();
                    this.Penalizer.Penalize(this.Recorder.Intersecting);
                    foreach (var instant in this.Recorder.Culling)
                    {
                        AddToCull(culling, instant);
                    }
                    foreach (var threshold in this.Penalizer.Violators)
                    {
                        AddToCull(culling, threshold);
                    }
                    return culling;
                })
                : Task.FromResult(new List<Terminal>());
        }

        protected virtual Task TryCull(List<Terminal> culling)
        {
            if (this.CullingPermitted && culling.Count != 0)
            {
                foreach (var c in culling)
                {
                    if (c.Culled)
                    {
                        continue;
                    }
                    if (c.Parent == null)
                    {
                        c.Culled = true;
                        continue;
                    }
                    // Can we remove the terminal?
                    if (c.Upstream.Start is Source s)
                    {
                        if (this.ThrowIfSourceCulled)
                        {
                            throw new TopologyException("Source node is being culled");
                        }
                        else
                        {
                            c.Parent = null;
                            c.Culled = true;
                            s.Child = null;
                        }
                    }
                    else
                    {
                        // Remove. We don't bother removing everything from the map immediately, it will fall out on its own.
                        var tr = Topology.CullTerminal(c);
                        if (this.PropagateTopology)
                        {
                            tr.Parent.Branch.PropagateLogicalUpstream();
                            if (this.PropagateGeometry)
                            {
                                tr.UpdatePhysicalAndPropagate();
                            }
                        }
                        this.TerminalCulled?.Invoke(c);
                    }
                }

                return Recalculate();
            }
            return Task.CompletedTask;
        }

        private Task Recalculate()
        {
            if (!this.PropagateTopology)
            {
                return networks.RunAsync(n =>
                {
                    n.Root.SetLogical();
                    n.Source.CalculatePhysical();
                });
            }
            else if (!this.PropagateGeometry)
            {
                return networks.RunAsync(n => n.Source.CalculatePhysical());
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        protected virtual Task Prepare()
        {
            var radiusAction = this.RadiusModification != null
                ? new Action<Network>(n => n.Source.PropagateRadiiDownstream(this.RadiusModification))
                : new Action<Network>(n => n.Source.PropagateRadiiDownstream());
            var boundsAction = this.BoundsPadding != 0.0
                ? new Action<Network>(n => n.Source.GenerateDownstreamBounds(this.BoundsPadding))
                : new Action<Network>(n => n.Source.GenerateDownstreamBounds());
            void networkAction(Network n)
            {
                radiusAction(n);
                boundsAction(n);
            }
            return networks.RunAsync(networkAction);
        }

        protected abstract Task Detect();
        protected abstract void AddToCull(ICollection<Terminal> toCull, TPenalizing obj);

        public virtual async Task Resolve()
        {
            var iterations = 0;
            while (await Advance(1) != 0)
            {
                iterations++;
            }
            this.Iterations = iterations;
        }
    }
}
