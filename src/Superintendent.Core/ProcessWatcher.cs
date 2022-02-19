using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Superintendent.Core
{
    public class ProcessWatcher : IDisposable
    {
        private Task? runTask;
        private CancellationTokenSource runCts = new CancellationTokenSource();
        private string[] processNames;
        private bool disposedValue;

        public Process? CurrentProcess { get; private set; } = null;

        private Func<Process, bool>? callback;
        private Action? detachCallback;
        private Action<int, Exception?>? failCallback;

        public ProcessWatcher(params string[] processNames)
        {
            this.processNames = processNames;
        }

        public void Run(Func<Process, bool> foundProcess, Action? processExit = null, Action<int, Exception?>? failureCallback = null)
        {
            if (runTask != null)
            {
                throw new Exception("Watcher is already running");
            }

            callback = foundProcess;
            detachCallback = processExit;
            failCallback = failureCallback;

            this.runTask = this.PollProcesses();
        }

        public async Task Stop()
        {
            this.runCts.Cancel();
            
            if(this.runTask != null)
            {
                await this.runTask;
                this.runTask = null;
            }
        }

        private void TryAttach()
        {
            foreach (var name in this.processNames)
            {
                var proc = Process.GetProcessesByName(name);

                if (proc != null && proc.Length > 0 && !proc[0].HasExited)
                {
                    if(this.callback?.Invoke(proc[0]) ?? false)
                    {
                        if(this.CurrentProcess != null)
                        {
                            this.CurrentProcess.Exited -= HandleProcessExit;
                            this.CurrentProcess.EnableRaisingEvents = false;
                        }

                        this.CurrentProcess = proc[0];
                        this.CurrentProcess.EnableRaisingEvents = true;
                        this.CurrentProcess.Exited += HandleProcessExit;
                        return;
                    }
                }
            }
        }

        private void HandleProcessExit(object? sender, EventArgs e)
        {
            this.detachCallback?.Invoke();
            this.CurrentProcess = null;
        }

        private async Task PollProcesses()
        {
            while(!this.runCts.IsCancellationRequested)
            {
                if(this.CurrentProcess != null && this.CurrentProcess.HasExited)
                {
                    this.detachCallback?.Invoke();
                    this.CurrentProcess = null;
                }

                if(this.CurrentProcess == null)
                {
                    try
                    {
                        this.TryAttach();
                    }
                    catch (Exception ex)
                    {
                        this.failCallback?.Invoke(this.CurrentProcess?.Id ?? 0, ex);
                    }
                }

                await Task.Delay(1000, this.runCts.Token);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.runCts?.Cancel();
                    this.runCts?.Dispose();

                    if (this.CurrentProcess != null)
                    {
                        this.detachCallback?.Invoke();
                        this.CurrentProcess = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
        }
    }
}
