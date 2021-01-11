using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;

namespace Vascular.Intersections.Triangulation
{
    public class MeshEnforcer : BranchEnforcer<TriangleIntersection, MeshRecorder>
    {
        private readonly IIntersectionEvaluator<TriangleIntersection>[] regions;

        public bool SuppressRadii
        {
            set
            {
                this.RadiusModification = value ? b => 0 : null;
            }
        }

        public MeshEnforcer(Network[] n, IIntersectionEvaluator<TriangleIntersection>[] r) : base(n)
        {
            regions = r;
        }

        protected override async Task Detect()
        {
            await regions.SelectMany(r => networks, (r, n) => (r, n))
               .RunAsync(async o =>
               {
                   var i = o.r.Evaluate(o.n);
                   await recorderSemaphore.WaitAsync();
                   try
                   {
                       this.Recorder.Record(i);
                   }
                   finally
                   {
                       recorderSemaphore.Release();
                   }
               });
        }
    }
}
