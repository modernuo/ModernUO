using Server.Gumps;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Server.Json;

namespace Server.Network;

public static class Assistants
{
    private const string _path = "Configuration/assistants.json";

    public static bool Enabled
    {
        get => _enabled;
        private set
        {
            _enabled = value;

            if (_enabled)
            {
                EventSink.Login += OnLogin;
            }
            else
            {
                EventSink.Login -= OnLogin;
            }
        }
    }

    public static AssistantSettings Settings { get; private set; }

    private const string _defaultWarningMessage = "The server was unable to negotiate features with your assistant. "
                                                  + "You must download and run an updated version of <A HREF=\"https://uosteam.com\">UOSteam</A>"
                                                  + " or <A HREF=\"https://github.com/markdwags/Razor/releases/latest\">Razor</A>."
                                                  + "<BR><BR>Make sure you've checked the option <B>Negotiate features with server</B>, "
                                                  + "once you have this box checked you may log in and play normally."
                                                  + "<BR><BR>You will be disconnected shortly.";

    public static unsafe void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("assistants.enabled", false);

        var path = Path.Join(Core.BaseDirectory, _path);

        if (File.Exists(path))
        {
            Settings = JsonConfig.Deserialize<AssistantSettings>(path);
        }
        else
        {
            Settings = new AssistantSettings
            {
                KickOnFailure = true,
                DisallowedFeatures = AssistantFeatures.None,
                HandshakeTimeout = TimeSpan.FromSeconds(30.0),
                DisconnectDelay = TimeSpan.FromSeconds(15.0),
                WarningMessage = _defaultWarningMessage
            };

            Save(path);
        }

        AssistantsProtocol.Register(0xFF, false, &OnResponse);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DisallowFeature(AssistantFeatures feature) => SetDisallowed(feature, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AllowFeature(AssistantFeatures feature) => SetDisallowed(feature, false);

    public static void SetDisallowed(AssistantFeatures feature, bool value)
    {
        if (value)
        {
            Settings.DisallowedFeatures |= feature;
        }
        else
        {
            Settings.DisallowedFeatures &= ~feature;
        }

        Save();
    }

    private static void Save(string path = null)
    {
        path ??= Path.Join(Core.BaseDirectory, _path);
        JsonConfig.Serialize(path, Settings);
    }

    private static readonly Dictionary<Mobile, Timer> _handshakes = new();
    private static bool _enabled;

    private static void OnLogin(Mobile m)
    {
        if (m?.NetState?.Running != true)
        {
            return;
        }

        m.NetState.SendAssistantHandshake();

        if (_handshakes.TryGetValue(m, out var t))
        {
            t?.Stop();
        }

        _handshakes[m] = Timer.DelayCall(Settings.HandshakeTimeout, OnTimeout, m);
    }

    private static void OnResponse(NetState state, CircularBufferReader reader, int packetLength)
    {
        if (state is { Mobile: null, Running: true })
        {
            return;
        }

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

        if (m.NetState is not { Running: true })
        {
            return;
        }

        if (Settings.KickOnFailure)
        {
            m.SendGump(new WarningGump(1060635, 30720, Settings.WarningMessage, 0xFFC000, 420, 250, null, false));

            if (m.AccessLevel <= AccessLevel.Player)
            {
                _handshakes[m] = Timer.DelayCall(Settings.DisconnectDelay, OnForceDisconnect, m);
            }
        }
        else
        {
            Console.WriteLine($"Player '{m}' failed to negotiate features.");
        }
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

    public static void SendAssistantHandshake(this NetState ns)
    {
        if (ns?.CannotSendPackets() != false)
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[12]);
        writer.Write((byte)0xF0); // Packet ID
        writer.Write((ushort)12);
        writer.Write((byte)0xFE); // Command
        writer.Write((ulong)Settings.DisallowedFeatures);
    }
}

public record AssistantSettings
{
    [JsonPropertyName("kickOnFailure")]
    public bool KickOnFailure { get; set; }

    [JsonPropertyName("features")]
    public AssistantFeatures DisallowedFeatures { get; set; }

    [JsonPropertyName("handshakeTimeout")]
    public TimeSpan HandshakeTimeout { get; set; }

    [JsonPropertyName("disconnectDelay")]
    public TimeSpan DisconnectDelay { get; set; }

    [JsonPropertyName("warningMessage")]
    public string WarningMessage { get; set; }
}
