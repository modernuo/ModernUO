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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Server.Accounting;
using Server.Gumps;
using Server.HuePickers;
using Server.Items;
using Server.Menus;

namespace Server.Network
{
    public delegate void NetStateCreatedCallback(NetState ns);

    public partial class NetState : IComparable<NetState>
    {
        private static int IncomingPipeSize = 1024 * 64;
        private static int OutgoingPipeSize = 1024 * 256;
        private static int GumpCap = 512;
        private static int HuePickerCap = 512;
        private static int MenuCap = 512;

        private static readonly ConcurrentQueue<NetState> m_Disposed = new ConcurrentQueue<NetState>();
        private static NetworkState m_NetworkState = NetworkState.ResumeState;

        public static NetStateCreatedCallback CreatedCallback { get; set; }

        private readonly string m_ToString;
        private int m_Disposing;
        private ClientVersion m_Version;
        private byte[] m_IncomingBuffer;
        private Pipe m_IncomingPipe;
        private byte[] m_OutgoingBuffer;
        private Pipe m_OutgoingPipe;
        private long m_NextCheckActivity;
        private volatile bool m_Running;

        internal int m_AuthID;
        internal int m_Seed;

        public static void Configure()
        {
            IncomingPipeSize = ServerConfiguration.GetOrUpdateSetting("netstate.incomingPipeSize", IncomingPipeSize);
            OutgoingPipeSize = ServerConfiguration.GetOrUpdateSetting("netstate.outgoingPipeSize", OutgoingPipeSize);
            GumpCap = ServerConfiguration.GetOrUpdateSetting("netstate.gumpCap", GumpCap);
            HuePickerCap = ServerConfiguration.GetOrUpdateSetting("netstate.huePickerCap", HuePickerCap);
            MenuCap = ServerConfiguration.GetOrUpdateSetting("netstate.menuCap", MenuCap);
        }

        public static void Initialize()
        {
            var checkAliveDuration = TimeSpan.FromMinutes(1.5);
            Timer.DelayCall(checkAliveDuration, checkAliveDuration, CheckAllAlive);
        }

        public NetState(Socket connection)
        {
            m_Running = false;
            Connection = connection;
            Seeded = false;
            Gumps = new List<Gump>();
            HuePickers = new List<HuePicker>();
            Menus = new List<IMenu>();
            Trades = new List<SecureTrade>();
            m_IncomingBuffer = new byte[IncomingPipeSize];
            m_IncomingPipe = new Pipe(m_IncomingBuffer);
            m_OutgoingBuffer = new byte[OutgoingPipeSize];
            m_OutgoingPipe = new Pipe(m_OutgoingBuffer);
            m_NextCheckActivity = Core.TickCount + 30000;

            try
            {
                Address = Utility.Intern((Connection?.RemoteEndPoint as IPEndPoint)?.Address);
                m_ToString = Address?.ToString() ?? "(error)";
            }
            catch (Exception ex)
            {
                TraceException(ex);
                Address = IPAddress.None;
                m_ToString = "(error)";
            }

            ConnectedOn = DateTime.UtcNow;

            CreatedCallback?.Invoke(this);
        }

        public DateTime ConnectedOn { get; }

        public TimeSpan ConnectedFor => DateTime.UtcNow - ConnectedOn;

        public DateTime ThrottledUntil { get; set; }

        public IPAddress Address { get; }

        public IPacketEncoder PacketEncoder { get; set; }

        public bool SentFirstPacket { get; set; }

        public bool BlockAllPackets { get; set; }

        public List<SecureTrade> Trades { get; }

        public bool Running => m_Running;

        public bool Seeded { get; set; }

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

        public bool IsDisposing => m_Disposing != 0;

        public int CompareTo(NetState other) => other == null ? 1 : m_ToString.CompareTo(other.m_ToString);

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

        public void WriteConsole(string text)
        {
            Console.WriteLine("Client: {0}: {1}", this, text);
        }

