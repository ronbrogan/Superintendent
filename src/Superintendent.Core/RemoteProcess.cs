using Superintendent.Core.Remote;
using System;
using System.Diagnostics;

namespace Superintendent.Core
{
    public abstract class RemoteProcess
    {
        public RpcRemoteProcess? Rpc { get; private set; }

        public RemoteProcess()
        {
            this.Rpc = new RpcRemoteProcess();
            Rpc.ProcessAttached += (s, a) => this.OnAttach(a);
            Rpc.ProcessDetached += (s, a) => this.OnDetach();
            Rpc.AttachException += (s, a) => this.OnAttachException(a);
            Rpc.Attach(this.AttachGuard, this.ProcessNames);
        }

        public abstract string[] ProcessNames { get; }
        public virtual bool AttachGuard(Process p) => true;
        public virtual void OnAttach(ProcessAttachArgs attach) { }
        public virtual void OnDetach() { }
        public virtual void OnAttachException(AttachExceptionArgs except) { }
    }

    public class RemoteProcessBuilder
    {
        private readonly string[] processNames;
        private Func<Process, bool>? guard;
        private Action<ProcessAttachArgs>? attach;
        private Action? detach;
        private Action<AttachExceptionArgs>? except;

        public RemoteProcessBuilder(params string[] processNames)
        {
            this.processNames = processNames;
        }

        public RemoteProcessBuilder WithAttachGuard(Func<Process, bool> guard)
        {
            this.guard = guard;
            return this;
        }

        public RemoteProcessBuilder WithAttachCallback(Action<ProcessAttachArgs> attach)
        {
            this.attach = attach;
            return this;
        }

        public RemoteProcessBuilder WithAttachExceptionHandler(Action<AttachExceptionArgs> except)
        {
            this.except = except;
            return this;
        }

        public RemoteProcessBuilder WithDetachCallback(Action detach)
        {
            this.detach = detach;
            return this;
        }

        public RpcRemoteProcess AttachRpc()
        {
            var proc = new RpcRemoteProcess();
            proc.ProcessAttached += (s, a) => this.attach?.Invoke(a);
            proc.ProcessDetached += (s, a) => this.detach?.Invoke();
            proc.AttachException += (s, a) => this.except?.Invoke(a);
            proc.Attach(this.guard, this.processNames);
            return proc;
        }
    }
}
