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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Server.Accounting;
using Server.Diagnostics;
using Server.Gumps;
using Server.HuePickers;
using Server.Items;
using Server.Menus;

namespace Server.Network
{
  public interface IPacketEncoder
  {
    void EncodeOutgoingPacket(NetState to, ref byte[] buffer, ref int length);
    void DecodeIncomingPacket(NetState from, ref byte[] buffer, ref int length);
  }

  public delegate void NetStateCreatedCallback(NetState ns);

  public class NetState : IComparable<NetState>
  {
    private byte[] m_RecvBuffer;
    private SendQueue m_SendQueue;

#if NewAsyncSockets
    private SocketAsyncEventArgs m_ReceiveEventArgs, m_SendEventArgs;
#else
		private AsyncCallback m_OnReceive, m_OnSend;
#endif

    private MessagePump m_MessagePump;
    private string m_ToString;
    private ClientVersion m_Version;

    public DateTime ConnectedOn{ get; }

    public TimeSpan ConnectedFor => DateTime.UtcNow - ConnectedOn;

    internal int m_Seed;
    internal int m_AuthID;

    public IPAddress Address{ get; }

    private static bool m_Paused;

    [Flags]
    private enum AsyncState
    {
      Pending = 0x01,
      Paused = 0x02
    }

    private AsyncState m_AsyncState;
    private object m_AsyncLock = new object();

    public IPacketEncoder PacketEncoder{ get; set; }

    public static NetStateCreatedCallback CreatedCallback{ get; set; }

    public bool SentFirstPacket{ get; set; }

    public bool BlockAllPackets{ get; set; }

    public ClientFlags Flags{ get; set; }

    public ClientVersion Version
    {
      get => m_Version;
      set
      {
        m_Version = value;

        if (value >= m_Version704565)
          _ProtocolChanges = ProtocolChanges.Version704565;
        else if (value >= m_Version70331)
          _ProtocolChanges = ProtocolChanges.Version70331;
        else if (value >= m_Version70300)
          _ProtocolChanges = ProtocolChanges.Version70300;
        else if (value >= m_Version70160)
          _ProtocolChanges = ProtocolChanges.Version70160;
        else if (value >= m_Version70130)
          _ProtocolChanges = ProtocolChanges.Version70130;
        else if (value >= m_Version7090)
          _ProtocolChanges = ProtocolChanges.Version7090;
        else if (value >= m_Version7000)
          _ProtocolChanges = ProtocolChanges.Version7000;
        else if (value >= m_Version60142)
          _ProtocolChanges = ProtocolChanges.Version60142;
        else if (value >= m_Version6017)
          _ProtocolChanges = ProtocolChanges.Version6017;
        else if (value >= m_Version6000)
          _ProtocolChanges = ProtocolChanges.Version6000;
        else if (value >= m_Version502b)
          _ProtocolChanges = ProtocolChanges.Version502b;
        else if (value >= m_Version500a)
          _ProtocolChanges = ProtocolChanges.Version500a;
        else if (value >= m_Version407a)
          _ProtocolChanges = ProtocolChanges.Version407a;
        else if (value >= m_Version400a) _ProtocolChanges = ProtocolChanges.Version400a;
      }
    }

    private static ClientVersion m_Version400a = new ClientVersion("4.0.0a");
    private static ClientVersion m_Version407a = new ClientVersion("4.0.7a");
    private static ClientVersion m_Version500a = new ClientVersion("5.0.0a");
    private static ClientVersion m_Version502b = new ClientVersion("5.0.2b");
    private static ClientVersion m_Version6000 = new ClientVersion("6.0.0.0");
    private static ClientVersion m_Version6017 = new ClientVersion("6.0.1.7");
    private static ClientVersion m_Version60142 = new ClientVersion("6.0.14.2");
    private static ClientVersion m_Version7000 = new ClientVersion("7.0.0.0");
    private static ClientVersion m_Version7090 = new ClientVersion("7.0.9.0");
    private static ClientVersion m_Version70130 = new ClientVersion("7.0.13.0");
    private static ClientVersion m_Version70160 = new ClientVersion("7.0.16.0");
    private static ClientVersion m_Version70300 = new ClientVersion("7.0.30.0");
    private static ClientVersion m_Version70331 = new ClientVersion("7.0.33.1");
    private static ClientVersion m_Version704565 = new ClientVersion("7.0.45.65");

    private ProtocolChanges _ProtocolChanges;

    private enum ProtocolChanges
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
      Version704565 = Version70331 | NewSecureTrading
    }

