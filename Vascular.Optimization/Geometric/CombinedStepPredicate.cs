using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure;

namespace Vascular.Optimization.Geometric;

/// <summary>
///
/// </summary>
public class CombinedStepPredicate : IGradientDescentStepPredicate
{
    private readonly IGradientDescentStepPredicate[] inner;

    /// <summary>
    ///
    /// </summary>
    /// <param name="inner"></param>
    public CombinedStepPredicate(IEnumerable<IGradientDescentStepPredicate> inner)
    {
        this.inner = inner.ToArray();
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    public bool Permitted(IMobileNode node, Vector3 step)
    {
        foreach (var p in inner)
        {
            if (!p.Permitted(node, step))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    public double MaximumPermitted(IMobileNode node, Vector3 step)
    {
        var max = inner[0].MaximumPermitted(node, step);
        for (var i = 1; i < inner.Length; ++i)
        {
            max = Math.Min(max, inner[i].MaximumPermitted(node, step));
        }
        return max;
    }
}
