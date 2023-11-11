using System.Runtime.Serialization;

namespace Vascular.Structure.Splitting
{
    /// <summary>
    /// A decorator for a <see cref="FlowContextualMurray"/> to prevent the exponent going outside of a given range.
    /// </summary>
    public class ClampedMurray : FlowContextualMurray
    {
        private readonly double elo, ehi, qlo, qhi;
        private readonly FlowContextualMurray murray;

        /// <summary>
        ///
        /// </summary>
        /// <param name="Q_lo"></param>
        /// <param name="e_lo"></param>
        /// <param name="Q_hi"></param>
        /// <param name="e_hi"></param>
        /// <param name="fcm"></param>
        public ClampedMurray(double Q_lo, double e_lo, double Q_hi, double e_hi, FlowContextualMurray fcm)
        {
            qlo = Q_lo;
            elo = e_lo;
            qhi = Q_hi;
            ehi = e_hi;
            murray = fcm;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="Q"></param>
        /// <returns></returns>
        public override double Exponent(double Q)
        {
            return Q < qlo ? elo : Q > qhi ? ehi : murray.Exponent(Q);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="Q"></param>
        /// <returns></returns>
        public override double ExponentGradient(double Q)
        {
            return Q < qlo || Q > qhi ? 0 : murray.ExponentGradient(Q);
        }
    }
}