        public void WriteConsole(string format, params object[] args)
        {
            WriteConsole(string.Format(format, args));
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
                WriteConsole("Exceeded menu cap, disconnecting...");
                Dispose();
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
                WriteConsole("Exceeded hue picker cap, disconnecting...");
                Dispose();
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
                WriteConsole("Exceeded gump cap, disconnecting...");
                Dispose();
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
            Send(new MessageLocalized(Serial.MinusOne, -1, MessageType.Label, 0x35, 3, 501231, "", ""));
            Send(new LaunchBrowser(url));
        }

        public override string ToString() => m_ToString;

        public static void Pause()
        {
            NetworkState.Pause(ref m_NetworkState);
        }

        public static void Resume()
        {
            NetworkState.Resume(ref m_NetworkState);
        }

        public virtual void Send(Packet p)
        {
            if (Connection == null || BlockAllPackets)
            {
                p.OnSend();
                return;
            }

            var writer = m_OutgoingPipe.Writer;

            try
            {
                byte[] buffer = p.Compile(CompressionEnabled, out var length);

                if (buffer.Length > 0 && length > 0)
                {
                    var result = writer.GetBytes();

                    if (result.Length >= length)
                    {
                        result.CopyFrom(buffer.AsSpan(0, length));
                        writer.Advance((uint)length);

                        // Flush at the end of the game loop
                    }
                    else
                    {
                        WriteConsole("Too much data pending, disconnecting...");
                        Dispose();
                    }
                }
                else
                {
                    WriteConsole("Didn't write anything!");
                }

                p.OnSend();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                TraceException(ex);
                Dispose();
            }
        }

        internal void Start()
        {
            if (Connection == null || m_Running)
            {
                return;
            }

            m_Running = true;

            ThreadPool.UnsafeQueueUserWorkItem(RecvTask, null);
            ThreadPool.UnsafeQueueUserWorkItem(SendTask, null);
        }

        private async void SendTask(object state)
        {
            var reader = m_OutgoingPipe.Reader;

            while (m_Running)
            {
                var result = await reader.GetBytes();

                if (result.Length <= 0)
                {
                    continue;
                }

                var buffer = result.Buffer;

                // WriteConsole("Sending: " + HexStringConverter.GetString(buffer[0]) + HexStringConverter.GetString(buffer[1]));

                var bytesWritten = await Connection.SendAsync(buffer, SocketFlags.None);

                if (bytesWritten > 0)
                {
                    m_NextCheckActivity = Core.TickCount + 90000;
                    reader.Advance((uint)bytesWritten);
                }
            }

            WriteConsole("Not running");
            Dispose();
        }

        private async void RecvTask(object state)
        {
            var socket = Connection;
            var writer = m_IncomingPipe.Writer;

            try
            {
                while (m_Running)
                {
                    if (m_NetworkState == NetworkState.PauseState)
                    {
                        continue;
                    }

                    var result = writer.GetBytes();

                    if (result.Length <= 0)
                    {
                        continue;
                    }

                    var buffer = result.Buffer;

                    var bytesWritten = await socket.ReceiveAsync(buffer, SocketFlags.None);

                    if (bytesWritten <= 0)
                    {
                        break;
                    }

                    writer.Advance((uint)bytesWritten);
                    m_NextCheckActivity = Core.TickCount + 90000;

                    // No need to flush
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex);
                TraceException(ex);
#endif
            }
            finally
            {
                Dispose();
            }
        }

        public static void HandleAllReceives()
        {
            var clients = TcpServer.Instances;

            for (int i = 0; i < clients.Count; ++i)
            {
                clients[i].HandleReceive();
            }
        }

