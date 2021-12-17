using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Superintendent.Core
{
    public class Tracer
    {
        public static Tracer Instance = new Tracer();

        public void TraceMicroseconds(string metric, ulong microseconds)
        {
            Console.WriteLine($"[METRIC] {metric}: {microseconds}");
        }
    }
}
