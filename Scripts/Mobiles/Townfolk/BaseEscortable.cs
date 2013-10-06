using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Network;
using Server.ContextMenus;
using EDI = Server.Mobiles.EscortDestinationInfo;
using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Definitions;
using Server.Engines.MLQuests.Objectives;

namespace Server.Mobiles
{
	public class BaseEscortable : BaseCreature
	{
		public static readonly TimeSpan EscortDelay = TimeSpan.FromMinutes( 5.0 );
		public static readonly TimeSpan AbandonDelay = MLQuestSystem.Enabled ? TimeSpan.FromMinutes( 1.0 ) : TimeSpan.FromMinutes( 2.0 );
		public static readonly TimeSpan DeleteTime = MLQuestSystem.Enabled ? TimeSpan.FromSeconds( 100 ) : TimeSpan.FromSeconds( 30 );

		public override bool StaticMLQuester { get { return false; } } // Suppress automatic quest registration on creation/deserialization

		private MLQuest m_MLQuest;

		protected override List<MLQuest> ConstructQuestList()
		{
			if ( m_MLQuest == null )
			{
				Region reg = Region;
				Type[] list = reg.IsPartOf( "Haven Island" ) ? m_MLQuestTypesNH : m_MLQuestTypes;

				int randomIdx = Utility.Random( list.Length );

				for ( int i = 0; i < list.Length; ++i )
				{
					Type questType = list[randomIdx];

					MLQuest quest = MLQuestSystem.FindQuest( questType );

					if ( quest != null )
					{
						bool okay = true;

						foreach ( BaseObjective obj in quest.Objectives )
						{
							if ( obj is EscortObjective && ( (EscortObjective)obj ).Destination.Contains( reg ) )
							{
								okay = false; // We're already there!
								break;
							}
						}

						if ( okay )
						{
							m_MLQuest = quest;
							break;
						}
					}
					else if ( MLQuestSystem.Debug )
					{
						Console.WriteLine( "Warning: Escortable cannot be assigned quest type '{0}', it is not registered", questType.Name );
					}

					randomIdx = ( randomIdx + 1 ) % list.Length;
				}

				if ( m_MLQuest == null )
				{
					if ( MLQuestSystem.Debug )
						Console.WriteLine( "Warning: No suitable quest found for escort {0}", Serial );

					return null;
				}
			}

			List<MLQuest> result = new List<MLQuest>();
			result.Add( m_MLQuest );

			return result;
		}

		public override bool CanShout { get { return ( !Controlled && !IsBeingDeleted ); } }

		public override void Shout( PlayerMobile pm )
		{
			/*
			 * 1072301 - You there!  Care to hear how to earn some easy gold?
			 * 1072302 - Adventurer!  I have an offer for you.
			 * 1072303 - Wait!  I have an opportunity for you to make some gold!
			 */
			MLQuestSystem.Tell( this, pm, Utility.Random( 1072301, 3 ) );
		}

		private EDI m_Destination;
		private string m_DestinationString;

		private DateTime m_DeleteTime;
		private Timer m_DeleteTimer;

		private bool m_DeleteCorpse = false;

		public bool IsBeingDeleted
		{
			get { return ( m_DeleteTimer != null ); }
		}

		public override bool Commandable { get { return false; } } // Our master cannot boss us around!
		public override bool DeleteCorpseOnDeath { get { return m_DeleteCorpse; } }

		[CommandProperty(AccessLevel.GameMaster)]
		public string Destination
		{
			get { return m_Destination == null ? null : m_Destination.Name; }
			set { m_DestinationString = value; m_Destination = EDI.Find(value); }
		}

		// Classic list
		// Used when: !MLQuestSystem.Enabled && !Core.ML
		private static string[] m_TownNames = new string[]
		{
			"Cove", "Britain", "Jhelom",
			"Minoc", "Ocllo", "Trinsic",
			"Vesper", "Yew", "Skara Brae",
			"Nujel'm", "Moonglow", "Magincia"
		};

		// ML list, pre-ML quest system
		// Used when: !MLQuestSystem.Enabled && Core.ML
		private static string[] m_MLTownNames = new string[]
		{
			"Cove", "Serpent's Hold", "Jhelom",
			"Nujel'm"
		};

