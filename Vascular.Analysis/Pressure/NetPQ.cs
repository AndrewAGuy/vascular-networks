using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vascular.Analysis.Pressure
{
    /// <summary>
    /// Wraps a python process that solves for pressures.
    /// </summary>
    public class NetPQ : IAsyncDisposable
    {
        private readonly Process process;
        private readonly Task error;

        /// <summary>
        /// 
        /// </summary>
        public Stream Input => process.StandardInput.BaseStream;

        /// <summary>
        /// 
        /// </summary>
        public Stream Output => process.StandardOutput.BaseStream;

        private readonly byte[] quitBuffer = new byte[sizeof(int)];
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        /// <summary>
        /// How long to wait on termination.
        /// </summary>
        public int TimeoutMilliseconds { get; set; } = 1000;

        /// <summary>
        /// For options, see NetPQ.py.
        /// </summary>
        /// <param name="pythonPath">Path to the python executable.</param>
        /// <param name="scriptPath">Path to the NetPQ.py script.</param>
        /// <param name="errorPath">Path to write stderr to.</param>
        /// <param name="sparse"></param>
        /// <param name="solver"></param>
        /// <param name="densify"></param>
        /// <param name="verbose"></param>
        public NetPQ(string pythonPath, string scriptPath, string errorPath = null,
            bool sparse = false, string solver = null, bool densify = false, bool verbose = false)
        {
            var args = new StringBuilder(scriptPath)
                .Append($"{(sparse ? " -s" : "")}")
                .Append($"{(solver != null ? $" -S {solver}" : "")}")
                .Append($"{(densify ? " -d" : "")}")
                .Append($"{(verbose ? " -v" : "")}")
                .ToString();
            var stderr = !string.IsNullOrWhiteSpace(errorPath);
            var info = new ProcessStartInfo(pythonPath, args)
            {
                UseShellExecute = false,
                RedirectStandardError = stderr,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                StandardErrorEncoding = Encoding.UTF8
            };
            process = Process.Start(info);
            error = stderr
                ? Task.Run(async () =>
                {
                    using var file = new FileStream(errorPath, FileMode.Create, FileAccess.Write);
                    await process.StandardError.BaseStream.CopyToAsync(file, cancellation.Token);
                })
                : Task.CompletedTask;
            var writer = new BinaryWriter(new MemoryStream(quitBuffer));
            writer.Write((int)Commands.Quit);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            async Task send()
            {
                try
                {
                    await this.Input.WriteAsync(quitBuffer, cancellation.Token);
                    await this.Input.FlushAsync(cancellation.Token);
                }
                catch { }
            }
            _ = send();
            try
            {
                var exit = Task.WhenAll(process.WaitForExitAsync(cancellation.Token), error);
                var first = await Task.WhenAny(exit, Task.Delay(this.TimeoutMilliseconds, cancellation.Token));
                if (first != exit && !process.HasExited)
                {
                    process.Kill();
                }
                first = await Task.WhenAny(exit, Task.Delay(this.TimeoutMilliseconds, cancellation.Token));
                if (first != exit)
                {
                    cancellation.Cancel();
                }
            }
            catch { }
        }
    }
}
