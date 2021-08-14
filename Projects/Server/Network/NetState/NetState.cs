/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using Server.Accounting;
using Server.Diagnostics;
using Server.Gumps;
using Server.HuePickers;
using Server.Items;
using Server.Logging;
using Server.Menus;

namespace Server.Network
{
    public delegate void NetStateCreatedCallback(NetState ns);

    public delegate void DecodePacket(CircularBuffer<byte> buffer, ref int length);
    public delegate void EncodePacket(ReadOnlySpan<byte> inputBuffer, CircularBuffer<byte> outputBuffer, out int length);

    public partial class NetState : IComparable<NetState>
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(NetState));

        private const int RecvPipeSize = 1024 * 64;
        private const int SendPipeSize = 1024 * 256;
        private static int GumpCap = 512;
        private static int HuePickerCap = 512;
        private static int MenuCap = 512;
        private static int PacketPerSecondThreshold = 3000;

        private static readonly Queue<NetState> FlushPending = new(2048);
        private static readonly ConcurrentQueue<NetState> Disposed = new();

        public static NetStateCreatedCallback CreatedCallback { get; set; }

        private readonly string _toString;
        private ClientVersion _version;
        private long _nextActivityCheck;
        private int _running;
        private volatile DecodePacket _packetDecoder;
        private volatile EncodePacket _packetEncoder;
        private bool _flushQueued;
        private readonly long[] _packetThrottles = new long[0x100];
        private readonly long[] _packetCounts = new long[0x100];
        private string _disconnectReason = string.Empty;

        internal int _authId;
        internal int _seed;
        internal ParserState _parserState = ParserState.Uninitialized;
        internal ProtocolState _protocolState = ProtocolState.Uninitialized;
        internal RecvState _recvState = RecvState.Uninitialized;
        internal SendState _sendState = SendState.Uninitialized;

        internal enum ParserState
        {
            Uninitialized,
            AwaitingNextPacket,
            AwaitingPartialPacket,
            ProcessingPacket,
            Throttled,
            Error
        }

        internal enum ProtocolState
        {
            Uninitialized,
            AwaitingSeed, // Based on the way the seed arrives, we know if this is a login server or a game server connection

            LoginServer_AwaitingLogin,
            LoginServer_AwaitingServerSelect,
            LoginServer_ServerSelectAck,

            GameServer_AwaitingGameServerLogin,
            GameServer_LoggedIn,

            Error
        }

        internal enum RecvState
        {
            Uninitialized,
            AwaitingMemory,
            AwaitingRecv,
            DataReceived,
            Exited,
        }

        internal enum SendState
        {
            Uninitialized,
            AwaitingData,
            Sending,
            SendCompleted,
            Exited,
        }

        public static void Configure()
        {
            GumpCap = ServerConfiguration.GetOrUpdateSetting("netstate.gumpCap", GumpCap);
            HuePickerCap = ServerConfiguration.GetOrUpdateSetting("netstate.huePickerCap", HuePickerCap);
            MenuCap = ServerConfiguration.GetOrUpdateSetting("netstate.menuCap", MenuCap);
            PacketPerSecondThreshold = ServerConfiguration.GetOrUpdateSetting("netstate.packetsPerSecondThreshold", PacketPerSecondThreshold);
        }

        public static void Initialize()
        {
            Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1.5), CheckAllAlive);
        }

        public NetState(ISocket connection)
        {
            Connection = connection;
            Seeded = false;
            Gumps = new List<Gump>();
            HuePickers = new List<HuePicker>();
            Menus = new List<IMenu>();
            Trades = new List<SecureTrade>();
            RecvPipe = new Pipe<byte>(GC.AllocateUninitializedArray<byte>(RecvPipeSize));
            SendPipe = new Pipe<byte>(GC.AllocateUninitializedArray<byte>(SendPipeSize));
            _nextActivityCheck = Core.TickCount + 30000;

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

            ConnectedOn = Core.Now;

            CreatedCallback?.Invoke(this);
        }

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

        public bool SentFirstPacket { get; set; }

        public bool BlockAllPackets { get; set; }

        public List<SecureTrade> Trades { get; }

        public bool Running
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _running == 1;
        }

        public bool Seeded { get; set; }

        public Pipe<byte> RecvPipe { get; }

        public Pipe<byte> SendPipe { get; }

        public ISocket Connection { get; }

        public bool CompressionEnabled { get; set; }

        public int Sequence { get; set; }

        public List<Gump> Gumps { get; private set; }

        public List<HuePicker> HuePickers { get; private set; }

        public List<IMenu> Menus { get; private set; }

        public CityInfo[] CityInfo { get; set; }

        public Mobile Mobile { get; set; }

        public ServerInfo[] ServerInfo { get; set; }

        public IAccount Account { get; set; }

        public int CompareTo(NetState other) => string.CompareOrdinal(_toString, other?._toString);

        private void SetPacketTime(int packetID)
        {
            if (packetID < 0 || packetID >= 0x100)
            {
                return;
            }

            _packetThrottles[packetID] = Core.TickCount;
        }

        public long GetPacketDelay(int packetID)
        {
            if (packetID < 0 || packetID >= 0x100)
            {
                return 0;
            }

            return _packetThrottles[packetID];
        }

        private void UpdatePacketCount(int packetID)
        {
            if (packetID < 0 || packetID >= 0x100)
            {
                return;
            }

            _packetCounts[packetID]++;
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
            logger.Information("Client: {0}: {1}", this, text);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogInfo(string format, params object[] args)
        {
            LogInfo(string.Format(format, args));
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

        public bool GetSendBuffer(out CircularBuffer<byte> cBuffer)
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
            var result = SendPipe.Writer.TryGetMemory();
            cBuffer = new CircularBuffer<byte>(result.Buffer);

            return !(result.IsClosed || result.Length <= 0);
        }

        public void Send(ReadOnlySpan<byte> span)
        {
            if (span == null || Connection == null || BlockAllPackets)
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
                    _packetEncoder(span, buffer, out length);
                }
                else
                {
                    buffer.CopyFrom(span);
                }

                SendPipe.Writer.Advance((uint)length);

                if (!_flushQueued)
                {
                    FlushPending.Enqueue(this);
                    _flushQueued = true;
                }

                prof?.Finish();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex);
