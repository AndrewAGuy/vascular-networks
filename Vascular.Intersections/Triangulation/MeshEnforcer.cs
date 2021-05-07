using System.Linq;
using System.Threading.Tasks;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;

namespace Vascular.Intersections.Triangulation
{
    /// <summary>
    /// There is very little to be done about intersections between meshes and networks in general.
    /// Typically, desired geometries are not so strange as to have multiple bottlenecks or having inlets near them,
    /// and removing intersecting branches works well.
    /// </summary>
    public class MeshEnforcer : BranchEnforcer<TriangleIntersection, MeshRecorder>
    {
        private readonly IIntersectionEvaluator<TriangleIntersection>[] regions;

        /// <summary>
        /// Treat the segments as infinitely thin lines.
        /// </summary>
        public bool SuppressRadii
        {
            set => this.RadiusModification = value ? b => 0 : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="r"></param>
        public MeshEnforcer(Network[] n, IIntersectionEvaluator<TriangleIntersection>[] r) : base(n)
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
