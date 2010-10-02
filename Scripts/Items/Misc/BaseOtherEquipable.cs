using System;
using System.Collections;
using System.Collections.Generic;
using Server.Network;
using Server.Engines.Craft;
using Server.Factions;
using AMA = Server.Items.ArmorMeditationAllowance;
using AMT = Server.Items.ArmorMaterialType;
using ABT = Server.Items.ArmorBodyType;


namespace Server.Items
{
	public abstract class BaseOtherEquipable : Item, ICraftable, IAosAttributes
	{
		private Mobile m_Crafter;
		private CraftQuality m_Quality;
		private CraftResource m_Resource;
		private bool m_PlayerConstructed;
		
		private AosAttributes m_AosAttributes;
		private AosArmorAttributes m_AosArmorAttributes;
		private AosSkillBonuses m_AosSkillBonuses;
		
		private BonusAttribute[] m_BonusRandomAttributes;
		private double m_OriginalWeight;
		
		private int m_StrReq = -1, m_DexReq = -1, m_IntReq = -1;

		public virtual bool AllowMaleWearer{ get{ return true; } }
		public virtual bool AllowFemaleWearer{ get{ return true; } }
		
		public virtual Race RequiredRace { get { return null; } }
		
		public virtual int AosStrReq{ get{ return 0; } }
		public virtual int AosDexReq{ get{ return 0; } }
		public virtual int AosIntReq{ get{ return 0; } }
		
		public virtual int OldStrReq{ get{ return 0; } }
		public virtual int OldDexReq{ get{ return 0; } }
		public virtual int OldIntReq{ get{ return 0; } }
		
		public virtual CraftResource DefaultResource{ get{ return CraftResource.RegularWood; } }
		
		public override void OnAfterDuped( Item newItem )
		{
			BaseOtherEquipable otherEquip = newItem as BaseOtherEquipable;

			if ( otherEquip == null )
				return;

			otherEquip.m_AosAttributes = new AosAttributes( newItem, m_AosAttributes );
			otherEquip.m_AosArmorAttributes = new AosArmorAttributes( newItem, m_AosArmorAttributes );
			otherEquip.m_AosSkillBonuses = new AosSkillBonuses( newItem, m_AosSkillBonuses );
			otherEquip.m_BonusRandomAttributes = m_BonusRandomAttributes;
		}
		
		#region Getters/Setters
		[CommandProperty( AccessLevel.GameMaster )]
		public bool PlayerConstructed
		{
			get{ return m_PlayerConstructed; }
			set{ m_PlayerConstructed = value; }
		}
		
