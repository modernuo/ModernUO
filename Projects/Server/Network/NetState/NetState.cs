/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Network;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Server.Accounting;
using Server.Collections;
using Server.Diagnostics;
using Server.Gumps;
using Server.HuePickers;
using Server.Items;
using Server.Logging;
using Server.Menus;

namespace Server.Network;

public delegate void NetStateCreatedCallback(NetState ns);

public delegate void DecodePacket(Span<byte> buffer, ref int length);
public delegate int EncodePacket(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer);

public partial class NetState : IComparable<NetState>, IValueLinkListNode<NetState>
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(NetState));

    private const int RecvPipeSize = 1024 * 64;
    private const int SendPipeSize = 1024 * 256;
    private const int GumpCap = 512;
    private const int HuePickerCap = 512;
    private const int MenuCap = 512;
    private const int PacketPerSecondThreshold = 3000;

    private static readonly GCHandle[] _polledStates = new GCHandle[2048];
    private static readonly IPollGroup _pollGroup = PollGroup.Create();
    private static readonly Queue<NetState> _flushPending = new(2048);
    private static readonly Queue<NetState> _flushedPartials = new(256);
    private static readonly Queue<NetState> _disposed = new(256);
    private static readonly Queue<NetState> _throttled = new(256);
    private static readonly Queue<NetState> _throttledPending = new(256);

    public static NetStateCreatedCallback CreatedCallback { get; set; }

    private readonly string _toString;
    private ClientVersion _version;
    private long _nextActivityCheck;
    private bool _running = true;
    private volatile DecodePacket _packetDecoder;
    private volatile EncodePacket _packetEncoder;
    private bool _flushQueued;
    private readonly long[] _packetThrottles = new long[0x100];
    private readonly long[] _packetCounts = new long[0x100];
    private string _disconnectReason = string.Empty;

    internal ParserState _parserState = ParserState.AwaitingNextPacket;
    internal ProtocolState _protocolState = ProtocolState.AwaitingSeed;
    internal GCHandle _handle;
    private bool _packetLogging;

    public GCHandle Handle => _handle;

    // Speed Hack Prevention
    internal long _movementCredit;
    internal long _nextMovementTime;

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
    }

    public static void Initialize()
    {
        Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1.5), CheckAllAlive);
    }

    public NetState(Socket connection)
    {
        Connection = connection;
        Seeded = false;
        Gumps = new List<Gump>();
        HuePickers = new List<HuePicker>();
        Menus = new List<IMenu>();
        Trades = new List<SecureTrade>();
        RecvPipe = new Pipe(RecvPipeSize);
        SendPipe = new Pipe(SendPipeSize);
        _nextActivityCheck = Core.TickCount + 30000;
        ConnectedOn = Core.Now;

        try
        {
            Address = Utility.Intern((Connection?.RemoteEndPoint as IPEndPoint)?.Address);
            _toString = Address?.ToString() ?? "(error)";
        }
        catch (Exception ex)
        {
            TraceException(ex);
            Address = IPAddress.None;
            _toString = "(error)";
        }

        _handle = GCHandle.Alloc(this);

        try
        {
            _pollGroup.Add(connection, _handle);
        }
        catch (Exception ex)
        {
            TraceException(ex);
            Disconnect("Unable to add socket to poll group");
        }

        CreatedCallback?.Invoke(this);
    }

    // Sectors
    public NetState Next { get; set; }
    public NetState Previous { get; set; }
    public bool OnLinkList { get; set; }

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

    public DecodePacket PacketDecoder
    {
        get => _packetDecoder;
        set => _packetDecoder = value;
    }

    public EncodePacket PacketEncoder
    {
        get => _packetEncoder;
        set => _packetEncoder = value;
    }

    public int CurrentPacket { get; internal set; }

    public bool SentFirstPacket { get; set; }

    public bool BlockAllPackets { get; set; }

    public List<SecureTrade> Trades { get; }

    public bool Seeded { get; set; }

    public Pipe RecvPipe { get; }

    public Pipe SendPipe { get; }

    public bool Running => _running;

    public Socket Connection { get; private set; }

    public bool CompressionEnabled { get; set; }

    public int Sequence { get; set; }

    public List<Gump> Gumps { get; private set; }

    public List<HuePicker> HuePickers { get; private set; }

    public List<IMenu> Menus { get; private set; }

    public CityInfo[] CityInfo { get; set; }

    public Mobile Mobile { get; set; }

    public ServerInfo[] ServerInfo { get; set; }

    public IAccount Account { get; set; }

    public string Assistant { get; set; }

    public int CompareTo(NetState other) => string.CompareOrdinal(_toString, other?._toString);

    private void SetPacketTime(int packetID)
    {
        if (packetID is >= 0 and < 0x100)
        {
            _packetThrottles[packetID] = Core.TickCount;
        }
    }

    public long GetPacketTime(int packetID) => packetID is >= 0 and < 0x100 ? _packetThrottles[packetID] : 0;

    private void UpdatePacketCount(int packetID)
    {
        if (packetID is >= 0 and < 0x100)
        {
            _packetCounts[packetID]++;
        }
    }

    public int CheckPacketCounts()
    {
        for (int i = 0; i < _packetCounts.Length; i++)
        {
            long count = _packetCounts[i];
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
        Menus ??= new List<IMenu>();

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
        HuePickers ??= new List<HuePicker>();

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

    public void AddGump(Gump gump)
    {
        Gumps ??= new List<Gump>();

        if (Gumps.Count < GumpCap)
        {
            Gumps.Add(gump);
        }
        else
        {
            LogInfo("Exceeded gump cap, disconnecting...");
            Disconnect("Exceeded gump cap.");
        }
    }

    public void RemoveGump(Gump gump)
    {
        Gumps?.Remove(gump);
    }

    public void RemoveGump(int index)
    {
        Gumps?.RemoveAt(index);
    }

    public void ClearGumps()
    {
        Gumps?.Clear();
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
                return;
            }
#endif
        buffer = SendPipe.Writer.AvailableToWrite();
        return !(SendPipe.Writer.IsClosed || buffer.Length <= 0);
    }

    public void Send(ReadOnlySpan<byte> span)
    {
        if (span == null || this.CannotSendPackets())
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
            PacketSendProfile prof = null;

            if (Core.Profiling)
            {
                prof = PacketSendProfile.Acquire(span[0]);
                prof.Start();
            }

            if (_packetEncoder != null)
            {
                length = _packetEncoder(span, buffer);
            }
            else
            {
                span.CopyTo(buffer);
            }

            if (PacketLogging)
            {
                LogPacket(span, false);
            }

            SendPipe.Writer.Advance((uint)length);

            if (!_flushQueued)
            {
                _flushPending.Enqueue(this);
                _flushQueued = true;
            }

            prof?.Finish();
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

    public void HandleReceive(bool throttled = false)
    {
        if (!_running)
        {
            return;
        }

        if (!throttled)
        {
            ReceiveData();
        }

        var reader = RecvPipe.Reader;

        try
        {
            // Process as many packets as we can synchronously
            while (_running && _parserState != ParserState.Error && _protocolState != ProtocolState.Error)
            {
                var buffer = reader.AvailableToRead();
                var length = buffer.Length;

                if (length <= 0)
                {
                    break;
                }

                var packetReader = new SpanReader(buffer);
                var packetId = packetReader.ReadByte();
                int packetLength = length;

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
                                    int newSeed = (packetId << 24) | (packetReader.ReadByte() << 16) | (packetReader.ReadByte() << 8) | packetReader.ReadByte();

                                    if (newSeed == 0)
                                    {
                                        HandleError(0, 0);
                                        return;
                                    }

                                    Seed = newSeed;
                                    packetLength = 4;

                                    _parserState = ParserState.AwaitingNextPacket;
                                    _protocolState = ProtocolState.GameServer_AwaitingGameServerLogin;
                                }
                                else
                                {
                                    _parserState = ParserState.AwaitingPartialPacket;
                                }
                                break;
                            }

                        case ProtocolState.LoginServer_AwaitingLogin:
                            {
                                if (packetId != 0xCF && packetId != 0x80)
                                {
                                    LogInfo("Possible encrypted client detected, disconnecting...");
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
                                if (packetId != 0x91 && packetId != 0x80)
                                {
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
                    reader.Advance((uint)packetLength);
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
        PacketHandler handler = IncomingPackets.GetHandler(packetId);
        int length = packetReader.Length;

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

        if (handler.Ingame)
        {
            if (Mobile == null)
            {
                LogInfo($"received packet 0x{packetId:X2} before having been attached to a mobile");
                return ParserState.Error;
            }

            if (Mobile.Deleted)
            {
                return ParserState.Error;
            }
        }

        var throttler = handler.ThrottleCallback;
        if (throttler != null)
        {
            if (!throttler(packetId, this, out bool drop))
            {
                return drop ? ParserState.AwaitingNextPacket : ParserState.Throttled;
            }

            SetPacketTime(packetId);
        }

        PacketReceiveProfile prof = null;

        if (Core.Profiling)
        {
            prof = PacketReceiveProfile.Acquire(packetId);
            prof?.Start();
        }

        UpdatePacketCount(packetId);

        if (PacketLogging)
        {
            LogPacket(packetReader.Buffer[..packetLength], true);
        }

        handler.OnReceive(this, packetReader, packetLength);

        prof?.Finish(packetLength);

        return ParserState.AwaitingNextPacket;
    }

    private bool Flush()
    {
        _flushQueued = false;

        if (Connection == null)
        {
            return true;
        }

        var reader = SendPipe.Reader;
        var buffer = reader.AvailableToRead();

        if (reader.IsClosed || buffer.Length == 0)
        {
            return true;
        }

        var bytesWritten = 0;

        try
        {
            bytesWritten = Connection.Send(buffer, SocketFlags.None);
        }
        catch (SocketException ex)
        {
            if (ex.SocketErrorCode != SocketError.WouldBlock)
            {
                logger.Debug(ex, "Disconnected due to a socket exception");
                Disconnect(string.Empty);
            }
        }
        catch (Exception ex)
        {
            Disconnect($"Disconnected with error: {ex}");
            TraceException(ex);
        }

        if (bytesWritten > 0)
        {
            _nextActivityCheck = Core.TickCount + 90000;
            reader.Advance((uint)bytesWritten);
        }

        return bytesWritten == buffer.Length;
    }

    private void DecodePacket(Span<byte> buffer, ref int length)
    {
        _packetDecoder?.Invoke(buffer, ref length);
    }

    private void ReceiveData()
    {
        var writer = RecvPipe.Writer;
        var buffer = writer.AvailableToWrite();

        if (writer.IsClosed || buffer.Length == 0)
        {
            return;
        }

        var bytesWritten = 0;

        try
        {
            bytesWritten = Connection.Receive(buffer, SocketFlags.None);
        }
        catch (SocketException ex)
        {
            if (ex.ErrorCode is not 54 and not 89 and not 995)
            {
                logger.Debug(ex, "Disconnected due to a socket exception");
            }

            Disconnect(string.Empty);
        }
        catch (Exception ex)
        {
            Disconnect($"Disconnected with error: {ex}");
            TraceException(ex);
        }

        if (bytesWritten <= 0)
        {
            Disconnect(string.Empty);
            return;
        }

        DecodePacket(buffer, ref bytesWritten);

        writer.Advance((uint)bytesWritten);
        _nextActivityCheck = Core.TickCount + 90000;
    }

    public static void FlushAll()
    {
        while (_flushPending.Count != 0)
        {
            _flushPending.Dequeue()?.Flush();
        }
    }

    public static void Slice()
    {
        while (_throttled.Count > 0)
        {
            var ns = _throttled.Dequeue();
            if (ns.Running)
            {
                ns.HandleReceive(true);
            }
        }

        // This is enqueued by HandleReceive if already throttled and still throttled
        while (_throttledPending.Count > 0)
        {
            _throttled.Enqueue(_throttledPending.Dequeue());
        }

        int count = _pollGroup.Poll(_polledStates);

        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                (_polledStates[i].Target as NetState)?.HandleReceive();
                _polledStates[i] = default;
            }
        }

        while (_flushPending.TryDequeue(out var ns))
        {
            if (!ns.Flush())
            {
                // Incomplete data, so we need to requeue
                _flushedPartials.Enqueue(ns);
            }
        }

        var hasDisposes = false;
        while (_disposed.TryDequeue(out var ns))
        {
            hasDisposes = true;
            ns.Dispose();
        }

        // If they weren't disconnected, requeue them
        while (_flushedPartials.TryDequeue(out var ns))
        {
            if (ns.Running)
            {
                _flushPending.Enqueue(ns);
            }
        }

        if (hasDisposes)
        {
            _pollGroup.Poll(_polledStates.Length);
        }
    }

    public void CheckAlive(long curTicks)
    {
        if (Connection != null && _nextActivityCheck - curTicks < 0)
        {
            LogInfo("Disconnecting due to inactivity...");
            Disconnect("Disconnecting due to inactivity.");
        }
    }

    public static void CheckAllAlive()
    {
        try
        {
            long curTicks = Core.TickCount;

            foreach (var ns in TcpServer.Instances)
            {
                ns.CheckAlive(curTicks);
            }
        }
        catch (Exception ex)
        {
            TraceException(ex);
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

    public void Disconnect(string reason)
    {
        if (!_running)
        {
            return;
        }

        _running = false;

#if THREADGUARD
            if (Thread.CurrentThread != Core.Thread)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Attempting to disconnect a netstate from an invalid thread!");
                Console.WriteLine(new StackTrace());
                Utility.PopColor();
                return;
            }
#endif

        _disconnectReason = reason;
        _disposed.Enqueue(this);
    }

    public static void TraceDisconnect(string reason, string ip)
    {
        if (reason == string.Empty)
        {
            return;
        }

        try
        {
            using StreamWriter op = new StreamWriter("network-disconnects.log", true);
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

    private void Dispose()
    {
        TraceDisconnect(_disconnectReason, _toString);

        if (_running)
        {
            throw new Exception("Disconnected a NetState that is still running.");
        }

#if THREADGUARD
            if (Thread.CurrentThread != Core.Thread)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Attempting to dispose a netstate from an invalid thread!");
                Console.WriteLine(new StackTrace());
                Utility.PopColor();
                return;
            }
#endif

        var m = Mobile;
        if (m?.NetState == this)
        {
            m.NetState = null;
        }

        TcpServer.Instances.Remove(this);
        try
        {
            _pollGroup.Remove(Connection, _handle);
        }
        catch (Exception ex)
        {
            TraceException(ex);
        }

        Connection.Close();
        _handle.Free();
        RecvPipe.Dispose();
        SendPipe.Dispose();

        Mobile = null;

        var a = Account;

        Gumps.Clear();
        Menus.Clear();
        HuePickers.Clear();
        Account = null;
        ServerInfo = null;
        CityInfo = null;
        Connection = null;

        var count = TcpServer.Instances.Count;

        LogInfo(a != null ? $"Disconnected. [{count} Online] [{a}]" : $"Disconnected. [{count} Online]");
    }
}
