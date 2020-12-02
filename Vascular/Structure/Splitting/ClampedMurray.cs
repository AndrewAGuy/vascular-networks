using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Structure.Splitting
{
    [Serializable]
    public class ClampedMurray : FlowContextualMurray
    {
        private readonly double elo;
        private readonly double ehi;
        private readonly double qlo;
        private readonly double qhi;
        private readonly FlowContextualMurray murray;

        public ClampedMurray(double Q_lo, double e_lo, double Q_hi, double e_hi, FlowContextualMurray fcm)
        {
            qlo = Q_lo;
            elo = e_lo;
            qhi = Q_hi;
            ehi = e_hi;
            murray = fcm;
        }

        public override double Exponent(double Q)
        {
            return Q < qlo ? elo : Q > qhi ? ehi : murray.Exponent(Q);
        }

        public override double ExponentGradient(double Q)
        {
            return Q < qlo || Q > qhi ? 0 : murray.ExponentGradient(Q);
        }
    }
}
