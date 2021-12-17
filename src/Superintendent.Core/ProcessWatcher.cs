using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Superintendent.Core
{
    public class ProcessWatcher : IDisposable
    {
        private Task? runTask;
        private CancellationTokenSource? runCts;
        private string[] processNames;
        private bool disposedValue;

        public Process? CurrentProcess { get; private set; } = null;

        private List<Action<Process>> callbacks = new();
        private List<Action> detachCallbacks = new();

        public ProcessWatcher(params string[] processNames)
        {
            this.processNames = processNames;
        }

        public async Task Run(Action<Process> foundProcess, Action? processExit = null)
        {
            if(runCts != null)
            {
                if (runCts.IsCancellationRequested)
                {
                    runCts.Cancel();
                    runCts.Dispose();
                    runCts = null;
                    callbacks.Clear();
                }
                else
                {
                    callbacks.Add(foundProcess);
                    return;
                }
            }

            if (runTask != null)
            {
                await this.runTask;
            }

            callbacks.Add(foundProcess);

            if(processExit != null)
                detachCallbacks.Add(processExit);

            this.TryAttach();
            this.runCts = new CancellationTokenSource();
            this.runTask = this.PollProcesses();
        }

        public async Task Stop()
        {
            this.runCts.Cancel();
            await this.runTask;
            this.runTask = null;
        }

        private void TryAttach()
        {
            foreach (var name in this.processNames)
            {
                var proc = Process.GetProcessesByName(name);

                if (proc != null && proc.Length > 0 && !proc[0].HasExited)
                {
                    var running = DateTime.Now - proc[0].StartTime;

                    // Don't attach during first 15 seconds of process to allow for process bootstrapping
                    if (running.TotalSeconds < 15)
                        continue;

                    this.CurrentProcess = proc[0];

                    foreach (var foundProcess in this.callbacks)
                        foundProcess(proc[0]);
                }
            }
        }

        private async Task PollProcesses()
        {
            while(!this.runCts.IsCancellationRequested)
            {
                if(this.CurrentProcess != null && this.CurrentProcess.HasExited)
                {
                    foreach (var cb in this.detachCallbacks)
                        cb();

                    this.CurrentProcess = null;
                }

                if(this.CurrentProcess == null)
                {
                    this.TryAttach();
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
                    this.runCts.Dispose();
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
