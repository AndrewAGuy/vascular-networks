namespace Vascular.Geometry.Generators
{
    /// <summary>
    /// Generates vectors, possibly randomly.
    /// </summary>
    public interface IVector3Generator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Vector3 NextVector3();
    }
}