		[CommandProperty( AccessLevel.GameMaster )]
		public int StrRequirement
		{
			get{ return ( m_StrReq == -1 ? Core.AOS ? AosStrReq : OldStrReq : m_StrReq ); }
			set{ m_StrReq = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int DexRequirement
		{
			get{ return ( m_DexReq == -1 ? Core.AOS ? AosDexReq : OldDexReq : m_DexReq ); }
			set{ m_DexReq = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int IntRequirement
		{
			get{ return ( m_IntReq == -1 ? Core.AOS ? AosIntReq : OldIntReq : m_IntReq ); }
			set{ m_IntReq = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public CraftResource Resource
		{
			get
			{
				return m_Resource;
			}
			set
			{
				if ( m_Resource != value )
				{
					if ( Core.AOS && !CraftResources.IsStandard( m_Resource ) )
					{
						BonusAttributesHelper.RemoveAttrsFrom( this, GetResourceBonusAttrs() );
						BonusAttributesHelper.RemoveAttrsFrom( this, RandomAttributes );
						RandomAttributes = null;
					}
					
					m_Resource = value;
					Hue = CraftResources.GetHue( m_Resource );
					
					if ( Core.AOS && !CraftResources.IsStandard( m_Resource ) )
					{
						// Enhance sets RandomAttributes so check if it's null/empty first before getting new random attributes.
						if ( RandomAttributes == null || RandomAttributes.Length == 0 )
							RandomAttributes = BonusAttributesHelper.GetRandomAttributes( GetResourceRandomAttrs(), GetResourceAttrs().RandomAttributeCount );
					
						BonusAttributesHelper.ApplyAttributesTo( this, GetResourceBonusAttrs() );
						BonusAttributesHelper.ApplyAttributesTo( this, RandomAttributes );
					}

					InvalidateProperties();
				}
			}
		}
		
		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile Crafter
		{
			get{ return m_Crafter; }
			set{ m_Crafter = value; InvalidateProperties(); }
		}
		
		[CommandProperty( AccessLevel.GameMaster )]
		public CraftQuality Quality
		{
			get{ return m_Quality; }
			set{ m_Quality = value; InvalidateProperties(); }
		}
		
		[CommandProperty( AccessLevel.GameMaster )]
		public AosAttributes Attributes
		{
			get{ return m_AosAttributes; }
			set{}
		}
		
		[CommandProperty( AccessLevel.GameMaster )]
		public AosArmorAttributes ArmorAttributes
		{
			get{ return m_AosArmorAttributes; }
			set{}
		}
		
		public BonusAttribute[] RandomAttributes
		{
			get { return m_BonusRandomAttributes; }
			set { m_BonusRandomAttributes = value; }
		}
		
		#endregion
		
		#region Old Item Serialization Vars
		/* Only used in serialization of other equipables that originally derived from Item */
		private bool m_InheritsItem;
		private int m_OldVersion;
		
		protected bool InheritsItem
		{
			get{ return m_InheritsItem; }
		}
		
		protected int OldVersion
		{
			get{ return m_OldVersion; }
		}
		#endregion
		
		public CraftAttributeInfo GetResourceAttrs()
		{
			CraftResourceInfo info = CraftResources.GetInfo( m_Resource );

			if ( info == null )
				return CraftAttributeInfo.Blank;

			return info.AttributeInfo;
		}
		
		public BonusAttribute[] GetResourceBonusAttrs()
		{
			CraftAttributeInfo attrInfo = GetResourceAttrs();
			
			return attrInfo.OtherAttributes;
		}
		
		public BonusAttribute[] GetResourceRandomAttrs()
		{
			CraftAttributeInfo attrInfo = GetResourceAttrs();
			
			return attrInfo.OtherRandomAttributes;
		}
		
		public int ComputeStatReq( StatType type )
		{
			int v;

			if ( type == StatType.Str )
				v = StrRequirement;
			else if ( type == StatType.Dex )
				v = DexRequirement;
			else
				v = IntRequirement;

			return AOS.Scale( v, 100 - GetLowerStatReq() );
		}
		
		
		public BaseOtherEquipable( int itemID ) : base( itemID )
		{
			m_Quality = CraftQuality.Regular;
			Resource = DefaultResource;
			
			m_AosAttributes = new AosAttributes( this );
			m_AosArmorAttributes = new AosArmorAttributes( this );
			m_AosSkillBonuses = new AosSkillBonuses( this );
			
			m_OriginalWeight = Weight;
		}
		
		public BaseOtherEquipable( Serial serial ) :  base( serial )
		{
		}
		
		#region Serialization/Deserialization
		private static void SetSaveFlag( ref SaveFlag flags, SaveFlag toSet, bool setIf )
		{
			if ( setIf )
				flags |= toSet;
		}

		private static bool GetSaveFlag( SaveFlag flags, SaveFlag toGet )
		{
			return ( (flags & toGet) != 0 );
		}

		[Flags]
		private enum SaveFlag
		{
			None					= 0x00000000,
			Attributes				= 0x00000001,
			ArmorAttributes			= 0x00000002,
			Crafter					= 0x00000004,
			Quality					= 0x00000008,
			Resource				= 0x00000010,
			StrReq					= 0x00000020,
			DexReq					= 0x00000040,
			IntReq					= 0x00000080,
			SkillBonuses			= 0x00000100,
			PlayerConstructed		= 0x00000200,
			BonusRandomAttributes	= 0x00000400
		}
		
				public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version

			SaveFlag flags = SaveFlag.None;

			SetSaveFlag( ref flags, SaveFlag.Attributes,			!m_AosAttributes.IsEmpty );
			SetSaveFlag( ref flags, SaveFlag.ArmorAttributes,		!m_AosArmorAttributes.IsEmpty );
			SetSaveFlag( ref flags, SaveFlag.Crafter,				m_Crafter != null );
			SetSaveFlag( ref flags, SaveFlag.Quality,				m_Quality != CraftQuality.Regular );
			SetSaveFlag( ref flags, SaveFlag.Resource,				m_Resource != DefaultResource );
			SetSaveFlag( ref flags, SaveFlag.StrReq,				m_StrReq != -1 );
			SetSaveFlag( ref flags, SaveFlag.DexReq,				m_DexReq != -1 );
			SetSaveFlag( ref flags, SaveFlag.IntReq,				m_IntReq != -1 );
			SetSaveFlag( ref flags, SaveFlag.SkillBonuses,			!m_AosSkillBonuses.IsEmpty );
			SetSaveFlag( ref flags, SaveFlag.PlayerConstructed,		m_PlayerConstructed != false );
			SetSaveFlag( ref flags, SaveFlag.BonusRandomAttributes,	m_BonusRandomAttributes != null && m_BonusRandomAttributes.Length > 0 );

			writer.WriteEncodedInt( (int) flags );

			if ( GetSaveFlag( flags, SaveFlag.Attributes ) )
				m_AosAttributes.Serialize( writer );
			
			if ( GetSaveFlag( flags, SaveFlag.ArmorAttributes ) )
				m_AosArmorAttributes.Serialize( writer );

			if ( GetSaveFlag( flags, SaveFlag.Crafter ) )
				writer.Write( (Mobile) m_Crafter );

			if ( GetSaveFlag( flags, SaveFlag.Quality ) )
				writer.WriteEncodedInt( (int) m_Quality );

			if ( GetSaveFlag( flags, SaveFlag.Resource ) )
				writer.WriteEncodedInt( (int) m_Resource );

			if ( GetSaveFlag( flags, SaveFlag.StrReq ) )
				writer.WriteEncodedInt( (int) m_StrReq );

			if ( GetSaveFlag( flags, SaveFlag.DexReq ) )
				writer.WriteEncodedInt( (int) m_DexReq );

			if ( GetSaveFlag( flags, SaveFlag.IntReq ) )
				writer.WriteEncodedInt( (int) m_IntReq );

			if ( GetSaveFlag( flags, SaveFlag.SkillBonuses ) )
				m_AosSkillBonuses.Serialize( writer );
			
			if ( GetSaveFlag( flags, SaveFlag.BonusRandomAttributes ) )
			{
				writer.Write( m_BonusRandomAttributes.Length );
				for ( int i = 0; i < m_BonusRandomAttributes.Length; i++ )
					m_BonusRandomAttributes[i].Serialize( writer );
			}
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				{
					SaveFlag flags = (SaveFlag)reader.ReadEncodedInt();
					
					if ( GetSaveFlag( flags, SaveFlag.Attributes ) )
						m_AosAttributes = new AosAttributes( this, reader );
					else
						m_AosAttributes = new AosAttributes( this );
					
					if ( GetSaveFlag( flags, SaveFlag.ArmorAttributes ) )
						m_AosArmorAttributes = new AosArmorAttributes( this, reader );
					else
						m_AosArmorAttributes = new AosArmorAttributes( this );

					if ( GetSaveFlag( flags, SaveFlag.Crafter ) )
						m_Crafter = reader.ReadMobile();

					if ( GetSaveFlag( flags, SaveFlag.Quality ) )
						m_Quality = (CraftQuality)reader.ReadEncodedInt();
					else
						m_Quality = CraftQuality.Regular;

					if ( GetSaveFlag( flags, SaveFlag.Resource ) )
						m_Resource = (CraftResource)reader.ReadEncodedInt();
					else
						m_Resource = DefaultResource;

					if ( m_Resource == CraftResource.None )
						m_Resource = DefaultResource;

					if ( GetSaveFlag( flags, SaveFlag.StrReq ) )
						m_StrReq = reader.ReadEncodedInt();
					else
						m_StrReq = -1;

					if ( GetSaveFlag( flags, SaveFlag.DexReq ) )
						m_DexReq = reader.ReadEncodedInt();
					else
						m_DexReq = -1;

					if ( GetSaveFlag( flags, SaveFlag.IntReq ) )
						m_IntReq = reader.ReadEncodedInt();
					else
						m_IntReq = -1;

					if ( GetSaveFlag( flags, SaveFlag.SkillBonuses ) )
						m_AosSkillBonuses = new AosSkillBonuses( this, reader );

					if ( GetSaveFlag( flags, SaveFlag.PlayerConstructed ) )
						m_PlayerConstructed = true;
					
					if ( GetSaveFlag( flags, SaveFlag.BonusRandomAttributes ) )
					{
						m_BonusRandomAttributes = new BonusAttribute[reader.ReadInt()];
						
						for( int i = 0; i < m_BonusRandomAttributes.Length; i++ )
							m_BonusRandomAttributes[i] =  new BonusAttribute( reader );
					}

					break;
				}
				case 0:
				{
					m_InheritsItem = true;
					m_OldVersion = version;
					m_AosAttributes = new AosAttributes( this );
					m_AosArmorAttributes = new AosArmorAttributes( this );
					m_Quality = CraftQuality.Regular;
					m_Resource = DefaultResource;
					m_PlayerConstructed = true;
					break;
				}
			}

			if ( m_AosSkillBonuses == null )
				m_AosSkillBonuses = new AosSkillBonuses( this );

			if ( Core.AOS && Parent is Mobile )
				m_AosSkillBonuses.AddTo( (Mobile)Parent );
			
			if ( Parent is Mobile )
				((Mobile)Parent).CheckStatTimers();
		}
		#endregion
		
		public int GetLowerStatReq()
		{
			if ( !Core.AOS )
				return 0;

			int v = m_AosArmorAttributes.LowerStatReq;

			if ( v > 100 )
				v = 100;

			return v;
		}
		
		public void ModifyWeight()
		{
			double v = 0;
		
			v = 100 - GetWeightBonus();
				
			Weight = m_OriginalWeight * (v / 100);
		}
		
		public int GetWeightBonus()
		{
			CraftAttributeInfo attrInfo = GetResourceAttrs();
			
			int v = Attributes.LowerWeight;
				
			if ( v > 100 )
				v = 100;
			
			return v;	
		}

		public override bool CanEquip( Mobile from )
		{
			if( !Ethics.Ethic.CheckEquip( from, this ) )
				return false;

			if( from.AccessLevel < AccessLevel.GameMaster )
			{
				if( RequiredRace != null && from.Race != RequiredRace )
				{
					if( RequiredRace == Race.Elf )
						from.SendLocalizedMessage( 1072203 ); // Only Elves may use this.
					else
						from.SendMessage( "Only {0} may use this.", RequiredRace.PluralName );

					return false;
				}
				else if( !AllowMaleWearer && !from.Female )
				{
					if( AllowFemaleWearer )
						from.SendLocalizedMessage( 1010388 ); // Only females can wear this.
					else
						from.SendMessage( "You may not wear this." );

					return false;
				}
				else if( !AllowFemaleWearer && from.Female )
				{
					if( AllowMaleWearer )
						from.SendLocalizedMessage( 1063343 ); // Only males can wear this.
					else
						from.SendMessage( "You may not wear this." );

					return false;
				}
				else
				{
					int strReq = ComputeStatReq( StatType.Str );
					int dexReq = ComputeStatReq( StatType.Dex );
					int intReq = ComputeStatReq( StatType.Int );

					if( from.Dex < dexReq )
					{
						from.SendLocalizedMessage( 502077 ); // You do not have enough dexterity to equip this item.
						return false;
					}
					else if( from.Str < strReq )
					{
						from.SendLocalizedMessage( 500213 ); // You are not strong enough to equip that.
						return false;
					}
					else if( from.Int < intReq )
					{
						from.SendMessage( "You are not smart enough to equip that." );
						return false;
					}
				}
			}

			return base.CanEquip( from );
		}
		
		public override bool OnEquip( Mobile from )
		{
			from.CheckStatTimers();

			int strBonus = m_AosAttributes.BonusStr;
			int dexBonus = m_AosAttributes.BonusDex;
			int intBonus = m_AosAttributes.BonusInt;

			if ( strBonus != 0 || dexBonus != 0 || intBonus != 0 )
			{
				string modName = this.Serial.ToString();

				if ( strBonus != 0 )
					from.AddStatMod( new StatMod( StatType.Str, modName + "Str", strBonus, TimeSpan.Zero ) );

				if ( dexBonus != 0 )
					from.AddStatMod( new StatMod( StatType.Dex, modName + "Dex", dexBonus, TimeSpan.Zero ) );

				if ( intBonus != 0 )
					from.AddStatMod( new StatMod( StatType.Int, modName + "Int", intBonus, TimeSpan.Zero ) );
			}

			return base.OnEquip( from );
		}
		
		public override void OnAdded( object parent )
		{
			if ( parent is Mobile )
			{
				Mobile from = (Mobile)parent;

				if ( Core.AOS )
					m_AosSkillBonuses.AddTo( from );
			}
		}
		
		public override void OnRemoved( object parent )
		{
			if ( parent is Mobile )
			{
				Mobile m = (Mobile)parent;
				string modName = this.Serial.ToString();

				m.RemoveStatMod( modName + "Str" );
				m.RemoveStatMod( modName + "Dex" );
				m.RemoveStatMod( modName + "Int" );

				if ( Core.AOS )
					m_AosSkillBonuses.Remove();
				
				m.CheckStatTimers();
			}

			base.OnRemoved( parent );
		}
		
		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( m_Crafter != null )
				list.Add( 1050043, m_Crafter.Name ); // crafted by ~1_NAME~
			
			if ( m_Quality == CraftQuality.Exceptional )
				list.Add( 1060636 ); // exceptional

			if ( m_AosSkillBonuses != null )
				m_AosSkillBonuses.GetProperties( list );

			int prop;
			
			if ( ( m_Resource >= CraftResource.OakWood && m_Resource <= CraftResource.Frostwood ) && Hue == CraftResources.GetHue( m_Resource ) )
				list.Add( CraftResources.GetLocalizationNumber( m_Resource ) );

			if ( (prop = m_AosAttributes.WeaponDamage) != 0 )
				list.Add( 1060401, prop.ToString() ); // damage increase ~1_val~%

			if ( (prop = m_AosAttributes.DefendChance) != 0 )
				list.Add( 1060408, prop.ToString() ); // defense chance increase ~1_val~%

			if ( (prop = m_AosAttributes.BonusDex ) != 0 )
				list.Add( 1060409, prop.ToString() ); // dexterity bonus ~1_val~

			if ( (prop = m_AosAttributes.EnhancePotions) != 0 )
				list.Add( 1060411, prop.ToString() ); // enhance potions ~1_val~%

			if ( (prop = m_AosAttributes.CastRecovery) != 0 )
				list.Add( 1060412, prop.ToString() ); // faster cast recovery ~1_val~

			if ( (prop = m_AosAttributes.CastSpeed) != 0 )
				list.Add( 1060413, prop.ToString() ); // faster casting ~1_val~

			if ( (prop = m_AosAttributes.AttackChance) != 0 )
				list.Add( 1060415, prop.ToString() ); // hit chance increase ~1_val~%

			if ( (prop = m_AosAttributes.BonusHits) != 0 )
				list.Add( 1060431, prop.ToString() ); // hit point increase ~1_val~

			if ( (prop = m_AosAttributes.BonusInt) != 0 )
				list.Add( 1060432, prop.ToString() ); // intelligence bonus ~1_val~

			if ( (prop = m_AosAttributes.LowerManaCost) != 0 )
				list.Add( 1060433, prop.ToString() ); // lower mana cost ~1_val~%

			if ( (prop = m_AosAttributes.LowerRegCost) != 0 )
				list.Add( 1060434, prop.ToString() ); // lower reagent cost ~1_val~%
			
			if ( (prop = GetLowerStatReq()) != 0 )
				list.Add( 1060435, prop.ToString() ); // lower requirements ~1_val~%

			if ( (prop = m_AosAttributes.Luck) != 0 )
				list.Add( 1060436, prop.ToString() ); // luck ~1_val~

			if ( (prop = m_AosAttributes.BonusMana) != 0 )
				list.Add( 1060439, prop.ToString() ); // mana increase ~1_val~

			if ( (prop = m_AosAttributes.RegenMana) != 0 )
				list.Add( 1060440, prop.ToString() ); // mana regeneration ~1_val~

			if ( (prop = m_AosAttributes.NightSight) != 0 )
				list.Add( 1060441 ); // night sight

			if ( (prop = m_AosAttributes.ReflectPhysical) != 0 )
				list.Add( 1060442, prop.ToString() ); // reflect physical damage ~1_val~%

			if ( (prop = m_AosAttributes.RegenStam) != 0 )
				list.Add( 1060443, prop.ToString() ); // stamina regeneration ~1_val~

			if ( (prop = m_AosAttributes.RegenHits) != 0 )
				list.Add( 1060444, prop.ToString() ); // hit point regeneration ~1_val~

			if ( (prop = m_AosAttributes.SpellChanneling) != 0 )
				list.Add( 1060482 ); // spell channeling

			if ( (prop = m_AosAttributes.SpellDamage) != 0 )
				list.Add( 1060483, prop.ToString() ); // spell damage increase ~1_val~%

			if ( (prop = m_AosAttributes.BonusStam) != 0 )
				list.Add( 1060484, prop.ToString() ); // stamina increase ~1_val~

			if ( (prop = m_AosAttributes.BonusStr) != 0 )
				list.Add( 1060485, prop.ToString() ); // strength bonus ~1_val~

			if ( (prop = m_AosAttributes.WeaponSpeed) != 0 )
				list.Add( 1060486, prop.ToString() ); // swing speed increase ~1_val~%

			if ( (prop = ComputeStatReq( StatType.Str )) > 0 )
				list.Add( 1061170, prop.ToString() ); // strength requirement ~1_val~
		}
		
		#region ICraftable Members

		public virtual int OnCraft( int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue )
		{
			if ( Core.ML )
			{
				Quality = (CraftQuality)quality;

				if ( makersMark )
					Crafter = from;
			
				Type resourceType = typeRes;

				if ( resourceType == null )
					resourceType = craftItem.Resources.GetAt( 0 ).ItemType;

				Resource = CraftResources.GetFromType( resourceType );
				PlayerConstructed = true;

				CraftContext context = craftSystem.GetContext( from );

				if ( context != null && context.DoNotColor )
					Hue = 0;
			}
			
			return quality;
		}
		#endregion
	}
}