    public bool NewSpellbook => (_ProtocolChanges & ProtocolChanges.NewSpellbook) != 0;
    public bool DamagePacket => (_ProtocolChanges & ProtocolChanges.DamagePacket) != 0;
    public bool Unpack => (_ProtocolChanges & ProtocolChanges.Unpack) != 0;
    public bool BuffIcon => (_ProtocolChanges & ProtocolChanges.BuffIcon) != 0;
    public bool NewHaven => (_ProtocolChanges & ProtocolChanges.NewHaven) != 0;
    public bool ContainerGridLines => (_ProtocolChanges & ProtocolChanges.ContainerGridLines) != 0;
    public bool ExtendedSupportedFeatures => (_ProtocolChanges & ProtocolChanges.ExtendedSupportedFeatures) != 0;
    public bool StygianAbyss => (_ProtocolChanges & ProtocolChanges.StygianAbyss) != 0;
    public bool HighSeas => (_ProtocolChanges & ProtocolChanges.HighSeas) != 0;
    public bool NewCharacterList => (_ProtocolChanges & ProtocolChanges.NewCharacterList) != 0;
    public bool NewCharacterCreation => (_ProtocolChanges & ProtocolChanges.NewCharacterCreation) != 0;
    public bool ExtendedStatus => (_ProtocolChanges & ProtocolChanges.ExtendedStatus) != 0;
    public bool NewMobileIncoming => (_ProtocolChanges & ProtocolChanges.NewMobileIncoming) != 0;
    public bool NewSecureTrading => (_ProtocolChanges & ProtocolChanges.NewSecureTrading) != 0;

    public bool IsUOTDClient =>
      (Flags & ClientFlags.UOTD) != 0 || m_Version != null && m_Version.Type == ClientType.UOTD;

    public bool IsSAClient => m_Version != null && m_Version.Type == ClientType.SA;

    public List<SecureTrade> Trades{ get; }

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

    public bool CompressionEnabled{ get; set; }

    public int Sequence{ get; set; }

    public List<Gump> Gumps{ get; private set; }

    public List<HuePicker> HuePickers{ get; private set; }

    public List<IMenu> Menus{ get; private set; }

    public static int GumpCap{ get; set; } = 512;

    public static int HuePickerCap{ get; set; } = 512;

    public static int MenuCap{ get; set; } = 512;

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
      if (Menus == null) Menus = new List<IMenu>();

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
      if (HuePickers == null) HuePickers = new List<HuePicker>();

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
      if (Gumps == null) Gumps = new List<Gump>();

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

    public CityInfo[] CityInfo{ get; set; }

    public Mobile Mobile{ get; set; }

    public ServerInfo[] ServerInfo{ get; set; }

    public IAccount Account{ get; set; }

    public override string ToString()
    {
      return m_ToString;
    }

    public static List<NetState> Instances{ get; } = new List<NetState>();

    private static BufferPool m_ReceiveBufferPool = new BufferPool("Receive", 2048, 2048);

    public NetState(Socket socket, MessagePump messagePump)
    {
      Socket = socket;
      Buffer = new ByteQueue();
      Seeded = false;
      Running = false;
      m_RecvBuffer = m_ReceiveBufferPool.AcquireBuffer();
      m_MessagePump = messagePump;
      Gumps = new List<Gump>();
      HuePickers = new List<HuePicker>();
      Menus = new List<IMenu>();
      Trades = new List<SecureTrade>();

      m_SendQueue = new SendQueue();

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

      CreatedCallback?.Invoke(this);
    }

    private bool _sending;
    private object _sendL = new object();

