using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server
{
	public class AOS
	{
		public static void DisableStatInfluences()
		{
			for( int i = 0; i < SkillInfo.Table.Length; ++i )
			{
				SkillInfo info = SkillInfo.Table[i];

				info.StrScale = 0.0;
				info.DexScale = 0.0;
				info.IntScale = 0.0;
				info.StatTotal = 0.0;
			}
		}

		public static int Damage( Mobile m, int damage, bool ignoreArmor, int phys, int fire, int cold, int pois, int nrgy )
		{
			return Damage( m, null, damage, ignoreArmor, phys, fire, cold, pois, nrgy );
		}

		public static int Damage( Mobile m, int damage, int phys, int fire, int cold, int pois, int nrgy )
		{
			return Damage( m, null, damage, phys, fire, cold, pois, nrgy );
		}

		public static int Damage( Mobile m, Mobile from, int damage, int phys, int fire, int cold, int pois, int nrgy )
		{
			return Damage( m, from, damage, false, phys, fire, cold, pois, nrgy, false );
		}

		public static int Damage( Mobile m, Mobile from, int damage, bool ignoreArmor, int phys, int fire, int cold, int pois, int nrgy )
		{
			return Damage( m, from, damage, ignoreArmor, phys, fire, cold, pois, nrgy, false );
		}

		public static int Damage( Mobile m, Mobile from, int damage, int phys, int fire, int cold, int pois, int nrgy, bool keepAlive )
		{
			return Damage( m, from, damage, false, phys, fire, cold, pois, nrgy, keepAlive );
		}

		public static int Damage( Mobile m, Mobile from, int damage, bool ignoreArmor, int phys, int fire, int cold, int pois, int nrgy, bool keepAlive )
		{
			if( m == null || m.Deleted || !m.Alive || damage <= 0 )
				return 0;

			if( phys == 0 && fire == 100 && cold == 0 && pois == 0 && nrgy == 0 )
				Mobiles.MeerMage.StopEffect( m, true );

			if( !Core.AOS )
			{
				m.Damage( damage, from );
				return damage;
			}

			Fix( ref phys );
			Fix( ref fire );
			Fix( ref cold );
			Fix( ref pois );
			Fix( ref nrgy );

			int totalDamage;

			if( !ignoreArmor )
			{
				// Armor Ignore on OSI ignores all defenses, not just physical.
				int resPhys = m.PhysicalResistance;
				int resFire = m.FireResistance;
				int resCold = m.ColdResistance;
				int resPois = m.PoisonResistance;
				int resNrgy = m.EnergyResistance;

				totalDamage  = damage * phys * (100 - resPhys);
				totalDamage += damage * fire * (100 - resFire);
				totalDamage += damage * cold * (100 - resCold);
				totalDamage += damage * pois * (100 - resPois);
				totalDamage += damage * nrgy * (100 - resNrgy);

				totalDamage /= 10000;

				if( totalDamage < 1 )
					totalDamage = 1;
			}
			else if( Core.ML && m is PlayerMobile && from is PlayerMobile )
			{
				totalDamage = Math.Min( damage, 35 );	//Direct Damage cap of 35
			}
			else
			{
				totalDamage = damage;
			}

			#region Dragon Barding
			if( (!Core.AOS || from == null || !from.Player) && m.Player && m.Mount is SwampDragon )
			{
				SwampDragon pet = m.Mount as SwampDragon;

				if( pet != null && pet.HasBarding )
				{
					int percent = (pet.BardingExceptional ? 20 : 10);
					int absorbed = Scale( totalDamage, percent );

					totalDamage -= absorbed;
					pet.BardingHP -= absorbed;

					if( pet.BardingHP < 0 )
					{
						pet.HasBarding = false;
						pet.BardingHP = 0;

						m.SendLocalizedMessage( 1053031 ); // Your dragon's barding has been destroyed!
					}
				}
			}
			#endregion

			if( keepAlive && totalDamage > m.Hits )
				totalDamage = m.Hits;

			if( from != null && !from.Deleted && from.Alive )
			{
				int reflectPhys = AosAttributes.GetValue( m, AosAttribute.ReflectPhysical );

				if( reflectPhys != 0 )
				{
					if( from is ExodusMinion && ((ExodusMinion)from).FieldActive || from is ExodusOverseer && ((ExodusOverseer)from).FieldActive )
					{
						from.FixedParticles( 0x376A, 20, 10, 0x2530, EffectLayer.Waist );
						from.PlaySound( 0x2F4 );
						m.SendAsciiMessage( "Your weapon cannot penetrate the creature's magical barrier" );
					}
					else
					{
						from.Damage( Scale( (damage * phys * (100 - (ignoreArmor ? 0 : m.PhysicalResistance))) / 10000, reflectPhys ), m );
					}
				}
			}

			m.Damage( totalDamage, from );
			return totalDamage;
		}

		public static void Fix( ref int val )
		{
			if( val < 0 )
				val = 0;
		}

		public static int Scale( int input, int percent )
		{
			return (input * percent) / 100;
		}
	}

	[Flags]
	public enum AosAttribute
	{
		RegenHits=0x00000001,
		RegenStam=0x00000002,
		RegenMana=0x00000004,
		DefendChance=0x00000008,
		AttackChance=0x00000010,
		BonusStr=0x00000020,
		BonusDex=0x00000040,
		BonusInt=0x00000080,
		BonusHits=0x00000100,
		BonusStam=0x00000200,
		BonusMana=0x00000400,
		WeaponDamage=0x00000800,
		WeaponSpeed=0x00001000,
		SpellDamage=0x00002000,
		CastRecovery=0x00004000,
		CastSpeed=0x00008000,
		LowerManaCost=0x00010000,
		LowerRegCost=0x00020000,
		ReflectPhysical=0x00040000,
		EnhancePotions=0x00080000,
		Luck=0x00100000,
		SpellChanneling=0x00200000,
		NightSight=0x00400000
	}

	public sealed class AosAttributes : BaseAttributes
	{
		public AosAttributes( Item owner )
			: base( owner )
		{
		}

		public AosAttributes( Item owner, GenericReader reader )
			: base( owner, reader )
		{
		}

		public static int GetValue( Mobile m, AosAttribute attribute )
		{
			if( !Core.AOS )
				return 0;

			List<Item> items = m.Items;
			int value = 0;

			for( int i = 0; i < items.Count; ++i )
			{
				Item obj = items[i];

				if( obj is BaseWeapon )
				{
					AosAttributes attrs = ((BaseWeapon)obj).Attributes;

					if( attrs != null )
						value += attrs[attribute];

					if( attribute == AosAttribute.Luck )
						value += ((BaseWeapon)obj).GetLuckBonus();
				}
				else if( obj is BaseArmor )
				{
					AosAttributes attrs = ((BaseArmor)obj).Attributes;

					if( attrs != null )
						value += attrs[attribute];

					if( attribute == AosAttribute.Luck )
						value += ((BaseArmor)obj).GetLuckBonus();
				}
				else if( obj is BaseJewel )
				{
					AosAttributes attrs = ((BaseJewel)obj).Attributes;

					if( attrs != null )
						value += attrs[attribute];
				}
				else if( obj is BaseClothing )
				{
					AosAttributes attrs = ((BaseClothing)obj).Attributes;

					if( attrs != null )
						value += attrs[attribute];
				}
				else if( obj is Spellbook )
				{
					AosAttributes attrs = ((Spellbook)obj).Attributes;

					if( attrs != null )
						value += attrs[attribute];
				}
			}

			return value;
		}

		public int this[AosAttribute attribute]
		{
			get { return GetValue( (int)attribute ); }
			set { SetValue( (int)attribute, value ); }
		}

		public override string ToString()
		{
			return "...";
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int RegenHits { get { return this[AosAttribute.RegenHits]; } set { this[AosAttribute.RegenHits] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int RegenStam { get { return this[AosAttribute.RegenStam]; } set { this[AosAttribute.RegenStam] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int RegenMana { get { return this[AosAttribute.RegenMana]; } set { this[AosAttribute.RegenMana] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int DefendChance { get { return this[AosAttribute.DefendChance]; } set { this[AosAttribute.DefendChance] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int AttackChance { get { return this[AosAttribute.AttackChance]; } set { this[AosAttribute.AttackChance] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int BonusStr { get { return this[AosAttribute.BonusStr]; } set { this[AosAttribute.BonusStr] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int BonusDex { get { return this[AosAttribute.BonusDex]; } set { this[AosAttribute.BonusDex] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int BonusInt { get { return this[AosAttribute.BonusInt]; } set { this[AosAttribute.BonusInt] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int BonusHits { get { return this[AosAttribute.BonusHits]; } set { this[AosAttribute.BonusHits] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int BonusStam { get { return this[AosAttribute.BonusStam]; } set { this[AosAttribute.BonusStam] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int BonusMana { get { return this[AosAttribute.BonusMana]; } set { this[AosAttribute.BonusMana] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int WeaponDamage { get { return this[AosAttribute.WeaponDamage]; } set { this[AosAttribute.WeaponDamage] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int WeaponSpeed { get { return this[AosAttribute.WeaponSpeed]; } set { this[AosAttribute.WeaponSpeed] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int SpellDamage { get { return this[AosAttribute.SpellDamage]; } set { this[AosAttribute.SpellDamage] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int CastRecovery { get { return this[AosAttribute.CastRecovery]; } set { this[AosAttribute.CastRecovery] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int CastSpeed { get { return this[AosAttribute.CastSpeed]; } set { this[AosAttribute.CastSpeed] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int LowerManaCost { get { return this[AosAttribute.LowerManaCost]; } set { this[AosAttribute.LowerManaCost] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int LowerRegCost { get { return this[AosAttribute.LowerRegCost]; } set { this[AosAttribute.LowerRegCost] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ReflectPhysical { get { return this[AosAttribute.ReflectPhysical]; } set { this[AosAttribute.ReflectPhysical] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int EnhancePotions { get { return this[AosAttribute.EnhancePotions]; } set { this[AosAttribute.EnhancePotions] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int Luck { get { return this[AosAttribute.Luck]; } set { this[AosAttribute.Luck] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int SpellChanneling { get { return this[AosAttribute.SpellChanneling]; } set { this[AosAttribute.SpellChanneling] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int NightSight { get { return this[AosAttribute.NightSight]; } set { this[AosAttribute.NightSight] = value; } }
	}

	[Flags]
	public enum AosWeaponAttribute
	{
		LowerStatReq=0x00000001,
		SelfRepair=0x00000002,
		HitLeechHits=0x00000004,
		HitLeechStam=0x00000008,
		HitLeechMana=0x00000010,
		HitLowerAttack=0x00000020,
		HitLowerDefend=0x00000040,
		HitMagicArrow=0x00000080,
		HitHarm=0x00000100,
		HitFireball=0x00000200,
		HitLightning=0x00000400,
		HitDispel=0x00000800,
		HitColdArea=0x00001000,
		HitFireArea=0x00002000,
		HitPoisonArea=0x00004000,
		HitEnergyArea=0x00008000,
		HitPhysicalArea=0x00010000,
		ResistPhysicalBonus=0x00020000,
		ResistFireBonus=0x00040000,
		ResistColdBonus=0x00080000,
		ResistPoisonBonus=0x00100000,
		ResistEnergyBonus=0x00200000,
		UseBestSkill=0x00400000,
		MageWeapon=0x00800000,
		DurabilityBonus=0x01000000
	}

	public sealed class AosWeaponAttributes : BaseAttributes
	{
		public AosWeaponAttributes( Item owner )
			: base( owner )
		{
		}

		public AosWeaponAttributes( Item owner, GenericReader reader )
			: base( owner, reader )
		{
		}

		public static int GetValue( Mobile m, AosWeaponAttribute attribute )
		{
			if( !Core.AOS )
				return 0;

			List<Item> items = m.Items;
			int value = 0;

			for( int i = 0; i < items.Count; ++i )
			{
				Item obj = items[i];

				if( obj is BaseWeapon )
				{
					AosWeaponAttributes attrs = ((BaseWeapon)obj).WeaponAttributes;

					if( attrs != null )
						value += attrs[attribute];
				}
			}

			return value;
		}

		public int this[AosWeaponAttribute attribute]
		{
			get { return GetValue( (int)attribute ); }
			set { SetValue( (int)attribute, value ); }
		}

		public override string ToString()
		{
			return "...";
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int LowerStatReq { get { return this[AosWeaponAttribute.LowerStatReq]; } set { this[AosWeaponAttribute.LowerStatReq] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int SelfRepair { get { return this[AosWeaponAttribute.SelfRepair]; } set { this[AosWeaponAttribute.SelfRepair] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HitLeechHits { get { return this[AosWeaponAttribute.HitLeechHits]; } set { this[AosWeaponAttribute.HitLeechHits] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HitLeechStam { get { return this[AosWeaponAttribute.HitLeechStam]; } set { this[AosWeaponAttribute.HitLeechStam] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HitLeechMana { get { return this[AosWeaponAttribute.HitLeechMana]; } set { this[AosWeaponAttribute.HitLeechMana] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HitLowerAttack { get { return this[AosWeaponAttribute.HitLowerAttack]; } set { this[AosWeaponAttribute.HitLowerAttack] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HitLowerDefend { get { return this[AosWeaponAttribute.HitLowerDefend]; } set { this[AosWeaponAttribute.HitLowerDefend] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HitMagicArrow { get { return this[AosWeaponAttribute.HitMagicArrow]; } set { this[AosWeaponAttribute.HitMagicArrow] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HitHarm { get { return this[AosWeaponAttribute.HitHarm]; } set { this[AosWeaponAttribute.HitHarm] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HitFireball { get { return this[AosWeaponAttribute.HitFireball]; } set { this[AosWeaponAttribute.HitFireball] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HitLightning { get { return this[AosWeaponAttribute.HitLightning]; } set { this[AosWeaponAttribute.HitLightning] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HitDispel { get { return this[AosWeaponAttribute.HitDispel]; } set { this[AosWeaponAttribute.HitDispel] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HitColdArea { get { return this[AosWeaponAttribute.HitColdArea]; } set { this[AosWeaponAttribute.HitColdArea] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HitFireArea { get { return this[AosWeaponAttribute.HitFireArea]; } set { this[AosWeaponAttribute.HitFireArea] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HitPoisonArea { get { return this[AosWeaponAttribute.HitPoisonArea]; } set { this[AosWeaponAttribute.HitPoisonArea] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HitEnergyArea { get { return this[AosWeaponAttribute.HitEnergyArea]; } set { this[AosWeaponAttribute.HitEnergyArea] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int HitPhysicalArea { get { return this[AosWeaponAttribute.HitPhysicalArea]; } set { this[AosWeaponAttribute.HitPhysicalArea] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ResistPhysicalBonus { get { return this[AosWeaponAttribute.ResistPhysicalBonus]; } set { this[AosWeaponAttribute.ResistPhysicalBonus] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ResistFireBonus { get { return this[AosWeaponAttribute.ResistFireBonus]; } set { this[AosWeaponAttribute.ResistFireBonus] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ResistColdBonus { get { return this[AosWeaponAttribute.ResistColdBonus]; } set { this[AosWeaponAttribute.ResistColdBonus] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ResistPoisonBonus { get { return this[AosWeaponAttribute.ResistPoisonBonus]; } set { this[AosWeaponAttribute.ResistPoisonBonus] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int ResistEnergyBonus { get { return this[AosWeaponAttribute.ResistEnergyBonus]; } set { this[AosWeaponAttribute.ResistEnergyBonus] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int UseBestSkill { get { return this[AosWeaponAttribute.UseBestSkill]; } set { this[AosWeaponAttribute.UseBestSkill] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int MageWeapon { get { return this[AosWeaponAttribute.MageWeapon]; } set { this[AosWeaponAttribute.MageWeapon] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int DurabilityBonus { get { return this[AosWeaponAttribute.DurabilityBonus]; } set { this[AosWeaponAttribute.DurabilityBonus] = value; } }
	}

	[Flags]
	public enum AosArmorAttribute
	{
		LowerStatReq=0x00000001,
		SelfRepair=0x00000002,
		MageArmor=0x00000004,
		DurabilityBonus=0x00000008
	}

	public sealed class AosArmorAttributes : BaseAttributes
	{
		public AosArmorAttributes( Item owner )
			: base( owner )
		{
		}

		public AosArmorAttributes( Item owner, GenericReader reader )
			: base( owner, reader )
		{
		}

		public static int GetValue( Mobile m, AosArmorAttribute attribute )
		{
			if( !Core.AOS )
				return 0;

			List<Item> items = m.Items;
			int value = 0;

			for( int i = 0; i < items.Count; ++i )
			{
				Item obj = items[i];

				if( obj is BaseArmor )
				{
					AosArmorAttributes attrs = ((BaseArmor)obj).ArmorAttributes;

					if( attrs != null )
						value += attrs[attribute];
				}
				else if( obj is BaseClothing )
				{
					AosArmorAttributes attrs = ((BaseClothing)obj).ClothingAttributes;

					if( attrs != null )
						value += attrs[attribute];
				}
			}

			return value;
		}

		public int this[AosArmorAttribute attribute]
		{
			get { return GetValue( (int)attribute ); }
			set { SetValue( (int)attribute, value ); }
		}

		public override string ToString()
		{
			return "...";
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int LowerStatReq { get { return this[AosArmorAttribute.LowerStatReq]; } set { this[AosArmorAttribute.LowerStatReq] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int SelfRepair { get { return this[AosArmorAttribute.SelfRepair]; } set { this[AosArmorAttribute.SelfRepair] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int MageArmor { get { return this[AosArmorAttribute.MageArmor]; } set { this[AosArmorAttribute.MageArmor] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int DurabilityBonus { get { return this[AosArmorAttribute.DurabilityBonus]; } set { this[AosArmorAttribute.DurabilityBonus] = value; } }
	}

	public sealed class AosSkillBonuses : BaseAttributes
	{
		private ArrayList m_Mods;

		public AosSkillBonuses( Item owner )
			: base( owner )
		{
		}

		public AosSkillBonuses( Item owner, GenericReader reader )
			: base( owner, reader )
		{
		}

		public void GetProperties( ObjectPropertyList list )
		{
			for( int i = 0; i < 5; ++i )
			{
				SkillName skill;
				double bonus;

				if( !GetValues( i, out skill, out bonus ) )
					continue;

				list.Add( 1060451 + i, "#{0}\t{1}", 1044060 + (int)skill, bonus );
			}
		}

		public void AddTo( Mobile m )
		{
			Remove();

			for( int i = 0; i < 5; ++i )
			{
				SkillName skill;
				double bonus;

				if( !GetValues( i, out skill, out bonus ) )
					continue;

				if( m_Mods == null )
					m_Mods = new ArrayList();

				SkillMod sk = new DefaultSkillMod( skill, true, bonus );
				sk.ObeyCap = true;
				m.AddSkillMod( sk );
				m_Mods.Add( sk );
			}
		}

		public void Remove()
		{
			if( m_Mods == null )
				return;

			for( int i = 0; i < m_Mods.Count; ++i )
				((SkillMod)m_Mods[i]).Remove();

			m_Mods = null;
		}

		public bool GetValues( int index, out SkillName skill, out double bonus )
		{
			int v = GetValue( 1 << index );
			int vSkill = 0;
			int vBonus = 0;

			for( int i = 0; i < 16; ++i )
			{
				vSkill <<= 1;
				vSkill |= (v & 1);
				v >>= 1;

				vBonus <<= 1;
				vBonus |= (v & 1);
				v >>= 1;
			}

			skill = (SkillName)vSkill;
			bonus = (double)vBonus / 10;

			return (bonus != 0);
		}

		public void SetValues( int index, SkillName skill, double bonus )
		{
			int v = 0;
			int vSkill = (int)skill;
			int vBonus = (int)(bonus * 10);

			for( int i = 0; i < 16; ++i )
			{
				v <<= 1;
				v |= (vBonus & 1);
				vBonus >>= 1;

				v <<= 1;
				v |= (vSkill & 1);
				vSkill >>= 1;
			}

			SetValue( 1 << index, v );
		}

		public SkillName GetSkill( int index )
		{
			SkillName skill;
			double bonus;

			GetValues( index, out skill, out bonus );

			return skill;
		}

		public void SetSkill( int index, SkillName skill )
		{
			SetValues( index, skill, GetBonus( index ) );
		}

		public double GetBonus( int index )
		{
			SkillName skill;
			double bonus;

			GetValues( index, out skill, out bonus );

			return bonus;
		}

		public void SetBonus( int index, double bonus )
		{
			SetValues( index, GetSkill( index ), bonus );
		}

		public override string ToString()
		{
			return "...";
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public double Skill_1_Value { get { return GetBonus( 0 ); } set { SetBonus( 0, value ); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public SkillName Skill_1_Name { get { return GetSkill( 0 ); } set { SetSkill( 0, value ); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public double Skill_2_Value { get { return GetBonus( 1 ); } set { SetBonus( 1, value ); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public SkillName Skill_2_Name { get { return GetSkill( 1 ); } set { SetSkill( 1, value ); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public double Skill_3_Value { get { return GetBonus( 2 ); } set { SetBonus( 2, value ); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public SkillName Skill_3_Name { get { return GetSkill( 2 ); } set { SetSkill( 2, value ); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public double Skill_4_Value { get { return GetBonus( 3 ); } set { SetBonus( 3, value ); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public SkillName Skill_4_Name { get { return GetSkill( 3 ); } set { SetSkill( 3, value ); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public double Skill_5_Value { get { return GetBonus( 4 ); } set { SetBonus( 4, value ); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public SkillName Skill_5_Name { get { return GetSkill( 4 ); } set { SetSkill( 4, value ); } }
	}

	[Flags]
	public enum AosElementAttribute
	{
		Physical=0x00000001,
		Fire=0x00000002,
		Cold=0x00000004,
		Poison=0x00000008,
		Energy=0x00000010
	}

	public sealed class AosElementAttributes : BaseAttributes
	{
		public AosElementAttributes( Item owner )
			: base( owner )
		{
		}

		public AosElementAttributes( Item owner, GenericReader reader )
			: base( owner, reader )
		{
		}

		public int this[AosElementAttribute attribute]
		{
			get { return GetValue( (int)attribute ); }
			set { SetValue( (int)attribute, value ); }
		}

		public override string ToString()
		{
			return "...";
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int Physical { get { return this[AosElementAttribute.Physical]; } set { this[AosElementAttribute.Physical] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int Fire { get { return this[AosElementAttribute.Fire]; } set { this[AosElementAttribute.Fire] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int Cold { get { return this[AosElementAttribute.Cold]; } set { this[AosElementAttribute.Cold] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int Poison { get { return this[AosElementAttribute.Poison]; } set { this[AosElementAttribute.Poison] = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int Energy { get { return this[AosElementAttribute.Energy]; } set { this[AosElementAttribute.Energy] = value; } }
	}

	[PropertyObject]
	public abstract class BaseAttributes
	{
		private Item m_Owner;
		private uint m_Names;
		private int[] m_Values;

		private static int[] m_Empty = new int[0];

		public bool IsEmpty { get { return (m_Names == 0); } }
		public Item Owner { get { return m_Owner; } }

		public BaseAttributes( Item owner )
		{
			m_Owner = owner;
			m_Values = m_Empty;
		}

		public BaseAttributes( Item owner, GenericReader reader )
		{
			m_Owner = owner;

			int version = reader.ReadByte();

			switch( version )
			{
				case 1:
				{
					m_Names = reader.ReadUInt();
					m_Values = new int[reader.ReadEncodedInt()];

					for( int i = 0; i < m_Values.Length; ++i )
						m_Values[i] = reader.ReadEncodedInt();

					break;
				}
				case 0:
				{
					m_Names = reader.ReadUInt();
					m_Values = new int[reader.ReadInt()];

					for( int i = 0; i < m_Values.Length; ++i )
						m_Values[i] = reader.ReadInt();

					break;
				}
			}
		}

		public void Serialize( GenericWriter writer )
		{
			writer.Write( (byte)1 ); // version;

			writer.Write( (uint)m_Names );
			writer.WriteEncodedInt( (int)m_Values.Length );

			for( int i = 0; i < m_Values.Length; ++i )
				writer.WriteEncodedInt( (int)m_Values[i] );
		}

		public int GetValue( int bitmask )
		{
			if( !Core.AOS )
				return 0;

			uint mask = (uint)bitmask;

			if( (m_Names & mask) == 0 )
				return 0;

			int index = GetIndex( mask );

			if( index >= 0 && index < m_Values.Length )
				return m_Values[index];

			return 0;
		}

		public void SetValue( int bitmask, int value )
		{
			if( (bitmask == (int)AosWeaponAttribute.DurabilityBonus) && (this is AosWeaponAttributes) )
			{
				if( m_Owner is BaseWeapon )
					((BaseWeapon)m_Owner).UnscaleDurability();
			}
			else if( (bitmask == (int)AosArmorAttribute.DurabilityBonus) && (this is AosArmorAttributes) )
			{
				if( m_Owner is BaseArmor )
					((BaseArmor)m_Owner).UnscaleDurability();
			}

			uint mask = (uint)bitmask;

			if( value != 0 )
			{
				if( (m_Names & mask) != 0 )
				{
					int index = GetIndex( mask );

					if( index >= 0 && index < m_Values.Length )
						m_Values[index] = value;
				}
				else
				{
					int index = GetIndex( mask );

					if( index >= 0 && index <= m_Values.Length )
					{
						int[] old = m_Values;
						m_Values = new int[old.Length + 1];

						for( int i = 0; i < index; ++i )
							m_Values[i] = old[i];

						m_Values[index] = value;

						for( int i = index; i < old.Length; ++i )
							m_Values[i + 1] = old[i];

						m_Names |= mask;
					}
				}
			}
			else if( (m_Names & mask) != 0 )
			{
				int index = GetIndex( mask );

				if( index >= 0 && index < m_Values.Length )
				{
					m_Names &= ~mask;

					if( m_Values.Length == 1 )
					{
						m_Values = m_Empty;
					}
					else
					{
						int[] old = m_Values;
						m_Values = new int[old.Length - 1];

						for( int i = 0; i < index; ++i )
							m_Values[i] = old[i];

						for( int i = index + 1; i < old.Length; ++i )
							m_Values[i - 1] = old[i];
					}
				}
			}

			if( (bitmask == (int)AosWeaponAttribute.DurabilityBonus) && (this is AosWeaponAttributes) )
			{
				if( m_Owner is BaseWeapon )
					((BaseWeapon)m_Owner).ScaleDurability();
			}
			else if( (bitmask == (int)AosArmorAttribute.DurabilityBonus) && (this is AosArmorAttributes) )
			{
				if( m_Owner is BaseArmor )
					((BaseArmor)m_Owner).ScaleDurability();
			}

			if( m_Owner.Parent is Mobile )
			{
				Mobile m = (Mobile)m_Owner.Parent;

				m.CheckStatTimers();
				m.UpdateResistances();
				m.Delta( MobileDelta.Stat | MobileDelta.WeaponDamage | MobileDelta.Hits | MobileDelta.Stam | MobileDelta.Mana );

				if( this is AosSkillBonuses )
				{
					((AosSkillBonuses)this).Remove();
					((AosSkillBonuses)this).AddTo( m );
				}
			}

			m_Owner.InvalidateProperties();
		}

		private int GetIndex( uint mask )
		{
			int index = 0;
			uint ourNames = m_Names;
			uint currentBit = 1;

			while( currentBit != mask )
			{
				if( (ourNames & currentBit) != 0 )
					++index;

				if( currentBit == 0x80000000 )
					return -1;

				currentBit <<= 1;
			}

			return index;
		}
	}
}