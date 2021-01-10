﻿using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Evaluators
{
    public class MeanlineEvaluator : IEvaluator<Branch>
    {
        public Evaluation<Branch> Evaluate(Branch o, Terminal t)
        {
            var oS = o.Start.Position;
            var oE = o.End.Position;
            var v = t.Position;
            var dir = oE - oS;
            var oR = o.Radius;

            // Suitability determined by degeneracy of triangle
            // Rejection determined by consumption within segment
            // TODO: Implement angle conditions beyond just cylinder test

            var f = LinearAlgebra.LineFactor(oS, dir, v);
            var p = oS + f * dir;
            // Clamp to valid range, get actual distance square
            var pc = oS + f.Clamp(0, 1) * dir;
            var d2 = Vector3.DistanceSquared(v, pc);
            var r2 = oR * oR;
            return r2 >= d2
                ? new Evaluation<Branch>(o, r2 - d2, true)
                : new Evaluation<Branch>(o, d2, Vector3.DistanceSquared(p, v) > r2);
        }
    }
}
