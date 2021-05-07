using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Vascular.Geometry.Bounds
{
    /// <summary>
    /// An implementation of a multi-level hash table in a single <see cref="Dictionary{TKey, TValue}"/> through the use of a level appended to the key.
    /// Taken from Eitz and Lixu, 'Hierarchical Spatial Hashing for Real-time Collision Detection'
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AxialBoundsHashTable<T> : IEnumerable<T>, IAxialBoundsQueryable<T>, IAxialBoundable where T : IAxialBoundable
    {
        private struct Key : IEquatable<Key>
        {
            public Key(int _x, int _y, int _z, int _w)
            {
                (x, y, z, w) = (_x, _y, _z, _w);
            }

            private readonly int x, y, z, w;

            public bool Equals(Key o)
            {
                return x == o.x && y == o.y && z == o.z && w == o.w;
            }

            public override bool Equals(object obj)
            {
                return obj is Key o && Equals(o);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(x, y, z, w);
            }
        }

        private readonly Dictionary<Key, LinkedList<T>> table;
        private readonly HashSet<int> levels;
        private readonly AxialBounds totalBounds = new AxialBounds();
        private readonly double factor;
        private readonly double baseStride;

        private int Level(double range)
        {
            return (int)Math.Ceiling(Math.Log(range / baseStride, factor));
        }

        private double Stride(int level)
        {
            return baseStride * Math.Pow(factor, level);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="stride">The base stride. All boxes will be multiples of this.</param>
        /// <param name="factor">The scaling factor between levels.</param>
        public AxialBoundsHashTable(IEnumerable<T> elements, double stride = 1.0, double factor = 2.0)
        {
            baseStride = stride;
            this.factor = factor;
            if (!elements.Any())
            {
                table = new Dictionary<Key, LinkedList<T>>();
                levels = new HashSet<int>();
                return;
            }
            var ranges = elements.Select(e => e.GetAxialBounds().Range.Max);
            var maxLevel = Level(ranges.Max());
            var minLevel = Level(ranges.Min());
            var numLevels = maxLevel - minLevel + 1;
            var buckets = ((int)Math.Pow(factor, numLevels) - 1) / ((int)factor - 1);
            table = new Dictionary<Key, LinkedList<T>>(buckets);
            levels = new HashSet<int>(numLevels);
            foreach (var element in elements)
            {
                Add(element);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        public void Add(T element)
        {
            var bounds = element.GetAxialBounds();
            totalBounds.Append(bounds);
            var level = Level(bounds.Range.Max);
            levels.Add(level);
            var stride = Stride(level);
            var lower = (bounds.Lower / stride).Floor;
            var upper = (bounds.Upper / stride).Ceiling;
            for (var i = lower.i; i < upper.i; ++i)
            {
                for (var j = lower.j; j < upper.j; ++j)
                {
                    for (var k = lower.k; k < upper.k; ++k)
                    {
                        var key = new Key(i, j, k, level);
                        table.ExistingOrNew(key).AddLast(element);
                    }
                }
            }
        }

        /// <summary>
        /// Does not update the total bounds.
        /// </summary>
        /// <param name="element"></param>
        public void Remove(T element)
        {
            var bounds = element.GetAxialBounds();
            var level = Level(bounds.Range.Max);
            var stride = Stride(level);
            var lower = (bounds.Lower / stride).Floor;
            var upper = (bounds.Upper / stride).Ceiling;
            for (var i = lower.i; i < upper.i; ++i)
            {
                for (var j = lower.j; j < upper.j; ++j)
                {
                    for (var k = lower.k; k < upper.k; ++k)
                    {
                        var key = new Key(i, j, k, level);
                        if (table.TryGetValue(key, out var bucket))
                        {
                            bucket.Remove(element);
                            if (bucket.Count == 0)
                            {
                                table.Remove(key);
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public AxialBounds GetAxialBounds()
        {
            return totalBounds;
        }

        /// <inheritdoc/>
        public void Query(AxialBounds query, Action<T> action)
        {
            query = query.Copy().Trim(totalBounds);
            foreach (var level in levels)
            {
                var stride = Stride(level);
                var lower = (query.Lower / stride).Floor;
                var upper = (query.Upper / stride).Ceiling;
                for (var i = lower.i; i < upper.i; ++i)
                {
                    for (var j = lower.j; j < upper.j; ++j)
                    {
                        for (var k = lower.k; k < upper.k; ++k)
                        {
                            var key = new Key(i, j, k, level);
                            if (table.TryGetValue(key, out var elements))
                            {
                                foreach (var element in elements)
                                {
                                    if (query.Intersects(element.GetAxialBounds()))
                                    {
                                        action(element);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var list in table.Values)
            {
                foreach (var value in list)
                {
                    yield return value;
                }
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
