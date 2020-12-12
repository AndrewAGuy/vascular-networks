using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vascular
{
    public static class Extensions
    {
        public static double Clamp(this double val, double min, double max)
        {
            return val < min ? min : val > max ? max : val;
        }

        public static int Clamp(this int val, int min, int max)
        {
            return val < min ? min : val > max ? max : val;
        }

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            return val.CompareTo(min) < 0 ? min : val.CompareTo(max) > 0 ? max : val;
        }

        public static async Task RunAsync<T>(this IEnumerable<T> source, Func<T, Task> run, int max, int taskCount = 0)
        {
            using var semaphore = new SemaphoreSlim(max);
            var tasks = new List<Task>(taskCount);
            foreach (var element in source)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await semaphore.WaitAsync();
                        await run(element);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
            // Must now await and not return the created task as we would dispose of the semaphore
            await Task.WhenAll(tasks);
        }

        public static Task RunAsync<T>(this IEnumerable<T> source, Action<T> run, int taskCount = 0)
        {
            var tasks = new List<Task>(taskCount);
            foreach (var element in source)
            {
                tasks.Add(Task.Run(() => run(element)));
            }
            return Task.WhenAll(tasks);
        }

        public static TReturn ValueOrDefault<TKey, TValue, TReturn>(this IDictionary<TKey, TValue> dict, TKey key, TReturn def = default)
        {
            return dict.TryGetValue(key, out var val) && val is TReturn t ? t : def;
        }

        public static TValue ExistingOrNew<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
        {
            if (!dict.TryGetValue(key, out var value))
            {
                value = new TValue();
                dict[key] = value;
            }
            return value;
        }

        public static TValue ExistingOrNew<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> add)
        {
            if (!dict.TryGetValue(key, out var value))
            {
                value = add();
                dict[key] = value;
            }
            return value;
        }

        public static void Serial<T>(this IEnumerable<T> networks, Action<T> action)
        {
            foreach (var n in networks)
            {
                action(n);
            }
        }

        //public static void Parallel<T>(this IEnumerable<T> networks, Action<T> action)
        //{
        //    var tasks = new List<Task>();
        //    foreach (var n in networks)
        //    {
        //        tasks.Add(Task.Run(() => action(n)));
        //    }
        //    tasks.WaitAll();
        //}

        public static void Permute<T>(this List<T> list, Random random = null)
        {
            random ??= new Random();
            for (var i = list.Count - 1; i > 0; i--)
            {
                var swap = random.Next(i + 1);
                var temp = list[i];
                list[i] = list[swap];
                list[swap] = temp;
            }
        }

        public delegate double CostFunction<T>(T t);

        public static T MinSuitable<T>(this IEnumerable<T> ts, CostFunction<T> f, Predicate<T> p) where T : class
        {
            T b = null;
            var v = 0.0;
            foreach (var t in ts)
            {
                if (p(t))
                {
                    var c = f(t);
                    if (b == null)
                    {
                        b = t;
                        v = c;
                    }
                    else if (c < v)
                    {
                        v = c;
                        b = t;
                    }
                }
            }
            return b;
        }

        public static T ArgMin<T>(this IEnumerable<T> ts, CostFunction<T> f) where T : class
        {
            T m = null;
            var v = 0.0;
            foreach (var t in ts)
            {
                var c = f(t);
                if (m == null)
                {
                    m = t;
                    v = c;
                }
                else if (c < v)
                {
                    m = t;
                    v = c;
                }
            }
            return m;
        }

        public static bool MinSuitable<T>(this IEnumerable<T> ts, CostFunction<T> f, Predicate<T> p, out T m, out double v)
        {
            m = default;
            v = double.PositiveInfinity;
            var e = ts.GetEnumerator();
            while (e.MoveNext())
            {
                var x = e.Current;
                if (p(x))
                {
                    v = f(x);
                    m = x;
                    goto NON_EMPTY;
                }
            }
            return false;

        NON_EMPTY:
            while (e.MoveNext())
            {
                var x = e.Current;
                if (p(x))
                {
                    var c = f(x);
                    if (c < v)
                    {
                        v = c;
                        m = x;
                    }
                }
            }
            return true;
        }

        public static bool ArgMin<T>(this IEnumerable<T> ts, CostFunction<T> f, out T m, out double v)
        {
            var e = ts.GetEnumerator();
            if (e.MoveNext())
            {
                m = e.Current;
                v = f(m);
                while (e.MoveNext())
                {
                    var x = e.Current;
                    var c = f(x);
                    if (c < v)
                    {
                        v = c;
                        m = x;
                    }
                }
                return true;
            }
            else
            {
                m = default;
                v = double.PositiveInfinity;
                return false;
            }
        }
    }
}
