using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrpcGateway
{
    public static class SendService
    {
        public static async Task SendWorldSave(double duration)
        {
            // The port number(5001) must match the port of the gRPC server.
            using var channel = GrpcChannel.ForAddress("http://localhost:5000");
            var client = new Events.EventsClient(channel);
            var reply = await client.WorldSaveAsync(
                              new WorldSaveDuration { Duration = duration });
            

        }

        public static async Task SendCharacterCreated(string name)
        {
            // The port number(5001) must match the port of the gRPC server.
            using var channel = GrpcChannel.ForAddress("http://localhost:5000");
            var client = new Events.EventsClient(channel);
            var reply = await client.CharacterCreatedAsync(
                              new CharacterName { Name = name });


        }
    }
}
