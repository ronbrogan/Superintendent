using Superintendent.CommandSink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Superintendent.Hooking
{
    public unsafe static class Hooker
    {
        private static ICommandSink commandSink;

        public static void UseCommandSink(ICommandSink commandSink)
        {
            Hooker.commandSink = commandSink;
        }

        public static void Install(Delegate method, nint address)
        {
            if (commandSink == null) throw new Exception("Call UseCommandSink first");

            if(JitAccess.MethodBodies.TryGetValue(method.Method.MethodHandle.Value, out var location))
            {
                var bytes = new Span<byte>((void*)location.Item1, location.Item2);

                commandSink.WriteAt(address, bytes);
            }
        }
    }
}
