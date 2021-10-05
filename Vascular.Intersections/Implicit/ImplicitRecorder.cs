using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;

namespace Vascular.Intersections.Implicit
{
    public class ImplicitRecorder : Recorder<ImplicitViolation, INode>
    {
        public override int Count => throw new NotImplementedException();

        public override void Finish()
        {
            throw new NotImplementedException();
        }

        protected override void RecordSingle(ImplicitViolation t)
        {
            throw new NotImplementedException();
        }
    }
}
