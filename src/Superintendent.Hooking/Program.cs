using Superintendent.Core.Remote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Superintendent.Hooking
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            JitAccess.Setup();

            SomeMethodHook(1, 2);

            Hooker.UseCommandSink(new RpcRemoteProcess());
            Hooker.Install(SomeMethodHook, 1234);
        }

        public static int SomeMethodHook(int a, int b)
        {
            return a + b;
        }
    }
}
