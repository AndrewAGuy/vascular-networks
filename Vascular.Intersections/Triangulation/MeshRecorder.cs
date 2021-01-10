using System.Collections.Generic;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;

namespace Vascular.Intersections.Triangulation
{
    public class MeshRecorder : Recorder<TriangleIntersection, Branch>
    {
        public bool CullOutwards { get; set; } = true;

        public override int Count => intersecting.Count;

        private Dictionary<Segment, MeshIntersectionExtrema> intersections = new Dictionary<Segment, MeshIntersectionExtrema>();

        public override void Reset()
        {
            intersections = new Dictionary<Segment, MeshIntersectionExtrema>();
            base.Reset();
        }

        public override void Finish()
        {
            foreach (var i in intersections)
            {
                if (i.Value.FirstOut != null)
                {
                    if (this.CullOutwards)
                    {
                        culling.Add(i.Key.Branch);
                    }
                }
            }
        }

        protected override void RecordSingle(TriangleIntersection t)
        {
            if (intersections.TryGetValue(t.Segment, out var existing))
            {
                existing.Add(t);
            }
            else
            {
                intersections[t.Segment] = new MeshIntersectionExtrema(t);
            }
            intersecting.Add(t.Segment.Branch);
        }
    }
}