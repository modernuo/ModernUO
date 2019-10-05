using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Server.Accounting;
using Server.Items;
using Server.Misc;
using Server.Network;

namespace Server.RemoteAdmin
{
  public class AdminNetwork
  {
    private const string ProtocolVersion = "2";

    private const string DateFormat = "MMMM dd hh:mm:ss.f tt";

    private static List<NetState> m_Auth = new List<NetState>();
    private static bool m_NewLine = true;
    private static StringBuilder m_ConsoleData = new StringBuilder();

    public static void Configure()
    {
      PacketHandlers.Register(0xF1, 0, false, OnReceive);
      Core.MultiConsoleOut.Add(new EventTextWriter(OnConsoleChar, OnConsoleLine, OnConsoleString));
      Timer.DelayCall(TimeSpan.FromMinutes(2.5), TimeSpan.FromMinutes(2.5), CleanUp);
    }

    public static void OnConsoleString(string str)
    {
      string outStr;
      if (m_NewLine)
      {
        outStr = $"[{DateTime.UtcNow.ToString(DateFormat)}]: {str}";
        m_NewLine = false;
      }
      else
      {
        outStr = str;
      }

      m_ConsoleData.Append(outStr);
      RoughTrimConsoleData();

      SendToAll(outStr);
    }

    public static void OnConsoleChar(char ch)
    {
      if (m_NewLine)
      {
        string outStr;
        outStr = $"[{DateTime.UtcNow.ToString(DateFormat)}]: {ch}";

        m_ConsoleData.Append(outStr);
        SendToAll(outStr);

        m_NewLine = false;
      }
      else
      {
        m_ConsoleData.Append(ch);
        SendToAll(ch);
      }

      RoughTrimConsoleData();
    }

    public static void OnConsoleLine(string line)
    {
      string outStr;
      if (m_NewLine)
        outStr = $"[{DateTime.UtcNow.ToString(DateFormat)}]: {line}{Console.Out.NewLine}";
      else
        outStr = $"{line}{Console.Out.NewLine}";

      m_ConsoleData.Append(outStr);
      RoughTrimConsoleData();

      SendToAll(outStr);

      m_NewLine = true;
    }

    private static void SendToAll(string outStr)
    {
      SendToAll(new ConsoleData(outStr));
    }

    private static void SendToAll(char ch)
    {
      SendToAll(new ConsoleData(ch));
    }

    private static void SendToAll(ConsoleData packet)
    {
      packet.Acquire();
      for (int i = 0; i < m_Auth.Count; i++)
        m_Auth[i].Send(packet);
      packet.Release();
    }

    private static void RoughTrimConsoleData()
    {
      if (m_ConsoleData.Length >= 4096)
        m_ConsoleData.Remove(0, 2048);
    }

    private static void TightTrimConsoleData()
    {
      if (m_ConsoleData.Length > 1024)
        m_ConsoleData.Remove(0, m_ConsoleData.Length - 1024);
    }

    public static void OnReceive(NetState state, PacketReader pvSrc)
    {
      byte cmd = pvSrc.ReadByte();
      if (cmd == 0x02)
      {
        Authenticate(state, pvSrc);
      }
      else if (cmd == 0xFE)
      {
        state.Send(new CompactServerInfo());
        state.Dispose();
      }
      else if (cmd == 0xFF)
      {
        string statStr =
          $", Name={ServerList.ServerName}, Age={(int)(DateTime.UtcNow - Clock.ServerStart).TotalHours}, Clients={NetState.Instances.Count}, Items={World.Items.Count}, Chars={World.Mobiles.Count}, Mem={(int)(GC.GetTotalMemory(false) / 1024)}K, Ver={ProtocolVersion}";
        state.Send(new UOGInfo(statStr));
        state.Dispose();
      }
      else if (!IsAuth(state))
      {
        Console.WriteLine("ADMIN: Unauthorized packet from {0}, disconnecting", state);
        Disconnect(state);
      }
      else
      {
        if (!RemoteAdminHandlers.Handle(cmd, state, pvSrc))
          Disconnect(state);
      }
    }

