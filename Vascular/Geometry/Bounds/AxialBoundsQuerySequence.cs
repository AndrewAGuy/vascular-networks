using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vascular.Geometry.Bounds
{
    public class AxialBoundsQuerySequence<T> : IAxialBoundsQueryable<T> where T : IAxialBoundable
    {
        public AxialBoundsQuerySequence(IEnumerable<T> sequence)
        {
            this.sequence = sequence ?? Array.Empty<T>();
        }

        private readonly IEnumerable<T> sequence;

        public void Query(AxialBounds query, Action<T> action)
        {
            foreach (var obj in sequence)
            {
                if (query.Intersects(obj.GetAxialBounds()))
                {
                    action(obj);
                }
            }
        }

        public AxialBounds GetAxialBounds()
        {
            return sequence.GetTotalBounds();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return sequence.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return sequence.GetEnumerator();
        }
    }
}
