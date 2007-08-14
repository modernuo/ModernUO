using System;
using Server;
using Server.Gumps;
using Server.Network;

namespace Server.Items
{
	public class PowerScroll : Item
	{
		private SkillName m_Skill;
		private double m_Value;

		private static SkillName[] m_Skills = new SkillName[]
			{
				SkillName.Blacksmith,
				SkillName.Tailoring,
				SkillName.Swords,
				SkillName.Fencing,
				SkillName.Macing,
				SkillName.Archery,
				SkillName.Wrestling,
				SkillName.Parry,
				SkillName.Tactics,
				SkillName.Anatomy,
				SkillName.Healing,
				SkillName.Magery,
				SkillName.Meditation,
				SkillName.EvalInt,
				SkillName.MagicResist,
				SkillName.AnimalTaming,
				SkillName.AnimalLore,
				SkillName.Veterinary,
				SkillName.Musicianship,
				SkillName.Provocation,
				SkillName.Discordance,
				SkillName.Peacemaking
			};

		private static SkillName[] m_AOSSkills = new SkillName[]
			{
				SkillName.Blacksmith,
				SkillName.Tailoring,
				SkillName.Swords,
				SkillName.Fencing,
				SkillName.Macing,
				SkillName.Archery,
				SkillName.Wrestling,
				SkillName.Parry,
				SkillName.Tactics,
				SkillName.Anatomy,
				SkillName.Healing,
				SkillName.Magery,
				SkillName.Meditation,
				SkillName.EvalInt,
				SkillName.MagicResist,
				SkillName.AnimalTaming,
				SkillName.AnimalLore,
				SkillName.Veterinary,
				SkillName.Musicianship,
				SkillName.Provocation,
				SkillName.Discordance,
				SkillName.Peacemaking,
				SkillName.Chivalry,
				SkillName.Focus,
				SkillName.Necromancy,
				SkillName.Stealing,
				SkillName.Stealth,
				SkillName.SpiritSpeak
			};

		private static SkillName[] m_SESkills = new SkillName[]
			{
				SkillName.Blacksmith,
				SkillName.Tailoring,
				SkillName.Swords,
				SkillName.Fencing,
				SkillName.Macing,
				SkillName.Archery,
				SkillName.Wrestling,
				SkillName.Parry,
				SkillName.Tactics,
				SkillName.Anatomy,
				SkillName.Healing,
				SkillName.Magery,
				SkillName.Meditation,
				SkillName.EvalInt,
				SkillName.MagicResist,
				SkillName.AnimalTaming,
				SkillName.AnimalLore,
				SkillName.Veterinary,
				SkillName.Musicianship,
				SkillName.Provocation,
				SkillName.Discordance,
				SkillName.Peacemaking,
				SkillName.Chivalry,
				SkillName.Focus,
				SkillName.Necromancy,
				SkillName.Stealing,
				SkillName.Stealth,
				SkillName.SpiritSpeak,
				SkillName.Ninjitsu,
				SkillName.Bushido
			};

		private static SkillName[] m_MLSkills = new SkillName[]
			{
				SkillName.Blacksmith,
				SkillName.Tailoring,
				SkillName.Swords,
				SkillName.Fencing,
				SkillName.Macing,
				SkillName.Archery,
				SkillName.Wrestling,
				SkillName.Parry,
				SkillName.Tactics,
				SkillName.Anatomy,
				SkillName.Healing,
				SkillName.Magery,
				SkillName.Meditation,
				SkillName.EvalInt,
				SkillName.MagicResist,
				SkillName.AnimalTaming,
				SkillName.AnimalLore,
				SkillName.Veterinary,
				SkillName.Musicianship,
				SkillName.Provocation,
				SkillName.Discordance,
				SkillName.Peacemaking,
				SkillName.Chivalry,
				SkillName.Focus,
				SkillName.Necromancy,
				SkillName.Stealing,
				SkillName.Stealth,
				SkillName.SpiritSpeak,
				SkillName.Ninjitsu,
				SkillName.Bushido,
				SkillName.Spellweaving
			};

		public static SkillName[] Skills{ get{ return ( Core.ML ? m_MLSkills : Core.SE ? m_SESkills : Core.AOS ? m_AOSSkills : m_Skills ); } }

