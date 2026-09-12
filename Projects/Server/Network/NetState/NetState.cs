/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: NetState.cs                                                     *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Accounting;
using Server.Collections;
using Server.HuePickers;
using Server.Items;
using Server.Logging;
using Server.Menus;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Network;
using System.Runtime.CompilerServices;

namespace Server.Network;

public partial class NetState : IComparable<NetState>, IValueLinkListNode<NetState>, IDisposable
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(NetState));

    private const int HuePickerCap = 512;
    private const int MenuCap = 512;
    private const int PacketPerSecondThreshold = 3000;
    internal const long DrainTimeoutMs = 10000; // graceful disconnect gets this long to drain

    private static readonly Queue<NetState> _flushPending = new(2048);
    private static readonly Queue<NetState> _pendingDisconnects = new(256); // Processed AFTER flush
    private static readonly Queue<NetState> _throttled = new(256);
    private static readonly Queue<NetState> _throttledPending = new(256);

    private static readonly Queue<NetState> _connectingQueue = new(2048);
    private static readonly HashSet<NetState> _instances = new(2048);
    public static HashSet<NetState> Instances => _instances;

    // Replaced by tests. Working set against the container-aware available memory sampled by
    // ConfigureNetwork and refreshed by MaintainSendBuffers.
    //
    // TotalAvailableMemoryBytes is container-aware (it reports the cgroup/job-object limit where one
    // exists) but equals the GC heap hard limit only when a hard limit is configured, and ModernUO
    // configures none. Environment.WorkingSet is resident memory, not managed heap size. The two are
    // therefore a heuristic guard against starving the host, not a hard bound on the process. An
    // unpopulated figure (<= 0) fails open: refusing growth on a number we do not have would
    // disconnect players for no reason.
    // network.memoryCeilingPercent 0 turns the check off; an unpopulated GC figure fails open.
    private static bool UnderMemoryCeiling() =>
        _memoryCeilingPercent <= 0 ||
        _availableMemoryBytes <= 0 ||
        Environment.WorkingSet < _availableMemoryBytes / 100 * _memoryCeilingPercent;

    private const long MemoryCeilingWarnIntervalMs = 60000;
    private static long _memoryCeilingWarnedAt;
    private static bool _memoryCeilingWarned;

    // ModernUO-side growth refusals, drained and reset by MaintainSendBuffers. Budget refusals are
    // counted by the transport itself and reported through its maintenance stats.
    private static int _ceilingRefusals;
    private static int _capRefusals;

    private readonly string _toString;
    private ClientVersion _version;
    private bool _running = true;
    private IClientEncryption _encryption;
    private bool _flushQueued;
    private bool _disconnectQueued; // Queued for disconnect processing (after flush)
    private long[] _packetThrottles;
    private long[] _packetCounts;
    internal string _disconnectReason = string.Empty;
    private long _drainDeadline;
    private bool _drainDeadlineArmed;

    internal bool _sendBufferGrown;
    internal long _sendBufferGrewAt;
    internal const long SendBufferHoldMs = 30000;

    internal ParserState _parserState = ParserState.AwaitingNextPacket;
    internal ProtocolState _protocolState = ProtocolState.AwaitingSeed;
    private bool _packetLogging;

    // Whether ANY inbound bytes have arrived: what separates a slow client from a socket held open on
    // purpose. See BanSettings.ReportBadConnects.
    internal bool _receivedData;

    // Managed socket with buffers (handles lifecycle automatically)
    internal RingSocket _socket;

    private IAccount _account;

    internal enum ParserState
    {
        AwaitingNextPacket,
        AwaitingPartialPacket,
        ProcessingPacket,
        Throttled,
        Error
    }

    internal enum ProtocolState
    {
        AwaitingSeed, // Based on the way the seed arrives, we know if this is a login server or a game server connection

        LoginServer_AwaitingLogin,
        LoginServer_AwaitingServerSelect,
        LoginServer_ServerSelectAck,

        GameServer_AwaitingGameServerLogin,
        GameServer_LoggedIn,

        Error
    }

    private static string _packetLoggingPath;

    public static void Configure()
    {
        _packetLoggingPath = ServerConfiguration.GetSetting("netstate.packetLoggingPath", Path.Combine(Core.BaseDirectory, "Packets"));

        // Initialize IORingGroup and buffer pools
        ConfigureNetwork();
    }

    // Internal constructor for accepted sockets
    private NetState(RingSocket socket, IPAddress address)
    {
        _socket = socket;
        Address = address;

        Seeded = false;
        NextActivityCheck = Core.TickCount + 30000;
        ConnectedOn = Core.Now;
        _toString = address?.ToString() ?? "(error)";

        _instances.Add(this);
        _connectingQueue.Enqueue(this);

        LogInfo($"Connected. [{_instances.Count} Online]");
    }

    // Sectors
    public NetState Next { get; set; }
    public NetState Previous { get; set; }
    public bool OnLinkList { get; set; }

    public long NextActivityCheck { get; private set; }

    // Only use this for debugging. This will make your server very slow!
    public bool PacketLogging
    {
        get => _packetLogging;
        set
        {
            _packetLogging = value;

            if (_packetLogging)
            {
                StartPacketLog();
            }
        }
    }

    public int AuthId { get; set; }

    public int Seed { get; set; }

    public DateTime ConnectedOn { get; }

    public TimeSpan ConnectedFor => Core.Now - ConnectedOn;

    public IPAddress Address { get; }

    public IClientEncryption Encryption
    {
        get => _encryption;
        set => _encryption = value;
    }

    public int CurrentPacket { get; internal set; }

    public bool SentFirstPacket { get; set; }

    public bool BlockAllPackets { get; set; }

    public List<SecureTrade> Trades { get; private set; }

    public bool Seeded { get; set; }

    public bool Running => _running;

    /// <summary>
    /// Gets whether the socket is connected.
    /// </summary>
    public bool IsConnected => _socket != null;

    /// <summary>
    /// Gets the socket handle.
    /// </summary>
    public nint SocketHandle => _socket?.Handle ?? 0;

    /// <summary>
    /// Gets the local endpoint (address/port) the client connected to.
    /// </summary>
    public IPEndPoint LocalEndPoint => _socket != null ? SocketHelper.GetLocalEndPoint(_socket.Handle) : null;

    /// <summary>
    /// Gets the send buffer for this connection.
    /// </summary>
    internal IORingBuffer SendBuffer => _socket?.SendBuffer;

    public bool CompressionEnabled { get; set; }

    public int Sequence { get; set; }

    public List<HuePicker> HuePickers { get; private set; }

    public List<IMenu> Menus { get; private set; }

    public CityInfo[] CityInfo { get; set; }

    public Mobile Mobile { get; set; }

    public ServerInfo[] ServerInfo { get; set; }

    public IAccount Account
    {
        get => _account;
        set => _account = value;
    }

    public string Assistant { get; set; }

    public int CompareTo(NetState other) => string.CompareOrdinal(_toString, other?._toString);

    private void SetPacketTime(int packetID)
    {
        if (packetID is >= 0 and < 0x100)
        {
            _packetThrottles ??= new long[0x100];
            _packetThrottles[packetID] = Core.TickCount;
        }
    }

    public long GetPacketTime(int packetID) =>
        packetID is >= 0 and < 0x100 && _packetThrottles != null ? _packetThrottles[packetID] : 0;

    private void UpdatePacketCount(int packetID)
    {
        if (packetID is >= 0 and < 0x100)
        {
            _packetCounts ??= new long[0x100];
            _packetCounts[packetID]++;
        }
    }

    public int CheckPacketCounts()
    {
        if (_packetCounts == null)
        {
            return 0;
        }

        for (var i = 0; i < _packetCounts.Length; i++)
        {
            var count = _packetCounts[i];
            _packetCounts[i] = 0;

            if (count > PacketPerSecondThreshold)
            {
                return i;
            }
        }

        return 0;
    }

    public void ValidateAllTrades()
    {
        if (Trades == null)
        {
            return;
        }

        for (var i = Trades.Count - 1; i >= 0; --i)
        {
            if (Trades == null)
            {
                break;
            }

            if (i >= Trades.Count)
            {
                continue;
            }

            var trade = Trades[i];

            if (trade.From.Mobile.Deleted || trade.To.Mobile.Deleted || !trade.From.Mobile.Alive ||
                !trade.To.Mobile.Alive || !trade.From.Mobile.InRange(trade.To.Mobile, 2) ||
                trade.From.Mobile.Map != trade.To.Mobile.Map)
            {
                trade.Cancel();
            }
        }
    }

    public void CancelAllTrades()
    {
        if (Trades == null)
        {
            return;
        }

        for (var i = Trades.Count - 1; i >= 0; --i)
        {
            // RemoveTrade() nulls the list once empty
            if (Trades == null)
            {
                break;
            }

            if (i < Trades.Count)
            {
                Trades[i].Cancel();
            }
        }
    }

    public void RemoveTrade(SecureTrade trade)
    {
        Trades?.Remove(trade);

        if (Trades?.Count == 0)
        {
            Trades = null;
        }
    }

    public SecureTrade FindTrade(Mobile m)
    {
        if (Trades == null)
        {
            return null;
        }

        for (var i = 0; i < Trades.Count; ++i)
        {
            var trade = Trades[i];

            if (trade.From.Mobile == m || trade.To.Mobile == m)
            {
                return trade;
            }
        }

        return null;
    }

    public SecureTradeContainer FindTradeContainer(Mobile m)
    {
        if (Trades == null)
        {
            return null;
        }

        for (var i = 0; i < Trades.Count; ++i)
        {
            var trade = Trades[i];

            var from = trade.From;
            var to = trade.To;

            if (from.Mobile == Mobile && to.Mobile == m)
            {
                return from.Container;
            }

            if (from.Mobile == m && to.Mobile == Mobile)
            {
                return to.Container;
            }
        }

        return null;
    }

    public SecureTradeContainer AddTrade(NetState state)
    {
        var newTrade = new SecureTrade(Mobile, state.Mobile);

        Trades ??= [];

        Trades.Add(newTrade);

        state.Trades ??= [];
        state.Trades.Add(newTrade);

        return newTrade.From.Container;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogInfo(string text)
    {
        logger.Information("Client: {NetState}: {Message}", this, text);
    }

    public void AddMenu(IMenu menu)
    {
        Menus ??= [];

        if (Menus.Count < MenuCap)
        {
            Menus.Add(menu);
        }
        else
        {
            LogInfo("Exceeded menu cap, disconnecting...");
            Disconnect("Exceeded menu cap.");
        }
    }

    public void RemoveMenu(IMenu menu)
    {
        Menus?.Remove(menu);
    }

    public void RemoveMenu(int index)
    {
        Menus?.RemoveAt(index);
    }

    public void ClearMenus()
    {
        Menus?.Clear();
    }

    public void AddHuePicker(HuePicker huePicker)
    {
        HuePickers ??= [];

        if (HuePickers.Count < HuePickerCap)
        {
            HuePickers.Add(huePicker);
        }
        else
        {
            LogInfo("Exceeded hue picker cap, disconnecting...");
            Disconnect("Exceeded hue picker cap.");
        }
    }

    public void RemoveHuePicker(HuePicker huePicker)
    {
        HuePickers?.Remove(huePicker);
    }

    public void RemoveHuePicker(int index)
    {
        HuePickers?.RemoveAt(index);
    }

    public void ClearHuePickers()
    {
        HuePickers?.Clear();
    }

    public void LaunchBrowser(ReadOnlySpan<char> url)
    {
        this.SendMessageLocalized(Serial.MinusOne, -1, MessageType.Label, 0x35, 3, 501231);
        this.SendLaunchBrowser(url);
    }

    public override string ToString() => _toString;

    public bool GetSendBuffer(out Span<byte> buffer)
    {
#if THREADGUARD
            if (Thread.CurrentThread != Core.Thread)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Attempting to get pipe buffer from wrong thread!");
                Console.WriteLine(new StackTrace());
                Utility.PopColor();

                buffer = Array.Empty<byte>();
                return false;
            }
#endif
        if (!_running || _socket == null)
        {
            buffer = Span<byte>.Empty;
            return false;
        }

        buffer = _socket.SendBuffer.GetWriteSpan();
        return buffer.Length > 0;
    }

    // Grows until `needed` fits or growth is refused. Refusal leaves the caller on today's path.
    internal bool TryGrowSendBuffer(int needed)
    {
        if (_socket == null)
        {
            return false;
        }

        while (_socket.SendBuffer.WritableBytes < needed)
        {
            if (!TryGrowSendBufferOneTier())
            {
                return false;
            }
        }

        return true;
    }

    // One tier step, ceiling-checked. For callers that cannot state how much they need up front
    // (compression), the only way to find out is to grow and try again.
    internal bool TryGrowSendBufferOneTier()
    {
        if (_socket == null)
        {
            return false;
        }

        if (!UnderMemoryCeiling())
        {
            _ceilingRefusals++;

            var now = Core.TickCount;
            if (!_memoryCeilingWarned || now - (_memoryCeilingWarnedAt + MemoryCeilingWarnIntervalMs) >= 0)
            {
                _memoryCeilingWarned = true;
                _memoryCeilingWarnedAt = now;
                logger.Warning("Send buffer growth refused: process is above {Percent}% of available memory", _memoryCeilingPercent);
            }

            return false;
        }

        if (!_socketManager.TryGrowSendBuffer(_socket))
        {
            // Already at the per-connection maximum; anything else the transport refused came out of
            // the shared growth budget, which the transport counts for itself.
            if (_socket.SendBuffer.PhysicalSize >= MaxSendBufferSize)
            {
                _capRefusals++;
            }

            return false;
        }

        _sendBufferGrown = true;
        _sendBufferGrewAt = Core.TickCount;
        logger.Debug("{NetState}: send buffer grown to {Size}", this, _socket.SendBuffer.PhysicalSize);
        return true;
    }

    // Demotes a drained socket once the hold since its last growth has passed. The pool keeps the
    // larger buffer on hand; the socket only needs to stop occupying it.
    internal bool TryShrinkSendBuffer(long curTicks)
    {
        if (!_sendBufferGrown || _socket == null || curTicks - (_sendBufferGrewAt + SendBufferHoldMs) < 0)
        {
            return false;
        }

        if (!_socketManager.TryShrinkSendBuffer(_socket))
        {
            return false;
        }

        _sendBufferGrown = false;
        logger.Debug("{NetState}: send buffer returned to {Size}", this, _socket.SendBuffer.PhysicalSize);
        return true;
    }

    public void Send(ReadOnlySpan<byte> span)
    {
        if (span == ReadOnlySpan<byte>.Empty || this.CannotSendPackets())
        {
            return;
        }

        var length = span.Length;
        if (length <= 0)
        {
            return;
        }

        // Closing; nothing to report
        if (!_running || _socket == null)
        {
            return;
        }

        try
        {
            // Never drop silently: the client would stay connected while missing game state.
            if (!GetSendBuffer(out var buffer))
            {
                if (!TryGrowSendBuffer(length) || !GetSendBuffer(out buffer))
                {
                    SendBufferExhausted(length);
                    return;
                }
            }

            // Apply encoding first (e.g., compression from UOContent)
            if (CompressionEnabled)
            {
                length = NetworkCompression.Compress(span, buffer);

                if (length <= 0)
                {
                    // Past this input length Compress refuses unconditionally, so growth cannot help.
                    if (span.Length > NetworkCompression.DefiniteOverflow)
                    {
                        SendBufferExhausted(span.Length);
                        return;
                    }

                    // The compressed length is only known by compressing, and the worst Huffman ratio
                    // is 11/8 - a 2N + 4 target over-states the need and manufactures exhaustion at the
                    // maximum. Grow a tier at a time and retry instead.
                    while (length <= 0 && TryGrowSendBufferOneTier() && GetSendBuffer(out buffer))
                    {
                        length = NetworkCompression.Compress(span, buffer);
                    }

                    if (length <= 0)
                    {
                        SendBufferExhausted(span.Length);
                        return;
                    }
                }
            }
            else if (span.Length > buffer.Length)
            {
                if (!TryGrowSendBuffer(span.Length) || !GetSendBuffer(out buffer))
                {
                    SendBufferExhausted(span.Length);
                    return;
                }

                span.CopyTo(buffer);
            }
            else
            {
                span.CopyTo(buffer);
            }

            // Then encrypt (if encryption is enabled)
            _encryption?.ServerEncrypt(buffer[..length]);

            if (PacketLogging)
            {
                LogPacket(span, false);
            }

            _socket.SendBuffer.CommitWrite(length);

            if (!_flushQueued)
            {
                _flushPending.Enqueue(this);
                _flushQueued = true;
            }
        }
        catch (Exception ex)
        {
            TraceException(ex);
            Disconnect("Exception while sending.");
        }
    }

    /// <summary>
    /// Handles a packet that cannot be placed in the send buffer.
    /// </summary>
    /// <remarks>
    /// High unacked means a slow client holding the buffer; needed approaching capacity means the
    /// buffer is too small for this shard and network.sendBufferSize should be raised.
    /// </remarks>
    private void SendBufferExhausted(int needed)
    {
        // One report per disconnect; the first reason wins
        if (_disconnectQueued)
        {
            return;
        }

        // Read writable alongside unacked and capacity: a caller that grew a tier and was then
        // refused still holds the pre-growth span, and reporting its length contradicts the capacity.
        var sendBuffer = _socket?.SendBuffer;
        var writable = sendBuffer?.WritableBytes ?? 0;
        var unacked = sendBuffer?.InFlightBytes ?? 0;
        var capacity = sendBuffer?.PhysicalSize ?? 0;

        logger.Warning(
            "{NetState}: send buffer exhausted - needed {Needed} bytes, {Writable} writable, {Unacked} awaiting acknowledgement, {Capacity} capacity. Raise network.sendBufferSize (power of two) if this recurs on healthy connections.",
            this,
            needed,
            writable,
            unacked,
            capacity
        );

        Disconnect($"Send buffer exhausted (needed {needed}, writable {writable}, unacked {unacked}, capacity {capacity})");
    }

    private void StartPacketLog()
    {
        try
        {
            var logDir = Path.Combine(_packetLoggingPath, _toString);
            PathUtility.EnsureDirectory(logDir);
            var logPath = Path.Combine(logDir, "packets.log");
            using var op = new StreamWriter(logPath, true);

            op.WriteLine(">>>>>>>>>> Logging started {0:yyyy/MM/dd HH:mm::ss} <<<<<<<<<<", Core.Now);
            op.WriteLine();
            op.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void LogPacket(ReadOnlySpan<byte> buffer, bool incoming)
    {
        try
        {
            var logDir = Path.Combine(_packetLoggingPath, _toString);
            PathUtility.EnsureDirectory(logDir);
            var logPath = Path.Combine(logDir, "packets.log");

            const string incomingStr = "Client -> Server";
            const string outgoingStr = "Server -> Client";

            using var sw = new StreamWriter(logPath, true);
            sw.WriteLine($"{Core.Now:HH:mm:ss.ffff}: {(incoming ? incomingStr : outgoingStr)} 0x{buffer[0]:X2} (Length: {buffer.Length})");
            sw.FormatBuffer(buffer);
            sw.WriteLine();
            sw.WriteLine();
        }
        catch
        {
            // ignored
        }
    }

    private void DecryptRecvBuffer(int bytesReceived)
    {
        if (_socket == null || _encryption == null)
        {
            return;
        }

        // Get the portion of the buffer that was just written (the new data)
        var readSpan = _socket.RecvBuffer.GetReadSpan();
        var newDataStart = Math.Max(0, readSpan.Length - bytesReceived);

        _encryption?.ClientDecrypt(readSpan.Slice(newDataStart, bytesReceived));
    }

    public void HandleReceive(bool throttled = false)
    {
        if (!_running || _socket == null)
        {
            return;
        }

        // Data already in recv buffer from recv completion - no need to call ReceiveData
        try
        {
            // Process as many packets as we can synchronously
            while (_running && _parserState != ParserState.Error && _protocolState != ProtocolState.Error)
            {
                var buffer = _socket.RecvBuffer.GetReadSpan();
                var length = buffer.Length;

                if (length <= 0)
                {
                    break;
                }

                var packetReader = new SpanReader(buffer);
                var packetId = packetReader.ReadByte();
                var packetLength = length;

                // These can arrive at any time and are only informational
                if (_protocolState != ProtocolState.AwaitingSeed && IncomingPackets.IsInfoPacket(packetId))
                {
                    _parserState = ParserState.ProcessingPacket;
                    _parserState = HandlePacket(packetReader, packetId, out packetLength);
                }
                else
                {
                    switch (_protocolState)
                    {
                        case ProtocolState.AwaitingSeed:
                            {
                                // Traffic that is positively another protocol. Unlike "does not look like a
                                // good client", this cannot misfire on a misconfigured one.
                                var foreign = ForeignProtocol.Identify(buffer, out var foreignKind);

                                if (foreign == ForeignProtocolMatch.Incomplete)
                                {
                                    _parserState = ParserState.AwaitingPartialPacket;
                                    break;
                                }

                                if (foreign == ForeignProtocolMatch.Confirmed)
                                {
                                    logger.Debug(
                                        "{Address} spoke {Protocol} on the game port; disconnecting",
                                        Address,
                                        foreignKind
                                    );

                                    if (Bans.BanConfiguration.Settings.ReportBadConnects)
                                    {
                                        Bans.BanChannel.Report(
                                            Address,
                                            Bans.BanConfiguration.Settings.BadConnectDuration,
                                            Bans.BanReasons.ForeignProtocol
                                        );
                                    }

                                    Disconnect(string.Empty);
                                    return;
                                }

                                if (packetId == 0xEF)
                                {
                                    _parserState = ParserState.ProcessingPacket;
                                    _parserState = HandlePacket(packetReader, packetId, out packetLength);
                                    if (_parserState == ParserState.AwaitingNextPacket)
                                    {
                                        _protocolState = ProtocolState.LoginServer_AwaitingLogin;
                                    }
                                }
                                else if (length >= 4)
                                {
                                    var newSeed = (packetId << 24) | (packetReader.ReadByte() << 16) | (packetReader.ReadByte() << 8) | packetReader.ReadByte();

                                    if (newSeed == 0)
                                    {
                                        // No real client sends a zero seed, so this is deliberate garbage
                                        // rather than a damaged connection — unlike the short-read branch
                                        // below, which a fragmented first segment can reach honestly.
                                        if (Bans.BanConfiguration.Settings.ReportBadConnects)
                                        {
                                            Bans.BanChannel.Report(
                                                Address,
                                                Bans.BanConfiguration.Settings.BadConnectDuration,
                                                Bans.BanReasons.InvalidSeed
                                            );
                                        }

                                        Disconnect(string.Empty);
                                        return;
                                    }

                                    Seed = newSeed;
                                    Seeded = true;
                                    packetLength = 4;

                                    _parserState = ParserState.AwaitingNextPacket;
                                    _protocolState = ProtocolState.GameServer_AwaitingGameServerLogin;
                                }
                                else
                                {
                                    // Disconnect rather than wait. Waiting would hold a connection slot for
                                    // the full ConnectingSocketIdleLimit per one- or two-byte client, which
                                    // is what a flood sends. Only pre-0xEF clients reach here.
                                    Disconnect(string.Empty);
                                }
                                break;
                            }

                        case ProtocolState.LoginServer_AwaitingLogin:
                            {
                                // Check for unencrypted login packet
                                if (packetId == 0x80)
                                {
                                    // Unencrypted - check if allowed
                                    if (EncryptionManager.Enabled && !EncryptionManager.Mode.HasFlag(EncryptionMode.Unencrypted))
                                    {
                                        LogInfo("Unencrypted client rejected by encryption policy.");
                                        HandleError(packetId, packetLength);
                                        return;
                                    }

                                    _parserState = ParserState.ProcessingPacket;
                                    _parserState = HandlePacket(packetReader, packetId, out packetLength);
                                    if (_parserState == ParserState.AwaitingNextPacket)
                                    {
                                        _protocolState = ProtocolState.LoginServer_AwaitingServerSelect;
                                    }
                                    break;
                                }

                                // First byte isn't 0x80 - might be encrypted
                                if (!EncryptionManager.Enabled)
                                {
                                    LogInfo("Possible encrypted client detected, disconnecting...");
                                    HandleError(packetId, packetLength);
                                    return;
                                }

                                // Need 62 bytes for login packet to attempt decryption
                                if (length < 62)
                                {
                                    _parserState = ParserState.AwaitingPartialPacket;
                                    break;
                                }

                                // Try to detect and decrypt encrypted login
                                if (!this.DetectLoginEncryption(buffer[..62], out var loginEncryption))
                                {
                                    LogInfo("Encrypted client detection failed, disconnecting...");
                                    HandleError(packetId, packetLength);
                                    return;
                                }

                                // Decryption succeeded - set up encryption and process
                                if (loginEncryption != null)
                                {
                                    _encryption = loginEncryption;

                                    // Decrypt the buffer in place for processing
                                    loginEncryption.ClientDecrypt(buffer[..62]);
                                }

                                // Now process as normal (first byte should now be 0x80)
                                packetReader = new SpanReader(buffer);
                                packetId = packetReader.ReadByte();

                                _parserState = ParserState.ProcessingPacket;
                                _parserState = HandlePacket(packetReader, packetId, out packetLength);
                                if (_parserState == ParserState.AwaitingNextPacket)
                                {
                                    _protocolState = ProtocolState.LoginServer_AwaitingServerSelect;
                                }
                                break;
                            }

                        case ProtocolState.LoginServer_AwaitingServerSelect:
                            {
                                if (packetId != 0xA0)
                                {
                                    HandleError(packetId, packetLength);
                                    return;
                                }

                                _parserState = ParserState.ProcessingPacket;
                                _parserState = HandlePacket(packetReader, packetId, out packetLength);
                                if (_parserState == ParserState.AwaitingNextPacket)
                                {
                                    _protocolState = ProtocolState.LoginServer_ServerSelectAck;
                                    Disconnect(string.Empty);
                                }
                                break;
                            }

                        case ProtocolState.LoginServer_ServerSelectAck:
                            {
#if STRICT_UO_PROTOCOL
                                HandleError(packetId, packetLength);
#else
                                // Reset the state because CUO/Orion do not reconnect
                                _parserState = ParserState.AwaitingNextPacket;
                                _protocolState = ProtocolState.AwaitingSeed;
#endif
                                return;
                            }

                        case ProtocolState.GameServer_AwaitingGameServerLogin:
                            {
                                // Some clients send 0x80 on game server connection
                                if (packetId == 0x80 || length == 62)
                                {
                                    goto case ProtocolState.LoginServer_AwaitingLogin;
                                }

                                // Check for unencrypted game login packet
                                if (packetId == 0x91)
                                {
                                    // Unencrypted - check if allowed
                                    if (EncryptionManager.Enabled && !EncryptionManager.Mode.HasFlag(EncryptionMode.Unencrypted))
                                    {
                                        LogInfo("Unencrypted game client rejected by encryption policy.");
                                        HandleError(packetId, packetLength);
                                        return;
                                    }

                                    _parserState = ParserState.ProcessingPacket;
                                    _parserState = HandlePacket(packetReader, packetId, out packetLength);
                                    if (_parserState == ParserState.AwaitingNextPacket)
                                    {
                                        _protocolState = ProtocolState.GameServer_LoggedIn;
                                    }
                                    break;
                                }

                                // First byte isn't 0x91 - might be encrypted
                                if (!EncryptionManager.Enabled)
                                {
                                    HandleError(packetId, packetLength);
                                    return;
                                }

                                // Need 65 bytes for game login packet to attempt decryption
                                if (length < 65)
                                {
                                    _parserState = ParserState.AwaitingPartialPacket;
                                    break;
                                }

                                // Try to detect and decrypt encrypted game login
                                if (!this.DetectGameEncryption(buffer[..65], out var gameEncryption))
                                {
                                    LogInfo("Encrypted game client detection failed, disconnecting...");
                                    HandleError(packetId, packetLength);
                                    return;
                                }

                                // Decryption succeeded - set up encryption and process
                                if (gameEncryption != null)
                                {
                                    _encryption = gameEncryption;

                                    // Decrypt the buffer in place for processing
                                    gameEncryption.ClientDecrypt(buffer[..65]);
                                }

                                // Now process as normal (first byte should now be 0x91)
                                packetReader = new SpanReader(buffer);
                                packetId = packetReader.ReadByte();

                                _parserState = ParserState.ProcessingPacket;
                                _parserState = HandlePacket(packetReader, packetId, out packetLength);
                                if (_parserState == ParserState.AwaitingNextPacket)
                                {
                                    _protocolState = ProtocolState.GameServer_LoggedIn;
                                }
                                break;
                            }

                        case ProtocolState.GameServer_LoggedIn:
                            {
                                _parserState = ParserState.ProcessingPacket;
                                _parserState = HandlePacket(packetReader, packetId, out packetLength);
                                break;
                            }
                    }
                }

                if (_parserState is ParserState.AwaitingNextPacket)
                {
                    _socket.RecvBuffer.CommitRead(packetLength);
                }
                else if (_parserState is ParserState.Throttled)
                {
                    if (!throttled)
                    {
                        _throttled.Enqueue(this);
                    }
                    else
                    {
                        _throttledPending.Enqueue(this);
                    }

                    break;
                }
                else if (_parserState is ParserState.AwaitingPartialPacket)
                {
                    break;
                }
                else if (_parserState is ParserState.Error)
                {
                    HandleError(packetId, packetLength);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            Console.WriteLine(ex);
#endif
            TraceException(ex);
            Disconnect("Exception during HandleReceive");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleError(byte packetId, int packetLength)
    {
        var msg =
            $"{this} entered bad state on packet 0x{packetId:X2} with length {packetLength} while in protocol state {_protocolState} and parser state {_parserState}";
        Disconnect(msg);
        _parserState = ParserState.Error;
        _protocolState = ProtocolState.Error;
    }

    /*
     * length is the total buffer length. We might be able to use packetReader.Capacity() instead.
     * packetLength is the length of the packet that this function actually found.
     */
    private unsafe ParserState HandlePacket(SpanReader packetReader, byte packetId, out int packetLength)
    {
        var handler = IncomingPackets.GetHandler(packetId);
        var length = packetReader.Length;

        if (handler == null)
        {
            LogInfo($"Received unknown packet 0x{packetId:X2} while in state {_protocolState}");
            packetLength = 1;
            return ParserState.Error;
        }

        packetLength = handler.GetLength(this);
        if (packetLength <= 0)
        {
            // Variable length packet. See if we have pulled in the length.
            if (length < 3)
            {
                return ParserState.AwaitingPartialPacket;
            }

            packetLength = packetReader.ReadUInt16();
            if (packetLength < 3)
            {
                return ParserState.Error;
            }
        }

        // Not enough data, let's wait for more to come in
        if (length < packetLength)
        {
            return ParserState.AwaitingPartialPacket;
        }

        if (handler.InGameOnly)
        {
            if (Mobile == null)
            {
                LogInfo($"Received packet 0x{packetId:X2} before having been attached to a mobile.");
                return ParserState.Error;
            }

            if (Mobile.Deleted)
            {
                LogInfo($"Received packet 0x{packetId:X2} after having been attached to a deleted mobile.");
                return ParserState.Error;
            }
        }

        if (handler.OutOfGameOnly && Mobile?.Deleted == false)
        {
            LogInfo($"Received packet 0x{packetId:X2} after having been attached to a mobile.");
            return ParserState.Error;
        }

        var throttler = handler.ThrottleCallback;
        if (throttler != null)
        {
            if (throttler(packetId, this))
            {
                return ParserState.Throttled;
            }

            SetPacketTime(packetId);
        }

        UpdatePacketCount(packetId);

        if (PacketLogging)
        {
            LogPacket(packetReader.Buffer[..packetLength], true);
        }

        // Make a new SpanReader that is limited to the length of the packet.
        // This allows us to use reader.Remaining for VendorBuyReply packet
        var start = packetReader.Position;
        var remainingLength = packetLength - packetReader.Position;

        handler.OnReceive(this, new SpanReader(packetReader.Buffer.Slice(start, remainingLength)));

        return ParserState.AwaitingNextPacket;
    }

    // Bounds the graceful drain. Send completions keep NextActivityCheck moving, so a slow peer
    // could otherwise hold a closing socket open indefinitely.
    internal void ArmDrainDeadline(long curTicks)
    {
        if (!_drainDeadlineArmed)
        {
            _drainDeadlineArmed = true;
            _drainDeadline = curTicks + DrainTimeoutMs;
        }
    }

    public void CheckAlive(long curTicks)
    {
        if (_socket == null)
        {
            return;
        }

        if (_socket.DisconnectPending)
        {
            ArmDrainDeadline(curTicks); // transport-initiated drains are first seen here

            if (curTicks - _drainDeadline >= 0 || NextActivityCheck - curTicks < 0)
            {
                LogInfo("Force disconnecting stuck socket...");
                _socketManager.DisconnectImmediate(_socket);
            }

            return;
        }

        if (NextActivityCheck - curTicks >= 0)
        {
            return;
        }

        // Authenticated pre-game clients (login screens): send keep-alive instead of disconnecting.
        // The 0xBD ClientVersionRequest resets NextActivityCheck via DataSent.
        if (_account != null && Mobile == null)
        {
            this.SendClientVersionRequest();
            return;
        }

        LogInfo("Disconnecting due to inactivity...");
        Disconnect("Disconnecting due to inactivity.");
    }

    public void Trace(ReadOnlySpan<byte> buffer)
    {
        // We don't have data, so nothing to trace
        if (buffer.Length == 0)
        {
            return;
        }

        try
        {
            using var sw = new StreamWriter("unhandled-packets.log", true);
            sw.WriteLine("Client: {0}: Unhandled packet 0x{1:X2}", this, buffer[0]);
            sw.FormatBuffer(buffer);
            sw.WriteLine();
            sw.WriteLine();
        }
        catch
        {
            // ignored
        }
    }

    public static void TraceException(Exception ex)
    {
        try
        {
            using var op = new StreamWriter("network-errors.log", true);
            op.WriteLine("# {0}", Core.Now);

            op.WriteLine(ex);

            op.WriteLine();
            op.WriteLine();
        }
        catch
        {
            // ignored
        }

        Console.WriteLine(ex);
    }

    /// <summary>
    /// Requests a graceful disconnect. Processed after the flush queue in Slice(): sends made before
    /// that handoff are flushed first, sends after it are dropped (see CannotSendPackets).
    /// </summary>
    public void Disconnect(string reason)
    {
        if (!_running || _socket == null)
        {
            return;
        }

        _disconnectReason = reason;

        if (!_disconnectQueued)
        {
            _disconnectQueued = true;
            _pendingDisconnects.Enqueue(this);
        }
    }

    public static void TraceDisconnect(string reason, string ip)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        try
        {
            using var op = new StreamWriter("network-disconnects.log", true);
            op.WriteLine($"# {Core.Now}");

            op.WriteLine($"NetState: {ip}");
            op.WriteLine(reason);

            op.WriteLine();
            op.WriteLine();
        }
        catch (Exception ex)
        {
            TraceException(ex);
        }
    }

    private void DisposeInternal() => Dispose();

    // Do not run this directly. Use Disconnect instead.
    // This is available for testing cleanup only.
    [Obsolete("Use Disconnect instead")]
    public void Dispose()
    {
        var wasRunning = _running;
        _running = false;
        // It's possible we could queue for dispose multiple times
        if (_socket == null)
        {
            return;
        }

        TraceDisconnect(_disconnectReason, _toString);

        // If still running, force immediate disconnect
        if (wasRunning)
        {
            _socketManager?.DisconnectImmediate(_socket);
        }

        var m = Mobile;
        if (m?.NetState == this)
        {
            m.NetState = null;
        }

        _instances.Remove(this);

        // Clear the NetState slot
        var slotId = _socket.Id;
        if (slotId >= 0 && slotId < _netStates.Length && _netStates[slotId] == this)
        {
            _netStates[slotId] = null;
        }

        // Note: RingSocketManager handles cleanup of ring resources (unregister, close, buffer release)
        // when it processes the disconnect event. We just clear our reference.
        _socket = null;

        Mobile = null;

        var a = Account;

        Menus?.Clear();
        Menus = null;

        HuePickers?.Clear();
        HuePickers = null;

        // Just in case, but should already be nulled when Mobile.NetState is set to null and CancelAllTrades is called.
        Trades?.Clear();
        Trades = null;

        Account = null;
        ServerInfo = null;
        CityInfo = null;

        var count = _instances.Count;

        LogInfo(a != null ? $"Disconnected. [{count} Online] [{a}]" : $"Disconnected. [{count} Online]");
    }
}
