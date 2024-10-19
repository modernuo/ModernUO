using System;
using System.Text.Json.Serialization;
using System.Threading;
using NATS.Client;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;

namespace Server;

[JsonSerializable(typeof(BroadcastMessage))]
internal partial class BroadcastJsonContext : JsonSerializerContext;

public record BroadcastMessage
{
    [JsonPropertyName("hue")]
    public int Hue { get; set; } = 0x482;

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public static class MessageBus
{
    private static NatsClient natsClient;
    private static INatsJSContext jetstreamContext;
    private static INatsJSConsumer incomingBroadcastsConsumer;
    private static CancellationTokenSource cancellationTokenSource;

    private static NatsJsonContextSerializer<BroadcastMessage> broadcastSerializer =
        new (BroadcastJsonContext.Default);

    public static void Start()
    {
        natsClient = new NatsClient();
        jetstreamContext = natsClient.CreateJetStreamContext();

        StartBroadcasts();
    }

    private static async void StartBroadcasts()
    {
        await jetstreamContext.CreateStreamAsync(
            new StreamConfig(name: "INCOMING_NOTIFICATIONS", subjects: ["incoming.notifications.>"])
        );

        incomingBroadcastsConsumer = await jetstreamContext.CreateOrUpdateConsumerAsync(
            "INCOMING_NOTIFICATIONS",
            new ConsumerConfig("incoming_broadcasts_consumer")
            {
                FilterSubject = "incoming.notifications.broadcast"
            }
        );

        cancellationTokenSource = new CancellationTokenSource();

        await foreach (var msg in incomingBroadcastsConsumer.ConsumeAsync(
                           cancellationToken: cancellationTokenSource.Token,
                           serializer: broadcastSerializer
                       ))
        {
            if (msg.Data == null)
            {
                Console.WriteLine($"Message Received w/ null Data");
                continue;
            }

            Console.WriteLine($"Message Received {msg.Data}");
            EventSink.InvokeIncomingMessage(msg.Data.Message, msg.Data.Hue);
            await msg.AckAsync(cancellationToken: cancellationTokenSource.Token);
        }
    }

    public static async void SendBroadcast(string message, int hue)
    {
        await natsClient.PublishAsync(
            subject: "incoming.notifications.broadcast",
            data: new BroadcastMessage()
            {
                Message = message,
                Hue = hue,
            },
            serializer: broadcastSerializer
        );
    }

    public static async void Publish<T>(string subject, T data, INatsSerializer<T> serializer = null)
    {
        await natsClient.PublishAsync<T>(subject, data);
    }

    public static void Kill()
    {
        cancellationTokenSource.Cancel();
    }
}
