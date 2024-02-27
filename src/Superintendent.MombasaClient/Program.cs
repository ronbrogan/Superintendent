
using Grpc.Net.Client;
using Mombasa;
using System.Text;

public class Program
{

    public static async Task Main(string[] args)
    {
        var channel = GrpcChannel.ForAddress("http://127.0.0.1:50051");
        var bridge = new MombasaBridge.MombasaBridgeClient(channel);

        
        var address = Convert.ToUInt64(Console.ReadLine(), 16);
        Console.WriteLine(address.ToString("x16"));

        var read = bridge.ReadMemory(new MemoryReadRequest()
        {
            Address = address,
            Count = 8
        });

        Console.WriteLine(Encoding.UTF8.GetString(read.Data.ToByteArray()));

        try
        {
            var resp = bridge.GetThreadLocalPointer(new GetThreadLocalPointerRequest());
            Console.WriteLine(resp.Value.ToString("x16"));

            Console.WriteLine("No exception!");
        }
        catch(Exception e)
        {
            Console.WriteLine("Exception caught: " + e.ToString());
        }
        

        //var resp = bridge.PollMemory(new MemoryPollRequest()
        //{
        //    Address = address,
        //    Count = 10,
        //    Interval = 15
        //});

        //var cts = new CancellationTokenSource();

        //Console.CancelKeyPress += (s,e) => cts.Cancel();

        //long lastWrite = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        //long count = 0;

        //while(await resp.ResponseStream.MoveNext(cts.Token))
        //{
        //    var item = resp.ResponseStream.Current;
        //    count++;

        //    //Console.WriteLine(item.Data.ToString(Encoding.UTF8));

        //    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        //    if (now > lastWrite)
        //    {
        //        lastWrite = now;
        //        var lastCount = Interlocked.Exchange(ref count, 0);
        //        Console.Write("\rRPS: " + lastCount);
        //    }
        //}
    }
}
