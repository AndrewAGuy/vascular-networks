using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Construction.ACCO.Evaluators
{
    public class Evaluation<T>
    {
        public Evaluation(T o, double c, bool s)
        {
            this.Object = o;
            this.Cost = c;
            this.Suitable = s;
        }

        public T Object { get; private set; }

        public double Cost { get; set; }

        public bool Suitable { get; set; }
    }
}
