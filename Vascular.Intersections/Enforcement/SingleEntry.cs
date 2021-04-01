using Vascular.Geometry;

namespace Vascular.Intersections.Enforcement
{
    /// <summary>
    /// Records a position and count.
    /// </summary>
    public class SingleEntry
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_v"></param>
        public SingleEntry(Vector3 _v)
        {
            this.Value = _v;
            n = 1;
        }

        private int n;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_v"></param>
        public void Add(Vector3 _v)
        {
            this.Value += _v;
            n++;
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector3 Value { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public Vector3 Mean => this.Value / n;
    }
}
