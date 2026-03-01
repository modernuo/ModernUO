/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
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

    private static readonly Queue<NetState> _flushPending = new(2048);
    private static readonly Queue<NetState> _pendingDisconnects = new(256); // Processed AFTER flush
    private static readonly Queue<NetState> _throttled = new(256);
    private static readonly Queue<NetState> _throttledPending = new(256);

    private static readonly Queue<NetState> _connectingQueue = new(2048);
    private static readonly HashSet<NetState> _instances = new(2048);
    public static IReadOnlySet<NetState> Instances => _instances;

    private readonly string _toString;
    private ClientVersion _version;
    private bool _running = true;
    private IClientEncryption _encryption;
    private bool _flushQueued;
    private bool _disconnectQueued; // Queued for disconnect processing (after flush)
    private long[] _packetThrottles;
    private long[] _packetCounts;
    private string _disconnectReason = string.Empty;

    internal ParserState _parserState = ParserState.AwaitingNextPacket;
    internal ProtocolState _protocolState = ProtocolState.AwaitingSeed;
    private bool _packetLogging;

    // Managed socket with buffers (handles lifecycle automatically)
    internal RingSocket _socket;

    // Speed Hack Prevention
    internal long _movementCredit;
    internal long _nextMovementTime;
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
        HuePickers = [];
        Menus = [];
        Trades = [];
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

    public List<SecureTrade> Trades { get; }

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
        for (var i = Trades.Count - 1; i >= 0; --i)
        {
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
        for (var i = Trades.Count - 1; i >= 0; --i)
        {
            if (i < Trades.Count)
            {
                Trades[i].Cancel();
            }
        }
    }

    public void RemoveTrade(SecureTrade trade)
    {
        Trades.Remove(trade);
    }

    public SecureTrade FindTrade(Mobile m)
    {
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

        Trades.Add(newTrade);
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

    public void LaunchBrowser(string url)
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

    public void Send(ReadOnlySpan<byte> span)
    {
        if (span == ReadOnlySpan<byte>.Empty || this.CannotSendPackets())
        {
            return;
        }

        var length = span.Length;
        if (length <= 0 || !GetSendBuffer(out var buffer))
        {
            return;
        }

        try
        {
            // Apply encoding first (e.g., compression from UOContent)
            if (CompressionEnabled)
            {
                length = NetworkCompression.Compress(span, buffer);
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
                                        Disconnect(string.Empty);
                                        return;
                                    }

                                    Seed = newSeed;
                                    Seeded = true;
                                    packetLength = 4;

                                    _parserState = ParserState.AwaitingNextPacket;
                                    _protocolState = ProtocolState.GameServer_AwaitingGameServerLogin;
                                }
                                else // Don't allow partial packets on initial connection, just disconnect them.
                                {
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
                                    var mutableBuffer = _socket.RecvBuffer.GetReadSpan();
                                    loginEncryption.ClientDecrypt(mutableBuffer[..62]);
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
                                if (packetId == 0x80)
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
                                    var mutableBuffer = _socket.RecvBuffer.GetReadSpan();
                                    gameEncryption.ClientDecrypt(mutableBuffer[..65]);
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

    public void CheckAlive(long curTicks)
    {
        if (_socket == null || NextActivityCheck - curTicks >= 0)
        {
            return;
        }

        if (_socket.DisconnectPending)
        {
            LogInfo("Force disconnecting stuck socket...");
            _socketManager.DisconnectImmediate(_socket);
        }
        else
        {
            LogInfo("Disconnecting due to inactivity...");
            Disconnect("Disconnecting due to inactivity.");
        }
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
    /// Requests a graceful disconnect. The disconnect is queued and processed after the flush
    /// queue in Slice(), ensuring Send() calls made in the same tick are processed first.
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

        Menus.Clear();
        HuePickers.Clear();
        Account = null;
        ServerInfo = null;
        CityInfo = null;

        var count = _instances.Count;

        LogInfo(a != null ? $"Disconnected. [{count} Online] [{a}]" : $"Disconnected. [{count} Online]");
    }
}
