using Vascular.Geometry;

namespace Vascular.Intersections.Enforcement
{
    /// <summary>
    /// Tracks a position and a direction.
    /// </summary>
    public class DoubleEntry
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_v"></param>
        /// <param name="_d"></param>
        public DoubleEntry(Vector3 _v, Vector3 _d)
        {
            v = _v;
            this.Direction = _d;
            n = 1;
        }

        private Vector3 v;
        private int n;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_v"></param>
        /// <param name="_d"></param>
        public void Add(Vector3 _v, Vector3 _d)
        {
            v += _v;
            this.Direction += _d;
            n++;
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector3 Direction { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public Vector3 Mean => (v + this.Direction) / n;
    }
}
