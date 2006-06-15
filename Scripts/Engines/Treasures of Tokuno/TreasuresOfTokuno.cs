using System;
using Server;
using Server.Network;
using System.Collections;
using Server.Items;
using Server.Gumps;
using Server.Misc;
using Server.Mobiles;

namespace Server.Misc
{
	public class TreasuresOfTokuno
	{
		private static bool m_Enabled = (Core.Expansion == Expansion.SE);
		public static bool Enabled { get { return m_Enabled; } }

		public const int ItemsPerReward = 10;

		private static Type[] m_LesserArtifacts = new Type[]
			{
				typeof( AncientFarmersKasa ), typeof( AncientSamuraiDo ), typeof( ArmsOfTacticalExcellence ), typeof( BlackLotusHood ),
				typeof( DaimyosHelm ), typeof( DemonForks ), typeof( DragonNunchaku ), typeof( Exiler ), typeof( GlovesOfTheSun ),
				typeof( HanzosBow ), typeof( LegsOfStability ), typeof( PeasantsBokuto ), typeof( PilferedDancerFans ), typeof( TheDestroyer ),
				typeof( TomeOfEnlightenment ), typeof( AncientUrn ), typeof( HonorableSwords ), typeof( PigmentsOfTokuno ), typeof( FluteOfRenewal ) //TODO: Chest of heirlooms
			};

		public static Type[] LesserArtifacts { get { return m_LesserArtifacts; } }

		private static Type[] m_GreaterArtifacts = null;
		
		public static Type[] GreaterArtifacts
		{
			get
			{
				if( m_GreaterArtifacts == null )
				{
					m_GreaterArtifacts = new Type[ToTRedeemGump.NormalRewards.Length];
					for( int i = 0; i < m_GreaterArtifacts.Length; i++ )
					{
						m_GreaterArtifacts[i] = ToTRedeemGump.NormalRewards[i].Type;
					}
				}

				return m_GreaterArtifacts;
			}
		}

		private static bool CheckLocation( Mobile m )
		{
			Region r = m.Region;

			if( r.IsPartOf( typeof( Server.Regions.HouseRegion ) ) || Server.Multis.BaseBoat.FindBoatAt( m, m.Map ) != null )
				return false;
			//TODO: a CanReach of something check as opposed to above?

			if( r.IsPartOf( "Yomotsu Mines" ) || r.IsPartOf( "Fan Dancer's Dojo" ) )
				return true;

			return (m.Map == Map.Tokuno);
		}

		public static void HandleKill( Mobile victim, Mobile killer )
		{
			PlayerMobile pm = killer as PlayerMobile;
			BaseCreature bc = victim as BaseCreature;

			if( !Enabled || pm == null || bc == null || !CheckLocation( bc ) || !CheckLocation( pm )|| !killer.InRange( victim, 18 ))
				return;

			if( bc.Controlled || bc.Owners.Count > 0 || bc.Fame <= 0 )
				return;

			//25000 for 1/100 chance, 10 hyrus
			//1500, 1/1000 chance, 20 lizard men for that chance.

			pm.ToTTotalMonsterFame += (int)(bc.Fame * (1 + Math.Sqrt( pm.Luck ) / 100));

			//This is the Exponentional regression with only 2 datapoints.
			//A log. func would also work, but it didn't make as much sense.
			//This function isn't OSI exact beign that I don't know OSI's func they used ;p
			int x = pm.ToTTotalMonsterFame;

			//const double A = 8.63316841 * Math.Pow( 10, -4 );
			const double A = 0.000863316841;
			//const double B = 4.25531915 * Math.Pow( 10, -6 );
			const double B = 0.00000425531915;

			double chance = A * Math.Pow( 10, B * x );

			if( chance > Utility.RandomDouble() )
			{
				Item i = null;

				try
				{
					i = Activator.CreateInstance( m_LesserArtifacts[Utility.Random( m_LesserArtifacts.Length )] ) as Item;
				}
				catch
				{ }

				if( i != null )
				{
					if( pm.AddToBackpack( i ) )
					{
						pm.SendLocalizedMessage( 1062317 ); // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
						pm.ToTTotalMonsterFame = 0;
					}
					else
					{
						//Place in bank possibly?
						i.Delete();
					}
				}
			}
		}
	}
}

namespace Server.Mobiles
{
	public class IharaSoko : BaseVendor
	{
		public override bool IsActiveVendor { get { return false; } }
		public override bool IsInvulnerable { get { return true; } }
		public override bool DisallowAllMoves { get { return true; } }
		public override bool ClickTitle { get { return true; } }
		public override bool CanTeach { get { return false; } }

		protected ArrayList m_SBInfos = new ArrayList();
		protected override ArrayList SBInfos { get { return m_SBInfos; } }
		public override void InitSBInfo()
		{
		}


