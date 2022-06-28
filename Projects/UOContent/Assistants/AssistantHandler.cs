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
        Enabled = ServerConfiguration.GetOrUpdateSetting("assistants.enableNegotiation", false);

        EventSink.Login -= OnLogin;

        IncomingPackets.Register(0xBE, 0, true, &AssistVersion);
        AssistantProtocol.Register(0xFF, false, &HandshakeResponse);
    }

    private static void FailedNegotiation(Mobile m)
    {
        var isPlayer = m.AccessLevel <= AccessLevel.Player;
        var willKick = AssistantConfiguration.Settings.KickOnFailure;
        var delay = AssistantConfiguration.Settings.DisconnectDelay;

        if (willKick && isPlayer && delay <= TimeSpan.Zero)
        {
            m.NetState.Disconnect("Failed to negotiate assistant features.");
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

        m.NetState.LogInfo("Failed to negotiate assistant features.");
    }

    private static void OnLogin(Mobile m)
    {
        if (m?.NetState?.Running != true || !AssistantConfiguration.Enabled)
        {
            return;
        }

        m.NetState.SendAssistVersionReq();
        m.NetState.SendAssistHandshake();

        if (_handshakes.TryGetValue(m, out var t))
        {
            t?.Stop();
        }

        _handshakes[m] = Timer.DelayCall(TimeSpan.FromSeconds(30), OnTimeout, m);
    }

    public static void AssistVersion(NetState state, CircularBufferReader reader, int packetLength)
    {
        // We are not supporting the old UOAssist protocol
        // var assistVersion = reader.ReadInt32();
        // var clientVersion = reader.ReadAscii();

        // Instead we are supporting razor community edition.
        var assistVersion = reader.ReadAscii();
        if (assistVersion.IndexOf(' ') == -1)
        {
            state.Assistant = $"Razor {assistVersion}";
        }
        else
        {
            state.Assistant = assistVersion;
        }
    }

    private static void HandshakeResponse(NetState state, CircularBufferReader reader, int packetLength)
    {
        Mobile m = state.Mobile;

        if (m == null || !_handshakes.TryGetValue(m, out var t))
        {
            return;
        }

        t?.Stop();
        _handshakes.Remove(m);
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

        FailedNegotiation(m);
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

    public static void SendAssistVersionReq(this NetState ns)
    {
        if (ns?.CannotSendPackets() != false)
        {
            return;
        }

        // Razor doesn't actually check this, but it is part of the "spec"
        var assistVersionSupported = AssistantConfiguration.Settings.SupportedRazorVersion;
        var length = 3 + assistVersionSupported.Length;

        var writer = new SpanWriter(stackalloc byte[length]);
        writer.Write((byte)0xBE); // Packet ID
        writer.Write((ushort)length);
        writer.WriteAscii(assistVersionSupported);
    }

    public static void SendAssistHandshake(this NetState ns)
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