		// ML quest system general list
		// Used when: MLQuestSystem.Enabled && !Region.IsPartOf( "Haven Island" )
		private static Type[] m_MLQuestTypes =
		{
			typeof( EscortToYew ),
			typeof( EscortToVesper ),
			typeof( EscortToTrinsic ),
			typeof( EscortToSkaraBrae ),
			typeof( EscortToSerpentsHold ),
			typeof( EscortToNujelm ),
			typeof( EscortToMoonglow ),
			typeof( EscortToMinoc ),
			typeof( EscortToMagincia ),
			typeof( EscortToJhelom ),
			typeof( EscortToCove ),
			typeof( EscortToBritain )
			// Ocllo was removed in pub 56
			//typeof( EscortToOcllo )
		};

		// ML quest system New Haven list
		// Used when: MLQuestSystem.Enabled && Region.IsPartOf( "Haven Island" )
		private static Type[] m_MLQuestTypesNH =
		{
			typeof( EscortToNHAlchemist ),
			typeof( EscortToNHBard ),
			typeof( EscortToNHWarrior ),
			typeof( EscortToNHTailor ),
			typeof( EscortToNHCarpenter ),
			typeof( EscortToNHMapmaker ),
			typeof( EscortToNHMage ),
			typeof( EscortToNHInn ),
			// Farm destination was removed
			//typeof( EscortToNHFarm ),
			typeof( EscortToNHDocks ),
			typeof( EscortToNHBowyer ),
			typeof( EscortToNHBank )
		};

		[Constructable]
		public BaseEscortable()
			: base(AIType.AI_Melee, FightMode.Aggressor, 22, 1, 0.2, 1.0)
		{
			InitBody();
			InitOutfit();

			Fame = 200;
			Karma = 4000;
		}

		public virtual void InitBody()
		{
			SetStr(90, 100);
			SetDex(90, 100);
			SetInt(15, 25);

			Hue = Utility.RandomSkinHue();

			if (Female = Utility.RandomBool())
			{
				Body = 401;
				Name = NameList.RandomName("female");
			}
			else
			{
				Body = 400;
				Name = NameList.RandomName("male");
			}
		}

		public virtual void InitOutfit()
		{
			AddItem(new FancyShirt(Utility.RandomNeutralHue()));
			AddItem(new ShortPants(Utility.RandomNeutralHue()));
			AddItem(new Boots(Utility.RandomNeutralHue()));

			Utility.AssignRandomHair(this);

			PackGold(200, 250);
		}

		public virtual bool SayDestinationTo(Mobile m)
		{
			EDI dest = GetDestination();

			if (dest == null || !m.Alive)
				return false;

			Mobile escorter = GetEscorter();

			if (escorter == null)
			{
				Say("I am looking to go to {0}, will you take me?", (dest.Name == "Ocllo" && m.Map == Map.Trammel) ? "Haven" : dest.Name);
				return true;
			}
			else if (escorter == m)
			{
				Say("Lead on! Payment will be made when we arrive in {0}.", (dest.Name == "Ocllo" && m.Map == Map.Trammel) ? "Haven" : dest.Name);
				return true;
			}

			return false;
		}

		private static Hashtable m_EscortTable = new Hashtable();

		public static Hashtable EscortTable
		{
			get { return m_EscortTable; }
		}

		public virtual bool AcceptEscorter(Mobile m)
		{
			EDI dest = GetDestination();

			if (dest == null)
				return false;

			Mobile escorter = GetEscorter();

			if (escorter != null || !m.Alive)
				return false;

			BaseEscortable escortable = (BaseEscortable)m_EscortTable[m];

			if (escortable != null && !escortable.Deleted && escortable.GetEscorter() == m)
			{
				Say("I see you already have an escort.");
				return false;
			}
			else if (m is PlayerMobile && (((PlayerMobile)m).LastEscortTime + EscortDelay) >= DateTime.UtcNow)
			{
				int minutes = (int)Math.Ceiling(((((PlayerMobile)m).LastEscortTime + EscortDelay) - DateTime.UtcNow).TotalMinutes);

				Say("You must rest {0} minute{1} before we set out on this journey.", minutes, minutes == 1 ? "" : "s");
				return false;
			}
			else if (SetControlMaster(m))
			{
				m_LastSeenEscorter = DateTime.UtcNow;

				if (m is PlayerMobile)
					((PlayerMobile)m).LastEscortTime = DateTime.UtcNow;

				Say("Lead on! Payment will be made when we arrive in {0}.", (dest.Name == "Ocllo" && m.Map == Map.Trammel) ? "Haven" : dest.Name);
				m_EscortTable[m] = this;
				StartFollow();
				return true;
			}

			return false;
		}

