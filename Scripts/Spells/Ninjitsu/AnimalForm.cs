using System;
using System.Collections;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Spells.Fifth;
using Server.Spells.Seventh;

namespace Server.Spells.Ninjitsu
{
	public class AnimalForm : NinjaSpell
	{

		public static void Initialize()
		{
			EventSink.Login += new LoginEventHandler( OnLogin );
		}

		public static void OnLogin( LoginEventArgs e )
		{
			AnimalFormContext context = AnimalForm.GetContext( e.Mobile );

			if( context != null && context.SpeedBoost )
				e.Mobile.Send( SpeedControl.MountSpeed );
		}

		private static SpellInfo m_Info = new SpellInfo(
			"Animal Form", null,
			-1,
			9002
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 1.0 ); } }

		public override double RequiredSkill{ get{ return 0.0; } }
		public override int RequiredMana{ get{ return (Core.ML ? 10 : 0); } }
		public override int CastRecoveryBase{ get { return (Core.ML ? 10 : base.CastRecoveryBase); } }

		public override bool BlockedByAnimalForm{ get{ return false; } }

		public AnimalForm( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override bool CheckCast()
		{
			if ( !Caster.CanBeginAction( typeof( PolymorphSpell ) ) )
			{
				Caster.SendLocalizedMessage( 1061628 ); // You can't do that while polymorphed.
				return false;
			}
			else if ( TransformationSpellHelper.UnderTransformation( Caster ) )
			{
				Caster.SendLocalizedMessage( 1063219 ); // You cannot mimic an animal while in that form.
				return false;
			}

			return base.CheckCast();
		}

		public override bool CheckDisturb( DisturbType type, bool firstCircle, bool resistable )
		{
			return false;
		}

		public override void OnBeginCast()
		{
			base.OnBeginCast();

			Caster.FixedEffect( 0x37C4, 10, 14, 4, 3 );
		}

		public override bool CheckFizzle()
		{
			// Spell is initially always successful, and with no skill gain.
			return true;
		}

		public override void OnCast()
		{
			if ( !Caster.CanBeginAction( typeof( PolymorphSpell ) ) )
			{
				Caster.SendLocalizedMessage( 1061628 ); // You can't do that while polymorphed.
			}
			else if( TransformationSpellHelper.UnderTransformation( Caster ) )
			{
				Caster.SendLocalizedMessage( 1063219 ); // You cannot mimic an animal while in that form.
			}
			else if ( !Caster.CanBeginAction( typeof( IncognitoSpell ) ) || (Caster.IsBodyMod && GetContext( Caster ) == null) )
			{
				DoFizzle();
			}
			else if ( CheckSequence() )
			{
				AnimalFormContext context = GetContext( Caster );

				int mana = ScaleMana( RequiredMana );
				if ( mana > Caster.Mana )
				{
					Caster.SendLocalizedMessage( 1060174, mana.ToString() ); // You must have at least ~1_MANA_REQUIREMENT~ Mana to use this ability.
				}
				else if ( context != null )
				{
						RemoveContext( Caster, context, true );
						Caster.Mana -= mana;
				}
				else if ( Caster is PlayerMobile )
				{
					if ( GetLastAnimalForm( Caster ) == -1 || DateTime.Now - Caster.LastMoveTime > Caster.ComputeMovementSpeed( Caster.Direction ) )
					{
						Caster.CloseGump( typeof( AnimalFormGump ) );
						Caster.SendGump( new AnimalFormGump( Caster, m_Entries, this ) );
					}
					else
					{
						if ( Morph( Caster, GetLastAnimalForm( Caster ) ) == MorphResult.Fail )
							DoFizzle();
						else
							Caster.Mana -= mana;
					}
				}
				else
				{
					if ( Morph( Caster, GetLastAnimalForm( Caster ) ) == MorphResult.Fail )
						DoFizzle();
					else
						Caster.Mana -= mana;
				}
			}

			FinishSequence();
		}

		private static Hashtable m_LastAnimalForms = new Hashtable();

		public int GetLastAnimalForm( Mobile m )
		{
			if ( m_LastAnimalForms.Contains( m ) )
				return (int)m_LastAnimalForms[m];

			return -1;
		}

		public enum MorphResult
		{
			Success,
			Fail,
			NoSkill
		}

		public static MorphResult Morph( Mobile m, int entryID )
		{
			if ( entryID < 0 || entryID >= m_Entries.Length )
				return MorphResult.Fail;

			AnimalFormEntry entry = m_Entries[entryID];

			m_LastAnimalForms[m] = entryID;	//On OSI, it's the last /attempted/ one not the last succeeded one

			if ( m.Skills.Ninjitsu.Value < entry.ReqSkill )
			{
				string args = String.Format( "{0}\t{1}\t ", entry.ReqSkill.ToString( "F1" ), SkillName.Ninjitsu );
				m.SendLocalizedMessage( 1063013, args ); // You need at least ~1_SKILL_REQUIREMENT~ ~2_SKILL_NAME~ skill to use that ability.
				return MorphResult.NoSkill;
			}

			/*
			if( !m.CheckSkill( SkillName.Ninjitsu, entry.ReqSkill, entry.ReqSkill + 37.5 ) )
				return MorphResult.Fail;
			 * 
			 * On OSI,it seems you can only gain starting at '0' using Animal form.  
			*/
			
			double ninjitsu = m.Skills.Ninjitsu.Value;

			if ( ninjitsu < entry.ReqSkill + 37.5 )
			{
				double chance = (ninjitsu - entry.ReqSkill) / 37.5;

				if ( chance < Utility.RandomDouble() )
					return MorphResult.Fail;
			}

			m.CheckSkill( SkillName.Ninjitsu, 0.0, 37.5 );

			BaseMount.Dismount( m );

			m.BodyMod = entry.BodyMod;

			if ( entry.HueMod > 0 )
				m.HueMod = entry.HueMod;

			if ( entry.SpeedBoost )
				m.Send( SpeedControl.MountSpeed );

			SkillMod mod = null;

			if ( entry.StealthBonus )
			{
				mod = new DefaultSkillMod( SkillName.Stealth, true, 20.0 );
				mod.ObeyCap = true;
				m.AddSkillMod( mod );
			}

			Timer timer = new AnimalFormTimer( m, entry.BodyMod, entry.HueMod );
			timer.Start();

			AddContext( m, new AnimalFormContext( timer, mod, entry.SpeedBoost, entry.Type ) );
			return MorphResult.Success;
		}


		private static Hashtable m_Table = new Hashtable();

		public static void AddContext( Mobile m, AnimalFormContext context )
		{
			m_Table[m] = context;

			if ( context.Type == typeof( BakeKitsune ) || context.Type == typeof( GreyWolf ) )
				m.CheckStatTimers();
		}

		public static void RemoveContext( Mobile m, bool resetGraphics )
		{
			AnimalFormContext context = GetContext( m );

			if ( context != null )
				RemoveContext( m, context, resetGraphics );
		}

		public static void RemoveContext( Mobile m, AnimalFormContext context, bool resetGraphics )
		{
			m_Table.Remove( m );

			if ( context.SpeedBoost )
				m.Send( SpeedControl.Disable );

			SkillMod mod = context.Mod;

			if ( mod != null )
				m.RemoveSkillMod( mod );

			if ( resetGraphics )
			{
				m.HueMod = -1;
				m.BodyMod = 0;
			}
			
			m.FixedParticles( 0x3728, 10, 13, 2023, EffectLayer.Waist );

			context.Timer.Stop();
		}

		public static AnimalFormContext GetContext( Mobile m )
		{
			return ( m_Table[m] as AnimalFormContext );
		}

		public static bool UnderTransformation( Mobile m )
		{
			return ( GetContext( m ) != null );
		}

		public static bool UnderTransformation( Mobile m, Type type )
		{
			AnimalFormContext context = GetContext( m );

			return ( context != null && context.Type == type );
		}
/*
		private delegate void AnimalFormCallback( Mobile from );
		private delegate bool AnimalFormRequirementCallback( Mobile from );
*/
 		public class AnimalFormEntry
		{
			private Type m_Type;
			private TextDefinition m_Name;
			private int m_ItemID;
			private int m_Hue;
			private int m_Tooltip;
			private double m_ReqSkill;
			private int m_BodyMod;
			private int m_HueMod;
			private bool m_StealthBonus;
			private bool m_SpeedBoost;

			public Type Type{ get{ return m_Type; } }
			public TextDefinition Name{ get{ return m_Name; } }
			public int ItemID{ get{ return m_ItemID; } }
			public int Hue{ get{ return m_Hue; } }
			public int Tooltip{ get{ return m_Tooltip; } }
			public double ReqSkill{ get{ return m_ReqSkill; } }
			public int BodyMod{ get{ return m_BodyMod; } }
			public int HueMod{ get{ return m_HueMod; } }
			public bool StealthBonus{ get{ return m_StealthBonus; } }
			public bool SpeedBoost{ get{ return m_SpeedBoost; } }
			/*
			private AnimalFormCallback m_TransformCallback;
			private AnimalFormCallback m_UntransformCallback;
			private AnimalFormRequirementCallback m_RequirementCallback;
			*/
			public AnimalFormEntry( Type type, TextDefinition name, int itemID, int hue, int tooltip, double reqSkill, int bodyMod, bool stealthBonus, bool speedBoost )
				: this( type, name, itemID, hue, tooltip, reqSkill, bodyMod, 0, stealthBonus, speedBoost )
			{
			}

			public AnimalFormEntry( Type type, TextDefinition name, int itemID, int hue, int tooltip, double reqSkill, int bodyMod, int hueMod, bool stealthBonus, bool speedBoost )
			{
				m_Type = type;
				m_Name = name;
				m_ItemID = itemID;
				m_Hue = hue;
				m_Tooltip = tooltip;
				m_ReqSkill = reqSkill;
				m_BodyMod = bodyMod;
				m_HueMod = hueMod;
				m_StealthBonus = stealthBonus;
				m_SpeedBoost = speedBoost;
			}
		}

		private static AnimalFormEntry[] m_Entries = new AnimalFormEntry[]
			{
				new AnimalFormEntry( typeof( Kirin ),        1029632,  9632,    0, 1070811, 100.0, 0x84, false,  true ),
				new AnimalFormEntry( typeof( Unicorn ),      1018214,  9678,    0, 1070812, 100.0, 0x7A, false,  true ),
				new AnimalFormEntry( typeof( BakeKitsune ),  1030083, 10083,    0, 1070810,	 82.5, 0xF6, false,  true ),
				new AnimalFormEntry( typeof( GreyWolf ),     1028482,  9681, 2309, 1070810,  82.5, 0x19, false,  true ),
				new AnimalFormEntry( typeof( Llama ),        1028438,  8438,    0, 1070809,  70.0, 0xDC, false,  true ),
				new AnimalFormEntry( typeof( ForestOstard ), 1018273,  8503, 2212, 1070809,  70.0, 0xDA, false,  true ),
				new AnimalFormEntry( typeof( BullFrog ),     1028496,  8496, 2003, 1070807,  50.0, 0x51, 0x5A3, false, false ),
				new AnimalFormEntry( typeof( GiantSerpent ), 1018114,  9663, 2009, 1070808,  50.0, 0x15, false, false ),
				new AnimalFormEntry( typeof( Dog ),          1018280,  8476, 2309, 1070806,  40.0, 0xD9, false, false ),
				new AnimalFormEntry( typeof( Cat ),          1018264,  8475, 2309, 1070806,  40.0, 0xC9, false, false ),
				new AnimalFormEntry( typeof( Rat ),          1018294,  8483, 2309, 1070805,  20.0, 0xEE,  true, false ),
				new AnimalFormEntry( typeof( Rabbit ),       1028485,  8485, 2309, 1070805,  20.0, 0xCD,  true, false )
			};

		public static AnimalFormEntry[] Entries{ get{ return m_Entries; } }

		public class AnimalFormGump : Gump
		{
			//TODO: Convert this for ML to the BaseImageTileButtonsgump
			private Mobile m_Caster;
			private AnimalForm m_Spell;

			public AnimalFormGump( Mobile caster, AnimalFormEntry[] entries, AnimalForm spell ) : base( 50, 50 )
			{
				m_Caster = caster;
				m_Spell = spell;
			
				AddPage( 0 );

				AddBackground( 0, 0, 520, 404, 0x13BE );
				AddImageTiled( 10, 10, 500, 20, 0xA40 );
				AddImageTiled( 10, 40, 500, 324, 0xA40 );
				AddImageTiled( 10, 374, 500, 20, 0xA40 );
				AddAlphaRegion( 10, 10, 500, 384 );
				
				AddHtmlLocalized( 14, 12, 500, 20, 1063394, 0x7FFF, false, false ); // <center>Polymorph Selection Menu</center>

				AddButton( 10, 374, 0xFB1, 0xFB2, 0, GumpButtonType.Reply, 0 );
				AddHtmlLocalized( 45, 376, 450, 20, 1011012, 0x7FFF, false, false ); // CANCEL
				
				double ninjitsu = caster.Skills.Ninjitsu.Value;
				
				int current = 0;

				for ( int i = 0; i < entries.Length; ++i )
				{
					bool enabled = ( ninjitsu >= entries[i].ReqSkill );
					
					int page = current / 10 + 1;
					int pos = current % 10;

					if ( pos == 0 )
					{
						if ( page > 1 )
						{
							AddButton( 400, 374, 0xFA5, 0xFA7, 0, GumpButtonType.Page, page );
							AddHtmlLocalized( 440, 376, 60, 20, 1043353, 0x7FFF, false, false ); // Next
						}

						AddPage( page );

						if ( page > 1 )
						{
							AddButton( 300, 374, 0xFAE, 0xFB0, 0, GumpButtonType.Page, 1 );
							AddHtmlLocalized( 340, 376, 60, 20, 1011393, 0x7FFF, false, false ); // Back
						}
					}
					
					if( enabled )
					{
						int x = ( pos % 2 == 0 ) ? 14 : 264;
						int y = ( pos / 2 ) * 64 + 44;
						
						Rectangle2D b = ItemBounds.Table[ entries[ i ].ItemID ];
						
						AddImageTiledButton( x, y, 0x918, 0x919, i + 1, GumpButtonType.Reply, 0, entries[i].ItemID, 0x0, 40 - b.Width / 2 - b.X, 30 - b.Height / 2 - b.Y, entries[i].Tooltip );
						AddHtmlLocalized( x + 84, y, 250, 60, entries[i].Name, 0x7FFF, false, false );
						
						current++;
					}
				}
			}

			public override void OnResponse( NetState sender, RelayInfo info )
			{
				int entryID = info.ButtonID - 1;
				
				if ( entryID < 0 || entryID >= m_Entries.Length )
					return;

				int mana = m_Spell.ScaleMana( m_Spell.RequiredMana );
				if ( mana > m_Caster.Mana )
				{
					m_Caster.SendLocalizedMessage( 1060174, mana.ToString() ); // You must have at least ~1_MANA_REQUIREMENT~ Mana to use this ability.
				}
				else
				{
					if ( AnimalForm.Morph( m_Caster, entryID ) == MorphResult.Fail )
					{
						m_Caster.LocalOverheadMessage( MessageType.Regular, 0x3B2, 502632 ); // The spell fizzles.
						m_Caster.FixedParticles( 0x3735, 1, 30, 9503, EffectLayer.Waist );
						m_Caster.PlaySound( 0x5C );
					}
					else
					{
						m_Caster.Mana -= mana;
					}
				}
			}
		}
	}

	public class AnimalFormContext
	{
		private Timer m_Timer;
		private SkillMod m_Mod;
		private bool m_SpeedBoost;
		private Type m_Type;

		public Timer Timer{ get{ return m_Timer; } }
		public SkillMod Mod{ get{ return m_Mod; } }
		public bool SpeedBoost{ get{ return m_SpeedBoost; } }
		public Type Type{ get{ return m_Type; } }

		public AnimalFormContext( Timer timer, SkillMod mod, bool speedBoost, Type type )
		{
			m_Timer = timer;
			m_Mod = mod;
			m_SpeedBoost = speedBoost;
			m_Type = type;
		}
	}

	public class AnimalFormTimer : Timer
	{
		private Mobile m_Mobile;
		private int m_Body;
		private int m_Hue;

		public AnimalFormTimer( Mobile from, int body, int hue ) : base( TimeSpan.FromSeconds( 1.0 ), TimeSpan.FromSeconds( 1.0 ) )
		{
			m_Mobile = from;
			m_Body = body;
			m_Hue = hue;

			Priority = TimerPriority.FiftyMS;
		}

		protected override void OnTick()
		{
			if ( m_Mobile.Deleted || !m_Mobile.Alive || m_Mobile.Body != m_Body || (m_Hue != 0 && m_Mobile.Hue != m_Hue) )
			{
				AnimalForm.RemoveContext( m_Mobile, true );
				Stop();
			}
		}
	}
}