		public static PowerScroll CreateRandom( int min, int max )
		{
			min /= 5;
			max /= 5;

			SkillName[] skills = PowerScroll.Skills;

			return new PowerScroll( skills[Utility.Random( skills.Length )], 100 + (Utility.RandomMinMax( min, max ) * 5));
		}

		public static PowerScroll CreateRandomNoCraft( int min, int max )
		{
			min /= 5;
			max /= 5;

			SkillName[] skills = PowerScroll.Skills;
			SkillName skillName;

			do
			{
				skillName = skills[Utility.Random( skills.Length )];
			} while ( skillName == SkillName.Blacksmith || skillName == SkillName.Tailoring );

			return new PowerScroll( skillName, 100 + (Utility.RandomMinMax( min, max ) * 5));
		}

		[Constructable]
		public PowerScroll( SkillName skill, double value ) : base( 0x14F0 )
		{
			base.Hue = 0x481;
			base.Weight = 1.0;

			m_Skill = skill;
			m_Value = value;
			if ( m_Value > 105.0 )
				LootType = LootType.Cursed;
		}

		public PowerScroll( Serial serial ) : base( serial )
		{
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public SkillName Skill
		{
			get
			{
				return m_Skill;
			}
			set
			{
				m_Skill = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public double Value
		{
			get
			{
				return m_Value;
			}
			set
			{
				m_Value = value;
			}
		}

		private string GetNameLocalized()
		{
			return String.Concat( "#", (1044060 + (int)m_Skill).ToString() );
		}

		private string GetName()
		{
			int index = (int)m_Skill;
			SkillInfo[] table = SkillInfo.Table;

			if ( index >= 0 && index < table.Length )
				return table[index].Name.ToLower();
			else
				return "???";
		}

		public override void AddNameProperty(ObjectPropertyList list)
		{
			if ( m_Value == 105.0 )
				list.Add( 1049639, GetNameLocalized() ); // a wonderous scroll of ~1_type~ (105 Skill)
			else if ( m_Value == 110.0 )
				list.Add( 1049640, GetNameLocalized() ); // an exalted scroll of ~1_type~ (110 Skill)
			else if ( m_Value == 115.0 )
				list.Add( 1049641, GetNameLocalized() ); // a mythical scroll of ~1_type~ (115 Skill)
			else if ( m_Value == 120.0 )
				list.Add( 1049642, GetNameLocalized() ); // a legendary scroll of ~1_type~ (120 Skill)
			else
				list.Add( "a power scroll of {0} ({1} Skill)", GetName(), m_Value );
		}

		public override void OnSingleClick( Mobile from )
		{
			if ( m_Value == 105.0 )
				base.LabelTo( from, 1049639, GetNameLocalized() ); // a wonderous scroll of ~1_type~ (105 Skill)
			else if ( m_Value == 110.0 )
				base.LabelTo( from, 1049640, GetNameLocalized() ); // an exalted scroll of ~1_type~ (110 Skill)
			else if ( m_Value == 115.0 )
				base.LabelTo( from, 1049641, GetNameLocalized() ); // a mythical scroll of ~1_type~ (115 Skill)
			else if ( m_Value == 120.0 )
				base.LabelTo( from, 1049642, GetNameLocalized() ); // a legendary scroll of ~1_type~ (120 Skill)
			else
				base.LabelTo( from, "a power scroll of {0} ({1} Skill)", GetName(), m_Value );
		}

		public void Use( Mobile from, bool firstStage )
		{
			if ( Deleted )
				return;

			if ( IsChildOf( from.Backpack ) )
			{
				Skill skill = from.Skills[m_Skill];

				if ( skill != null )
				{
					if ( skill.Cap >= m_Value )
					{
						from.SendLocalizedMessage( 1049511, GetNameLocalized() ); // Your ~1_type~ is too high for this power scroll.
					}
					else
					{
						if ( firstStage )
						{
							from.CloseGump( typeof( StatCapScroll.InternalGump ) );
							from.CloseGump( typeof( PowerScroll.InternalGump ) );
							from.SendGump( new InternalGump( from, this ) );
						}
						else
						{
							from.SendLocalizedMessage( 1049513, GetNameLocalized() ); // You feel a surge of magic as the scroll enhances your ~1_type~!

							skill.Cap = m_Value;

							Effects.SendLocationParticles( EffectItem.Create( from.Location, from.Map, EffectItem.DefaultDuration ), 0, 0, 0, 0, 0, 5060, 0 );
							Effects.PlaySound( from.Location, from.Map, 0x243 );

							Effects.SendMovingParticles( new Entity( Serial.Zero, new Point3D( from.X - 6, from.Y - 6, from.Z + 15 ), from.Map ), from, 0x36D4, 7, 0, false, true, 0x497, 0, 9502, 1, 0, (EffectLayer)255, 0x100 );
							Effects.SendMovingParticles( new Entity( Serial.Zero, new Point3D( from.X - 4, from.Y - 6, from.Z + 15 ), from.Map ), from, 0x36D4, 7, 0, false, true, 0x497, 0, 9502, 1, 0, (EffectLayer)255, 0x100 );
							Effects.SendMovingParticles( new Entity( Serial.Zero, new Point3D( from.X - 6, from.Y - 4, from.Z + 15 ), from.Map ), from, 0x36D4, 7, 0, false, true, 0x497, 0, 9502, 1, 0, (EffectLayer)255, 0x100 );

							Effects.SendTargetParticles( from, 0x375A, 35, 90, 0x00, 0x00, 9502, (EffectLayer)255, 0x100 );

							Delete();
						}
					}
				}
			}
			else
			{
				from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
			}
		}

		public override void OnDoubleClick( Mobile from )
		{
			Use( from, true );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( (int) m_Skill );
			writer.Write( (double) m_Value );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_Skill = (SkillName)reader.ReadInt();
					m_Value = reader.ReadDouble();

					break;
				}
			}

			if ( m_Value == 105.0 )
			{
				LootType = LootType.Regular;
			}
			else
			{
				LootType = LootType.Cursed;

				if ( Insured )
					Insured = false;
			}
		}

		public class InternalGump : Gump
		{
			private Mobile m_Mobile;
			private PowerScroll m_Scroll;

			public InternalGump( Mobile mobile, PowerScroll scroll ) : base( 25, 50 )
			{
				m_Mobile = mobile;
				m_Scroll = scroll;

				AddPage( 0 );

				AddBackground( 25, 10, 420, 200, 5054 );

				AddImageTiled( 33, 20, 401, 181, 2624 );
				AddAlphaRegion( 33, 20, 401, 181 );

				AddHtmlLocalized( 40, 48, 387, 100, 1049469, true, true ); /* Using a scroll increases the maximum amount of a specific skill or your maximum statistics.
																			* When used, the effect is not immediately seen without a gain of points with that skill or statistics.
																			* You can view your maximum skill values in your skills window.
																			* You can view your maximum statistic value in your statistics window.
																			*/

				AddHtmlLocalized( 125, 148, 200, 20, 1049478, 0xFFFFFF, false, false ); // Do you wish to use this scroll?

				AddButton( 100, 172, 4005, 4007, 1, GumpButtonType.Reply, 0 );
				AddHtmlLocalized( 135, 172, 120, 20, 1046362, 0xFFFFFF, false, false ); // Yes

				AddButton( 275, 172, 4005, 4007, 0, GumpButtonType.Reply, 0 );
				AddHtmlLocalized( 310, 172, 120, 20, 1046363, 0xFFFFFF, false, false ); // No

				double value = scroll.m_Value;

				if ( value == 105.0 )
					AddHtmlLocalized( 40, 20, 260, 20, 1049635, 0xFFFFFF, false, false ); // Wonderous Scroll (105 Skill):
				else if ( value == 110.0 )
					AddHtmlLocalized( 40, 20, 260, 20, 1049636, 0xFFFFFF, false, false ); // Exalted Scroll (110 Skill):
				else if ( value == 115.0 )
					AddHtmlLocalized( 40, 20, 260, 20, 1049637, 0xFFFFFF, false, false ); // Mythical Scroll (115 Skill):
				else if ( value == 120.0 )
					AddHtmlLocalized( 40, 20, 260, 20, 1049638, 0xFFFFFF, false, false ); // Legendary Scroll (120 Skill):
				else
					AddHtml( 40, 20, 260, 20, String.Format( "<basefont color=#FFFFFF>Power Scroll ({0} Skill):</basefont>", value ), false, false );

				AddHtmlLocalized( 310, 20, 120, 20, 1044060 + (int)scroll.m_Skill, 0xFFFFFF, false, false );
			}

			public override void OnResponse( NetState state, RelayInfo info )
			{
				if ( info.ButtonID == 1 )
					m_Scroll.Use( m_Mobile, false );
			}
		}
	}
}