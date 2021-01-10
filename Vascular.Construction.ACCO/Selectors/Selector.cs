using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Construction.ACCO.Evaluators;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Selectors
{
    public abstract class Selector
    {
        public IEvaluator<Branch> Evaluator { get; set; } = new MeanlineEvaluator();

        public abstract Evaluation<Branch> Select(Branch from, Terminal to);
    }
}
