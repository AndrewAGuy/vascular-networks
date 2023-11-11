using Vascular.Geometry;

namespace Vascular.Structure
{
    /// <summary>
    /// Links <see cref="Segment"/> instances together to form a tree.
    /// </summary>
    public interface INode
    {
        /// <summary>
        ///
        /// </summary>
        Segment? Parent { get; set; }

        /// <summary>
        ///
        /// </summary>
        Segment[] Children { get; }

        /// <summary>
        ///
        /// </summary>
        Vector3 Position { get; set; }
    }
}
