namespace Vascular.Geometry.Bounds
{
    /// <summary>
    /// An object that can have bounds taken.
    /// </summary>
    public interface IAxialBoundable
    {
        /// <summary>
        /// Generates or returns cached value, implementation defined.
        /// </summary>
        /// <returns></returns>
        AxialBounds GetAxialBounds();
    }
}
