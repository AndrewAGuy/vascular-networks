using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;

namespace Vascular.Intersections.Segmental
{
    public class SegmentEnforcer : BranchEnforcer<SegmentIntersection, SegmentalRecorder>
    {
        private readonly SegmentRegion[] regions;

        public SegmentEnforcer(Network[] n, SegmentRegion[] r) : base(n)
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