    public virtual void Send(Packet p)
    {
      if (Socket == null || BlockAllPackets)
      {
        p.OnSend();
        return;
      }

      int length;
      byte[] buffer = p.Compile(CompressionEnabled, out length);

      if (buffer != null)
      {
        if (buffer.Length <= 0 || length <= 0)
        {
          p.OnSend();
          return;
        }

        PacketSendProfile prof = null;

        if (Core.Profiling) prof = PacketSendProfile.Acquire(p.GetType());

        prof?.Start();

        PacketEncoder?.EncodeOutgoingPacket(this, ref buffer, ref length);

        try
        {
          SendQueue.Gram gram;

          lock (_sendL)
          {
            lock (m_SendQueue)
            {
              gram = m_SendQueue.Enqueue(buffer, length);
            }

            if (gram != null && !_sending)
            {
              _sending = true;
#if NewAsyncSockets
              m_SendEventArgs.SetBuffer(gram.Buffer, 0, gram.Length);
              Send_Start();
#else
							try {
									m_Socket.BeginSend(gram.Buffer, 0, gram.Length, SocketFlags.None, m_OnSend, m_Socket);
							}
							catch (Exception ex) {
								TraceException(ex);
								Dispose(false);
							}
#endif
            }
          }
        }
        catch (CapacityExceededException)
        {
          Console.WriteLine("Client: {0}: Too much data pending, disconnecting...", this);
          Dispose(false);
        }

        p.OnSend();

        prof?.Finish(length);
      }
      else
      {
        Console.WriteLine("Client: {0}: null buffer send, disconnecting...", this);
        using (StreamWriter op = new StreamWriter("null_send.log", true))
        {
          op.WriteLine("{0} Client: {1}: null buffer send, disconnecting...", DateTime.UtcNow, this);
          op.WriteLine(new StackTrace());
        }

        Dispose();
      }
    }

#if NewAsyncSockets
    public void Start()
    {
      m_ReceiveEventArgs = new SocketAsyncEventArgs();
      m_ReceiveEventArgs.Completed += Receive_Completion;
      m_ReceiveEventArgs.SetBuffer(m_RecvBuffer, 0, m_RecvBuffer.Length);

      m_SendEventArgs = new SocketAsyncEventArgs();
      m_SendEventArgs.Completed += Send_Completion;

      Running = true;

      if (Socket == null || m_Paused) return;

      Receive_Start();
    }

    private void Receive_Start()
    {
      try
      {
        bool result = false;

        do
        {
          lock (m_AsyncLock)
          {
            if ((m_AsyncState & (AsyncState.Pending | AsyncState.Paused)) == 0)
            {
              m_AsyncState |= AsyncState.Pending;
              result = !Socket.ReceiveAsync(m_ReceiveEventArgs);

              if (result)
                Receive_Process(m_ReceiveEventArgs);
            }
          }
        } while (result);
      }
      catch (Exception ex)
      {
        TraceException(ex);
        Dispose(false);
      }
    }

    private void Receive_Completion(object sender, SocketAsyncEventArgs e)
    {
      Receive_Process(e);

      if (!IsDisposing)
        Receive_Start();
    }

    private void Receive_Process(SocketAsyncEventArgs e)
    {
      int byteCount = e.BytesTransferred;

      if (e.SocketError != SocketError.Success || byteCount <= 0)
      {
        Dispose(false);
        return;
      }

      if (IsDisposing) return;

      m_NextCheckActivity = Core.TickCount + 90000;

      byte[] buffer = m_RecvBuffer;

      PacketEncoder?.DecodeIncomingPacket(this, ref buffer, ref byteCount);

      lock (Buffer)
      {
        Buffer.Enqueue(buffer, 0, byteCount);
      }

      m_MessagePump.OnReceive(this);

      lock (m_AsyncLock)
      {
        m_AsyncState &= ~AsyncState.Pending;
      }
    }

    private void Send_Start()
    {
      try
      {
        bool result = false;

        do
        {
          result = !Socket.SendAsync(m_SendEventArgs);

          if (result)
            Send_Process(m_SendEventArgs);
        } while (result);
      }
      catch (Exception ex)
      {
        TraceException(ex);
        Dispose(false);
      }
    }

    private void Send_Completion(object sender, SocketAsyncEventArgs e)
    {
      Send_Process(e);

      if (IsDisposing)
        return;

      if (CoalesceSleep >= 0) Thread.Sleep(CoalesceSleep);

      SendQueue.Gram gram;

      lock (m_SendQueue)
      {
        gram = m_SendQueue.Dequeue();

        if (gram == null && m_SendQueue.IsFlushReady)
          gram = m_SendQueue.CheckFlushReady();
      }

      if (gram != null)
      {
        m_SendEventArgs.SetBuffer(gram.Buffer, 0, gram.Length);
        Send_Start();
      }
      else
      {
        lock (_sendL)
        {
          _sending = false;
        }
      }
    }

    private void Send_Process(SocketAsyncEventArgs e)
    {
      int bytes = e.BytesTransferred;

      if (e.SocketError != SocketError.Success || bytes <= 0)
      {
        Dispose(false);
        return;
      }

      m_NextCheckActivity = Core.TickCount + 90000;
    }

    public static void Pause()
    {
      m_Paused = true;

      for (int i = 0; i < Instances.Count; ++i)
      {
        NetState ns = Instances[i];

        lock (ns.m_AsyncLock)
        {
          ns.m_AsyncState |= AsyncState.Paused;
        }
      }
    }

