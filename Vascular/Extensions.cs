using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vascular
{
    public static class Extensions
    {
        public static void Permute<T>(this IList<T> list, Random random = null)
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

        public static IEnumerable<T> Permutation<T>(this IList<T> list, Random random = null)
        {
            random ??= new Random();
            for (var i = list.Count - 1; i > 0; --i)
            {
                var swap = random.Next(i + 1);
                var temp = list[i];
                list[i] = list[swap];
                list[swap] = temp;
                yield return list[i];
            }
            yield return list[0];
        }

        public static LinkedList<T> ToLinkedList<T>(this IEnumerable<T> source)
        {
            return new LinkedList<T>(source);
        }

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

        public static async Task RunAsync<T>(this IEnumerable<T> source, Func<T, Task> run, int max,
            bool waitInside = false, CancellationToken cancellationToken = default)
        {
            using var semaphore = new SemaphoreSlim(max);
            if (waitInside)
            {
                await Task.WhenAll(source.Select(item =>
                    Task.Run(async () =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            await run(item);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken)));
            }
            else
            {
                var tasks = new List<Task>(source.Count());
                foreach (var item in source)
                {
                    await semaphore.WaitAsync(cancellationToken);
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await run(item);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken));
                }
                await Task.WhenAll(tasks);
            }
        }

        public static async Task RunAsync<T>(this IEnumerable<T> source, Action<T> run, int max,
            bool waitInside = false, CancellationToken cancellationToken = default)
        {
            using var semaphore = new SemaphoreSlim(max);
            if (waitInside)
            {
                await Task.WhenAll(source.Select(item =>
                   Task.Run(async () =>
                   {
                       await semaphore.WaitAsync(cancellationToken);
                       try
                       {
                           run(item);
                       }
                       finally
                       {
                           semaphore.Release();
                       }
                   }, cancellationToken)));
            }
            else
            {
                var tasks = new List<Task>(source.Count());
                foreach (var item in source)
                {
                    await semaphore.WaitAsync(cancellationToken);
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            run(item);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken));
                }
                await Task.WhenAll(tasks);
            }
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

        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value, Func<TValue, TValue> add, Func<TValue, TValue, TValue> update)
        {
            dict[key] = dict.TryGetValue(key, out var current) ? update(current, value) : add(value);
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

        public static bool MinSuitable<T>(this IEnumerable<T> ts, Func<T, double> f, Predicate<T> p, out T m, out double v)
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

        public static bool ArgMin<T>(this IEnumerable<T> ts, Func<T, double> f, out T m, out double v)
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

        public static async Task<int> ReadBufferAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken = default)
        {
            // Patches async stream reading.
            // From the docs for ReadAsync:
            // "The result value can be less than the number of bytes allocated in the buffer if that many bytes are not currently available, 
            // or it can be 0 (zero) if the end of the stream has been reached."
            // That there is no version that only returns less if EOF reached is ridiculous.
            var total = 0;
            while (total < buffer.Length)
            {
                var memory = new Memory<byte>(buffer, total, buffer.Length - total);
                var read = await stream.ReadAsync(memory, cancellationToken);
                if (read == 0)
                {
                    return total;
                }
                total += read;
            }
            return total;
        }
    }
}
