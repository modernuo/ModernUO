using Grpc.Net.Client;
using System;
using System.Threading.Tasks;

namespace GrpcGateway
{
    public static class Methodes
    {

        public static async Task Greet()
        {
            // The port number(5001) must match the port of the gRPC server.
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new Greeter.GreeterClient(channel);
            var reply = await client.SayHelloAsync(
                              new HelloRequest { Name = "GreeterClient" });
            Console.WriteLine("Greeting: " + reply.Message);
            Console.WriteLine("Press any key to exit...");
            
        }
    }
}