		public override void InitOutfit()
		{
			AddItem( new Waraji( 0x711 ) );
			AddItem( new Backpack() );
			AddItem( new Kamishimo( 0x483 ) );

			Item item = new LightPlateJingasa();
			item.Hue = 0x711;

			AddItem( item );
		}


		[Constructable]
		public IharaSoko() : base( "the Imperial Minister of Trade" )
		{
			Name = "Ihara Soko";
			Female = false;
			Body = 0x190;
			Hue = 0x8403;
		}

		public IharaSoko( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}

		public override bool CanBeDamaged()
		{
			return false;
		}

		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			//if( !TreasuresOfTokuno.Enabled )	//He still accepts items even if ToTs are turned off.
			//	return;

			if( m.Alive && m is PlayerMobile )
			{
				PlayerMobile pm = (PlayerMobile)m;

				int range = 3;

				if( m.Alive && Math.Abs( Z - m.Z ) < 16 && InRange( m, range ) && !InRange( oldLocation, range ) )
				{
					if( pm.ToTItemsTurnedIn >= TreasuresOfTokuno.ItemsPerReward )
					{
						SayTo( pm, 1070980 ); // Congratulations! You have turned in enough minor treasures to earn a greater reward.

						pm.CloseGump( typeof( ToTTurnInGump ) );	//Sanity

						if( !pm.HasGump( typeof( ToTRedeemGump ) ) )
							pm.SendGump( new ToTRedeemGump( this, false ) );
					}
					else
					{
						if( pm.ToTItemsTurnedIn == 0 )
							SayTo( pm, 1071013 ); // Bring me 10 of the lost treasures of Tokuno and I will reward you with a valuable item.
						else
							SayTo( pm, 1070981, String.Format( "{0}\t{1}", pm.ToTItemsTurnedIn, TreasuresOfTokuno.ItemsPerReward ) ); // You have turned in ~1_COUNT~ minor artifacts. Turn in ~2_NUM~ to receive a reward.

						ArrayList buttons = ToTTurnInGump.FindRedeemableItems( pm );

						if( buttons.Count > 0 && !pm.HasGump( typeof( ToTTurnInGump ) ) )
							pm.SendGump( new ToTTurnInGump( this, buttons ) );
					}
				}

				int leaveRange = 7;

				if( !InRange( m, leaveRange ) && InRange( oldLocation, leaveRange ) )
				{
					pm.CloseGump( typeof( ToTRedeemGump ) );
					pm.CloseGump( typeof( ToTTurnInGump ) );
				}
			}
		}

		public override void TurnToTokuno(){}
	}
}

namespace Server.Gumps
{
	public class ItemTileButtonInfo : ImageTileButtonInfo
	{
		private Item m_Item;

		public Item Item
		{
			get { return m_Item; }
			set { m_Item = value; }
		}

		public ItemTileButtonInfo( Item i ) : base( i.ItemID, i.Hue, ((i.Name == null || i.Name.Length <= 0)? (TextDefinition)i.LabelNumber : (TextDefinition)i.Name ) )
		{
			m_Item = i;
		}
	}

	public class ToTTurnInGump : BaseImageTileButtonsGump
	{
		public static ArrayList FindRedeemableItems( Mobile m )
		{
			Backpack pack = (Backpack)m.Backpack;
			if( pack == null )
				return new ArrayList();

			ArrayList items = new ArrayList( pack.FindItemsByType( TreasuresOfTokuno.LesserArtifacts ) );
			ArrayList buttons = new ArrayList();

			for( int i = 0; i < items.Count; i++ )
			{
				Item item = (Item)items[i];
				//bool acceptable = true;

				/*	TODO:
						if( item is ChestOfHeirlooms )
				*/
				if( item is PigmentsOfTokuno && ((PigmentsOfTokuno)item).Type != PigmentType.None )
					continue;

				buttons.Add( new ItemTileButtonInfo( item ) );
			}

			return buttons;
		}

		Mobile m_Collector;

		public ToTTurnInGump( Mobile collector, ArrayList buttons ) : base( 1071012, buttons ) // Click a minor artifact to give it to Ihara Soko.
		{
			m_Collector = collector;
		}

		public ToTTurnInGump( Mobile collector, ItemTileButtonInfo[] buttons ) : base( 1071012, buttons ) // Click a minor artifact to give it to Ihara Soko.
		{
			m_Collector = collector;
		}