		public override bool HandlesOnSpeech(Mobile from)
		{
			if ( MLQuestSystem.Enabled )
				return false;

			if (from.InRange(this.Location, 3))
				return true;

			return base.HandlesOnSpeech(from);
		}

		public override void OnSpeech(SpeechEventArgs e)
		{
			base.OnSpeech(e);

			EDI dest = GetDestination();

			if (dest != null && !e.Handled && e.Mobile.InRange(this.Location, 3))
			{
				if (e.HasKeyword(0x1D)) // *destination*
					e.Handled = SayDestinationTo(e.Mobile);
				else if (e.HasKeyword(0x1E)) // *i will take thee*
					e.Handled = AcceptEscorter(e.Mobile);
			}
		}

		public override void OnAfterDelete()
		{
			if (m_DeleteTimer != null)
				m_DeleteTimer.Stop();

			m_DeleteTimer = null;

			base.OnAfterDelete();
		}

		public override void OnThink()
		{
			base.OnThink();
			CheckAtDestination();
		}

		protected override bool OnMove(Direction d)
		{
			if (!base.OnMove(d))
				return false;

			CheckAtDestination();

			return true;
		}

		// TODO: Pre-ML methods below, might be mergeable with the ML methods in EscortObjective

		public virtual void StartFollow()
		{
			StartFollow(GetEscorter());
		}

		public virtual void StartFollow(Mobile escorter)
		{
			if (escorter == null)
				return;

			ActiveSpeed = 0.1;
			PassiveSpeed = 0.2;

			ControlOrder = OrderType.Follow;
			ControlTarget = escorter;

			if ((IsPrisoner == true) && (CantWalk == true))
			{
				CantWalk = false;
			}
			CurrentSpeed = 0.1;
		}

		public virtual void StopFollow()
		{
			ActiveSpeed = 0.2;
			PassiveSpeed = 1.0;

			ControlOrder = OrderType.None;
			ControlTarget = null;

			CurrentSpeed = 1.0;
		}

		private DateTime m_LastSeenEscorter;

		public virtual Mobile GetEscorter()
		{
			if ( !Controlled )
				return null;

			Mobile master = ControlMaster;

			if ( MLQuestSystem.Enabled || master == null )
				return master;

			if (master.Deleted || master.Map != this.Map || !master.InRange(Location, 30) || !master.Alive)
			{
				StopFollow();

				TimeSpan lastSeenDelay = DateTime.UtcNow - m_LastSeenEscorter;

				if (lastSeenDelay >= AbandonDelay)
				{
					master.SendLocalizedMessage(1042473); // You have lost the person you were escorting.
					Say(1005653); // Hmmm. I seem to have lost my master.

					SetControlMaster(null);
					m_EscortTable.Remove(master);

					Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerCallback(Delete));
					return null;
				}
				else
				{
					ControlOrder = OrderType.Stay;
					return master;
				}
			}

			if (ControlOrder != OrderType.Follow)
				StartFollow(master);

			m_LastSeenEscorter = DateTime.UtcNow;
			return master;
		}

		public virtual void BeginDelete()
		{
			if (m_DeleteTimer != null)
				m_DeleteTimer.Stop();

			m_DeleteTime = DateTime.UtcNow + DeleteTime;

			m_DeleteTimer = new DeleteTimer(this, m_DeleteTime - DateTime.UtcNow);
			m_DeleteTimer.Start();
		}