    public static void Resume()
    {
      m_Paused = false;

      for (int i = 0; i < Instances.Count; ++i)
      {
        NetState ns = Instances[i];

        if (ns.Socket == null) continue;

        lock (ns.m_AsyncLock)
        {
          ns.m_AsyncState &= ~AsyncState.Paused;

          if ((ns.m_AsyncState & AsyncState.Pending) == 0)
            ns.Receive_Start();
        }
      }
    }

    public bool Flush()
    {
      if (Socket == null)
        return false;

      lock (_sendL)
      {
        if (_sending)
          return false;

        SendQueue.Gram gram;

        lock (m_SendQueue)
        {
          if (!m_SendQueue.IsFlushReady)
            return false;

          gram = m_SendQueue.CheckFlushReady();
        }

        if (gram != null)
        {
          _sending = true;
          m_SendEventArgs.SetBuffer(gram.Buffer, 0, gram.Length);
          Send_Start();
        }
      }

      return false;
    }

#else
		public void Start() {
			m_OnReceive = new AsyncCallback( OnReceive );
			m_OnSend = new AsyncCallback( OnSend );

			m_Running = true;

			if ( m_Socket == null || m_Paused ) {
				return;
			}

			try {
				lock ( m_AsyncLock ) {
					if ( ( m_AsyncState & ( AsyncState.Pending | AsyncState.Paused ) ) == 0 ) {
						InternalBeginReceive();
					}
				}
			} catch ( Exception ex ) {
				TraceException( ex );
				Dispose( false );
			}
		}

		private void InternalBeginReceive() {
			m_AsyncState |= AsyncState.Pending;

			m_Socket.BeginReceive( m_RecvBuffer, 0, m_RecvBuffer.Length, SocketFlags.None, m_OnReceive, m_Socket );
		}

		private void OnReceive( IAsyncResult asyncResult ) {
			Socket s = (Socket)asyncResult.AsyncState;

			try {
				int byteCount = s.EndReceive( asyncResult );

				if ( byteCount > 0 ) {
					m_NextCheckActivity = Core.TickCount + 90000;

					byte[] buffer = m_RecvBuffer;

					if ( m_Encoder != null )
						m_Encoder.DecodeIncomingPacket( this, ref buffer, ref byteCount );

					lock ( m_Buffer )
						m_Buffer.Enqueue( buffer, 0, byteCount );

					m_MessagePump.OnReceive( this );

					lock ( m_AsyncLock ) {
						m_AsyncState &= ~AsyncState.Pending;

						if ( ( m_AsyncState & AsyncState.Paused ) == 0 ) {
							try {
								InternalBeginReceive();
							} catch ( Exception ex ) {
								TraceException( ex );
								Dispose( false );
							}
						}
					}
				} else {
					Dispose( false );
				}
			} catch {
				Dispose( false );
			}
		}

		private void OnSend( IAsyncResult asyncResult ) {
			Socket s = (Socket)asyncResult.AsyncState;

			try {
				int bytes = s.EndSend( asyncResult );

				if ( bytes <= 0 ) {
					Dispose( false );
					return;
				}

				m_NextCheckActivity = Core.TickCount + 90000;

				if (m_CoalesceSleep >= 0) {
					Thread.Sleep(m_CoalesceSleep);
				}

				SendQueue.Gram gram;

				lock (m_SendQueue) {
					gram = m_SendQueue.Dequeue();

					if (gram == null && m_SendQueue.IsFlushReady)
						gram = m_SendQueue.CheckFlushReady();
				}

				if (gram != null) {
					try {
						s.BeginSend(gram.Buffer, 0, gram.Length, SocketFlags.None, m_OnSend, s);
					} catch (Exception ex) {
						TraceException(ex);
						Dispose(false);
					}
				} else {
					lock (_sendL)
						_sending = false;
				}
			} catch ( Exception ){
				Dispose( false );
			}
		}

		public static void Pause() {
			m_Paused = true;

			for ( int i = 0; i < m_Instances.Count; ++i ) {
				NetState ns = m_Instances[i];

				lock ( ns.m_AsyncLock ) {
					ns.m_AsyncState |= AsyncState.Paused;
				}
			}
		}

		public static void Resume() {
			m_Paused = false;

			for ( int i = 0; i < m_Instances.Count; ++i ) {
				NetState ns = m_Instances[i];

				if ( ns.m_Socket == null ) {
					continue;
				}

				lock ( ns.m_AsyncLock ) {
					ns.m_AsyncState &= ~AsyncState.Paused;

					try {
						if ( ( ns.m_AsyncState & AsyncState.Pending ) == 0 )
							ns.InternalBeginReceive();
					} catch ( Exception ex ) {
						TraceException( ex );
						ns.Dispose( false );
					}
				}
			}
		}

