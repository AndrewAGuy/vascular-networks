using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Structure.Splitting
{
    [Serializable]
    public class ExponentialMurray : FlowContextualMurray
    {
        protected double a, b, c;

        public ExponentialMurray(double Q_min, double e_max, double Q_max, double e_min, double rate)
        {
            Set(Q_min, e_max, Q_max, e_min, rate);
        }

        public ExponentialMurray(double Q_min, double e_max, double Q_max, double e_min, double Q_mid, double e_mid, double tol, int iter)
        {
            // Find sensible guess of initial rate

            var r0 = 1.0 / (Q_max - Q_min);

            // Try to minimise discrepancy between Q_mid, e_mid with various values of b
            // Low guess will give exponent above midpoint, but can't ever be zero

            Set(Q_min, e_max, Q_max, e_min, r0);
            var d0 = ExponentBase(Q_mid) - e_mid;
            double rlo = r0, rhi = r0;
            double dhi = d0, dlo = d0;
            if (d0 > tol)
            {
                while (dhi > tol)
                {
                    rhi *= 2.0;
                    Set(Q_min, e_max, Q_max, e_min, rhi);
                    dhi = ExponentBase(Q_mid) - e_mid;
                }
            }
            else if (d0 < -tol)
            {
                while (dlo < -tol)
                {
                    rlo *= 0.5;
                    Set(Q_min, e_max, Q_max, e_min, rlo);
                    dlo = ExponentBase(Q_mid) - e_mid;
                }
            }
            else
            {
                return;
            }

            // Now we have a (lo,hi) interval in which we know Q_mid -> e_mid lies
            // Firstly, check endpoints for closeness to tolerance

            if (Math.Abs(dlo) < tol)
            {
                Set(Q_min, e_max, Q_max, e_min, rlo);
                return;
            }
            else if (Math.Abs(dhi) < tol)
            {
                Set(Q_min, e_max, Q_max, e_min, rhi);
                return;
            }

            // Next, test midpoints until convergence
            for (var i = 0; i < iter; ++i)
            {
                var mid = (rlo + rhi) * 0.5;
                Set(Q_min, e_max, Q_max, e_min, mid);
                var dm = ExponentBase(Q_mid) - e_mid;
                if (dm > 0)
                {
                    if (dm < tol)
                    {
                        return;
                    }
                    rlo = mid;
                }
                else
                {
                    if (dm > -tol)
                    {
                        return;
                    }
                    rhi = mid;
                }
            }
        }

        private void Set(double Q_min, double e_max, double Q_max, double e_min, double rate)
        {
            b = rate;
            a = (e_max - e_min) / (Math.Exp(-b * Q_min) - Math.Exp(-b * Q_max));
            c = e_max - a * Math.Exp(-b * Q_min);
        }

        private double ExponentBase(double Q)
        {
            return a * Math.Exp(-b * Q) + c;
        }

        public override double Exponent(double Q)
        {
            return a * Math.Exp(-b * Q) + c;
        }

        public override double ExponentGradient(double Q)
        {
            return -a * b * Math.Exp(-b * Q);
        }
    }
}
