using System;
using System.Collections.Generic;
using Server;
using Server.Network;
using Server.Gumps;

namespace Server.Misc
{
	public static partial class Assistants
	{
		private static class Settings
		{
			public const bool Enabled = false;
			public const bool KickOnFailure = true; // It will also kick clients running without assistants

			public static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(30.0);
			public static readonly TimeSpan DisconnectDelay = TimeSpan.FromSeconds(15.0);

			public const string WarningMessage = "The server was unable to negotiate features with your assistant. "
								+ "You must download and run an updated version of <A HREF=\"http://uosteam.com\">UOSteam</A>"
								+ " or <A HREF=\"https://bitbucket.org/msturgill/razor-releases/downloads\">Razor</A>."
								+ "<BR><BR>Make sure you've checked the option <B>Negotiate features with server</B>, "
								+ "once you have this box checked you may log in and play normally."
								+ "<BR><BR>You will be disconnected shortly.";

			public static void Configure()
			{
				//DisallowFeature( Features.FilterWeather );
			}

			[Flags]
			public enum Features : ulong
			{
				None = 0,

				FilterWeather = 1 << 0,  // Weather Filter
				FilterLight = 1 << 1,  // Light Filter
				SmartTarget = 1 << 2,  // Smart Last Target
				RangedTarget = 1 << 3,  // Range Check Last Target
				AutoOpenDoors = 1 << 4,  // Automatically Open Doors
				DequipOnCast = 1 << 5,  // Unequip Weapon on spell cast
				AutoPotionEquip = 1 << 6,  // Un/re-equip weapon on potion use
				PoisonedChecks = 1 << 7,  // Block heal If poisoned/Macro If Poisoned condition/Heal or Cure self
				LoopedMacros = 1 << 8,  // Disallow looping or recursive macros
				UseOnceAgent = 1 << 9,  // The use once agent
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

			private static Features m_DisallowedFeatures = Features.None;

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
					m_DisallowedFeatures |= feature;
				else
					m_DisallowedFeatures &= ~feature;
			}

			public static Features DisallowedFeatures { get { return m_DisallowedFeatures; } }
		}

		private static class Negotiator
		{
			private static Dictionary<Mobile, Timer> m_Dictionary = new Dictionary<Mobile, Timer>();

			private static TimerStateCallback OnHandshakeTimeout_Callback = new TimerStateCallback(OnHandshakeTimeout);
			private static TimerStateCallback OnForceDisconnect_Callback = new TimerStateCallback(OnForceDisconnect);

			public static void Initialize()
			{
				if (Settings.Enabled)
				{
					EventSink.Login += new LoginEventHandler(EventSink_Login);
					ProtocolExtensions.Register(0xFF, true, new OnPacketReceive(OnHandshakeResponse));
				}
			}

			private static void EventSink_Login(LoginEventArgs e)
			{
				Mobile m = e.Mobile;

				if (m != null && m.NetState != null && m.NetState.Running)
				{
					Timer t;
					m.Send(new BeginHandshake());

					if (Settings.KickOnFailure)
						m.Send(new BeginHandshake());

					if (m_Dictionary.TryGetValue(m, out t) && t != null)
						t.Stop();

					m_Dictionary[m] = t = Timer.DelayCall(Settings.HandshakeTimeout, OnHandshakeTimeout_Callback, m);
					t.Start();
				}
			}

			private static void OnHandshakeResponse(NetState state, PacketReader pvSrc)
			{
				pvSrc.Trace(state);

				if (state == null || state.Mobile == null || !state.Running)
					return;

				Timer t;
				Mobile m = state.Mobile;

				if (m_Dictionary.TryGetValue(m, out t))
				{
					if (t != null)
						t.Stop();

					m_Dictionary.Remove(m);
				}
			}

			private static void OnHandshakeTimeout(object state)
			{
				Timer t = null;
				Mobile m = state as Mobile;

				if (m == null)
					return;

				m_Dictionary.Remove(m);

				if (!Settings.KickOnFailure)
				{
					Console.WriteLine("Player '{0}' failed to negotiate features.", m);
				}
				else if (m.NetState != null && m.NetState.Running)
				{
					m.SendGump(new Gumps.WarningGump(1060635, 30720, Settings.WarningMessage, 0xFFC000, 420, 250, null, null));

					if (m.AccessLevel <= AccessLevel.Player)
					{
						m_Dictionary[m] = t = Timer.DelayCall(Settings.DisconnectDelay, OnForceDisconnect_Callback, m);
						t.Start();
					}
				}
			}

			private static void OnForceDisconnect(object state)
			{
				if (state is Mobile)
				{
					Mobile m = (Mobile)state;

					if (m.NetState != null && m.NetState.Running)
						m.NetState.Dispose();

					m_Dictionary.Remove(m);

					Console.WriteLine("Player {0} kicked (Failed assistant handshake)", m);
				}
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