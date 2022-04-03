using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;

namespace Vascular.Optimization
{
    public static class ActionEstimates
    {
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

        //public static double CreateBifurcation(BranchNode adding, Branch from, Vector3 position)
        //{

        //}

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
