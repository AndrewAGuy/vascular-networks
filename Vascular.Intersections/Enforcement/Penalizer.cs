using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Vascular.Intersections.Enforcement
{
    public class Penalizer<T>
    {
        public class Entry
        {
            public int points;
            public bool active = true;
            public Entry(int i)
            {
                points = i;
            }
        }

        private readonly Dictionary<T, Entry> tracked = new Dictionary<T, Entry>();
        private readonly List<T> violators = new List<T>();
        private readonly List<T> dropping = new List<T>();

        public IReadOnlyDictionary<T, Entry> Tracked => tracked;
        public IReadOnlyList<T> Violators => violators;
        public IReadOnlyList<T> Dropped => dropping;

        private int penalty = 4;
        private int decay = 1;
        private int threshold = 20;

        public int Penalty
        {
            get
            {
                return penalty;
            }
            set
            {
                if (value > 0)
                {
                    penalty = value;
                }
            }
        }

        public int Decay
        {
            get
            {
                return decay;
            }
            set
            {
                // Allowed to have sitation where no reduction possible
                if (value >= 0)
                {
                    decay = value;
                }
            }
        }

        public int Threshold
        {
            get
            {
                return threshold;
            }
            set
            {
                if (value > 0)
                {
                    threshold = value;
                }
            }
        }

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

        public void Clear()
        {
            tracked.Clear();
        }
    }
}
