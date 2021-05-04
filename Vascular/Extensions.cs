using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Vascular
{
    /// <summary>
    /// 
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="random"></param>
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

        /// <summary>
        /// Makes a copy of <paramref name="list"/>, then permutes and returns the elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        public static IEnumerable<T> Permutation<T>(this IList<T> list, Random random = null)
        {
            random ??= new Random();
            list = list.ToArray();
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

        /// <summary>
        /// Get all pairs in a round-robin pattern.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static IEnumerable<(T a, T b)> Pairs<T>(this IEnumerable<T> t)
        {
            var ts = t.ToArray();
            for (var i = 0; i < ts.Length; ++i)
            {
                for (var j = i + 1; j < ts.Length; ++j)
                {
                    yield return (ts[i], ts[j]);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static LinkedList<T> ToLinkedList<T>(this IEnumerable<T> source)
        {
            return new LinkedList<T>(source);
        }

        /// <summary>
        /// Converts a single <typeparamref name="T"/> instance <paramref name="t"/> to a single element array.
        /// Useful for methods that take <see cref="IEnumerable{T}"/> or arrays.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static T[] AsArray<T>(this T t)
        {
            return new T[] { t };
        }

        /// <summary>
        /// Creates a single element sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static IEnumerable<T> AsEnumerable<T>(this T t)
        {
            yield return t;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double Clamp(this double val, double min, double max)
        {
            return val < min ? min : val > max ? max : val;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int Clamp(this int val, int min, int max)
        {
            return val < min ? min : val > max ? max : val;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            return val.CompareTo(min) < 0 ? min : val.CompareTo(max) > 0 ? max : val;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        public static IEnumerable<double> CumSum(this IEnumerable<double> V)
        {
            var s = 0.0;
            foreach (var v in V)
            {
                s += v;
                yield return s;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        public static IEnumerable<int> CumSum(this IEnumerable<int> V)
        {
            var s = 0;
            foreach (var v in V)
            {
                s += v;
                yield return s;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double Dot(this IList<double> a, IList<double> b)
        {
            var total = 0.0;
            for (var i = 0; i < a.Count; ++i)
            {
                total += a[i] * b[i];
            }
            return total;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="v"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static T[] Repeat<T>(this T v, int n)
        {
            var V = new T[n];
            for (var i = 0; i < n; ++i)
            {
                V[i] = v;
            }
            return V;
        }

        /// <summary>
        /// Enumerates <paramref name="source"/>, launching a new task from each with <paramref name="run"/>.
        /// A semaphore is used to limit the number of concurrently running tasks to <paramref name="max"/>.
        /// Whether a task runs then waits for the semaphore, or whether task launching is delayed by the semaphore, is controlled by <paramref name="waitInside"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="run"></param>
        /// <param name="max"></param>
        /// <param name="waitInside"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Similar to <see cref="RunAsync{T}(IEnumerable{T}, Func{T, Task}, int, bool, CancellationToken)"/> but for synchronous tasks.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="run"></param>
        /// <param name="max"></param>
        /// <param name="waitInside"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Runs with no limit on concurrency.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="run"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task RunAsync<T>(this IEnumerable<T> source, Func<T, Task> run, CancellationToken cancellationToken = default)
        {
            return Task.WhenAll(source.Select(element => Task.Run(async () => await run(element), cancellationToken)));
        }

        /// <summary>
        /// Runs with no limit on concurrency.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="run"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task RunAsync<T>(this IEnumerable<T> source, Action<T> run, CancellationToken cancellationToken = default)
        {
            return Task.WhenAll(source.Select(element => Task.Run(() => run(element), cancellationToken)));
        }

        /// <summary>
        /// If <paramref name="key"/> is present in <paramref name="dict"/>, return the value, else returns <paramref name="def"/>.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="def"></param>
        /// <returns></returns>
        public static TReturn ValueOrDefault<TKey, TValue, TReturn>(this IDictionary<TKey, TValue> dict, TKey key, TReturn def = default)
        {
            return dict.TryGetValue(key, out var val) && val is TReturn t ? t : def;
        }

        /// <summary>
        /// If <paramref name="key"/> is present in <paramref name="dict"/>, sets to <paramref name="update"/>(current, <paramref name="value"/>).
        /// Otherwise, sets the value associated with <paramref name="key"/> using <paramref name="add"/>(<paramref name="value"/>).
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="add"></param>
        /// <param name="update"></param>
        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value,
            Func<TValue, TValue> add, Func<TValue, TValue, TValue> update)
        {
            dict[key] = dict.TryGetValue(key, out var current) ? update(current, value) : add(value);
        }

        /// <summary>
        /// If <paramref name="key"/> is present in <paramref name="dict"/>, sets to <paramref name="update"/>(current, <paramref name="value"/>).
        /// Otherwise, sets the value associated with <paramref name="key"/> to <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="update"></param>
        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value,
            Func<TValue, TValue, TValue> update)
        {
            dict[key] = dict.TryGetValue(key, out var current) ? update(current, value) : value;
        }

        /// <summary>
        /// If <paramref name="key"/> is present in <paramref name="dict"/>, sets to <paramref name="update"/>(current).
        /// Otherwise, sets the value associated with <paramref name="key"/> to <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="update"></param>
        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value,
            Func<TValue, TValue> update)
        {
            dict[key] = dict.TryGetValue(key, out var current) ? update(current) : value;
        }

        /// <summary>
        /// If <paramref name="key"/> is present in <paramref name="dict"/>, return the value.
        /// If not, add a new instance of <typeparamref name="TValue"/> and return this.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static TValue ExistingOrNew<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
        {
            if (!dict.TryGetValue(key, out var value))
            {
                value = new TValue();
                dict[key] = value;
            }
            return value;
        }

        /// <summary>
        /// Similar to <see cref="ExistingOrNew{TKey, TValue}(IDictionary{TKey, TValue}, TKey)"/> but with the default constructor
        /// requirement replaced by a function <paramref name="add"/>.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="add"></param>
        /// <returns></returns>
        public static TValue ExistingOrNew<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> add)
        {
            if (!dict.TryGetValue(key, out var value))
            {
                value = add();
                dict[key] = value;
            }
            return value;
        }

        /// <summary>
        /// Equivalent to a foreach extension method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="action"></param>
        public static void Serial<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var n in source)
            {
                action(n);
            }
        }

        /// <summary>
        /// Similar to <see cref="ArgMin{T}(IEnumerable{T}, Func{T, double}, out T, out double)"/> on the sequence filtered by <paramref name="p"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ts"></param>
        /// <param name="f"></param>
        /// <param name="p"></param>
        /// <param name="m"></param>
        /// <param name="v"></param>
        /// <returns><c>True</c> if a suitable element was found.</returns>
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

        /// <summary>
        /// Finds the value <paramref name="m"/> in <paramref name="ts"/> that minimizes <paramref name="f"/>, 
        /// returning the object and <paramref name="v"/>=<paramref name="f"/>(<paramref name="m"/>).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ts"></param>
        /// <param name="f"></param>
        /// <param name="m"></param>
        /// <param name="v"></param>
        /// <returns><c>True</c> if at least one element is in <paramref name="ts"/>.</returns>
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

        /// <summary>
        /// Similar to <see cref="ArgMin{T}(IEnumerable{T}, Func{T, double}, out T, out double)"/> but where the value isn't required and
        /// it is known that the sequence is empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ts"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static T ArgMin<T>(this IEnumerable<T> ts, Func<T, double> f)
        {
            return ts.ArgMin(f, out var m, out _)
                ? m : throw new InvalidOperationException("No entries in method that cannot return default");
        }

        /// <summary>
        /// Reads either the entire buffer, or EOF is reached.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
