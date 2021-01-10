using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Evaluators
{
    public interface IEvaluator<T>
    {
        Evaluation<T> Evaluate(T o, Terminal t);
    }
}
