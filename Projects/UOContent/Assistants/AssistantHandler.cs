using System;
using System.Buffers;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Assistants;

public static class AssistantHandler
{
    public static bool Enabled { get; private set; }

    private static Dictionary<Mobile, Timer> _handshakes = new();

    public static unsafe void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("assistants.enableNegotiation", false);

        IncomingPackets.Register(0xBE, 0, false, &AssistVersion);
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
            m.SendGump(new AssistantFailureWarningGump());
        }

        if (isPlayer)
        {
            _handshakes[m] = Timer.DelayCall(delay, OnForceDisconnect, m);
        }

        m.NetState.LogInfo("Failed to negotiate assistant features.");
    }

    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void OnLogin(PlayerMobile pm)
    {
        if (pm?.NetState?.Running != true || !Enabled)
        {
            return;
        }

        pm.NetState.SendAssistVersionReq();
        pm.NetState.SendAssistHandshake();

        if (_handshakes.TryGetValue(pm, out var t))
        {
            t?.Stop();
        }

        _handshakes[pm] = Timer.DelayCall(TimeSpan.FromSeconds(30), OnTimeout, pm);
    }

    public static void AssistVersion(NetState state, SpanReader reader)
    {
        // We are not supporting the old UOAssist protocol
        // var assistVersion = reader.ReadInt32();
        // var clientVersion = reader.ReadAscii();

        // Instead we are supporting razor community edition.
        var assistVersion = reader.ReadAscii();
        state.Assistant = assistVersion.ContainsOrdinal(' ') ? assistVersion : $"RazorCE {assistVersion}";
    }

    private static void HandshakeResponse(NetState state, SpanReader reader)
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
        if (ns.CannotSendPackets())
        {
            return;
        }

        ns.Send(stackalloc byte[] { 0xBE, 0x00, 0x03 });
    }

    public static void SendAssistHandshake(this NetState ns)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[12]);
        writer.Write((byte)0xF0); // Packet ID
        writer.Write((ushort)12);
        writer.Write((byte)0xFE); // Command
        writer.Write((ulong)AssistantConfiguration.Settings.DisallowedFeatures);

        ns.Send(writer.Span);
    }

    private class AssistantFailureWarningGump : StaticWarningGump<AssistantFailureWarningGump>
    {
        private static readonly TextDefinition _warningMessage = AssistantConfiguration.Settings.WarningMessage;

        public override int StaticLocalizedContent => _warningMessage?.Number ?? 0;
        public override string Content => _warningMessage.String;
        public override int Width => 420;
        public override int Height => 250;
        public override bool CancelButton => false;
    }
}
