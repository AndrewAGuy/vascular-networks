namespace Vascular.Construction.ACCO.Evaluators
{
    /// <summary>
    /// The evaluation of an object with respect to a terminal.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Evaluation<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <param name="c"></param>
        /// <param name="s"></param>
        public Evaluation(T o, double c, bool s)
        {
            this.Object = o;
            this.Cost = c;
            this.Suitable = s;
        }

        /// <summary>
        /// 
        /// </summary>
        public T Object { get; private set; }

        /// <summary>
        /// If negative, indicates a fatally unsuitable evaluation that terminates the search. Otherwise, smaller is better.
        /// </summary>
        public double Cost { get; set; }

        /// <summary>
        /// Selectors try to pick the lowest cost that is suitable.
        /// </summary>
        public bool Suitable { get; set; }
    }
}