        public void HandleReceive()
        {
            if (Connection == null)
            {
                return;
            }

            try
            {
                var reader = m_IncomingPipe.Reader;

                // Process as many packets as we can synchronously
                while (true)
                {
                    var result = reader.TryGetBytes();

                    if (result.Length <= 0)
                    {
                        return;
                    }

                    var bytesProcessed = PacketHandlers.ProcessPacket(this, result.Buffer);

                    if (bytesProcessed <= 0)
                    {
                        // Error
                        // TODO: Throw exception instead?
                        if (bytesProcessed < 0)
                        {
                            Dispose();
                        }
                        else
                        {
                            WriteConsole("Not enough room for an entire packet!");
                        }

                        return;
                    }

                    reader.Advance((uint)bytesProcessed);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex);
                TraceException(ex);
#endif
                Dispose();
            }
        }

        public void Flush()
        {
            if (Connection != null)
            {
                m_OutgoingPipe.Writer.Flush();
            }
        }

        public static void FlushAll()
        {
            var clients = TcpServer.Instances;

            for (int i = 0; i < clients.Count; ++i)
            {
                clients[i].Flush();
            }
        }

        public void CheckAlive(long curTicks)
        {
            if (Connection != null && m_NextCheckActivity - curTicks < 0)
            {
                WriteConsole("Disconnecting due to inactivity...");
                Dispose();
            }
        }

        public static void CheckAllAlive()
        {
            try
            {
                long curTicks = Core.TickCount;

                var clients = TcpServer.Instances;

                if (clients.Count >= 1024)
                {
                    Parallel.ForEach(clients, ns => ns.CheckAlive(curTicks));
                }
                else
                {
                    for (int i = 0; i < clients.Count; ++i)
                    {
                        clients[i].CheckAlive(curTicks);
                    }
                }
            }
            catch (Exception ex)
            {
                TraceException(ex);
            }
        }

        public bool CheckEncrypted(int packetID)
        {
            if (!SentFirstPacket && packetID != 0xF0 && packetID != 0xF1 && packetID != 0xCF && packetID != 0x80 &&
                packetID != 0x91 && packetID != 0xA4 && packetID != 0xEF)
            {
                WriteConsole("Encrypted client detected, disconnecting");
                Dispose();
                return true;
            }

            return false;
        }

        public PacketHandler GetHandler(int packetID) =>
            ContainerGridLines ? PacketHandlers.Get6017Handler(packetID) : PacketHandlers.GetHandler(packetID);

        public static void TraceException(Exception ex)
        {
            try
            {
                using var op = new StreamWriter("network-errors.log", true);
                op.WriteLine("# {0}", DateTime.UtcNow);

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

        public virtual void Dispose()
        {
            if (Connection == null || Interlocked.Exchange(ref m_Disposing, 1) != 0)
            {
                return;
            }

            m_OutgoingPipe.Writer.Close();

            try
            {
                Connection.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                TraceException(ex);
            }
            finally
            {
                m_Disposed.Enqueue(this);
            }
        }

        public static void ProcessDisposedQueue()
        {
            var breakout = 0;

            while (breakout++ < 200)
            {
                if (!m_Disposed.TryDequeue(out var ns))
                {
                    break;
                }

                var m = ns.Mobile;
                var a = ns.Account;

                if (m != null)
                {
                    m.NetState = null;
                    ns.Mobile = null;
                }

                ns.m_Running = false;
                ns.Connection = null;
                ns.m_IncomingBuffer = null;
                ns.m_IncomingPipe = null;
                ns.m_OutgoingBuffer = null;
                ns.m_OutgoingPipe = null;
                ns.Gumps.Clear();
                ns.Menus.Clear();
                ns.HuePickers.Clear();
                ns.Account = null;
                ns.ServerInfo = null;
                ns.CityInfo = null;

                TcpServer.Instances.Remove(ns);

                if (a != null)
                {
                    ns.WriteConsole("Disconnected. [{0} Online] [{1}]", TcpServer.Instances.Count, a);
                }
                else
                {
                    ns.WriteConsole("Disconnected. [{0} Online]", TcpServer.Instances.Count);
                }
            }
        }
    }
}