		public virtual bool CheckAtDestination()
		{
			if ( MLQuestSystem.Enabled )
				return false;

			EDI dest = GetDestination();

			if (dest == null)
				return false;

			Mobile escorter = GetEscorter();

			if (escorter == null)
				return false;

			if (dest.Contains(Location))
			{
				Say(1042809, escorter.Name); // We have arrived! I thank thee, ~1_PLAYER_NAME~! I have no further need of thy services. Here is thy pay.

				// not going anywhere
				m_Destination = null;
				m_DestinationString = null;

				Container cont = escorter.Backpack;

				if (cont == null)
					cont = escorter.BankBox;

				Gold gold = new Gold(500, 1000);

				if (!cont.TryDropItem(escorter, gold, false))
					gold.MoveToWorld(escorter.Location, escorter.Map);

				StopFollow();
				SetControlMaster(null);
				m_EscortTable.Remove(escorter);
				BeginDelete();

				Misc.Titles.AwardFame(escorter, 10, true);

				bool gainedPath = false;

				PlayerMobile pm = escorter as PlayerMobile;

				if (pm != null)
				{
					if (pm.CompassionGains > 0 && DateTime.UtcNow > pm.NextCompassionDay)
					{
						pm.NextCompassionDay = DateTime.MinValue;
						pm.CompassionGains = 0;
					}

					if (pm.CompassionGains >= 5) // have already gained 5 times in one day, can gain no more
					{
						pm.SendLocalizedMessage(1053004); // You must wait about a day before you can gain in compassion again.
					}
					else if (VirtueHelper.Award(pm, VirtueName.Compassion, this.IsPrisoner ? 400 : 200, ref gainedPath))
					{
						if (gainedPath)
							pm.SendLocalizedMessage(1053005); // You have achieved a path in compassion!
						else
							pm.SendLocalizedMessage(1053002); // You have gained in compassion.

						pm.NextCompassionDay = DateTime.UtcNow + TimeSpan.FromDays(1.0); // in one day CompassionGains gets reset to 0
						++pm.CompassionGains;

						if (pm.CompassionGains >= 5)
							pm.SendLocalizedMessage(1053004); // You must wait about a day before you can gain in compassion again.
					}
					else
					{
						pm.SendLocalizedMessage(1053003); // You have achieved the highest path of compassion and can no longer gain any further.
					}
				}

				return true;
			}

			return false;
		}

		public override bool OnBeforeDeath()
		{
			m_DeleteCorpse = ( Controlled || IsBeingDeleted );

			return base.OnBeforeDeath();
		}

		public BaseEscortable(Serial serial)
			: base(serial)
		{
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write((int)1); // version

			EDI dest = GetDestination();

			writer.Write(dest != null);

			if (dest != null)
				writer.Write(dest.Name);

			writer.Write(m_DeleteTimer != null);

			if (m_DeleteTimer != null)
				writer.WriteDeltaTime(m_DeleteTime);

			MLQuestSystem.WriteQuestRef( writer, StaticMLQuester ? null : m_MLQuest );
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);

			int version = reader.ReadInt();

			if (reader.ReadBool())
				m_DestinationString = reader.ReadString(); // NOTE: We cannot EDI.Find here, regions have not yet been loaded :-(

			if (reader.ReadBool())
			{
				m_DeleteTime = reader.ReadDeltaTime();
				m_DeleteTimer = new DeleteTimer(this, m_DeleteTime - DateTime.UtcNow);
				m_DeleteTimer.Start();
			}

