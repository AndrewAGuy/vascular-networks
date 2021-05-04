using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Vascular
{
    public class CircularBuffer<T> : ICollection<T>, IList<T>
    {
        public CircularBuffer(int num)
        {
            data = new T[num];
        }

        private int offset = 0;
        private readonly T[] data;

        public int Count => data.Length;

        private int IndexToPosition(int index)
        {
            if (index > data.Length || index < -data.Length)
            {
                throw new IndexOutOfRangeException();
            }

            var position = offset + index;
            position = position < 0
                ? position + data.Length
                : position >= data.Length
                ? position - data.Length
                : position;
            return position;
        }

        public T this[int index]
        {
            get => data[IndexToPosition(index)];
            set => data[IndexToPosition(index)] = value;
        }

        public bool IsReadOnly => false;       

        public void Add(T item)
        {
            data[offset] = item;
            offset++;
            if (offset == data.Length)
            {
                offset = 0;
            }
        }

        public void Clear()
        {
            for (var i = 0; i < data.Length; ++i)
            {
                data[i] = default;
            }
        }

        public bool Contains(T item)
        {
            return data.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var item in this)
            {
                array[arrayIndex] = item;
                arrayIndex++;
            }
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        private class Enumerator : IEnumerator<T>
        {
            public Enumerator(CircularBuffer<T> buffer)
            {
                this.buffer = buffer;
                position = buffer.offset;
            }

            private readonly CircularBuffer<T> buffer;
            private int position = 0;

            public T Current => buffer.data[position];

            object IEnumerator.Current => buffer.data[position];

            public bool MoveNext()
            {
                position++;
                if (position == buffer.data.Length)
                {
                    position = 0;
                }
                return position != buffer.offset;
            }

            public void Reset()
            {
                position = buffer.offset;
            }

            public void Dispose()
            {
                
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public int IndexOf(T item)
        {
            for (var i = 0; i < data.Length; ++i)
            {
                if (item.Equals(data[i]))
                {
                    var index = i - offset;
                    index = index < 0
                        ? index + data.Length
                        : index;
                    return index;
                }
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }
    }
}
