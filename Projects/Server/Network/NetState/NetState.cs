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

using Server.Accounting;
using Server.Collections;
using Server.HuePickers;
using Server.Items;
using Server.Logging;
using Server.Menus;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Network;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server.Network;

public delegate void DecodePacket(Span<byte> buffer, ref int length);
public delegate int EncodePacket(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer);

public partial class NetState : IComparable<NetState>, IValueLinkListNode<NetState>
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(NetState));

    private static readonly TimeSpan ConnectingSocketIdleLimit = TimeSpan.FromMilliseconds(5000); // 5 seconds
    private const int RecvPipeSize = 1024 * 64;
    private const int SendPipeSize = 1024 * 256;
    private const int HuePickerCap = 512;
    private const int MenuCap = 512;
    private const int PacketPerSecondThreshold = 3000;

    private static readonly GCHandle[] _polledStates = new GCHandle[2048];
    private static readonly IPollGroup _pollGroup = PollGroup.Create();
    private static readonly Queue<NetState> _flushPending = new(2048);
    private static readonly Queue<NetState> _flushedPartials = new(256);
    private static readonly ConcurrentQueue<NetState> _disposed = new();
    private static readonly Queue<NetState> _throttled = new(256);
    private static readonly Queue<NetState> _throttledPending = new(256);

    private static readonly SortedSet<NetState> _connecting = new(NetStateConnectingComparer.Instance);
    private static readonly HashSet<NetState> _instances = new(2048);
    public static IReadOnlySet<NetState> Instances => _instances;

    private readonly string _toString;
    private ClientVersion _version;
    private bool _running = true;
    private volatile DecodePacket _packetDecoder;
    private volatile EncodePacket _packetEncoder;
    private bool _flushQueued;
    private long[] _packetThrottles;
    private long[] _packetCounts;
    private string _disconnectReason = string.Empty;

    internal ParserState _parserState = ParserState.AwaitingNextPacket;
    internal ProtocolState _protocolState = ProtocolState.AwaitingSeed;
    internal GCHandle _handle;
    private bool _packetLogging;

    public GCHandle Handle => _handle;

    // Speed Hack Prevention - Movement Queue System
    internal struct QueuedMovement
    {
        public Direction Direction;
        public int Sequence;
        public long QueuedAt;
    }

    // Movement history record for rate calculation (8 bytes, cache-aligned)
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct MovementRecord
    {
        public short Interval;      // Time since previous packet (ms), capped at 32767
        public ushort TargetSpeed;  // Expected interval (100ms mounted running, etc.)
        public byte QueueDepth;     // Queue size when received
        public byte Flags;          // MovementRecordFlags
        public short Reserved;      // Padding to 8 bytes for cache alignment
    }

    [Flags]
    internal enum MovementRecordFlags : byte
    {
        None = 0,
        Running = 1,
        Mounted = 2,
        DirectionChangeOnly = 4,  // Cost was 0 (turn in place)
        WasQueued = 8             // Packet was queued, not executed immediately
    }

    internal Queue<QueuedMovement> _movementQueue;          // Lazy initialized
    internal long _movementCredit;                          // Credit buffer for timing jitter
    internal long _nextMovementTime = Core.TickCount;       // When next movement is allowed
    internal int _sustainedQueueDepth;                      // Tracks sustained high queue depth
    internal long _lastQueueDepthCheck;                     // Throttle depth check frequency
    internal bool _hasQueuedMovements;                      // Fast check for Slice()

    // Movement history for rate-based speed hack detection (lazy initialized)
    internal MovementRecord[] _movementHistory;             // Circular buffer
    internal int _movementHistoryIndex;                     // Next write position (also serves as count until full)
    internal bool _movementHistoryFull;                     // True once buffer has wrapped
    internal long _lastMovementRecordTime;                  // For calculating intervals

    // Detection state
    internal int _consecutiveHighRateSeconds;               // Sustained detection counter
    internal long _lastSpeedHackNotification;               // Rate-limit notifications

    // RTT Measurement - Using ClientVersionRequest (0xBD) as probe
    internal long _rttProbeTime;                            // When we sent the probe (0 = not waiting)
    internal long _lastRtt;                                 // Most recent RTT measurement
    internal long[] _rttHistory;                            // Rolling history (lazy init)
    internal int _rttHistoryIndex;                          // Current position in history
    internal int _rttSampleCount;                           // Number of samples collected (saturates at RttHistorySize)
    internal long _rttVariance;                             // Calculated variance for stability
    internal long _nextRttProbe;                            // When to send next probe

    // Movement packet rate tracking (for speed hack detection)
    internal long _movementWindowStart;                     // Start of current 1-second window
    internal int _movementsInWindow;                        // Count in current window
    internal int _peakMovementRate;                         // Highest rate seen (packets/sec)

    // General packet throttle state (used for other throttled packets)
    internal bool _isThrottled;

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
    }

    public static void Initialize()
    {
        Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1.5), CheckAllAlive);
    }

    public NetState(Socket connection)
    {
        Connection = connection;
        Seeded = false;
        HuePickers = [];
        Menus = [];
        Trades = [];
        RecvPipe = new Pipe(RecvPipeSize);
        SendPipe = new Pipe(SendPipeSize);
        NextActivityCheck = Core.TickCount + 30000;
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

        _instances.Add(this);
        _connecting.Add(this);
        _handle = GCHandle.Alloc(this);

        LogInfo($"Connected. [{_instances.Count} Online]");

        try
        {
            _pollGroup.Add(connection, _handle);
        }
        catch (Exception ex)
        {
            TraceException(ex);
            Disconnect("Unable to add socket to poll group");
        }
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

    public List<HuePicker> HuePickers { get; private set; }

    public List<IMenu> Menus { get; private set; }

    public CityInfo[] CityInfo { get; set; }

    public Mobile Mobile { get; set; }

    public ServerInfo[] ServerInfo { get; set; }

    public IAccount Account
    {
        get => _account;
        set
        {
            if (_account != null)
            {
                _connecting.Remove(this);
            }

            _account = value;
        }
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

    /// <summary>
    /// Resets movement state when sequence needs to be cleared (paralysis, teleport, map change, etc.)
    /// </summary>
    public void ResetMovementState()
    {
        _movementQueue?.Clear();
        Sequence = 0;
        _nextMovementTime = Core.TickCount;
        _movementCredit = 0;
        _hasQueuedMovements = false;
        _sustainedQueueDepth = 0;

        // Reset movement history - next movement starts a new chain
        _lastMovementRecordTime = 0;
        _movementHistoryIndex = 0;
        _movementHistoryFull = false;

        // Reset detection state - sustained detection loses context on teleport/map change
        _consecutiveHighRateSeconds = 0;
        _rttProbeInterval = RttProbeIntervalNormal;

        // Reset packet rate window
        _movementWindowStart = 0;
        _movementsInWindow = 0;
    }

    // RTT Measurement Configuration
    private const int RttProbeIntervalNormal = 5000;      // Normal: probe every 5 seconds
    private const int RttProbeIntervalSuspicious = 2000;  // Suspicious: probe every 2 seconds
    private const int RttProbeIntervalDefinite = 1000;    // Definite cheater: probe every 1 second
    private const int RttProbeJitter = 500;               // Random jitter to prevent bursts
    private const int RttHistorySize = 8;                 // Keep 8 samples
    private const long StableVarianceThreshold = 2500;    // Variance < 50ms std dev = stable

    // Current probe interval (can be adjusted based on suspicion level)
    internal int _rttProbeInterval = RttProbeIntervalNormal;

    /// <summary>
    /// Sets the RTT probe interval based on suspicion level.
    /// More suspicious = more frequent probes for better evidence.
    /// </summary>
    public void SetProbeFrequency(int suspicionLevel)
    {
        _rttProbeInterval = suspicionLevel switch
        {
            >= 3 => RttProbeIntervalDefinite,   // Definite cheater
            >= 2 => RttProbeIntervalSuspicious, // Likely cheater
            _ => RttProbeIntervalNormal         // Normal or Possible
        };
    }

    // High-resolution timestamp for RTT measurement (Stopwatch ticks, not game loop ticks)
    private long _rttProbeTimestampHiRes;

    /// <summary>
    /// Sends an RTT probe if enough time has passed since the last one.
    /// Called from movement validation when player is actively moving.
    /// </summary>
    public void MaybeSendRttProbe()
    {
        // Only probe logged-in players
        if (Mobile?.Deleted != false)
        {
            return;
        }

        var now = Core.TickCount;

        // Don't send if we're still waiting for a response
        if (_rttProbeTime > 0)
        {
            // Timeout after 10 seconds - connection is probably dead or very laggy
            if (now - _rttProbeTime > 10000)
            {
                _rttProbeTime = 0;
                _rttProbeTimestampHiRes = 0;
            }
            return;
        }

        // First probe: send immediately when player starts moving
        // Subsequent probes: send when interval has passed
        if (_nextRttProbe == 0 || now >= _nextRttProbe)
        {
            _rttProbeTime = now;
            _rttProbeTimestampHiRes = System.Diagnostics.Stopwatch.GetTimestamp();
            _nextRttProbe = now + _rttProbeInterval + Utility.Random(RttProbeJitter);
            Console.WriteLine($"[RTT-Probe] Sending at TickCount={now}, HiRes={_rttProbeTimestampHiRes}");
            this.SendClientVersionRequest();
        }
    }

    /// <summary>
    /// Records an RTT measurement when ClientVersion response is received.
    /// </summary>
    public void RecordRttMeasurement()
    {
        var nowHiRes = System.Diagnostics.Stopwatch.GetTimestamp();
        var now = Core.TickCount;

        if (_rttProbeTime <= 0)
        {
            Console.WriteLine($"[RTT-Response] Received at TickCount={now} but no probe pending (client-initiated?)");
            return; // Not expecting a response (client-initiated version send)
        }

        var rtt = now - _rttProbeTime;

        // High-resolution RTT in microseconds
        var rttHiResUs = (nowHiRes - _rttProbeTimestampHiRes) * 1_000_000 / System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine($"[RTT-Response] TickCount: {_rttProbeTime} → {now} = {rtt}ms | HiRes: {rttHiResUs}µs ({rttHiResUs/1000.0:F2}ms)");

        _rttProbeTime = 0;
        _rttProbeTimestampHiRes = 0;

        // Sanity check - RTT should be positive and reasonable
        if (rtt is <= 0 or > 10000)
        {
            Console.WriteLine($"[RTT-Response] Invalid RTT {rtt}ms, discarding");
            return;
        }

        // Lazy init history
        _rttHistory ??= new long[RttHistorySize];

        // Update history
        _rttHistory[_rttHistoryIndex++ & (RttHistorySize - 1)] = rtt;
        _lastRtt = rtt;

        // Track sample count (saturates at buffer size)
        if (_rttSampleCount < RttHistorySize)
        {
            _rttSampleCount++;
        }

        // Recalculate variance
        UpdateRttVariance();
        Console.WriteLine($"[RTT-Response] Recorded RTT={rtt}ms, AvgRTT={AverageRtt}ms, Variance={_rttVariance}, Samples={_rttSampleCount}, Stable={HasStableConnection}");
    }

    /// <summary>
    /// Calculates the variance of RTT measurements for connection stability assessment.
    /// </summary>
    private void UpdateRttVariance()
    {
        if (_rttHistory == null)
        {
            _rttVariance = 0;
            return;
        }

        long sum = 0;
        long sumSq = 0;
        int count = 0;

        for (int i = 0; i < RttHistorySize; i++)
        {
            var sample = _rttHistory[i];
            if (sample > 0)
            {
                sum += sample;
                sumSq += sample * sample;
                count++;
            }
        }

        if (count < 2)
        {
            _rttVariance = 0;
            return;
        }

        var mean = sum / count;
        _rttVariance = sumSq / count - mean * mean;
    }

    /// <summary>
    /// Gets the average RTT from recent measurements.
    /// </summary>
    public long AverageRtt
    {
        get
        {
            if (_rttHistory == null)
            {
                return 0;
            }

            long sum = 0;
            int count = 0;

            for (int i = 0; i < RttHistorySize; i++)
            {
                var sample = _rttHistory[i];
                if (sample > 0)
                {
                    sum += sample;
                    count++;
                }
            }

            return count > 0 ? sum / count : 0;
        }
    }

    /// <summary>
    /// Returns true if the connection has stable, low-variance latency.
    /// Requires at least 3 samples to make a stability determination.
    /// Low variance (including 0 for identical samples) indicates stability.
    /// </summary>
    public bool HasStableConnection => _rttSampleCount >= 3 && _rttVariance < StableVarianceThreshold;

    /// <summary>
    /// Tracks movement packet rate. Called for each movement packet received.
    /// Returns the current rate (packets per second in the last window).
    /// </summary>
    public int TrackMovementRate()
    {
        var now = Core.TickCount;

        // Check if we're in a new 1-second window
        if (now - _movementWindowStart >= 1000)
        {
            // Record peak rate if this window had movements
            if (_movementsInWindow > _peakMovementRate)
            {
                _peakMovementRate = _movementsInWindow;
            }

            // Start new window
            _movementWindowStart = now;
            _movementsInWindow = 1;
            return 1;
        }

        // Same window, increment count
        _movementsInWindow++;
        return _movementsInWindow;
    }

    /// <summary>
    /// Gets the current movement rate (packets in the current 1-second window).
    /// </summary>
    public int CurrentMovementRate => _movementsInWindow;

    /// <summary>
    /// Gets the peak movement rate observed for this session.
    /// </summary>
    public int PeakMovementRate => _peakMovementRate;

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
        buffer = SendPipe.Writer.AvailableToWrite();
        return !(SendPipe.Writer.IsClosed || buffer.Length <= 0);
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
                                if (packetId != 0x80)
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
                                if (packetId == 0x80)
                                {
                                    goto case ProtocolState.LoginServer_AwaitingLogin;
                                }

                                if (packetId != 0x91)
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

    private bool Flush()
    {
        _flushQueued = false;

        // We don't have a running check since we need to send the last bits of data even after a disconnect, but before a dispose.
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
                return true;
            }
        }
        catch (Exception ex)
        {
            Disconnect($"Disconnected with error: {ex}");
            TraceException(ex);
            return true;
        }

        if (bytesWritten > 0)
        {
            NextActivityCheck = Core.TickCount + 90000;
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
        NextActivityCheck = Core.TickCount + 90000;
    }

    private static void DisconnectUnattachedSockets()
    {
        var now = Core.Now;

        // Clear out any sockets that have been connecting for too long
        while (_connecting.Count > 0)
        {
            var ns = _connecting.Min;
            var socketTime = ns.ConnectedOn;

            // If the socket has been connected for less than the limit, we can stop checking
            if (now - socketTime < ConnectingSocketIdleLimit)
            {
                break;
            }

            // Socket must have finished the entire authentication process or be forcibly disconnected.
            if (!ns.Running || !ns.SentFirstPacket || !ns.Seeded || ns.Account == null)
            {
                // Not sending a message because it will fill up the logs.
                ns.Disconnect(null);
            }

            _connecting.Remove(ns);
        }
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
        DisconnectUnattachedSockets();

        while (_throttled.Count > 0)
        {
            var ns = _throttled.Dequeue();
            ns._isThrottled = false;
            if (ns.Running)
            {
                ns.HandleReceive(true);
            }
        }

        // This is enqueued by HandleReceive if already throttled and still throttled
        while (_throttledPending.Count > 0)
        {
            var throttled = _throttledPending.Dequeue();
            throttled._isThrottled = true;
            _throttled.Enqueue(throttled);
        }

        // Process queued movements at proper intervals
        MovementThrottle.ProcessAllQueues();

        // RTT probes are now event-driven from movement packets (see MovementThrottle)
        // No need to loop all NetStates - we only probe players who are actively moving

        var count = _pollGroup.Poll(_polledStates);

        if (count > 0)
        {
            for (var i = 0; i < count; i++)
            {
                if (_polledStates[i].Target is NetState { _isThrottled: false } ns)
                {
                    ns.HandleReceive();
                }
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
        if (Connection != null && NextActivityCheck - curTicks < 0)
        {
            LogInfo("Disconnecting due to inactivity...");
            Disconnect("Disconnecting due to inactivity.");
        }
    }

    public static void CheckAllAlive()
    {
        try
        {
            var curTicks = Core.TickCount;

            foreach (var ns in Instances)
            {
                ns.CheckAlive(curTicks);
            }
        }
        catch (Exception ex)
        {
            TraceException(ex);
        }
    }

    /// <summary>
    /// Sends RTT probes to all logged-in players that are due for measurement.
    /// </summary>
    public static void SendRttProbes()
    {
        foreach (var ns in Instances)
        {
            ns.MaybeSendRttProbe();
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

        _disconnectReason = reason;
        _disposed.Enqueue(this);
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

    private void Dispose()
    {
        // It's possible we could queue for dispose multiple times
        if (Connection == null)
        {
            return;
        }

        TraceDisconnect(_disconnectReason, _toString);

        if (_running)
        {
            throw new Exception("Disconnected a NetState that is still running.");
        }

        var m = Mobile;
        if (m?.NetState == this)
        {
            m.NetState = null;
        }

        _instances.Remove(this);
        _connecting.Remove(this);

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

        Menus.Clear();
        HuePickers.Clear();
        Account = null;
        ServerInfo = null;
        CityInfo = null;
        Connection = null;

        var count = _instances.Count;

        LogInfo(a != null ? $"Disconnected. [{count} Online] [{a}]" : $"Disconnected. [{count} Online]");
    }

    private class NetStateConnectingComparer : IComparer<NetState>
    {
        public static readonly IComparer<NetState> Instance = new NetStateConnectingComparer();

        public int Compare(NetState x, NetState y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            var connectedOn = x.ConnectedOn.CompareTo(y.ConnectedOn);
            if (connectedOn != 0)
            {
                return connectedOn;
            }

            return x.CompareTo(y);
        }
    }
}
