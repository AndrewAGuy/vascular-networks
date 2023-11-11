using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Vascular
{
    /// <summary>
    /// A class with storage for a fixed number of elements which rewrites old data when new elements are added.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CircularBuffer<T> : ICollection<T>, IList<T>
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="num"></param>
        public CircularBuffer(int num)
        {
            data = new T[num];
        }

        private int offset = 0;
        private readonly T[] data;

        /// <summary>
        ///
        /// </summary>
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

        /// <summary>
        /// Accepts indices up to +/- <see cref="Count"/>. Accesses data from current offset.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get => data[IndexToPosition(index)];
            set => data[IndexToPosition(index)] = value;
        }

        /// <summary>
        ///
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Overwrites the element at the current offset and advances the offset.
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            data[offset] = item;
            offset++;
            if (offset == data.Length)
            {
                offset = 0;
            }
        }

        /// <summary>
        /// Sets all elements to the default for <typeparamref name="T"/>.
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < data.Length; ++i)
            {
                data[i] = default!;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            return data.Contains(item);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var item in this)
            {
                array[arrayIndex] = item;
                arrayIndex++;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        private class Enumerator : IEnumerator<T>
        {
            public Enumerator(CircularBuffer<T> buffer)
            {
                this.buffer = buffer;
                Reset();
            }

            private readonly CircularBuffer<T> buffer;
            private int position;

            public T Current => buffer[position];

            object IEnumerator.Current => buffer[position]!;

            public bool MoveNext()
            {
                position++;
                return position < buffer.data.Length;
            }

            public void Reset()
            {
                position = -1;
            }

            public void Dispose()
            {

            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        ///
        /// </summary>
        public IEqualityComparer<T> EqualityComparer { get; set; } = EqualityComparer<T>.Default;

        /// <summary>
        /// Returns a postitive index from the current offset, such that <see cref="this[int]"/>
        /// refers to <paramref name="item"/> if found. Returns -1 if not found.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(T item)
        {
            for (var i = 0; i < data.Length; ++i)
            {
                if (this.EqualityComparer.Equals(item, data[i]))
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }
    }
}
