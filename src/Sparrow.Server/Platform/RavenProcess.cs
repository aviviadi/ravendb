using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Sparrow.Logging;

namespace Sparrow.Server.Platform
{
    public class ProcessExitedEventArgs : EventArgs
    {
        public int ExitCode { get; set; }
        public IntPtr Pid { get; set; }
    }

    public class LineOutputEventArgs : EventArgs
    {
        public string line { get; set; }
    }

    public class RavenProcess : IDisposable
    {
        public ProcessStartInfo StartInfo { get; set; }
        private bool _hasExited;

        public event EventHandler ProcessExited;
        private void OnProcessExited(EventArgs e)
        {
            EventHandler handler = ProcessExited;
            handler?.Invoke(this, e);
        }

        public event EventHandler LineOutput;
        private void OnLineOutput(EventArgs e, CancellationToken ctk)
        {
            if (ctk.IsCancellationRequested == false)
            {
                EventHandler handler = LineOutput;
                handler?.Invoke(this, e);
            }
        }

        private readonly Logger _logger = LoggingSource.Instance.GetLogger<RavenProcess>("RavenProcess");
        private IntPtr Pid = IntPtr.Zero;
        private SafeFileHandle StandardOutAndErr { get; set; }
        private SafeFileHandle StandardIn { get; set; }
        private CancellationToken _ctk;

        public void Start(CancellationToken ctk)
        {
            _ctk = ctk;
            if (StartInfo?.FileName == null)
                throw new InvalidOperationException("RavenProcess Start() must be supplied with valid startInfo object and set Filename");

            Console.WriteLine("ADIADI::spawning : " + StartInfo.FileName + " " + StartInfo.Arguments);
            var rc = Pal.rvn_spawn_process(StartInfo.FileName, StartInfo.Arguments, out var pid, out var stdin, out var stdout, out var errorCode);
            if (rc != PalFlags.FailCodes.Success)
                PalHelper.ThrowLastError(rc, errorCode, $"Failed to spawn command '{StartInfo.FileName} {StartInfo.Arguments}'");

            Pid = pid;
            StandardOutAndErr = stdout;
            StandardIn = stdin;
        }

        public static void Start(string filename, string arguments)
        {
            using (var ravenProcess = new RavenProcess {StartInfo = new ProcessStartInfo {FileName = filename, Arguments = arguments}})
            {
                ravenProcess.Start(CancellationToken.None);
                ravenProcess.ReadTo(Console.Out);
            }
        }

        private void ReadTo(TextWriter output)
        {
            using (var fs = new FileStream(StandardOutAndErr, FileAccess.Read))
            {
                var buffer = new byte[4096];
                var read = fs.Read(buffer, 0, 4096);
                while (read != 0)
                {
                    output.Write(Encoding.UTF8.GetString(buffer, 0, read));
                    output.Flush();
                    if (output == Console.Out)
                        Console.Out.Flush();
                    read = fs.Read(buffer, 0, 4096);
                }
            }
        }

        public Task<string> ReadToEndAsync()
        {
            Console.WriteLine("ADIADI::ReadToEndAsync : " + StartInfo.FileName + " " + StartInfo.Arguments);
            using (var fs = new FileStream(StandardOutAndErr, FileAccess.Read))
            using (var sr = new StreamReader(fs))
            {
                return sr.ReadToEndAsync();
            }
        }

        private void Kill()
        {
            Console.WriteLine("ADIADI::Kill : " + StartInfo.FileName + " " + StartInfo.Arguments);
            if (Pid != IntPtr.Zero)
            {
                var rc = Pal.rvn_kill_process(Pid, out var errorCode);
                if (rc != PalFlags.FailCodes.Success)
                    PalHelper.ThrowLastError(rc, errorCode, $"Failed to kill proc id={Pid.ToInt64()}. Command: '{StartInfo.FileName} {StartInfo.Arguments}'");
            }
        }

        public static void Execute(string command, string arguments, int pollingTimeoutInSeconds, EventHandler exitHandler, EventHandler lineOutputHandler, CancellationToken ctk)
        {
            Console.WriteLine("ADIADI::Execute " + command + " " + arguments);
            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments
            };

            using (var process = new RavenProcess { StartInfo = startInfo })
            {
                if (exitHandler != null)
                    process.ProcessExited += exitHandler;
                if (lineOutputHandler != null)
                    process.LineOutput += lineOutputHandler;

                process.Start(ctk);

                using (var fs = new FileStream(process.StandardOutAndErr, FileAccess.Read))
                {
                    while (ctk.IsCancellationRequested == false)
                    {
                        var rc = Pal.rvn_wait_for_close_process(process.Pid, pollingTimeoutInSeconds, out var exitCode, out var errorCode);
                        Console.WriteLine($"ADIADI::wait rc={rc}, {exitCode}, {errorCode}");
                        if (rc == PalFlags.FailCodes.Success ||
                            rc == PalFlags.FailCodes.FailChildProcessFailure)
                        {
                            process._hasExited = true;
                            var args = new ProcessExitedEventArgs
                            {
                                ExitCode = exitCode,
                                Pid = process.Pid
                            };

                            process.OnProcessExited(args);
                        }


                        //process.ReadTo(Console.Out);

                        try
                        {
                            string read = null;
                            do
                            {
                                read = process.ReadLineAsync(fs, ctk).Result;
                                if (read != null)
                                {
                                    var args = new LineOutputEventArgs() {line = read};
                                    process.OnLineOutput(args, ctk);
                                }
                                else
                                {
                                    var args = new LineOutputEventArgs() {line = null};
                                    process.OnLineOutput(args, ctk);
                                }
                            } while (read != null && ctk.IsCancellationRequested == false);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("ADIADI::ERR=" + e.Message);
                        }

                        if (process._hasExited)
                            break;
                    }
                }

                Console.WriteLine("ADIADI::done loop");
            }
        }

        private Task<string> ReadLineAsync(FileStream fs, CancellationToken ctk)
        {
            // Console.WriteLine("ADIADI::ReadLineAsync : " + StartInfo.FileName + " " + StartInfo.Arguments);
            StringBuilder sb = null;
            var buffer = new byte[1];
            var read = fs.Read(buffer, 0, 1);
            while (read != 0)
            {
                if (sb == null)
                    sb = new StringBuilder();

                var c = Encoding.UTF8.GetString(buffer, 0, read);

                if (buffer[0] == '\n')
                    break;

                sb.Append(c);

                if (ctk.IsCancellationRequested)
                    break;

                read = fs.Read(buffer, 0, 1);
            }
            return Task.FromResult(sb?.ToString());
        }

        public void Dispose()
        {
            Console.WriteLine("ADIADI::Dispose");
            if (_hasExited == false)
            {
                _hasExited = true;
                try
                {
                    var _ = ReadToEndAsync().Result;
                }
                catch
                {
                    // nothing.. just clear buffers
                }

                var rc = Pal.rvn_wait_for_close_process(Pid, 5, out var exitCode, out var errorCode); // ADIADI::what timeout to set ?  5 secs?
                if (rc != PalFlags.FailCodes.FailTimeout)
                    return;

                Console.WriteLine($"ADIADI::Waited 5 second for {StartInfo.FileName} to close, but it didn't, trying to kill");
                if (_logger.IsInfoEnabled)
                    _logger.Info($"Waited 5 seconds for {StartInfo.FileName} to close, but it didn't, trying to kill");
                try
                {
                    Kill();
                }
                catch (Exception ex)
                {
                    if (_logger.IsInfoEnabled)
                        _logger.Info($"Kill {StartInfo.FileName} failed", ex);
                }
            }
        }
    }
}