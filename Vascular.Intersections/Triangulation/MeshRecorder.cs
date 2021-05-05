using System;
using System.Collections.Generic;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;

namespace Vascular.Intersections.Triangulation
{
    /// <summary>
    /// 
    /// </summary>
    public class MeshRecorder : Recorder<TriangleIntersection, Branch>
    {
        /// <summary>
        /// All outwards intersections are added to <see cref="Recorder{TIntersection, TPenalizing}.Culling"/>.
        /// </summary>
        public bool CullOutwards { get; set; } = true;

        /// <summary>
        /// 
        /// </summary>
        public override int Count => intersecting.Count;

        private Dictionary<Segment, MeshIntersectionExtrema> intersections = new Dictionary<Segment, MeshIntersectionExtrema>();

        /// <summary>
        /// Some use cases require branches to cross from outside to inside a mesh.
        /// In this case, we might want to ignore an intersection in the root branches and focus only on the extremities.
        /// </summary>
        public Func<Branch, bool> Ignore { get; set; } = b => false;

        /// <summary>
        /// 
        /// </summary>
        public override void Reset()
        {
            intersections = new Dictionary<Segment, MeshIntersectionExtrema>();
            base.Reset();
        }

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        protected override void RecordSingle(TriangleIntersection t)
        {
            if (this.Ignore(t.Segment.Branch))
            {
                return;
            }

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