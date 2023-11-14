using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Vascular.Optimization.Geometric;

/// <summary>
///
/// </summary>
public class ConvergenceTest : IGradientDescentTermination
{
    private CircularBuffer<double> buffer;
    private readonly int order;
    private readonly double fraction;
    private readonly int samples;
    private int iteration;

    /// <summary>
    ///
    /// </summary>
    /// <param name="samples"></param>
    /// <param name="order"></param>
    /// <param name="fraction"></param>
    public ConvergenceTest(int samples, int order = 1, double fraction = 1e-3)
    {
        this.order = order;
        this.fraction = fraction;
        this.samples = samples;
        Reset();
    }

    /// <summary>
    ///
    /// </summary>
    [MemberNotNull("buffer")]
    public void Reset()
    {
        buffer = new(samples);
        iteration = 0;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public bool Terminate(double v)
    {
        buffer.Add(v);
        ++iteration;
        if (iteration < samples)
        {
            return false;
        }
        // Fit curve of v = v0 + v1 * n^-k
        // If we are within fractional tolerance of V0, terminate
        var power = Enumerable.Range(iteration - samples + 1, samples)
            .Select(i => Math.Pow(i, -order))
            .ToArray();
        // Fit data of form V = [ 1, n^-k ] [ v0; v1 ]
        // Least squares, A^t A will be 2x2
        var aa00 = (double)samples;     // unit * unit
        var aa01 = power.Sum();         // unit * power
        var aa11 = power.Dot(power);
        var ab0 = buffer.Sum();         // unit * values
        var ab1 = power.Dot(buffer);
        // Solve (A^t A)^-1 A^t y for what we are interested in
        var det = aa00 * aa11 - aa01 * aa01;
        if (det == 0)
        {
            return false;
        }
        var v0 = (aa11 * ab0 - aa01 * ab1) / det;
        var v1 = (aa00 * ab1 - aa01 * ab0) / det;
        // Test: how much of v1 is left to go?
        return Math.Abs(v - v0) <= fraction * Math.Abs(v1);
    }
}