			if ( version >= 1 )
			{
				MLQuest quest = MLQuestSystem.ReadQuestRef( reader );

				if ( MLQuestSystem.Enabled && quest != null && !StaticMLQuester )
					m_MLQuest = quest;
			}
		}

		public override bool CanBeRenamedBy(Mobile from)
		{
			return (from.AccessLevel >= AccessLevel.GameMaster);
		}

		public override void AddCustomContextEntries( Mobile from, List<ContextMenuEntry> list )
		{
			if ( from.Alive )
			{
				Mobile escorter = GetEscorter();

				if ( !MLQuestSystem.Enabled && GetDestination() != null )
				{
					if ( escorter == null || escorter == from )
						list.Add( new AskDestinationEntry( this, from ) );

					if ( escorter == null )
						list.Add( new AcceptEscortEntry( this, from ) );
				}

				if ( escorter == from )
					list.Add( new AbandonEscortEntry( this, from ) );
			}

			base.AddCustomContextEntries( from, list );
		}

		public virtual string[] GetPossibleDestinations()
		{
			if (!Core.ML)
				return m_TownNames;
			else
				return m_MLTownNames;
		}

		public virtual string PickRandomDestination()
		{
			if (Map.Felucca.Regions.Count == 0 || Map == null || Map == Map.Internal || Location == Point3D.Zero)
				return null; // Not yet fully initialized

			string[] possible = GetPossibleDestinations();
			string picked = null;

			while (picked == null)
			{
				picked = possible[Utility.Random(possible.Length)];
				EDI test = EDI.Find(picked);

				if (test != null && test.Contains(Location))
					picked = null;
			}

			return picked;
		}

		public EDI GetDestination()
		{
			if ( MLQuestSystem.Enabled )
				return null;

			if (m_DestinationString == null && m_DeleteTimer == null)
				m_DestinationString = PickRandomDestination();

			if (m_Destination != null && m_Destination.Name == m_DestinationString)
				return m_Destination;

			if (Map.Felucca.Regions.Count > 0)
				return (m_Destination = EDI.Find(m_DestinationString));

			return (m_Destination = null);
		}

		private class DeleteTimer : Timer
		{
			private Mobile m_Mobile;

			public DeleteTimer(Mobile m, TimeSpan delay)
				: base(delay)
			{
				m_Mobile = m;

				Priority = TimerPriority.OneSecond;
			}

			protected override void OnTick()
			{
				m_Mobile.Delete();
			}
		}
	}

	public class EscortDestinationInfo
	{
		private string m_Name;
		private Region m_Region;
		//private Rectangle2D[] m_Bounds;

		public string Name
		{
			get { return m_Name; }
		}

		public Region Region
		{
			get { return m_Region; }
		}

		/*public Rectangle2D[] Bounds
		{
			get{ return m_Bounds; }
		}*/

		public bool Contains(Point3D p)
		{
			return m_Region.Contains(p);
		}

		public EscortDestinationInfo(string name, Region region)
		{
			m_Name = name;
			m_Region = region;
		}

		private static Hashtable m_Table;

		public static void LoadTable()
		{
			ICollection list = Map.Felucca.Regions.Values;

			if (list.Count == 0)
				return;

			m_Table = new Hashtable();

			foreach (Region r in list)
			{
				if (r.Name == null)
					continue;

				if (r is Regions.DungeonRegion || r is Regions.TownRegion)
					m_Table[r.Name] = new EscortDestinationInfo(r.Name, r);
			}
		}

		public static EDI Find(string name)
		{
			if (m_Table == null)
				LoadTable();

			if (name == null || m_Table == null)
				return null;

			return (EscortDestinationInfo)m_Table[name];
		}
	}

	public class AskDestinationEntry : ContextMenuEntry
	{
		private BaseEscortable m_Mobile;
		private Mobile m_From;

		public AskDestinationEntry(BaseEscortable m, Mobile from)
			: base(6100, 3)
		{
			m_Mobile = m;
			m_From = from;
		}

		public override void OnClick()
		{
			m_Mobile.SayDestinationTo(m_From);
		}
	}

	public class AcceptEscortEntry : ContextMenuEntry
	{
		private BaseEscortable m_Mobile;
		private Mobile m_From;

		public AcceptEscortEntry(BaseEscortable m, Mobile from)
			: base(6101, 3)
		{
			m_Mobile = m;
			m_From = from;
		}

		public override void OnClick()
		{
			m_Mobile.AcceptEscorter(m_From);
		}
	}

	public class AbandonEscortEntry : ContextMenuEntry
	{
		private BaseEscortable m_Mobile;
		private Mobile m_From;

		public AbandonEscortEntry(BaseEscortable m, Mobile from)
			: base(6102, 3)
		{
			m_Mobile = m;
			m_From = from;
		}

		public override void OnClick()
		{
			m_Mobile.Delete(); // OSI just seems to delete instantly
		}
	}
}