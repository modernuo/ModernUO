using System;
using System.Text.Json.Serialization;
using System.Threading;
using Server.Network;
using NATS.Client;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;

namespace Server.Commands;

[JsonSerializable(typeof(BroadcastPayload))]
internal partial class BroadcastJsonContext : JsonSerializerContext;

public record BroadcastPayload
{
    [JsonPropertyName("hue")]
    public int Hue { get; set; } = 0x482;

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public static class Broadcast
{
    private static INatsJSConsumer incomingBroadcastsConsumer;

    private static readonly NatsJsonContextSerializer<BroadcastPayload> broadcastSerializer =
        new(BroadcastJsonContext.Default);

    public static void Configure()
    {
        CommandSystem.Register("BCast", AccessLevel.GameMaster, BroadcastMessage_OnCommand);
        CommandSystem.Register("SMsg", AccessLevel.Counselor, StaffMessage_OnCommand);
    }

    public static void Initialize()
    {
        StartBroadcasts();
    }

    [Usage("BCast <text>")]
    [Aliases("B", "BC")]
    [Description("Broadcasts a message to everyone online.")]
    public static void BroadcastMessage_OnCommand(CommandEventArgs e)
    {
        EmitBroadcast($"Staff message from {e.Mobile.Name}:", 0x482);
        EmitBroadcast($"{e.ArgString}", 0x21);
        // BroadcastMessage(AccessLevel.Player, 0x482, $"Staff message from {e.Mobile.Name}:");
        // BroadcastMessage(AccessLevel.Player, 0x482, e.ArgString);
    }

    [Usage("SMsg <text>")]
    [Aliases("S", "SM")]
    [Description("Broadcasts a message to all online staff.")]
    public static void StaffMessage_OnCommand(CommandEventArgs e)
    {
        BroadcastMessage(AccessLevel.Counselor, e.Mobile.SpeechHue, $"[{e.Mobile.Name}] {e.ArgString}");
    }

    public static async void EmitBroadcast(string message, int hue)
    {
        await MessageBus.Client.PublishAsync(
            subject: "broadcast.all",
            data: new BroadcastPayload()
            {
                Message = message,
                Hue = hue,
            },
            serializer: broadcastSerializer
        );
    }

    public static void BroadcastMessage(AccessLevel ac, int hue, string message)
    {
        foreach (var state in NetState.Instances)
        {
            var m = state.Mobile;

            if (m?.AccessLevel >= ac)
            {
                m.SendMessage(hue, message);
            }
        }
    }

    private static async void StartBroadcasts()
    {
        await MessageBus.Context.CreateStreamAsync(
            new StreamConfig(name: "BROADCASTS", subjects: ["broadcast.>"])
        );

        incomingBroadcastsConsumer = await MessageBus.Context.CreateOrUpdateConsumerAsync(
            "BROADCASTS",
            new ConsumerConfig("broadcast_incoming_consumer")
        );

        CancellationTokenSource cancellationTokenSource = new();

        await foreach (var msg in incomingBroadcastsConsumer.ConsumeAsync(
                           serializer: broadcastSerializer,
                           cancellationToken: cancellationTokenSource.Token
                       ))
        {
            if (msg.Data == null)
            {
                Console.WriteLine($"Message Received w/ null Data");
                continue;
            }

            Console.WriteLine($"Message Received {msg.Data}");
            EventSink.InvokeIncomingMessage(msg.Data.Message, msg.Data.Hue);
            await msg.AckAsync(
                cancellationToken: cancellationTokenSource.Token
            );
        }
    }
}
