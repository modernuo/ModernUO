using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Network;

namespace Server.Misc
{
  public static class Assistants
  {
    private static class Settings
    {
      [Flags]
      public enum Features : ulong
      {
        None = 0,

        FilterWeather = 1 << 0, // Weather Filter
        FilterLight = 1 << 1, // Light Filter
        SmartTarget = 1 << 2, // Smart Last Target
        RangedTarget = 1 << 3, // Range Check Last Target
        AutoOpenDoors = 1 << 4, // Automatically Open Doors
        DequipOnCast = 1 << 5, // Unequip Weapon on spell cast
        AutoPotionEquip = 1 << 6, // Un/re-equip weapon on potion use
        PoisonedChecks = 1 << 7, // Block heal If poisoned/Macro If Poisoned condition/Heal or Cure self
        LoopedMacros = 1 << 8, // Disallow looping or recursive macros
        UseOnceAgent = 1 << 9, // The use once agent
        RestockAgent = 1 << 10, // The restock agent
        SellAgent = 1 << 11, // The sell agent
        BuyAgent = 1 << 12, // The buy agent
        PotionHotkeys = 1 << 13, // All potion hotkeys
        RandomTargets = 1 << 14, // All random target hotkeys (not target next, last target, target self)
        ClosestTargets = 1 << 15, // All closest target hotkeys
        OverheadHealth = 1 << 16, // Health and Mana/Stam messages shown over player's heads
        AutolootAgent = 1 << 17, // The autoloot agent
        BoneCutterAgent = 1 << 18, // The bone cutter agent
        AdvancedMacros = 1 << 19, // Advanced macro engine
        AutoRemount = 1 << 20, // Auto remount after dismount
        AutoBandage = 1 << 21, // Auto bandage friends, self, last and mount option
        EnemyTargetShare = 1 << 22, // Enemy target share on guild, party or alliance chat
        FilterSeason = 1 << 23, // Season Filter
        SpellTargetShare = 1 << 24, // Spell target share on guild, party or alliance chat

        All = ulong.MaxValue
      }

      public const bool Enabled = false;
      public const bool KickOnFailure = true; // It will also kick clients running without assistants

      public const string WarningMessage = "The server was unable to negotiate features with your assistant. "
                                           + "You must download and run an updated version of <A HREF=\"http://uosteam.com\">UOSteam</A>"
                                           + " or <A HREF=\"https://bitbucket.org/msturgill/razor-releases/downloads\">Razor</A>."
                                           + "<BR><BR>Make sure you've checked the option <B>Negotiate features with server</B>, "
                                           + "once you have this box checked you may log in and play normally."
                                           + "<BR><BR>You will be disconnected shortly.";

      public static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(30.0);
      public static readonly TimeSpan DisconnectDelay = TimeSpan.FromSeconds(15.0);

      public static Features DisallowedFeatures{ get; private set; } = Features.None;

      public static void Configure()
      {
        //DisallowFeature( Features.FilterWeather );
      }

      public static void DisallowFeature(Features feature)
      {
        SetDisallowed(feature, true);
      }

      public static void AllowFeature(Features feature)
      {
        SetDisallowed(feature, false);
      }

      public static void SetDisallowed(Features feature, bool value)
      {
        if (value)
          DisallowedFeatures |= feature;
        else
          DisallowedFeatures &= ~feature;
      }
    }

    private static class Negotiator
    {
      private static Dictionary<Mobile, Timer> m_Dictionary = new Dictionary<Mobile, Timer>();

      public static void Initialize()
      {
        if (Settings.Enabled)
        {
          EventSink.Login += EventSink_Login;
          ProtocolExtensions.Register(0xFF, true, OnHandshakeResponse);
        }
      }

      private static void EventSink_Login(LoginEventArgs e)
      {
        Mobile m = e.Mobile;

        if (m?.NetState != null && m.NetState.Running)
        {
          m.Send(new BeginHandshake());

          if (Settings.KickOnFailure)
            m.Send(new BeginHandshake());

          if (m_Dictionary.TryGetValue(m, out Timer t))
            t?.Stop();

          m_Dictionary[m] = t = Timer.DelayCall(Settings.HandshakeTimeout, OnHandshakeTimeout, m);
          t.Start();
        }
      }

      private static void OnHandshakeResponse(NetState state, PacketReader pvSrc)
      {
        pvSrc.Trace(state);

        if (state?.Mobile == null || !state.Running)
          return;

        Mobile m = state.Mobile;
        if (m_Dictionary.TryGetValue(m, out Timer t))
        {
          t?.Stop();

          m_Dictionary.Remove(m);
        }
      }

      private static void OnHandshakeTimeout(Mobile m)
      {
        if (m == null)
          return;

        m_Dictionary.Remove(m);

//				if (!Settings.KickOnFailure)
//				{
//					Console.WriteLine("Player '{0}' failed to negotiate features.", m);
//				}

        if (m.NetState?.Running == true)
        {
          m.SendGump(new WarningGump(1060635, 30720, Settings.WarningMessage, 0xFFC000, 420, 250));

          if (m.AccessLevel <= AccessLevel.Player)
          {
            Timer t;
            m_Dictionary[m] = t = Timer.DelayCall(Settings.DisconnectDelay, OnForceDisconnect, m);
            t.Start();
          }
        }
      }

      private static void OnForceDisconnect(Mobile m)
      {
        if (m == null)
          return;
        
        if (m.NetState != null && m.NetState.Running)
          m.NetState.Dispose();

        m_Dictionary.Remove(m);

        Console.WriteLine("Player {0} kicked (Failed assistant handshake)", m);
      }

      private sealed class BeginHandshake : ProtocolExtension
      {
        public BeginHandshake()
          : base(0xFE, 8)
        {
          m_Stream.Write((uint)((ulong)Settings.DisallowedFeatures >> 32));
          m_Stream.Write((uint)((ulong)Settings.DisallowedFeatures & 0xFFFFFFFF));
        }
      }
    }
  }
}