		public override void HandleButtonResponse( NetState sender, int adjustedButton, ImageTileButtonInfo buttonInfo )
		{
			PlayerMobile pm = sender.Mobile as PlayerMobile;

			Item item = ((ItemTileButtonInfo)buttonInfo).Item;

			if( !( pm != null && item.IsChildOf( pm.Backpack ) && pm.InRange( m_Collector.Location, 7 )) )
				return;

			item.Delete();

			if( ++pm.ToTItemsTurnedIn >= TreasuresOfTokuno.ItemsPerReward )
			{
				m_Collector.SayTo( pm, 1070980 ); // Congratulations! You have turned in enough minor treasures to earn a greater reward.

				pm.CloseGump( typeof( ToTTurnInGump ) );	//SAnity

				if( !pm.HasGump( typeof( ToTRedeemGump ) ) )
					pm.SendGump( new ToTRedeemGump( m_Collector, false ) );
			}
			else
			{
				m_Collector.SayTo( pm, 1070981, String.Format( "{0}\t{1}", pm.ToTItemsTurnedIn, TreasuresOfTokuno.ItemsPerReward ) ); // You have turned in ~1_COUNT~ minor artifacts. Turn in ~2_NUM~ to receive a reward.

				ArrayList buttons = FindRedeemableItems( pm );

				pm.CloseGump( typeof( ToTTurnInGump ) ); //Sanity

				if( buttons.Count > 0 )
					pm.SendGump( new ToTTurnInGump( m_Collector, buttons ) );
			}
		}

		public override void HandleCancel( NetState sender )
		{
			PlayerMobile pm = sender.Mobile as PlayerMobile;

			if( pm == null || !pm.InRange( m_Collector.Location, 7 ) )
				return;
			
			if( pm.ToTItemsTurnedIn == 0 )
				m_Collector.SayTo( pm, 1071013 ); // Bring me 10 of the lost treasures of Tokuno and I will reward you with a valuable item.
			else if( pm.ToTItemsTurnedIn < TreasuresOfTokuno.ItemsPerReward )	//This case should ALWAYS be true with this gump, jsut a sanity check
				m_Collector.SayTo( pm, 1070981, String.Format( "{0}\t{1}", pm.ToTItemsTurnedIn, TreasuresOfTokuno.ItemsPerReward ) ); // You have turned in ~1_COUNT~ minor artifacts. Turn in ~2_NUM~ to receive a reward.
			else
				m_Collector.SayTo( pm, 1070982 ); // When you wish to choose your reward, you have but to approach me again.
		}

	}
}

namespace Server.Gumps
{
	public class ToTRedeemGump : BaseImageTileButtonsGump
	{
		public class TypeTileButtonInfo : ImageTileButtonInfo
		{
			private Type m_Type;

			public Type Type { get { return m_Type; } }

			public TypeTileButtonInfo( Type type, int itemID, int hue, TextDefinition label, int localizedToolTip ) : base( itemID, hue, label, localizedToolTip )
			{
				m_Type = type;
			}

			public TypeTileButtonInfo( Type type, int itemID, TextDefinition label ) : this( type, itemID, 0, label, -1 )
			{
			}

			public TypeTileButtonInfo( Type type, int itemID, TextDefinition label, int localizedToolTip ) : this( type, itemID, 0, label, localizedToolTip )
			{
			}
		}

		public class PigmentsTileButtonInfo : ImageTileButtonInfo
		{
			private PigmentType m_Pigment;

			public PigmentType Pigment
			{
				get
				{
					return m_Pigment;
				}

				set
				{
					m_Pigment = value;
				}
			}

			public PigmentsTileButtonInfo( PigmentType p ) : base( 0xEFF, PigmentsOfTokuno.PigmentInfo.GetInfo( p ).Hue, PigmentsOfTokuno.PigmentInfo.GetInfo( p ).Label )
			{
				m_Pigment = p;
			}
		}

		private static TypeTileButtonInfo[] m_NormalRewards = new TypeTileButtonInfo[]
			{
				new TypeTileButtonInfo( typeof( SwordsOfProsperity ),	 0x27A9, 1070963, 1071002 ),
				new TypeTileButtonInfo( typeof( SwordOfTheStampede ),	 0x27A2, 1070964, 1070978 ),
				new TypeTileButtonInfo( typeof( WindsEdge ),			 0x27A3, 1070965, 1071003 ),
				new TypeTileButtonInfo( typeof( DarkenedSky ),			 0x27AD, 1070966, 1071004 ),
				new TypeTileButtonInfo( typeof( TheHorselord ),			 0x27A5, 1070967, 1071005 ),
				new TypeTileButtonInfo( typeof( RuneBeetleCarapace ),	 0x277D, 1070968, 1071006 ),
				new TypeTileButtonInfo( typeof( KasaOfTheRajin ),		 0x2798, 1070969, 1071007 ),
				new TypeTileButtonInfo( typeof( Stormgrip ),			 0x2792, 1070970, 1071008 ),
				new TypeTileButtonInfo( typeof( TomeOfLostKnowledge ),	 0xEFA,	 0x530, 1070971, 1071009 ),
				new TypeTileButtonInfo( typeof( PigmentsOfTokuno ),		 0xEFF,	 1070933, 1071011 )
			};

