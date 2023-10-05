using System;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization
{
    /// <summary>
    ///
    /// </summary>
    [Obsolete]
    public static class ActionEstimates
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="hc"></param>
        /// <param name="m"></param>
        /// <param name="t"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static double MoveBifurcation(HierarchicalCosts hc, Branch m, Branch t, Vector3 x)
        {
            var dQt = m.Flow;
            var dQp = -dQt;
            var p = m.Parent;
            var s = m.FirstSibling;
            var RtD = t.End.ReducedResistance;
            var RmD = m.End.ReducedResistance;
            var RsD = s.End.ReducedResistance;

            var Ltp = Vector3.Distance(t.Start.Position, x);
            var Ltm = Vector3.Distance(m.End.Position, x);
            var Lts = Vector3.Distance(t.End.Position, x);
            var Rtm = Ltm + RmD;
            var Rts = Lts + RtD;
            var (ftm, fts) = m.Network.Splitting.Fractions(Rtm, dQt, Rts, t.Flow);
            var Rtp = Ltp + 1.0 / (Math.Pow(ftm, 4) / Rtm + Math.Pow(fts, 4) / Rts);
            var dRt = Rtp - t.ReducedResistance;

            var Lp = Vector3.Distance(p.Start.Position, s.End.Position);
            var Rp = Lp + RsD;
            var dRp = Rp - p.ReducedResistance;

            var dC = 0.0;
            if (hc.WorkFactor != 0.0)
            {
                var (dW_dQt, dW_dRt) = hc.Work.Gradients(t);
                var (dW_dQp, dW_dRp) = hc.Work.Gradients(p);
                dC = hc.WorkFactor * (dW_dQt * dQt + dW_dRt * dRt
                    + dW_dQp * dQp + dW_dRp * dRp);
            }

            foreach (var sc in hc.SchreinerCosts)
            {
                var el = sc.EffectiveLengths;
                var ELtD = el.Values[t] - Math.Pow(t.Length, el.ExpL);
                var ELmD = el.Values[m] - Math.Pow(m.Length, el.ExpL);
                var ELt = Math.Pow(Ltp, el.ExpL)
                    + Math.Pow(ftm, el.ExpR) * (Math.Pow(Ltm, el.ExpL) + ELmD)
                    + Math.Pow(fts, el.ExpR) * (Math.Pow(Lts, el.ExpL) + ELtD);
                var dLt = ELt - el.Values[t];

                var ELsD = el.Values[s] - Math.Pow(s.Length, el.ExpL);
                var ELp = Math.Pow(Lp, el.ExpL) + ELsD;
                var dLp = ELp - el.Values[p];

                var (dC_dQt, dC_dRt, dC_dLt) = sc.Gradients(t);
                var (dC_dQp, dC_dRp, dC_dLp) = sc.Gradients(p);
                dC += sc.Multiplier * (dC_dQt * dQt + dC_dRt * dRt + dC_dLt * dLt
                    + dC_dQp * dQp + dC_dRp * dRp + dC_dLp * dLp);
            }
            return dC;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="hc"></param>
        /// <param name="t"></param>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static double CreateBifurcation(HierarchicalCosts hc, Terminal t, Branch f, Vector3 x)
        {
            var dQ = t.Flow;

            var Lp = Vector3.Distance(f.Start.Position, x);
            var Ls = Vector3.Distance(f.End.Position, x);
            var Lt = Vector3.Distance(t.Position, x);

            var Rs = Ls + f.End.ReducedResistance;
            var Rt = Lt;
            var (fs, ft) = f.Network.Splitting.Fractions(Rs, f.Flow, Rt, dQ);
            var Rp = Lp + 1.0 / (Math.Pow(fs, 4) / Rs + Math.Pow(ft, 4) / Rt);
            var dR = Rp - f.ReducedResistance;

            var dC = 0.0;
            if (hc.WorkFactor != 0.0)
            {
                var (dW_dQ, dW_dR) = hc.Work.Gradients(f);
                dC = hc.WorkFactor * (dW_dQ * dQ + dW_dR * dR);
            }

            foreach (var sc in hc.SchreinerCosts)
            {
                var el = sc.EffectiveLengths;

                var ELsD = el.Values[f] - Math.Pow(f.Length, el.ExpL);
                var EL = Math.Pow(Lp, el.ExpL)
                    + Math.Pow(ft, el.ExpR) * Math.Pow(Lt, el.ExpL)
                    + Math.Pow(fs, el.ExpR) * (Math.Pow(Ls, el.ExpL) + ELsD);
                var dL = EL - el.Values[f];

                var (dC_dQ, dC_dR, dC_dL) = sc.Gradients(f);
                dC += sc.Multiplier * (dC_dQ * dQ + dC_dR * dR + dC_dL * dL);
            }
            return dC;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="hc"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double SwapEnds(HierarchicalCosts hc, Branch a, Branch b)
        {
            var dQa = b.Flow - a.Flow;
            var dQb = -dQa;

            var La = Vector3.Distance(a.Start.Position, b.End.Position);
            var Lb = Vector3.Distance(b.Start.Position, a.End.Position);
            var dRa = La + b.End.ReducedResistance - a.ReducedResistance;
            var dRb = Lb + a.End.ReducedResistance - b.ReducedResistance;

            var dC = 0.0;
            if (hc.WorkFactor != 0.0)
            {
                var (dW_dQa, dW_dRa) = hc.Work.Gradients(a);
                var (dW_dQb, dW_dRb) = hc.Work.Gradients(b);
                dC = hc.WorkFactor * (dW_dQa * dQa + dW_dRa * dRa
                    + dW_dQb * dQb + dW_dRb * dRb);
            }

            foreach (var sc in hc.SchreinerCosts)
            {
                var el = sc.EffectiveLengths;
                var ELaD = el.Values[a] - Math.Pow(a.Length, el.ExpL);
                var ELbD = el.Values[b] - Math.Pow(b.Length, el.ExpL);
                var ELa = Math.Pow(La, el.ExpL) + ELbD;
                var ELb = Math.Pow(Lb, el.ExpL) + ELaD;
                var dLa = ELa - el.Values[a];
                var dLb = ELb - el.Values[b];

                var (dC_dQa, dC_dRa, dC_dLa) = sc.Gradients(a);
                var (dC_dQb, dC_dRb, dC_dLb) = sc.Gradients(b);
                dC += sc.Multiplier * (dC_dQa * dQa + dC_dRa * dRa + dC_dLa * dLa
                    + dC_dQb * dQb + dC_dRb * dRb + dC_dLb * dLb);
            }
            return dC;
        }
    }
}