#endif
                TraceException(ex);
                Disconnect("Exception while sending.");
            }
        }

        internal void Start()
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) == 1 || Connection == null)
            {
                return;
            }

            _parserState = ParserState.AwaitingNextPacket;
            _protocolState = ProtocolState.AwaitingSeed;

            ThreadPool.UnsafeQueueUserWorkItem(RecvTask, null);
            ThreadPool.UnsafeQueueUserWorkItem(SendTask, null);
        }

        // Return true if there was any data to be processed. False otherwise. Used for idle detection.
        public bool HandleReceive()
        {
            if (!Running)
            {
                return false;
            }

            bool active = false;

            var reader = RecvPipe.Reader;

            try
            {
                // Process as many packets as we can synchronously
                while (Running && _parserState != ParserState.Error && _protocolState != ProtocolState.Error)
                {
                    var result = reader.TryRead();
                    var length = result.Length;

                    if (length <= 0)
                    {
                        break;
                    }

                    // There was at least some data found, so it's not idle.
                    active = true;

                    var packetReader = new CircularBufferReader(result.Buffer);
                    var packetId = packetReader.ReadByte();
                    int packetLength = length;

                    // These can arrive at any time and are only informational
                    if (_protocolState != ProtocolState.AwaitingSeed && IncomingPackets.IsInfoPacket(packetId))
                    {
                        _parserState = ParserState.ProcessingPacket;
                        _parserState = HandlePacket(packetReader, packetId, length, out packetLength);
                    }
                    else
                    {
                        switch (_protocolState)
                        {
                            case ProtocolState.Uninitialized:
                                {
                                    HandleError(packetId, packetLength);
                                    return true;
                                }

                            case ProtocolState.AwaitingSeed:
                                {
                                    if (packetId == 0xEF)
                                    {
                                        _parserState = ParserState.ProcessingPacket;
                                        _parserState = HandlePacket(packetReader, packetId, length, out packetLength);
                                        if (_parserState == ParserState.AwaitingNextPacket)
                                        {
                                            _protocolState = ProtocolState.LoginServer_AwaitingLogin;
                                        }
                                    }
                                    else if (length >= 4)
                                    {
                                        int seed = (packetId << 24) | (packetReader.ReadByte() << 16) | (packetReader.ReadByte() << 8) | packetReader.ReadByte();

                                        if (seed == 0)
                                        {
                                            HandleError(0, 0);
                                            return true;
                                        }

                                        _seed = seed;
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
                                        return true;
                                    }

                                    _parserState = ParserState.ProcessingPacket;
                                    _parserState = HandlePacket(packetReader, packetId, length, out packetLength);
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
                                        return true;
                                    }

                                    _parserState = ParserState.ProcessingPacket;
                                    _parserState = HandlePacket(packetReader, packetId, length, out packetLength);
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
                                    return true;
                                }

                            case ProtocolState.GameServer_AwaitingGameServerLogin:
                                {
                                    if (packetId != 0x91 && packetId != 0x80)
                                    {
                                        HandleError(packetId, packetLength);
                                        return true;
                                    }

                                    _parserState = ParserState.ProcessingPacket;
                                    _parserState = HandlePacket(packetReader, packetId, length, out packetLength);
                                    if (_parserState == ParserState.AwaitingNextPacket)
                                    {
                                        _protocolState = ProtocolState.GameServer_LoggedIn;
                                    }
                                    break;
                                }

                            case ProtocolState.GameServer_LoggedIn:
                                {
                                    _parserState = ParserState.ProcessingPacket;
                                    _parserState = HandlePacket(packetReader, packetId, length, out packetLength);
                                    break;
                                }
                        }
                    }

                    if (_parserState == ParserState.AwaitingNextPacket)
                    {
                        reader.Advance((uint)packetLength);
                    }
                    else if (_parserState == ParserState.AwaitingPartialPacket || _parserState == ParserState.Throttled)
                    {
                        break;
                    }
                    else
                    {
                        HandleError(packetId, packetLength);
                        break;
                    }
                }

                reader.Commit();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex);
