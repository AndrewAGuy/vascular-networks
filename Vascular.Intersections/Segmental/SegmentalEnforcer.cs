using System.Linq;
using System.Threading.Tasks;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;

namespace Vascular.Intersections.Segmental
{
    /// <summary>
    /// 
    /// </summary>
    public class SegmentEnforcer : BranchEnforcer<SegmentIntersection, SegmentalRecorder>
    {
        private readonly SegmentRegion[] regions;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="r"></param>
        public SegmentEnforcer(Network[] n, SegmentRegion[] r) : base(n)
        {
            regions = r;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
