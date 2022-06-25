using System;
using System.Buffers;
using System.Collections.Generic;
using Server.Gumps;
using Server.Network;

namespace Server.Assistants;

public static class AssistantHandler
{
    public static bool Enabled { get; private set; }

    private static Dictionary<Mobile, Timer> _handshakes = new();

    public static unsafe void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("assistants.enableAssistUONegotiation", false);

        EventSink.AssistantAuth += OnAssistantAuth;
        EventSink.Login -= OnLogin;
        AssistantProtocol.Register(0xFF, false, &AssistUOHandshakeResponse);
    }

    private static void OnAssistantAuth(AssistantAuthEventArgs e)
    {
        if (e.AuthOk)
        {
            return;
        }

        var m = e.State.Mobile;
        var isPlayer = m.AccessLevel <= AccessLevel.Player;
        var willKick = AssistantConfiguration.Settings.KickOnFailure;
        var delay = AssistantConfiguration.Settings.DisconnectDelay;

        if (willKick && isPlayer && delay <= TimeSpan.Zero)
        {
            e.State.Disconnect("Failed to negotiate assistant features.");
            return;
        }

        if (AssistantConfiguration.Settings.WarnOnFailure)
        {
            var warningGump = new WarningGump(
                1060635,
                30720,
                AssistantConfiguration.Settings.WarningMessage,
                0xFFC000,
                420,
                250,
                null,
                false
            );

            m.SendGump(warningGump);
        }

        if (isPlayer)
        {
            _handshakes[m] = Timer.DelayCall(delay, OnForceDisconnect, m);
        }

        e.State.LogInfo("Failed to negotiate assistant features.");
    }

    private static void OnLogin(Mobile m)
    {
        if (m?.NetState?.Running != true || !AssistantConfiguration.Enabled)
        {
            return;
        }

        m.NetState.SendAssistUOHandshake();

        if (_handshakes.TryGetValue(m, out var t))
        {
            t?.Stop();
        }

        _handshakes[m] = Timer.DelayCall(TimeSpan.FromSeconds(30), OnTimeout, m);
    }

    private static void AssistUOHandshakeResponse(NetState state, CircularBufferReader reader, int packetLength)
    {
        Mobile m = state.Mobile;

        if (m == null || !_handshakes.TryGetValue(m, out var t))
        {
            return;
        }

        t?.Stop();
        _handshakes.Remove(m);

        EventSink.InvokeAssistantAuth(new AssistantAuthEventArgs(m.NetState, m.Account, true));
    }

    private static void OnTimeout(Mobile m)
    {
        if (m == null || !_handshakes.TryGetValue(m, out var t))
        {
            return;
        }

        t?.Stop();
        _handshakes.Remove(m);

        if (m.NetState?.Running != true || !Enabled)
        {
            return;
        }

        EventSink.InvokeAssistantAuth(new AssistantAuthEventArgs(m.NetState, m.Account, false));
    }

    private static void OnForceDisconnect(Mobile m)
    {
        if (m == null)
        {
            return;
        }

        m.NetState?.Disconnect($"Player {m} kicked (Failed assistant handshake)");
        _handshakes.Remove(m);
    }

    public static void SendAssistUOHandshake(this NetState ns)
    {
        if (ns?.CannotSendPackets() != false)
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[12]);
        writer.Write((byte)0xF0); // Packet ID
        writer.Write((ushort)12);
        writer.Write((byte)0xFE); // Command
        writer.Write((ulong)AssistantConfiguration.Settings.DisallowedFeatures);
    }
}
