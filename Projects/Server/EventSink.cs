/***************************************************************************
 *                                EventSink.cs
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
using System.Net;
using System.Net.Sockets;
using Server.Accounting;
using Server.Guilds;
using Server.Network;

namespace Server
{
  public delegate void CharacterCreatedEventHandler(CharacterCreatedEventArgs e);

  public delegate void OpenDoorMacroEventHandler(OpenDoorMacroEventArgs e);

  public delegate void SpeechEventHandler(SpeechEventArgs e);

  public delegate void LoginEventHandler(LoginEventArgs e);

  public delegate void ServerListEventHandler(ServerListEventArgs e);

  public delegate void MovementEventHandler(MovementEventArgs e);

  public delegate void HungerChangedEventHandler(HungerChangedEventArgs e);

  public delegate void CrashedEventHandler(CrashedEventArgs e);

  public delegate void ShutdownEventHandler(ShutdownEventArgs e);

  public delegate void HelpRequestEventHandler(HelpRequestEventArgs e);

  public delegate void DisarmRequestEventHandler(DisarmRequestEventArgs e);

  public delegate void StunRequestEventHandler(StunRequestEventArgs e);

  public delegate void OpenSpellbookRequestEventHandler(OpenSpellbookRequestEventArgs e);

  public delegate void CastSpellRequestEventHandler(CastSpellRequestEventArgs e);

  public delegate void BandageTargetRequestEventHandler(BandageTargetRequestEventArgs e);

  public delegate void AnimateRequestEventHandler(AnimateRequestEventArgs e);

  public delegate void LogoutEventHandler(LogoutEventArgs e);

  public delegate void SocketConnectEventHandler(SocketConnectEventArgs e);

  public delegate void ConnectedEventHandler(ConnectedEventArgs e);

  public delegate void DisconnectedEventHandler(DisconnectedEventArgs e);

  public delegate void RenameRequestEventHandler(RenameRequestEventArgs e);

  public delegate void PlayerDeathEventHandler(PlayerDeathEventArgs e);

  public delegate void VirtueGumpRequestEventHandler(VirtueGumpRequestEventArgs e);

  public delegate void VirtueItemRequestEventHandler(VirtueItemRequestEventArgs e);

  public delegate void VirtueMacroRequestEventHandler(VirtueMacroRequestEventArgs e);

  public delegate void ChatRequestEventHandler(ChatRequestEventArgs e);

  public delegate void AccountLoginEventHandler(AccountLoginEventArgs e);

  public delegate void PaperdollRequestEventHandler(PaperdollRequestEventArgs e);

  public delegate void ProfileRequestEventHandler(ProfileRequestEventArgs e);

  public delegate void ChangeProfileRequestEventHandler(ChangeProfileRequestEventArgs e);

  public delegate void AggressiveActionEventHandler(AggressiveActionEventArgs e);

  public delegate void GameLoginEventHandler(GameLoginEventArgs e);

  public delegate void DeleteRequestEventHandler(DeleteRequestEventArgs e);

  public delegate void WorldLoadEventHandler();

  public delegate void WorldSaveEventHandler(WorldSaveEventArgs e);

  public delegate void SetAbilityEventHandler(SetAbilityEventArgs e);

  public delegate void FastWalkEventHandler(FastWalkEventArgs e);

  public delegate void ServerStartedEventHandler();

  public delegate void CreateGuildHandler(CreateGuildEventArgs e);

  public delegate void GuildGumpRequestHandler(GuildGumpRequestArgs e);

  public delegate void QuestGumpRequestHandler(QuestGumpRequestArgs e);

  public delegate void ClientVersionReceivedHandler(ClientVersionReceivedArgs e);

  public class ClientVersionReceivedArgs : EventArgs
  {
    public ClientVersionReceivedArgs(NetState state, ClientVersion cv)
    {
      State = state;
      Version = cv;
    }

    public NetState State{ get; }

    public ClientVersion Version{ get; }
  }

  public class CreateGuildEventArgs : EventArgs
  {
    public CreateGuildEventArgs(uint id)
    {
      Id = id;
    }

    public uint Id{ get; set; }

    public BaseGuild Guild{ get; set; }
  }

  public class GuildGumpRequestArgs : EventArgs
  {
    public GuildGumpRequestArgs(Mobile mobile)
    {
      Mobile = mobile;
    }

    public Mobile Mobile{ get; }
  }

  public class QuestGumpRequestArgs : EventArgs
  {
    public QuestGumpRequestArgs(Mobile mobile)
    {
      Mobile = mobile;
    }

    public Mobile Mobile{ get; }
  }

  public class SetAbilityEventArgs : EventArgs
  {
    public SetAbilityEventArgs(Mobile mobile, int index)
    {
      Mobile = mobile;
      Index = index;
    }

    public Mobile Mobile{ get; }

    public int Index{ get; }
  }

  public class DeleteRequestEventArgs : EventArgs
  {
    public DeleteRequestEventArgs(NetState state, int index)
    {
      State = state;
      Index = index;
    }

    public NetState State{ get; }

    public int Index{ get; }
  }

  public class GameLoginEventArgs : EventArgs
  {
    public GameLoginEventArgs(NetState state, string un, string pw)
    {
      State = state;
      Username = un;
      Password = pw;
    }

    public NetState State{ get; }

    public string Username{ get; }

    public string Password{ get; }

    public bool Accepted{ get; set; }

    public CityInfo[] CityInfo{ get; set; }
  }

  public class AggressiveActionEventArgs : EventArgs
  {
    private static Queue<AggressiveActionEventArgs> m_Pool = new Queue<AggressiveActionEventArgs>();

    private AggressiveActionEventArgs(Mobile aggressed, Mobile aggressor, bool criminal)
    {
      Aggressed = aggressed;
      Aggressor = aggressor;
      Criminal = criminal;
    }

    public Mobile Aggressed{ get; private set; }

    public Mobile Aggressor{ get; private set; }

    public bool Criminal{ get; private set; }

    public static AggressiveActionEventArgs Create(Mobile aggressed, Mobile aggressor, bool criminal)
    {
      AggressiveActionEventArgs args;

      if (m_Pool.Count > 0)
      {
        args = m_Pool.Dequeue();

        args.Aggressed = aggressed;
        args.Aggressor = aggressor;
        args.Criminal = criminal;
      }
      else
      {
        args = new AggressiveActionEventArgs(aggressed, aggressor, criminal);
      }

      return args;
    }

    public void Free()
    {
      m_Pool.Enqueue(this);
    }
  }

  public class ProfileRequestEventArgs : EventArgs
  {
    public ProfileRequestEventArgs(Mobile beholder, Mobile beheld)
    {
      Beholder = beholder;
      Beheld = beheld;
    }

    public Mobile Beholder{ get; }

    public Mobile Beheld{ get; }
  }

  public class ChangeProfileRequestEventArgs : EventArgs
  {
    public ChangeProfileRequestEventArgs(Mobile beholder, Mobile beheld, string text)
    {
      Beholder = beholder;
      Beheld = beheld;
      Text = text;
    }

    public Mobile Beholder{ get; }

    public Mobile Beheld{ get; }

    public string Text{ get; }
  }

  public class PaperdollRequestEventArgs : EventArgs
  {
    public PaperdollRequestEventArgs(Mobile beholder, Mobile beheld)
    {
      Beholder = beholder;
      Beheld = beheld;
    }

    public Mobile Beholder{ get; }

    public Mobile Beheld{ get; }
  }

  public class AccountLoginEventArgs : EventArgs
  {
    public AccountLoginEventArgs(NetState state, string username, string password)
    {
      State = state;
      Username = username;
      Password = password;
    }

    public NetState State{ get; }

    public string Username{ get; }

    public string Password{ get; }

    public bool Accepted{ get; set; }

    public ALRReason RejectReason{ get; set; }
  }

  public class VirtueItemRequestEventArgs : EventArgs
  {
    public VirtueItemRequestEventArgs(Mobile beholder, Mobile beheld, int gumpID)
    {
      Beholder = beholder;
      Beheld = beheld;
      GumpID = gumpID;
    }

    public Mobile Beholder{ get; }

    public Mobile Beheld{ get; }

    public int GumpID{ get; }
  }

  public class VirtueGumpRequestEventArgs : EventArgs
  {
    public VirtueGumpRequestEventArgs(Mobile beholder, Mobile beheld)
    {
      Beholder = beholder;
      Beheld = beheld;
    }

    public Mobile Beholder{ get; }

    public Mobile Beheld{ get; }
  }

  public class VirtueMacroRequestEventArgs : EventArgs
  {
    public VirtueMacroRequestEventArgs(Mobile mobile, int virtueID)
    {
      Mobile = mobile;
      VirtueID = virtueID;
    }

    public Mobile Mobile{ get; }

    public int VirtueID{ get; }
  }

  public class ChatRequestEventArgs : EventArgs
  {
    public ChatRequestEventArgs(Mobile mobile)
    {
      Mobile = mobile;
    }

    public Mobile Mobile{ get; }
  }

  public class PlayerDeathEventArgs : EventArgs
  {
    public PlayerDeathEventArgs(Mobile mobile)
    {
      Mobile = mobile;
    }

    public Mobile Mobile{ get; }
  }

  public class RenameRequestEventArgs : EventArgs
  {
    public RenameRequestEventArgs(Mobile from, Mobile target, string name)
    {
      From = from;
      Target = target;
      Name = name;
    }

    public Mobile From{ get; }

    public Mobile Target{ get; }

    public string Name{ get; }
  }

  public class LogoutEventArgs : EventArgs
  {
    public LogoutEventArgs(Mobile m)
    {
      Mobile = m;
    }

    public Mobile Mobile{ get; }
  }

  public class SocketConnectEventArgs : EventArgs
  {
    public SocketConnectEventArgs(Socket s)
    {
      Socket = s;
      AllowConnection = true;
    }

    public Socket Socket{ get; }

    public bool AllowConnection{ get; set; }
  }

  public class ConnectedEventArgs : EventArgs
  {
    public ConnectedEventArgs(Mobile m)
    {
      Mobile = m;
    }

    public Mobile Mobile{ get; }
  }

  public class DisconnectedEventArgs : EventArgs
  {
    public DisconnectedEventArgs(Mobile m)
    {
      Mobile = m;
    }

    public Mobile Mobile{ get; }
  }

  public class AnimateRequestEventArgs : EventArgs
  {
    public AnimateRequestEventArgs(Mobile m, string action)
    {
      Mobile = m;
      Action = action;
    }

    public Mobile Mobile{ get; }

    public string Action{ get; }
  }

  public class CastSpellRequestEventArgs : EventArgs
  {
    public CastSpellRequestEventArgs(Mobile m, int spellID, Item book)
    {
      Mobile = m;
      Spellbook = book;
      SpellID = spellID;
    }

    public Mobile Mobile{ get; }

    public Item Spellbook{ get; }

    public int SpellID{ get; }
  }

  public class BandageTargetRequestEventArgs : EventArgs
  {
    public BandageTargetRequestEventArgs(Mobile m, Item bandage, Mobile target)
    {
      Mobile = m;
      Bandage = bandage;
      Target = target;
    }

    public Mobile Mobile{ get; }

    public Item Bandage{ get; }

    public Mobile Target{ get; }
  }

  public class OpenSpellbookRequestEventArgs : EventArgs
  {
    public OpenSpellbookRequestEventArgs(Mobile m, int type)
    {
      Mobile = m;
      Type = type;
    }

    public Mobile Mobile{ get; }

    public int Type{ get; }
  }

  public class StunRequestEventArgs : EventArgs
  {
    public StunRequestEventArgs(Mobile m)
    {
      Mobile = m;
    }

    public Mobile Mobile{ get; }
  }

  public class DisarmRequestEventArgs : EventArgs
  {
    public DisarmRequestEventArgs(Mobile m)
    {
      Mobile = m;
    }

    public Mobile Mobile{ get; }
  }

  public class HelpRequestEventArgs : EventArgs
  {
    public HelpRequestEventArgs(Mobile m)
    {
      Mobile = m;
    }

    public Mobile Mobile{ get; }
  }

  public class ShutdownEventArgs : EventArgs
  {
  }

  public class CrashedEventArgs : EventArgs
  {
    public CrashedEventArgs(Exception e)
    {
      Exception = e;
    }

    public Exception Exception{ get; }

    public bool Close{ get; set; }
  }

  public class HungerChangedEventArgs : EventArgs
  {
    public HungerChangedEventArgs(Mobile mobile, int oldValue)
    {
      Mobile = mobile;
      OldValue = oldValue;
    }

    public Mobile Mobile{ get; }

    public int OldValue{ get; }
  }

  public class MovementEventArgs : EventArgs
  {
    private static Queue<MovementEventArgs> m_Pool = new Queue<MovementEventArgs>();

    public MovementEventArgs(Mobile mobile, Direction dir)
    {
      Mobile = mobile;
      Direction = dir;
    }

    public Mobile Mobile{ get; private set; }

    public Direction Direction{ get; private set; }

    public bool Blocked{ get; set; }

    public static MovementEventArgs Create(Mobile mobile, Direction dir)
    {
      MovementEventArgs args;

      if (m_Pool.Count > 0)
      {
        args = m_Pool.Dequeue();

        args.Mobile = mobile;
        args.Direction = dir;
        args.Blocked = false;
      }
      else
      {
        args = new MovementEventArgs(mobile, dir);
      }

      return args;
    }

    public void Free()
    {
      m_Pool.Enqueue(this);
    }
  }

  public class ServerListEventArgs : EventArgs
  {
    public ServerListEventArgs(NetState state, IAccount account)
    {
      State = state;
      Account = account;
      Servers = new List<ServerInfo>();
    }

    public NetState State{ get; }

    public IAccount Account{ get; }

    public bool Rejected{ get; set; }

    public List<ServerInfo> Servers{ get; }

    public void AddServer(string name, IPEndPoint address)
    {
      AddServer(name, 0, TimeZoneInfo.Local, address);
    }

    public void AddServer(string name, int fullPercent, TimeZoneInfo tz, IPEndPoint address)
    {
      Servers.Add(new ServerInfo(name, fullPercent, tz, address));
    }
  }

  public struct SkillNameValue
  {
    public SkillName Name{ get; }

    public int Value{ get; }

    public SkillNameValue(SkillName name, int value)
    {
      Name = name;
      Value = value;
    }
  }

  public class CharacterCreatedEventArgs : EventArgs
  {
    public CharacterCreatedEventArgs(NetState state, IAccount a, string name, bool female, int hue, int str, int dex,
      int intel, CityInfo city, SkillNameValue[] skills, int shirtHue, int pantsHue, int hairID, int hairHue,
      int beardID, int beardHue, int profession, Race race)
    {
      State = state;
      Account = a;
      Name = name;
      Female = female;
      Hue = hue;
      Str = str;
      Dex = dex;
      Int = intel;
      City = city;
      Skills = skills;
      ShirtHue = shirtHue;
      PantsHue = pantsHue;
      HairID = hairID;
      HairHue = hairHue;
      BeardID = beardID;
      BeardHue = beardHue;
      Profession = profession;
      Race = race;
    }

    public NetState State{ get; }

    public IAccount Account{ get; }

    public Mobile Mobile{ get; set; }

    public string Name{ get; }

    public bool Female{ get; }

    public int Hue{ get; }

    public int Str{ get; }

    public int Dex{ get; }

    public int Int{ get; }

    public CityInfo City{ get; }

    public SkillNameValue[] Skills{ get; }

    public int ShirtHue{ get; }

    public int PantsHue{ get; }

    public int HairID{ get; }

    public int HairHue{ get; }

    public int BeardID{ get; }

    public int BeardHue{ get; }

    public int Profession{ get; set; }

    public Race Race{ get; }
  }

  public class OpenDoorMacroEventArgs : EventArgs
  {
    public OpenDoorMacroEventArgs(Mobile mobile)
    {
      Mobile = mobile;
    }

    public Mobile Mobile{ get; }
  }

  public class SpeechEventArgs : EventArgs
  {
    public SpeechEventArgs(Mobile mobile, string speech, MessageType type, int hue, int[] keywords)
    {
      Mobile = mobile;
      Speech = speech;
      Type = type;
      Hue = hue;
      Keywords = keywords;
    }

    public Mobile Mobile{ get; }

    public string Speech{ get; set; }

    public MessageType Type{ get; }

    public int Hue{ get; }

    public int[] Keywords{ get; }

    public bool Handled{ get; set; }

    public bool Blocked{ get; set; }

    public bool HasKeyword(int keyword)
    {
      for (int i = 0; i < Keywords.Length; ++i)
        if (Keywords[i] == keyword)
          return true;

      return false;
    }
  }

  public class LoginEventArgs : EventArgs
  {
    public LoginEventArgs(Mobile mobile)
    {
      Mobile = mobile;
    }

    public Mobile Mobile{ get; }
  }

  public class WorldSaveEventArgs : EventArgs
  {
    public WorldSaveEventArgs(bool msg)
    {
      Message = msg;
    }

    public bool Message{ get; }
  }

  public class FastWalkEventArgs : EventArgs
  {
    public FastWalkEventArgs(NetState state)
    {
      NetState = state;
      Blocked = false;
    }

    public NetState NetState{ get; }

    public bool Blocked{ get; set; }
  }

  public static class EventSink
  {
    public static event CharacterCreatedEventHandler CharacterCreated;
    public static event OpenDoorMacroEventHandler OpenDoorMacroUsed;
    public static event SpeechEventHandler Speech;
    public static event LoginEventHandler Login;
    public static event ServerListEventHandler ServerList;
    public static event MovementEventHandler Movement;
    public static event HungerChangedEventHandler HungerChanged;
    public static event CrashedEventHandler Crashed;
    public static event ShutdownEventHandler Shutdown;
    public static event HelpRequestEventHandler HelpRequest;
    public static event DisarmRequestEventHandler DisarmRequest;
    public static event StunRequestEventHandler StunRequest;
    public static event OpenSpellbookRequestEventHandler OpenSpellbookRequest;
    public static event CastSpellRequestEventHandler CastSpellRequest;
    public static event BandageTargetRequestEventHandler BandageTargetRequest;
    public static event AnimateRequestEventHandler AnimateRequest;
    public static event LogoutEventHandler Logout;
    public static event SocketConnectEventHandler SocketConnect;
    public static event ConnectedEventHandler Connected;
    public static event DisconnectedEventHandler Disconnected;
    public static event RenameRequestEventHandler RenameRequest;
    public static event PlayerDeathEventHandler PlayerDeath;
    public static event VirtueGumpRequestEventHandler VirtueGumpRequest;
    public static event VirtueItemRequestEventHandler VirtueItemRequest;
    public static event VirtueMacroRequestEventHandler VirtueMacroRequest;
    public static event ChatRequestEventHandler ChatRequest;
    public static event AccountLoginEventHandler AccountLogin;
    public static event PaperdollRequestEventHandler PaperdollRequest;
    public static event ProfileRequestEventHandler ProfileRequest;
    public static event ChangeProfileRequestEventHandler ChangeProfileRequest;
    public static event AggressiveActionEventHandler AggressiveAction;
    public static event CommandEventHandler Command;
    public static event GameLoginEventHandler GameLogin;
    public static event DeleteRequestEventHandler DeleteRequest;
    public static event WorldLoadEventHandler WorldLoad;
    public static event WorldSaveEventHandler WorldSave;
    public static event SetAbilityEventHandler SetAbility;
    public static event FastWalkEventHandler FastWalk;
    public static event CreateGuildHandler CreateGuild;
    public static event ServerStartedEventHandler ServerStarted;
    public static event GuildGumpRequestHandler GuildGumpRequest;
    public static event QuestGumpRequestHandler QuestGumpRequest;
    public static event ClientVersionReceivedHandler ClientVersionReceived;

    /* The following is a .NET 2.0 "Generic EventHandler" implementation.
     * It is a breaking change; we would have to refactor all event handlers.
     * This style does not appear to be in widespread use.
     * We could also look into .NET 4.0 Action<T>/Func<T> implementations.
     */

    /*
    public static event EventHandler<CharacterCreatedEventArgs> CharacterCreated;
    public static event EventHandler<OpenDoorMacroEventArgs> OpenDoorMacroUsed;
    public static event EventHandler<SpeechEventArgs> Speech;
    public static event EventHandler<LoginEventArgs> Login;
    public static event EventHandler<ServerListEventArgs> ServerList;
    public static event EventHandler<MovementEventArgs> Movement;
    public static event EventHandler<HungerChangedEventArgs> HungerChanged;
    public static event EventHandler<CrashedEventArgs> Crashed;
    public static event EventHandler<ShutdownEventArgs> Shutdown;
    public static event EventHandler<HelpRequestEventArgs> HelpRequest;
    public static event EventHandler<DisarmRequestEventArgs> DisarmRequest;
    public static event EventHandler<StunRequestEventArgs> StunRequest;
    public static event EventHandler<OpenSpellbookRequestEventArgs> OpenSpellbookRequest;
    public static event EventHandler<CastSpellRequestEventArgs> CastSpellRequest;
    public static event EventHandler<BandageTargetRequestEventArgs> BandageTargetRequest;
    public static event EventHandler<AnimateRequestEventArgs> AnimateRequest;
    public static event EventHandler<LogoutEventArgs> Logout;
    public static event EventHandler<SocketConnectEventArgs> SocketConnect;
    public static event EventHandler<ConnectedEventArgs> Connected;
    public static event EventHandler<DisconnectedEventArgs> Disconnected;
    public static event EventHandler<RenameRequestEventArgs> RenameRequest;
    public static event EventHandler<PlayerDeathEventArgs> PlayerDeath;
    public static event EventHandler<VirtueGumpRequestEventArgs> VirtueGumpRequest;
    public static event EventHandler<VirtueItemRequestEventArgs> VirtueItemRequest;
    public static event EventHandler<VirtueMacroRequestEventArgs> VirtueMacroRequest;
    public static event EventHandler<ChatRequestEventArgs> ChatRequest;
    public static event EventHandler<AccountLoginEventArgs> AccountLogin;
    public static event EventHandler<PaperdollRequestEventArgs> PaperdollRequest;
    public static event EventHandler<ProfileRequestEventArgs> ProfileRequest;
    public static event EventHandler<ChangeProfileRequestEventArgs> ChangeProfileRequest;
    public static event EventHandler<AggressiveActionEventArgs> AggressiveAction;
    public static event EventHandler<CommandEventArgs> Command;
    public static event EventHandler<GameLoginEventArgs> GameLogin;
    public static event EventHandler<DeleteRequestEventArgs> DeleteRequest;
    public static event EventHandler<EventArgs> WorldLoad;
    public static event EventHandler<WorldSaveEventArgs> WorldSave;
    public static event EventHandler<SetAbilityEventArgs> SetAbility;
    public static event EventHandler<FastWalkEventArgs> FastWalk;
    public static event EventHandler<CreateGuildEventArgs> CreateGuild;
    public static event EventHandler<EventArgs> ServerStarted;
    public static event EventHandler<GuildGumpRequestArgs> GuildGumpRequest;
    public static event EventHandler<QuestGumpRequestArgs> QuestGumpRequest;
    public static event EventHandler<ClientVersionReceivedArgs> ClientVersionReceived;
    */

    public static void InvokeClientVersionReceived(ClientVersionReceivedArgs e)
    {
      ClientVersionReceived?.Invoke(e);
    }

    public static void InvokeServerStarted()
    {
      ServerStarted?.Invoke();
    }

    public static void InvokeCreateGuild(CreateGuildEventArgs e)
    {
      CreateGuild?.Invoke(e);
    }

    public static void InvokeSetAbility(SetAbilityEventArgs e)
    {
      SetAbility?.Invoke(e);
    }

    public static void InvokeGuildGumpRequest(GuildGumpRequestArgs e)
    {
      GuildGumpRequest?.Invoke(e);
    }

    public static void InvokeQuestGumpRequest(QuestGumpRequestArgs e)
    {
      QuestGumpRequest?.Invoke(e);
    }

    public static void InvokeFastWalk(FastWalkEventArgs e)
    {
      FastWalk?.Invoke(e);
    }

    public static void InvokeDeleteRequest(DeleteRequestEventArgs e)
    {
      DeleteRequest?.Invoke(e);
    }

    public static void InvokeGameLogin(GameLoginEventArgs e)
    {
      GameLogin?.Invoke(e);
    }

    public static void InvokeCommand(CommandEventArgs e)
    {
      Command?.Invoke(e);
    }

    public static void InvokeAggressiveAction(AggressiveActionEventArgs e)
    {
      AggressiveAction?.Invoke(e);
    }

    public static void InvokeProfileRequest(ProfileRequestEventArgs e)
    {
      ProfileRequest?.Invoke(e);
    }

    public static void InvokeChangeProfileRequest(ChangeProfileRequestEventArgs e)
    {
      ChangeProfileRequest?.Invoke(e);
    }

    public static void InvokePaperdollRequest(PaperdollRequestEventArgs e)
    {
      PaperdollRequest?.Invoke(e);
    }

    public static void InvokeAccountLogin(AccountLoginEventArgs e)
    {
      AccountLogin?.Invoke(e);
    }

    public static void InvokeChatRequest(ChatRequestEventArgs e)
    {
      ChatRequest?.Invoke(e);
    }

    public static void InvokeVirtueItemRequest(VirtueItemRequestEventArgs e)
    {
      VirtueItemRequest?.Invoke(e);
    }

    public static void InvokeVirtueGumpRequest(VirtueGumpRequestEventArgs e)
    {
      VirtueGumpRequest?.Invoke(e);
    }

    public static void InvokeVirtueMacroRequest(VirtueMacroRequestEventArgs e)
    {
      VirtueMacroRequest?.Invoke(e);
    }

    public static void InvokePlayerDeath(PlayerDeathEventArgs e)
    {
      PlayerDeath?.Invoke(e);
    }

    public static void InvokeRenameRequest(RenameRequestEventArgs e)
    {
      RenameRequest?.Invoke(e);
    }

    public static void InvokeLogout(LogoutEventArgs e)
    {
      Logout?.Invoke(e);
    }

    public static void InvokeSocketConnect(SocketConnectEventArgs e)
    {
      SocketConnect?.Invoke(e);
    }

    public static void InvokeConnected(ConnectedEventArgs e)
    {
      Connected?.Invoke(e);
    }

    public static void InvokeDisconnected(DisconnectedEventArgs e)
    {
      Disconnected?.Invoke(e);
    }

    public static void InvokeAnimateRequest(AnimateRequestEventArgs e)
    {
      AnimateRequest?.Invoke(e);
    }

    public static void InvokeCastSpellRequest(CastSpellRequestEventArgs e)
    {
      CastSpellRequest?.Invoke(e);
    }

    public static void InvokeBandageTargetRequest(BandageTargetRequestEventArgs e)
    {
      BandageTargetRequest?.Invoke(e);
    }

    public static void InvokeOpenSpellbookRequest(OpenSpellbookRequestEventArgs e)
    {
      OpenSpellbookRequest?.Invoke(e);
    }

    public static void InvokeDisarmRequest(DisarmRequestEventArgs e)
    {
      DisarmRequest?.Invoke(e);
    }

    public static void InvokeStunRequest(StunRequestEventArgs e)
    {
      StunRequest?.Invoke(e);
    }

    public static void InvokeHelpRequest(HelpRequestEventArgs e)
    {
      HelpRequest?.Invoke(e);
    }

    public static void InvokeShutdown(ShutdownEventArgs e)
    {
      Shutdown?.Invoke(e);
    }

    public static void InvokeCrashed(CrashedEventArgs e)
    {
      Crashed?.Invoke(e);
    }

    public static void InvokeHungerChanged(HungerChangedEventArgs e)
    {
      HungerChanged?.Invoke(e);
    }

    public static void InvokeMovement(MovementEventArgs e)
    {
      Movement?.Invoke(e);
    }

    public static void InvokeServerList(ServerListEventArgs e)
    {
      ServerList?.Invoke(e);
    }

    public static void InvokeLogin(LoginEventArgs e)
    {
      Login?.Invoke(e);
    }

    public static void InvokeSpeech(SpeechEventArgs e)
    {
      Speech?.Invoke(e);
    }

    public static void InvokeCharacterCreated(CharacterCreatedEventArgs e)
    {
      CharacterCreated?.Invoke(e);
    }

    public static void InvokeOpenDoorMacroUsed(OpenDoorMacroEventArgs e)
    {
      OpenDoorMacroUsed?.Invoke(e);
    }

    public static void InvokeWorldLoad()
    {
      WorldLoad?.Invoke();
    }

    public static void InvokeWorldSave(WorldSaveEventArgs e)
    {
      WorldSave?.Invoke(e);
    }

    public static void Reset()
    {
      CharacterCreated = null;
      OpenDoorMacroUsed = null;
      Speech = null;
      Login = null;
      ServerList = null;
      Movement = null;
      HungerChanged = null;
      Crashed = null;
      Shutdown = null;
      HelpRequest = null;
      DisarmRequest = null;
      StunRequest = null;
      OpenSpellbookRequest = null;
      CastSpellRequest = null;
      BandageTargetRequest = null;
      AnimateRequest = null;
      Logout = null;
      SocketConnect = null;
      Connected = null;
      Disconnected = null;
      RenameRequest = null;
      PlayerDeath = null;
      VirtueGumpRequest = null;
      VirtueItemRequest = null;
      VirtueMacroRequest = null;
      ChatRequest = null;
      AccountLogin = null;
      PaperdollRequest = null;
      ProfileRequest = null;
      ChangeProfileRequest = null;
      AggressiveAction = null;
      Command = null;
      GameLogin = null;
      DeleteRequest = null;
      WorldLoad = null;
      WorldSave = null;
      SetAbility = null;
      GuildGumpRequest = null;
      QuestGumpRequest = null;
    }
  }
}
