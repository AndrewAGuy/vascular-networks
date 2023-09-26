using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vascular.Optimization.Hierarchical
{
    /// <summary>
    /// Formulations of standard costs for a range of scenarios. Costs can be of the form:
    /// <br/>
    /// - "maximize useful output": <see cref="ProjectedArea"/> for the retina or <see cref="Volume"/> in 3D;
    /// <br/>
    /// - "minimize operating cost": <see cref="Resistance"/> and <see cref="Work"/> for open circuit applications (filtration) 
    /// or <see cref="Murray"/> for closed systems (incorporating the cost of blood maintenance);
    /// <br/>
    /// - "maximize efficiency": <see cref="ProjectedAreaEfficiency"/> and <see cref="ProjectedAreaEfficiencyVariableFlow"/>  in 2D, 
    /// <see cref="VolumeEfficiency"/> and <see cref="VolumeEfficiencyVariableFlow"/> in 3D 
    /// (the variable flow variants account for the reduction in metabolic requirements when useful tissue is lost to vasculature).
    /// <para>
    /// Future costs may consider a contribution to 3D constructs where increased surface area is desirable (e.g. cell seeding),
    /// the term <see cref="SurfaceArea"/> is implemented in anticipation of this. 
    /// </para>
    /// </summary>
    public static class StandardCosts
    {
        /// <summary>
        /// Vessel surface area
        /// </summary>
        /// <param name="g"></param>
        /// <param name="lu"></param>
        /// <returns></returns>
        public static HierarchicalCost SurfaceArea(HierarchicalGradients g, double lu = 1e3)
        {
            return new PolynomialCost(g, 2 * Math.PI / Math.Pow(lu, 2), 1, 1);
        }

        /// <summary>
        /// Vessel projected area, for 2D situations
        /// </summary>
        /// <param name="g"></param>
        /// <param name="lu"></param>
        /// <returns></returns>
        public static HierarchicalCost ProjectedArea(HierarchicalGradients g, double lu = 1e3)
        {
            return new PolynomialCost(g, 2 / Math.Pow(lu, 2), 1, 1);
        }

        /// <summary>
        /// Vessel volume
        /// </summary>
        /// <param name="g"></param>
        /// <param name="lu"></param>
        /// <returns></returns>
        public static HierarchicalCost Volume(HierarchicalGradients g, double lu = 1e3)
        {
            return new PolynomialCost(g, Math.PI / Math.Pow(lu, 3), 1, 2);
        }

        /// <summary>
        /// Pump work for a given viscosity
        /// </summary>
        /// <param name="g"></param>
        /// <param name="viscosity"></param>
        /// <param name="lu"></param>
        /// <param name="Q"></param>
        /// <returns></returns>
        public static HierarchicalCost Work(HierarchicalGradients g,
            double viscosity = 3.6e-3, double lu = 1e3, double Q = 0.0)
        {
            return new PumpingWorkCost(g, viscosity * 8 / (Math.PI * Math.Pow(lu, 3)), Q);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="g"></param>
        /// <param name="viscosity"></param>
        /// <param name="lu"></param>
        /// <returns></returns>
        public static HierarchicalCost Resistance(HierarchicalGradients g,
            double viscosity = 3.6e-3, double lu = 1e3)
        {
            return new PumpingWorkCost(g, viscosity * 8 / (Math.PI * Math.Pow(lu, 3)), 1);
        }

        /// <summary>
        /// A balance of work and metabolic cost to maintain blood volume, 
        /// as considered in the derivation of Murray's law.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="v"></param>
        /// <param name="mb"></param>
        /// <param name="lu"></param>
        /// <param name="Q"></param>
        /// <returns></returns>
        public static HierarchicalCost Murray(HierarchicalGradients g,
            double v = 3.6e-3, double mb = 640, double lu = 1e3, double Q = 0.0)
        {
            var V = new PolynomialCost(g, Math.PI / Math.Pow(lu, 3) * mb, 1, 2);
            var W = Work(g, v, lu, Q);
            return new CombinedCost(g,
                new[] { W, V },
                C => C[0].Cost + C[1].Cost,
                C => new[] { 1.0, 1.0 });
        }

        /// <summary>
        /// For a domain of volume <paramref name="dV"/>, the cost which when minimized yields the 
        /// maximum useful tissue per unit power required to run the organ.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="dV"></param>
        /// <param name="v"></param>
        /// <param name="mb"></param>
        /// <param name="lu"></param>
        /// <param name="Q"></param>
        /// <returns></returns>
        public static HierarchicalCost VolumeEfficiency(HierarchicalGradients g,
            double dV, double v = 3.6e-3, double mb = 640, double lu = 1e3, double Q = 0.0)
        {
            var V = Volume(g, lu);
            var W = Work(g, v, lu, Q);
            return new CombinedCost(g,
                new[] { W, V },
                C => -(dV - C[1].Cost) / (C[0].Cost + mb * C[1].Cost),
                C => new[]
                {
                    (dV - C[1].Cost) / Math.Pow(C[0].Cost + mb * C[1].Cost, 2),
                    (mb * dV + C[0].Cost) / Math.Pow(C[0].Cost + mb * C[1].Cost, 2)
                });
        }

        /// <summary>
        /// For a 2D domain with area <paramref name="dA"/>, the cost which when minimized yields the
        /// maximum non-occluded area per unit power required to run the organ.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="dA"></param>
        /// <param name="v"></param>
        /// <param name="mb"></param>
        /// <param name="lu"></param>
        /// <param name="Q"></param>
        /// <returns></returns>
        public static HierarchicalCost ProjectedAreaEfficiency(HierarchicalGradients g,
            double dA, double v = 3.6e-3, double mb = 640, double lu = 1e3, double Q = 0.0)
        {
            var A = ProjectedArea(g, lu);
            var W = Work(g, v, lu, Q);
            if (mb != 0)
            {
                var V = Volume(g, lu);
                return new CombinedCost(g,
                    new[] { W, V, A },
                    C => -(dA - C[2].Cost) / (C[0].Cost + mb * C[1].Cost),
                    C => new[]
                    {
                        (dA - C[2].Cost) / Math.Pow(C[0].Cost + mb * C[1].Cost, 2),
                        (dA - C[2].Cost) * mb / Math.Pow(C[0].Cost + mb * C[1].Cost, 2),
                        1 / (C[0].Cost + mb * C[1].Cost)
                    });
            }
            else
            {
                return new CombinedCost(g,
                    new[] { W, A },
                    C => -(dA - C[1].Cost) / C[0].Cost,
                    C => new[]
                    {
                        (dA - C[1].Cost) / Math.Pow(C[0].Cost, 2),
                        1 / C[0].Cost
                    });
            }
        }

        /// <summary>
        /// For a 3D domain with volume <paramref name="dV"/>, the cost which when minimized yields the
        /// maximum useful tissue per unit power required to run the organ.
        /// Uses the spatial flow rate distribution given by the terminal distribution, with the
        /// overall flow rate depending on the domain volume less that lost to vessels multiplied
        /// by a flow rate density <paramref name="Q0"/>.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="dV"></param>
        /// <param name="v"></param>
        /// <param name="mb"></param>
        /// <param name="lu"></param>
        /// <param name="Q0"></param>
        /// <returns></returns>
        public static HierarchicalCost VolumeEfficiencyVariableFlow(HierarchicalGradients g,
            double dV, double v = 3.6e-3, double mb = 640, double lu = 1e3, double Q0 = 1.0 / 60.0)
        {
            // Cost now has Q = Q0 * (dV - V), to account for lost tissue
            var V = Volume(g, lu);
            var W = Resistance(g, v, lu);
            return new CombinedCost(g,
                new[] { W, V },
                C => -(dV - C[1].Cost) / (Math.Pow(Q0 * (dV - C[1].Cost), 2) * C[0].Cost + mb * C[1].Cost),
                C =>
                {
                    var denom = Math.Pow(Q0 * (dV - C[1].Cost), 2) * C[0].Cost + mb * C[1].Cost;
                    var dDen_dV = mb - Math.Pow(Q0, 2) * C[0].Cost * 2 * (dV - C[1].Cost);
                    var dC_dV = (denom + (dV - C[1].Cost) * dDen_dV) / Math.Pow(denom, 2);
                    return new[]
                    {
                        (dV - C[1].Cost) * Math.Pow(Q0 * (dV - C[1].Cost), 2) / denom,
                        dC_dV
                    };
                });
        }

        /// <summary>
        /// Experimental, for retinal use.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="dA"></param>
        /// <param name="v"></param>
        /// <param name="mb"></param>
        /// <param name="lu"></param>
        /// <param name="Q0"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static HierarchicalCost ProjectedAreaEfficiencyVariableFlow(HierarchicalGradients g,
            double dA, double v = 3.6e-3, double mb = 640, double lu = 1e3, double Q0 = 1.0 / 60.0, double t = 0.0)
        {
            // What we get: (dA - A)
            // What we pay for: ((dA - A)*Q0)^2 R + mb V
            // Or possibly something of the form Q = (dV - V)*Q0? - e.g. if embedded in tissue rather than above.
            // Specify this with thickness parameter - if this is set then dV = t*dA, Q0 is volume.
            // Otherwise we have no loss of volume if vessels sit above, but space that doesn't do useful light collection.

            var A = ProjectedArea(g, lu);
            var R = Resistance(g, v, lu);
            if (t <= 0.0 && mb == 0.0)
            {
                return new CombinedCost(g,
                    new[] { R, A },
                    C => -(dA - C[1].Cost) / (Math.Pow(Q0 * (dA - C[1].Cost), 2) * C[0].Cost),
                    C => 
                    {
                        var num = -(dA - C[1].Cost);
                        var den = Math.Pow(Q0 * (dA - C[1].Cost), 2) * C[0].Cost;
                        var dDen_dA = -2 * Math.Pow(Q0, 2) * C[0].Cost * (dA - C[1].Cost);
                        var dDen_dR = Math.Pow(Q0 * (dA - C[1].Cost), 2);
                        return new double[] 
                        {
                            -num * dDen_dR / Math.Pow(den, 2),
                            (den - num * dDen_dA) / Math.Pow(den, 2)
                        };
                    });
            }

            var V = Volume(g, lu);
            if (t > 0)
            {
                var dV = dA * t;
                return new CombinedCost(g,
                    new[] { R, A, V },
                    C => -(dA - C[1].Cost) / (Math.Pow(Q0 * (dV - C[2].Cost), 2) * C[0].Cost + mb * C[2].Cost),
                    C =>
                    {
                        var num = -(dA - C[1].Cost);
                        var den = Math.Pow(Q0 * (dV - C[2].Cost), 2) * C[0].Cost + mb * C[2].Cost;
                        var dDen_dV = mb - Math.Pow(Q0, 2) * C[0].Cost * 2 * (dV - C[2].Cost);
                        var dDen_dR = Math.Pow(Q0 * (dV - C[2].Cost), 2);
                        return new double[] 
                        {
                            -num * dDen_dR / Math.Pow(den, 2),
                            1.0 / den,
                            -num * dDen_dV / Math.Pow(den, 2)
                        };
                    });
            }
            else
            {
                return new CombinedCost(g,
                    new[] { R, A, V },
                    C => -(dA - C[1].Cost) / (Math.Pow(Q0 * (dA - C[1].Cost), 2) * C[0].Cost + mb * C[2].Cost),
                    C =>
                    {
                        var num = -(dA - C[1].Cost);
                        var den = Math.Pow(Q0 * (dA - C[1].Cost), 2) * C[0].Cost + mb * C[2].Cost;
                        var dDen_dV = mb;
                        var dDen_dA = -2 * Math.Pow(Q0, 2) * C[0].Cost * (dA - C[1].Cost);
                        var dDen_dR = Math.Pow(Q0 * (dA - C[1].Cost), 2);
                        return new double[] 
                        {
                            -num * dDen_dR / Math.Pow(den, 2),
                            (den - num * dDen_dA) / Math.Pow(den, 2),
                            -num * dDen_dV / Math.Pow(den, 2)
                        };
                    });
            } 
        }
    }
}