		public static TypeTileButtonInfo[] NormalRewards { get { return m_NormalRewards; } }

		private static PigmentsTileButtonInfo[] m_PigmentRewards = new PigmentsTileButtonInfo[]
			{
				new PigmentsTileButtonInfo( PigmentType.ParagonGold ),
				new PigmentsTileButtonInfo( PigmentType.VioletCouragePurple ),
				new PigmentsTileButtonInfo( PigmentType.InvulnerabilityBlue ),
				new PigmentsTileButtonInfo( PigmentType.LunaWhite ),
				new PigmentsTileButtonInfo( PigmentType.DryadGreen ),
				new PigmentsTileButtonInfo( PigmentType.ShadowDancerBlack ),
				new PigmentsTileButtonInfo( PigmentType.BerserkerRed ),
				new PigmentsTileButtonInfo( PigmentType.NoxGreen ),
				new PigmentsTileButtonInfo( PigmentType.RumRed ),
				new PigmentsTileButtonInfo( PigmentType.FireOrange )
			};

		public static PigmentsTileButtonInfo[] PigmentRewards { get { return m_PigmentRewards; } }

		private Mobile m_Collector;

		public ToTRedeemGump( Mobile collector, bool pigments ) : base( pigments ? 1070986 : 1070985, pigments ? (ImageTileButtonInfo[])m_PigmentRewards : (ImageTileButtonInfo[])m_NormalRewards )
		{
			m_Collector = collector;
		}

		public override void HandleButtonResponse( NetState sender, int adjustedButton, ImageTileButtonInfo buttonInfo )
		{
			PlayerMobile pm = sender.Mobile as PlayerMobile;

			if( pm == null || !pm.InRange( m_Collector.Location, 7 ) || !(pm.ToTItemsTurnedIn >= TreasuresOfTokuno.ItemsPerReward) )
				return;

			bool pigments = (buttonInfo is PigmentsTileButtonInfo);

			Item item = null;

			if( pigments )
			{
				PigmentsTileButtonInfo p = buttonInfo as PigmentsTileButtonInfo;

				item = new PigmentsOfTokuno( p.Pigment );
			}
			else
			{
				TypeTileButtonInfo t = buttonInfo as TypeTileButtonInfo;

				if( t.Type == typeof( PigmentsOfTokuno ) )	//Special case of course.
				{
					pm.CloseGump( typeof( ToTTurnInGump ) );	//Sanity
					pm.CloseGump( typeof( ToTRedeemGump ) );

					pm.SendGump( new ToTRedeemGump( m_Collector, true ) );

					return;
				}

				try
				{
					item = (Item)Activator.CreateInstance( t.Type );
				}
				catch { }
			}

			if( item == null )
				return; //Sanity

			if( pm.AddToBackpack( item ) )
			{
				pm.ToTItemsTurnedIn -= TreasuresOfTokuno.ItemsPerReward;
				m_Collector.SayTo( pm, 1070984, (item.Name == null || item.Name.Length <= 0)? String.Format( "#{0}", item.LabelNumber ) : item.Name ); // You have earned the gratitude of the Empire. I have placed the ~1_OBJTYPE~ in your backpack.
			}
			else
			{
				item.Delete();
				m_Collector.SayTo( pm, 500722 ); // You don't have enough room in your backpack!
				m_Collector.SayTo( pm, 1070982 ); // When you wish to choose your reward, you have but to approach me again.
			}
		}


		public override void HandleCancel( NetState sender )
		{
			PlayerMobile pm = sender.Mobile as PlayerMobile;

			if( pm == null || !pm.InRange( m_Collector.Location, 7 ) )
				return;

			if( pm.ToTItemsTurnedIn == 0 )
				m_Collector.SayTo( pm, 1071013 ); // Bring me 10 of the lost treasures of Tokuno and I will reward you with a valuable item.
			else if( pm.ToTItemsTurnedIn < TreasuresOfTokuno.ItemsPerReward )	//This and above case should ALWAYS be FALSE with this gump, jsut a sanity check
				m_Collector.SayTo( pm, 1070981, String.Format( "{0}\t{1}", pm.ToTItemsTurnedIn, TreasuresOfTokuno.ItemsPerReward ) ); // You have turned in ~1_COUNT~ minor artifacts. Turn in ~2_NUM~ to receive a reward.
			else
				m_Collector.SayTo( pm, 1070982 ); // When you wish to choose your reward, you have but to approach me again.

		}
	}
}

/* Notes

Pigments of tokuno do NOT check for if item is already hued 0;  APPARENTLY he still accepts it if it's < 10 charges.

Chest of Heirlooms don't show if unlocked.

Chest of heirlooms, locked, HARD to pick at 100 lock picking but not impossible.  had 95 health to 0, cause it's trapped >< (explosion i think)
*/