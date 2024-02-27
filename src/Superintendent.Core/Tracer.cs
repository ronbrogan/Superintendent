namespace Superintendent.Core
{
    public class Tracer
    {
        public static Tracer Instance = new Tracer();

        public void TraceMicroseconds(string metric, ulong microseconds)
        {
            Logger.LogTrace($"[METRIC] {metric}: {microseconds}");
        }
    }
}