#endif
                TraceException(ex);
                Disconnect("Exception during HandleReceive");
            }

            return active;
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
        private ParserState HandlePacket(CircularBufferReader packetReader, byte packetId, int length, out int packetLength)
        {
            PacketHandler handler = GetHandler(packetId);
            if (handler == null)
            {
                LogInfo($"received unknown packet 0x{packetId:X2} while in state {_protocolState}");
                packetLength = length;
                return ParserState.Error;
            }

            packetLength = handler.Length;
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

            ThrottlePacketCallback throttler = handler.ThrottleCallback;
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

            handler.OnReceive(this, packetReader, ref packetLength);

            prof?.Finish(packetLength);

            return ParserState.AwaitingNextPacket;
        }

        private async void SendTask(object state)
        {
            var reader = SendPipe.Reader;

            try
            {
                while (Running)
                {
                    _sendState = SendState.AwaitingData;

                    var result = await reader.Read();

                    if (result.IsClosed || !Running)
                    {
                        break;
                    }

                    if (result.Length <= 0)
                    {
                        continue;
                    }

                    _sendState = SendState.Sending;

                    var bytesWritten = await Connection.SendAsync(result.Buffer, SocketFlags.None);

                    _sendState = SendState.SendCompleted;

                    if (bytesWritten > 0)
                    {
                        _nextActivityCheck = Core.TickCount + 90000;
                        reader.Advance((uint)bytesWritten);
                    }
                }

                // Grab any remaining data and flush it
                var data = reader.TryRead();

                if (data.Length > 0)
                {
                    _sendState = SendState.Sending;
                    Connection.Send(data.Buffer, SocketFlags.None);

                    reader.Advance((uint)data.Length);
                }
            }
            catch (SocketException ex)
            {
                // If the user closes the connection (or the recv side does)
                // between the check for m_Running above and the call to SendAsync,
                // we can still get a socket exception here. That's ok.
#if DEBUG
                Console.WriteLine(ex);
#endif
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex);
#endif
                TraceException(ex);
            }
            finally
            {
                try
                {
                    Connection.Shutdown(SocketShutdown.Both);
                    Connection.Close();
                }
                catch (Exception ex)
                {
                    TraceException(ex);
                }

                Disconnect("Exiting SendTask.");
                _sendState = SendState.Exited;
            }
        }

        private void DecodePacket(ArraySegment<byte>[] buffer, ref int length)
        {
            CircularBuffer<byte> cBuffer = new CircularBuffer<byte>(buffer);
            _packetDecoder?.Invoke(cBuffer, ref length);
        }

        private async void RecvTask(object state)
        {
            var socket = Connection;
            var writer = RecvPipe.Writer;

            try
            {
                while (Running)
                {
                    _recvState = RecvState.AwaitingMemory;
                    var result = await writer.GetMemory();

                    if (result.IsClosed || !Running)
                    {
                        break;
                    }

                    if (result.Length <= 0)
                    {
                        continue;
                    }

                    _recvState = RecvState.AwaitingRecv;

                    var bytesWritten = await socket.ReceiveAsync(result.Buffer, SocketFlags.None);
                    if (bytesWritten <= 0)
                    {
                        break;
                    }

                    _recvState = RecvState.DataReceived;

                    DecodePacket(result.Buffer, ref bytesWritten);

                    writer.Advance((uint)bytesWritten);
                    _nextActivityCheck = Core.TickCount + 90000;

                    // No need to flush
                }

                Disconnect(string.Empty);
            }
            catch (SocketException ex)
            {
#if DEBUG
                if (ex.ErrorCode != 54 && ex.ErrorCode != 89 && ex.ErrorCode != 995)
                {
                    Console.WriteLine(ex);
                }
#endif
                Disconnect(string.Empty);
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex);
#endif
                Disconnect("RecvTask exited unexpectedly.");
                TraceException(ex);
            }
            finally
            {
                _recvState = RecvState.Exited;
            }
        }

        public static int HandleAllReceives()
        {
            int count = 0;

            foreach (var ns in TcpServer.Instances)
            {
                if (ns.HandleReceive())
                {
                    count++;
                }
            }

            return count;
        }

        public void Flush()
        {
            if (Connection != null)
            {
                SendPipe.Writer.Flush();
            }

            _flushQueued = false;
        }

        public static void FlushAll()
        {
            while (FlushPending.Count != 0)
            {
                FlushPending.Dequeue()?.Flush();
            }
        }

        public static int Slice()
        {
            int count = 0;
            while (FlushPending.Count != 0)
            {
                FlushPending.Dequeue()?.Flush();
                count++;
            }

            while (Disposed.TryDequeue(out var ns))
            {
                TcpServer.Instances.Remove(ns);
                ns.Dispose();
            }

            return count;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PacketHandler GetHandler(int packetID) => IncomingPackets.GetHandler(packetID);

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
            if (Interlocked.CompareExchange(ref _running, 0, 1) == 0)
            {
                return;
            }

            try
            {
                if (_disconnectReason != string.Empty)
                {
                    throw new Exception("Attempted to disconnect a netstate twice.");
                }
            }
            catch (Exception ex)
            {
                TraceException(ex);
            }

            _disconnectReason = reason;

            Disposed.Enqueue(this);
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

            RecvPipe.Writer.Close();
            SendPipe.Writer.Close();

            var m = Mobile;
            Mobile = null;

            if (m?.NetState == this)
            {
                m.NetState = null;
            }

            var a = Account;

            Gumps.Clear();
            Menus.Clear();
            HuePickers.Clear();
            Account = null;
            ServerInfo = null;
            CityInfo = null;

            var count = TcpServer.Instances.Count;

            if (a != null)
            {
                LogInfo("Disconnected. [{0} Online] [{1}]", count, a);
            }
            else
            {
                LogInfo("Disconnected. [{0} Online]", count);
            }
        }
    }
}
