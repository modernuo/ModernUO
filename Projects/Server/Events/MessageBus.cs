using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Net;

namespace Server;

public static class MessageBus
{
    private static NatsClient natsClient;
    public static NatsClient Client => natsClient;

    private static INatsJSContext jetstreamContext;
    public static INatsJSContext Context => jetstreamContext;

    public static void Start()
    {
        natsClient = new NatsClient();
        jetstreamContext = natsClient.CreateJetStreamContext();
    }
}
