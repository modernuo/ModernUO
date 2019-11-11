/***************************************************************************
 *                                NetState.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Server.Gumps;
using Server.Items;
using Server.Menus;
using NetworkPackets = Server.Network.Packets;

namespace Server.Network
{
  public interface IPacketEncoder
  {
    void EncodeOutgoingPacket(NetState to, ref Memory<byte> seq);
    void DecodeIncomingPacket(NetState from, ref Memory<byte> seq);
  }

  public delegate void NetStateCreatedCallback(NetState ns);

  [Flags]
  public enum ProtocolChanges
  {
    NewSpellbook = 0x00000001,
    DamagePacket = 0x00000002,
    Unpack = 0x00000004,
    BuffIcon = 0x00000008,
    NewHaven = 0x00000010,
    ContainerGridLines = 0x00000020,
    ExtendedSupportedFeatures = 0x00000040,
    StygianAbyss = 0x00000080,
    HighSeas = 0x00000100,
    NewCharacterList = 0x00000200,
    NewCharacterCreation = 0x00000400,
    ExtendedStatus = 0x00000800,
    NewMobileIncoming = 0x00001000,
    NewSecureTrading = 0x00002000,
    UltimaStore = 0x00004000,
    EndlessJourney = 0x00008000,

    Version400a = NewSpellbook,
    Version407a = Version400a | DamagePacket,
    Version500a = Version407a | Unpack,
    Version502b = Version500a | BuffIcon,
    Version6000 = Version502b | NewHaven,
    Version6017 = Version6000 | ContainerGridLines,
    Version60142 = Version6017 | ExtendedSupportedFeatures,
    Version7000 = Version60142 | StygianAbyss,
    Version7090 = Version7000 | HighSeas,
    Version70130 = Version7090 | NewCharacterList,
    Version70160 = Version70130 | NewCharacterCreation,
    Version70300 = Version70160 | ExtendedStatus,
    Version70331 = Version70300 | NewMobileIncoming,
    Version704565 = Version70331 | NewSecureTrading,
    Version70500 = Version704565 | UltimaStore,
    Version70610 = Version70500 | EndlessJourney
  }

  public class AsyncState
  {
    public bool Paused { get; set; }
    public AsyncState(bool paused) => Paused = paused;
  }

  public class NetState : IComparable<NetState>
  {
    private string m_ToString;
    private ClientVersion m_Version;

    public DateTime ConnectedOn { get; }

    public TimeSpan ConnectedFor => DateTime.UtcNow - ConnectedOn;

    public DateTime ThrottledUntil { get; set; }

    internal int m_Seed;
    internal int m_AuthID;

    public IPAddress Address { get; }

    private static AsyncState m_PauseState = new AsyncState(true);
    private static AsyncState m_ResumeState = new AsyncState(false);

    private static AsyncState m_AsyncState = m_ResumeState;

    public bool UseCompression { get; set; }

    public IPacketEncoder PacketEncoder { get; set; }

    public static NetStateCreatedCallback CreatedCallback { get; set; }

    public bool SentFirstPacket { get; set; }

    public bool BlockAllPackets { get; set; }

    public ClientFlags Flags { get; set; }

    public ClientVersion Version
    {
      get => m_Version;
      set
      {
        m_Version = value;

        if (value >= m_Version70610)
          ProtocolChanges = ProtocolChanges.Version70610;
        if (value >= m_Version70500)
          ProtocolChanges = ProtocolChanges.Version70500;
        if (value >= m_Version704565)
          ProtocolChanges = ProtocolChanges.Version704565;
        else if (value >= m_Version70331)
          ProtocolChanges = ProtocolChanges.Version70331;
        else if (value >= m_Version70300)
          ProtocolChanges = ProtocolChanges.Version70300;
        else if (value >= m_Version70160)
          ProtocolChanges = ProtocolChanges.Version70160;
        else if (value >= m_Version70130)
          ProtocolChanges = ProtocolChanges.Version70130;
        else if (value >= m_Version7090)
          ProtocolChanges = ProtocolChanges.Version7090;
        else if (value >= m_Version7000)
          ProtocolChanges = ProtocolChanges.Version7000;
        else if (value >= m_Version60142)
          ProtocolChanges = ProtocolChanges.Version60142;
        else if (value >= m_Version6017)
          ProtocolChanges = ProtocolChanges.Version6017;
        else if (value >= m_Version6000)
          ProtocolChanges = ProtocolChanges.Version6000;
        else if (value >= m_Version502b)
          ProtocolChanges = ProtocolChanges.Version502b;
        else if (value >= m_Version500a)
          ProtocolChanges = ProtocolChanges.Version500a;
        else if (value >= m_Version407a)
          ProtocolChanges = ProtocolChanges.Version407a;
        else if (value >= m_Version400a)
          ProtocolChanges = ProtocolChanges.Version400a;
      }
    }

    private static readonly ClientVersion m_Version400a = new ClientVersion("4.0.0a");
    private static readonly ClientVersion m_Version407a = new ClientVersion("4.0.7a");
    private static readonly ClientVersion m_Version500a = new ClientVersion("5.0.0a");
    private static readonly ClientVersion m_Version502b = new ClientVersion("5.0.2b");
    private static readonly ClientVersion m_Version6000 = new ClientVersion("6.0.0.0");
    private static readonly ClientVersion m_Version6017 = new ClientVersion("6.0.1.7");
    private static readonly ClientVersion m_Version60142 = new ClientVersion("6.0.14.2");
    private static readonly ClientVersion m_Version7000 = new ClientVersion("7.0.0.0");
    private static readonly ClientVersion m_Version7090 = new ClientVersion("7.0.9.0");
    private static readonly ClientVersion m_Version70130 = new ClientVersion("7.0.13.0");
    private static readonly ClientVersion m_Version70160 = new ClientVersion("7.0.16.0");
    private static readonly ClientVersion m_Version70300 = new ClientVersion("7.0.30.0");
    private static readonly ClientVersion m_Version70331 = new ClientVersion("7.0.33.1");
    private static readonly ClientVersion m_Version704565 = new ClientVersion("7.0.45.65");
    private static readonly ClientVersion m_Version70500 = new ClientVersion("7.0.50.0");
    private static readonly ClientVersion m_Version70610 = new ClientVersion("7.0.61.0");

    public bool NewSpellbook => (ProtocolChanges & ProtocolChanges.NewSpellbook) != 0;
    public bool DamagePacket => (ProtocolChanges & ProtocolChanges.DamagePacket) != 0;
    public bool Unpack => (ProtocolChanges & ProtocolChanges.Unpack) != 0;
    public bool BuffIcon => (ProtocolChanges & ProtocolChanges.BuffIcon) != 0;
    public bool NewHaven => (ProtocolChanges & ProtocolChanges.NewHaven) != 0;
    public bool ContainerGridLines => (ProtocolChanges & ProtocolChanges.ContainerGridLines) != 0;
    public bool ExtendedSupportedFeatures => (ProtocolChanges & ProtocolChanges.ExtendedSupportedFeatures) != 0;
    public bool StygianAbyss => (ProtocolChanges & ProtocolChanges.StygianAbyss) != 0;
    public bool HighSeas => (ProtocolChanges & ProtocolChanges.HighSeas) != 0;
    public bool NewCharacterList => (ProtocolChanges & ProtocolChanges.NewCharacterList) != 0;
    public bool NewCharacterCreation => (ProtocolChanges & ProtocolChanges.NewCharacterCreation) != 0;
    public bool ExtendedStatus => (ProtocolChanges & ProtocolChanges.ExtendedStatus) != 0;
    public bool NewMobileIncoming => (ProtocolChanges & ProtocolChanges.NewMobileIncoming) != 0;
    public bool NewSecureTrading => (ProtocolChanges & ProtocolChanges.NewSecureTrading) != 0;

    public bool IsUOTDClient =>
      (Flags & ClientFlags.UOTD) != 0 || m_Version?.Type == ClientType.UOTD;

    public bool IsSAClient => m_Version?.Type == ClientType.SA;

    public List<SecureTrade> Trades { get; }

    public void ValidateAllTrades()
    {
      for (int i = Trades.Count - 1; i >= 0; --i)
      {
        if (i >= Trades.Count) continue;

        SecureTrade trade = Trades[i];

        if (trade.From.Mobile.Deleted || trade.To.Mobile.Deleted || !trade.From.Mobile.Alive ||
            !trade.To.Mobile.Alive || !trade.From.Mobile.InRange(trade.To.Mobile, 2) ||
            trade.From.Mobile.Map != trade.To.Mobile.Map) trade.Cancel();
      }
    }

    public void CancelAllTrades()
    {
      for (int i = Trades.Count - 1; i >= 0; --i)
        if (i < Trades.Count)
          Trades[i].Cancel();
    }

    public void RemoveTrade(SecureTrade trade)
    {
      Trades.Remove(trade);
    }

    public SecureTrade FindTrade(Mobile m)
    {
      for (int i = 0; i < Trades.Count; ++i)
      {
        SecureTrade trade = Trades[i];

        if (trade.From.Mobile == m || trade.To.Mobile == m) return trade;
      }

      return null;
    }

    public SecureTradeContainer FindTradeContainer(Mobile m)
    {
      for (int i = 0; i < Trades.Count; ++i)
      {
        SecureTrade trade = Trades[i];

        SecureTradeInfo from = trade.From;
        SecureTradeInfo to = trade.To;

        if (from.Mobile == Mobile && to.Mobile == m) return from.Container;

        if (from.Mobile == m && to.Mobile == Mobile) return to.Container;
      }

      return null;
    }

    public SecureTradeContainer AddTrade(NetState state)
    {
      SecureTrade newTrade = new SecureTrade(Mobile, state.Mobile);

      Trades.Add(newTrade);
      state.Trades.Add(newTrade);

      return newTrade.From.Container;
    }

    public bool Running { get; private set; }

    public bool Seeded { get; set; }

    public Socket Socket { get; private set; }

    public byte Sequence { get; set; }

    public List<Gump> Gumps { get; private set; }

    public List<HuePicker> HuePickers { get; private set; }

    public List<IMenu> Menus { get; private set; }

    public static int GumpCap { get; set; } = 512;

    public static int HuePickerCap { get; set; } = 512;

    public static int MenuCap { get; set; } = 512;

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
        Menus.Add(menu);
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
        HuePickers.Add(huePicker);
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
        Gumps.Add(gump);
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
      NetworkPackets.SendMessageLocalized(this, Serial.MinusOne, -1, MessageType.Label, 0x35, 3, 501231);
      NetworkPackets.SendLaunchBrowser(this, url);
    }

    public CityInfo[] CityInfo { get; set; }

    public Mobile Mobile { get; set; }

    public ServerInfo[] ServerInfo { get; set; }

    public IAccount Account { get; set; }

    public override string ToString() => m_ToString;

    public static List<NetState> Instances { get; } = new List<NetState>();

    public NetState(Socket socket, MessagePump pump)
    {
      Socket = socket;
      Seeded = false;
      Running = false;
      Gumps = new List<Gump>();
      HuePickers = new List<HuePicker>();
      Menus = new List<IMenu>();
      Trades = new List<SecureTrade>();

      m_NextCheckActivity = Core.TickCount + 30000;

      Instances.Add(this);

      try
      {
        Address = Utility.Intern(((IPEndPoint)Socket.RemoteEndPoint).Address);
        m_ToString = Address.ToString();
      }
      catch (Exception ex)
      {
        TraceException(ex);
        Address = IPAddress.None;
        m_ToString = "(error)";
      }

      ConnectedOn = DateTime.UtcNow;
      _ = Start(pump);

      CreatedCallback?.Invoke(this);
      Console.WriteLine("Client: {0}: Connected. [{1} Online]", this, NetState.Instances.Count);
    }

    public static void Pause()
    {
      m_AsyncState = Interlocked.Exchange(ref m_AsyncState, m_PauseState);
    }

    public static void Resume()
    {
      m_AsyncState = Interlocked.Exchange(ref m_AsyncState, m_ResumeState);
    }

    public bool CheckEncrypted(int packetID)
    {
      if (!SentFirstPacket && packetID != 0xF0 && packetID != 0xF1 && packetID != 0xCF && packetID != 0x80 &&
          packetID != 0x91 && packetID != 0xA4 && packetID != 0xEF)
      {
        Console.WriteLine("Client: {0}: Encrypted client detected, disconnecting", this);
        Dispose();
        return true;
      }

      return false;
    }

    public Pipe RecvdPipe { get; private set; }
    public Pipe SendPipe { get; private set; }

    private async Task Start(MessagePump pump)
    {
      RecvdPipe = new Pipe();
      SendPipe = new Pipe();
      Running = true;

      await Task.WhenAll(HandlePackets(pump), ProcessSends(), ProcessRecvs());
    }

    private async Task ProcessRecvs()
    {
      PipeWriter w = RecvdPipe.Writer;

      while (!Core.Closing)
      {
        if (m_AsyncState.Paused)
        {
          Interlocked.Exchange(ref m_NextCheckActivity, Core.TickCount + 90000);
          await Timer.Pause(50);
          continue;
        }

        try
        {
          Memory<byte> memory = w.GetMemory();
          int bytesRead = await Socket.ReceiveAsync(memory, SocketFlags.None);
          if (bytesRead == 0)
            break;

          Interlocked.Exchange(ref m_NextCheckActivity, Core.TickCount + 90000);

          w.Advance(bytesRead);

          FlushResult result = await w.FlushAsync();
          if (result.IsCompleted || result.IsCanceled)
            break;
        }
        catch
        {
          break;
        }
      }

      w.Complete();
      Dispose();
    }

    private async Task HandlePackets(MessagePump pump)
    {
      while (true)
        try
        {
          Packet p = await m_SendQueue?.DequeueAsync();
          if (p == null)
            break;

        long pos = PacketHandlers.ProcessPacket(pump, this, seq);

        if (pos <= 0)
          break;

        pr.AdvanceTo(seq.GetPosition(pos, seq.Start));

        if (result.IsCompleted || result.IsCanceled)
          break;
      }

      pr.Complete();
    }

    public async Task Flush(int bytesWritten)
    {
      if (IsDisposing || BlockAllPackets)
        return;

      SendPipe.Writer.Advance(bytesWritten);
      FlushResult result = await SendPipe.Writer.FlushAsync();

      if (result.IsCompleted || result.IsCanceled)
        Dispose();
    }

          prof?.Finish(length);
        }
        catch (SocketException ex)
        {
          Console.WriteLine(ex);
          TraceException(ex);
          Dispose();
          break;
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex);
        }
    }

    private async Task ProcessSends()
    {
      PipeReader pr = SendPipe.Reader;

      while (!Core.Closing)
      {
        if (m_AsyncState.Paused)
        {
          await Timer.Pause(50);
          continue;
        }

        ReadResult result = await pr.ReadAsync();
        ReadOnlySequence<byte> seq = result.Buffer;
        if (seq.Length == 0)
          break;

        if (!SequenceMarshal.TryGetReadOnlyMemory(seq, out ReadOnlyMemory<byte> memory))
          break;

        try
        {
          int bytesWritten = await Socket.SendAsync(memory, SocketFlags.None);

          pr.AdvanceTo(seq.GetPosition(bytesWritten));
        }
        catch (SocketException ex)
        {
          Console.WriteLine(ex);
          TraceException(ex);
          break;
        }

        if (result.IsCompleted || result.IsCanceled)
          break;
      }

      Dispose();
    }

    public PacketHandler GetHandler(int packetID) =>
      ContainerGridLines ? PacketHandlers.Get6017Handler(packetID) :
        PacketHandlers.GetHandler(packetID);

    private long m_NextCheckActivity;

    public void CheckAlive(long curTicks)
    {
      if (Socket == null || m_NextCheckActivity - curTicks >= 0)
        return;

      Console.WriteLine("Client: {0}: Disconnecting due to inactivity...", this);
      Dispose();
    }

    public static void TraceException(Exception ex)
    {
      if (!Core.Debug)
        return;

      try
      {
        using StreamWriter op = new StreamWriter("network-errors.log", true);
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

    private int m_Disposing;

    public bool IsDisposing { get => m_Disposing != 0;
      private set => m_Disposing = value ? 1 : 0;
    }

    public virtual void Dispose()
    {
      int disposing = Interlocked.Exchange(ref m_Disposing, 1);
      if (disposing == 1)
        return;

      Task.Run(async () =>
      {
        RecvdPipe.Reader.CancelPendingRead();
        RecvdPipe.Reader.Complete();
        await SendPipe.Writer.FlushAsync();
        SendPipe.Writer.Complete();

        try { Socket.Shutdown(SocketShutdown.Both); } catch (Exception ex) { TraceException(ex); }
        try { Socket.Close(); } catch (Exception ex) { TraceException(ex); }

        Socket = null;
        RecvdPipe = null;
        SendPipe = null;
        m_Disposed.Enqueue(this);
      });
    }

    public static void Initialize()
    {
      Timer.DelayCall(TimeSpan.FromMinutes(1.0), TimeSpan.FromMinutes(1.5), CheckAllAlive);
    }

    public static void CheckAllAlive()
    {
      try
      {
        long curTicks = Core.TickCount;

        if (Instances.Count >= 1024)
          Parallel.ForEach(Instances, ns => ns.CheckAlive(curTicks));
        else
          for (int i = 0; i < Instances.Count; ++i)
            Instances[i].CheckAlive(curTicks);
      }
      catch (Exception ex)
      {
        TraceException(ex);
      }
    }

    private static ConcurrentQueue<NetState> m_Disposed = new ConcurrentQueue<NetState>();

    public static void ProcessDisposedQueue()
    {
      int breakout = 0;

      while (breakout++ < 200)
      {
        if (!m_Disposed.TryDequeue(out NetState ns))
          break;

        Mobile m = ns.Mobile;
        IAccount a = ns.Account;

        if (m != null)
        {
          m.NetState = null;
          ns.Mobile = null;
        }

        ns.Gumps.Clear();
        ns.Menus.Clear();
        ns.HuePickers.Clear();
        ns.Account = null;
        ns.ServerInfo = null;
        ns.CityInfo = null;

        Instances.Remove(ns);

        if (a != null)
          ns.WriteConsole("Disconnected. [{0} Online] [{1}]", Instances.Count, a);
        else
          ns.WriteConsole("Disconnected. [{0} Online]", Instances.Count);
      }
    }

    public ExpansionInfo ExpansionInfo
    {
      get
      {
        for (int i = ExpansionInfo.Table.Length - 1; i >= 0; i--)
        {
          ExpansionInfo info = ExpansionInfo.Table[i];

          if (info.RequiredClient != null && Version >= info.RequiredClient || (Flags & info.ClientFlags) != 0)
            return info;
        }

        return ExpansionInfo.GetInfo(Expansion.None);
      }
    }

    public Expansion Expansion => (Expansion)ExpansionInfo.ID;

    public ProtocolChanges ProtocolChanges { get; set; }

    public bool SupportsExpansion(ExpansionInfo info, bool checkCoreExpansion = true)
    {
      if (info == null || checkCoreExpansion && (int)Core.Expansion < info.ID)
        return false;

      return info.RequiredClient != null ? Version >= info.RequiredClient : (Flags & info.ClientFlags) != 0;
    }

    public bool SupportsExpansion(Expansion ex, bool checkCoreExpansion = true) => SupportsExpansion(ExpansionInfo.GetInfo(ex), checkCoreExpansion);

    public int CompareTo(NetState other) => other == null ? 1 : m_ToString.CompareTo(other.m_ToString);
  }
}