    private static void DelayedDisconnect(NetState ns)
    {
      Timer.DelayCall(TimeSpan.FromSeconds(15.0), () => Disconnect(ns));
    }

    private static void Disconnect(NetState ns)
    {
      m_Auth.Remove(ns);
      ns.Dispose();
    }

    public static void Authenticate(NetState state, PacketReader pvSrc)
    {
      string user = pvSrc.ReadString(30);
      string pw = pvSrc.ReadString(30);

      if (!(Accounts.GetAccount(user) is Account a))
      {
        state.Send(new Login(LoginResponse.NoUser));
        Console.WriteLine("ADMIN: Invalid username '{0}' from {1}", user, state);
        DelayedDisconnect(state);
      }
      else if (!a.HasAccess(state))
      {
        state.Send(new Login(LoginResponse.BadIP));
        Console.WriteLine("ADMIN: Access to '{0}' from {1} denied.", user, state);
        DelayedDisconnect(state);
      }
      else if (!a.CheckPassword(pw))
      {
        state.Send(new Login(LoginResponse.BadPass));
        Console.WriteLine("ADMIN: Invalid password for user '{0}' from {1}", user, state);
        DelayedDisconnect(state);
      }
      else if (a.AccessLevel < AccessLevel.Administrator || a.Banned)
      {
        Console.WriteLine("ADMIN: Account '{0}' does not have admin access. Connection Denied.", user);
        state.Send(new Login(LoginResponse.NoAccess));
        DelayedDisconnect(state);
      }
      else
      {
        Console.WriteLine("ADMIN: Access granted to '{0}' from {1}", user, state);
        state.Account = a;
        a.LogAccess(state);
        a.LastLogin = DateTime.UtcNow;

        state.Send(new Login(LoginResponse.OK));
        TightTrimConsoleData();
        state.Send(Compress(new ConsoleData(m_ConsoleData.ToString())));
        m_Auth.Add(state);
      }
    }

    public static bool IsAuth(NetState state) => m_Auth.Contains(state);

    private static void CleanUp()
    {
      //remove dead instances from m_Auth
      List<NetState> list = new List<NetState>();
      for (int i = 0; i < m_Auth.Count; i++)
      {
        NetState ns = m_Auth[i];
        if (ns.Running)
          list.Add(ns);
      }

      m_Auth = list;
    }

    public static Packet Compress(Packet p)
    {
      byte[] source = p.Compile(false, out int length);

      if (length > 100 && length < 60000)
      {
        byte[] dest = new byte[(int)(length * 1.001) + 10];
        int destSize = dest.Length;

        ZLibError error = Compression.Pack(dest, ref destSize, source, length, ZLibQuality.Default);

        if (error != ZLibError.Okay)
        {
          Console.WriteLine("WARNING: Unable to compress admin packet, zlib error: {0}", error);
          return p;
        }

        return new AdminCompressedPacket(dest, destSize, length);
      }

      return p;
    }
  }

  public class EventTextWriter : TextWriter
  {
    public delegate void OnConsoleChar(char ch);

    public delegate void OnConsoleLine(string line);

    public delegate void OnConsoleStr(string str);

    private OnConsoleChar m_OnChar;
    private OnConsoleLine m_OnLine;
    private OnConsoleStr m_OnStr;

    public EventTextWriter(OnConsoleChar onChar, OnConsoleLine onLine, OnConsoleStr onStr)
    {
      m_OnChar = onChar;
      m_OnLine = onLine;
      m_OnStr = onStr;
    }

    public override Encoding Encoding => Encoding.ASCII;

    public override void Write(char ch)
    {
      m_OnChar?.Invoke(ch);
    }

    public override void Write(string str)
    {
      m_OnStr?.Invoke(str);
    }

    public override void WriteLine(string line)
    {
      m_OnLine?.Invoke(line);
    }
  }
}
