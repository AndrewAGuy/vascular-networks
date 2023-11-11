using System.Collections.Generic;

namespace Vascular.Intersections.Enforcement
{
    /// <summary>
    /// Tracks objects that are being penalized, and deciding when to cull.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Penalizer<T> where T : notnull
    {
        /// <summary>
        /// Tracks the score and relevance of an object.
        /// </summary>
        public class Entry
        {
            /// <summary>
            /// Points mean prizes. (The prize is death)
            /// </summary>
            public int points;

            /// <summary>
            /// Set to false after an iteration. During an iteration, set to true if freshly added or revisited.
            /// If false after all violators tracked, <see cref="points"/> gets decreased.
            /// </summary>
            public bool active = true;

            /// <summary>
            ///
            /// </summary>
            /// <param name="i"></param>
            public Entry(int i)
            {
                points = i;
            }
        }

        private readonly Dictionary<T, Entry> tracked = new();
        private readonly List<T> violators = new();
        private readonly List<T> dropping = new();

        /// <summary>
        ///
        /// </summary>
        public IReadOnlyDictionary<T, Entry> Tracked => tracked;

        /// <summary>
        /// Entries which have passed <see cref="Threshold"/>.
        /// </summary>
        public IReadOnlyList<T> Violators => violators;

        /// <summary>
        /// Entries which have fallen below 0 and are no longer being tracked.
        /// </summary>
        public IReadOnlyList<T> Dropped => dropping;

        private int penalty = 4;
        private int decay = 1;
        private int threshold = 20;

        /// <summary>
        /// The number of points for being present.
        /// </summary>
        public int Penalty
        {
            get => penalty;
            set
            {
                if (value > 0)
                {
                    penalty = value;
                }
            }
        }

        /// <summary>
        /// The number of points lost each iteration where <see cref="Entry.active"/> remains false.
        /// </summary>
        public int Decay
        {
            get => decay;
            set
            {
                // Allowed to have sitation where no reduction possible
                if (value >= 0)
                {
                    decay = value;
                }
            }
        }

        /// <summary>
        /// The threshold for culling.
        /// </summary>
        public int Threshold
        {
            get => threshold;
            set
            {
                if (value > 0)
                {
                    threshold = value;
                }
            }
        }

        /// <summary>
        /// Updates <see cref="Violators"/> and <see cref="Dropped"/>.
        /// Must pass all objects for this iteration at once - there is no incremental update.
        /// </summary>
        /// <param name="P"></param>
        public void Penalize(IEnumerable<T> P)
        {
            violators.Clear();
            dropping.Clear();

            foreach (var p in P)
            {
                if (tracked.TryGetValue(p, out var entry))
                {
                    entry.active = true;
                    entry.points += penalty;
                }
                else
                {
                    tracked[p] = new Entry(penalty);
                }
            }

            foreach (var t in tracked)
            {
                if (t.Value.points >= threshold)
                {
                    violators.Add(t.Key);
                }
                else
                {
                    if (!t.Value.active)
                    {
                        t.Value.points -= decay;
                        if (t.Value.points <= 0)
                        {
                            dropping.Add(t.Key);
                        }
                    }
                    else
                    {
                        t.Value.active = false;
                    }
                }
            }

            foreach (var d in dropping)
            {
                tracked.Remove(d);
            }

            foreach (var v in violators)
            {
                tracked.Remove(v);
            }
        }

        /// <summary>
        ///
        /// </summary>
        public void Clear()
        {
            tracked.Clear();
        }
    }
}
