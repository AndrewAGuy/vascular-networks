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

        public static async Task RunAsync<T>(this IEnumerable<T> source, Func<T, Task> run, int max, CancellationToken cancellationToken = default)
        {
            using var semaphore = new SemaphoreSlim(max);
            await Task.WhenAll(source.Select(element =>
                Task.Run(async () =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        await run(element);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken)));
        }

        public static async Task RunAsync<T>(this IEnumerable<T> source, Action<T> run, int max, CancellationToken cancellationToken = default)
        {
            using var semaphore = new SemaphoreSlim(max);
            await Task.WhenAll(source.Select(element =>
                Task.Run(async () =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        run(element);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken)));
        }

        public static Task RunAsync<T>(this IEnumerable<T> source, Func<T, Task> run, CancellationToken cancellationToken = default)
        {
            return Task.WhenAll(source.Select(element => Task.Run(async () => await run(element), cancellationToken)));
        }

        public static Task RunAsync<T>(this IEnumerable<T> source, Action<T> run, CancellationToken cancellationToken = default)
        {
            return Task.WhenAll(source.Select(element => Task.Run(() => run(element), cancellationToken)));
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

        public delegate double CostFunction<T>(T t);

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