		public bool Flush() {
			if (m_Socket == null)
				return false;

			lock (_sendL) {
				if (_sending)
					return false;

				SendQueue.Gram gram;

				lock (m_SendQueue) {
					if (!m_SendQueue.IsFlushReady)
						return false;

					gram = m_SendQueue.CheckFlushReady();
				}

				if (gram != null) {
					try {
						_sending = true;
						m_Socket.BeginSend(gram.Buffer, 0, gram.Length, SocketFlags.None, m_OnSend, m_Socket);
						return true;
					} catch (Exception ex) {
						TraceException(ex);
						Dispose(false);
					}
				}
			}

			return false;
		}
#endif

    public PacketHandler GetHandler(int packetID)
    {
      if (ContainerGridLines)
        return PacketHandlers.Get6017Handler(packetID);
      return PacketHandlers.GetHandler(packetID);
    }

    public static void FlushAll()
    {
      if (Instances.Count >= 1024)
        Parallel.ForEach(Instances, ns => ns.Flush());
      else
        for (int i = 0; i < Instances.Count; ++i)
          Instances[i].Flush();
    }

    public static int CoalesceSleep{ get; set; } = -1;

    private long m_NextCheckActivity;

    public void CheckAlive(long curTicks)
    {
      if (Socket == null)
        return;

      if (m_NextCheckActivity - curTicks >= 0) return;

      Console.WriteLine("Client: {0}: Disconnecting due to inactivity...", this);

      Dispose();
    }

    public static void TraceException(Exception ex)
    {
      if (!Core.Debug)
        return;

      try
      {
        using (StreamWriter op = new StreamWriter("network-errors.log", true))
        {
          op.WriteLine("# {0}", DateTime.UtcNow);

          op.WriteLine(ex);

          op.WriteLine();
          op.WriteLine();
        }
      }
      catch
      {
      }

      try
      {
        Console.WriteLine(ex);
      }
      catch
      {
      }
    }

    public bool IsDisposing{ get; private set; }

    public void Dispose()
    {
      Dispose(true);
    }

    public virtual void Dispose(bool flush)
    {
      if (Socket == null || IsDisposing) return;

      IsDisposing = true;

      if (flush)
        flush = Flush();

      try
      {
        Socket.Shutdown(SocketShutdown.Both);
      }
      catch (SocketException ex)
      {
        TraceException(ex);
      }

      try
      {
        Socket.Close();
      }
      catch (SocketException ex)
      {
        TraceException(ex);
      }

      if (m_RecvBuffer != null)
        lock (m_ReceiveBufferPool)
        {
          m_ReceiveBufferPool.ReleaseBuffer(m_RecvBuffer);
        }

      Socket = null;

      Buffer = null;
      m_RecvBuffer = null;

#if NewAsyncSockets
      m_ReceiveEventArgs = null;
      m_SendEventArgs = null;
#else
			m_OnReceive = null;
			m_OnSend = null;
#endif

      Running = false;

      lock (m_Disposed)
      {
        m_Disposed.Enqueue(this);
      }

      lock (m_SendQueue)
      {
        if ( /*!flush &&*/ !m_SendQueue.IsEmpty)
          m_SendQueue.Clear();
      }
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

    private static Queue<NetState> m_Disposed = new Queue<NetState>();

    public static void ProcessDisposedQueue()
    {
      lock (m_Disposed)
      {
        int breakout = 0;

        while (breakout < 200 && m_Disposed.Count > 0)
        {
          ++breakout;
          NetState ns = m_Disposed.Dequeue();

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
    }

    public bool Running{ get; private set; }

    public bool Seeded{ get; set; }

    public Socket Socket{ get; private set; }

    public ByteQueue Buffer{ get; private set; }

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

    public bool SupportsExpansion(ExpansionInfo info, bool checkCoreExpansion)
    {
      if (info == null || checkCoreExpansion && (int)Core.Expansion < info.ID)
        return false;

      if (info.RequiredClient != null)
        return Version >= info.RequiredClient;

      return (Flags & info.ClientFlags) != 0;
    }

    public bool SupportsExpansion(Expansion ex, bool checkCoreExpansion)
    {
      return SupportsExpansion(ExpansionInfo.GetInfo(ex), checkCoreExpansion);
    }

    public bool SupportsExpansion(Expansion ex)
    {
      return SupportsExpansion(ex, true);
    }

    public bool SupportsExpansion(ExpansionInfo info)
    {
      return SupportsExpansion(info, true);
    }

    public int CompareTo(NetState other)
    {
      if (other == null)
        return 1;

      return m_ToString.CompareTo(other.m_ToString);
    }
  }
}