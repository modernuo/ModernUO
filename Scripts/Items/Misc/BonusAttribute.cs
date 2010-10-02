using System;
using System.Collections;
using System.Collections.Generic;
using Server.Items;

namespace Server
{
	public static class BonusAttributesHelper
	{
		#region Random Attributes
		
		/// <summary>
		/// <para>Returns a <see cref="BonusAttribute"/> array populated with one unique random attribute 
		/// chosen from '<paramref name="randAttrs"/>' array -or- returns null if a null or empty array is supplied.</para>
		/// </summary>
		public static BonusAttribute[] GetRandomAttributes( BonusAttribute[] randAttrs )
		{
			return GetRandomAttributes( randAttrs, 1 );
		}
		
		/// <summary>
		/// <para>Returns a <see cref="BonusAttribute"/> array populated with '<paramref name="numOfAttr"/>' unique random attributes 
		/// chosen from '<paramref name="randAttrs"/>' array -or- returns null if a null or empty array is supplied.</para>
		/// </summary>
		public static BonusAttribute[] GetRandomAttributes( BonusAttribute[] randAttrs, int numOfAttr )
		{
			BonusAttribute[] toAttrs = null;
			
			if ( randAttrs != null && randAttrs.Length > 0 && numOfAttr > 0)
			{
				toAttrs = new BonusAttribute[numOfAttr];
					
				int avail = 0;
				
				List<int> numList = new List<int>();
				
				for ( int i = 0; i < randAttrs.Length; ++i )
					numList.Add( avail++ );
				
				if ( numOfAttr > randAttrs.Length )
					numOfAttr = randAttrs.Length;
				
				int v;
				
				for ( int i = 0; i < numOfAttr; ++i )
				{				
					v = Utility.Random(avail--);
					
					toAttrs[i] = randAttrs[numList[v]];
						
					numList.RemoveAt(v);
				}
			}
			
			return toAttrs;
		}
		#endregion
		
		#region Apply Attributes
		public static void ApplyAttributesTo( Item item, BonusAttribute[] attributes )
		{
			if ( attributes != null && attributes.Length > 0 )
			{
				int cold = 0, nrgy = 0, fire = 0, pois = 0, chaos = 0, direct = 0;

				for ( int i = 0; i < attributes.Length; i++ )
				{
					if ( attributes[i].Attribute == null )
						continue;
					
					Type attrType = attributes[i].Attribute.GetType();

					if ( attrType == typeof( AosAttribute ) )
					{
						if ( item is IAosAttributes )
							ApplyAttribute( ((IAosAttributes)item).Attributes, (AosAttribute)attributes[i].Attribute, attributes[i].Amount );
					}
					else if ( attrType == typeof( AosArmorAttribute ) )
					{ 
						if ( (AosArmorAttribute)attributes[i].Attribute == AosArmorAttribute.DurabilityBonus )
							ApplyDurability( item, attributes[i].Amount );
						else if ( item is BaseArmor )
							ApplyAttribute( ((BaseArmor)item).ArmorAttributes, (AosArmorAttribute)attributes[i].Attribute, attributes[i].Amount );
						else if ( item is BaseOtherEquipable )
							ApplyAttribute( ((BaseOtherEquipable)item).ArmorAttributes, (AosArmorAttribute)attributes[i].Attribute, attributes[i].Amount );
					}
					else if ( attrType == typeof( AosWeaponAttribute ) )
					{
						if ( (AosWeaponAttribute)attributes[i].Attribute == AosWeaponAttribute.DurabilityBonus )
							ApplyDurability( item, attributes[i].Amount );
						else if ( item is BaseWeapon )
							ApplyAttribute( ((BaseWeapon)item).WeaponAttributes, (AosWeaponAttribute)attributes[i].Attribute, attributes[i].Amount );
					}
					else if ( attrType == typeof( AosElementAttribute ) )
					{
						if ( item is BaseWeapon )
						{
							switch ( (AosElementAttribute)attributes[i].Attribute )
							{
								case AosElementAttribute.Cold: 		cold = attributes[i].Amount; break;
								case AosElementAttribute.Energy:	nrgy = attributes[i].Amount; break;
								case AosElementAttribute.Fire:		fire = attributes[i].Amount; break;
								case AosElementAttribute.Poison:	pois = attributes[i].Amount; break;
								case AosElementAttribute.Chaos:		chaos = attributes[i].Amount; break;
								case AosElementAttribute.Direct:	direct = attributes[i].Amount; break;
							}
						}
					}
					else if ( attrType == typeof( ResistanceType ) )
					{
						if ( item is BaseArmor )
							ApplyResistance( (BaseArmor)item, (ResistanceType)attributes[i].Attribute, attributes[i].Amount );
					}
				}
				
				if ( item is BaseWeapon )
					ApplyElementDamages( (BaseWeapon)item, cold, nrgy, fire, pois, chaos, direct );
			}
		}
		
		public static void ApplyAttribute( AosAttributes attrs, AosAttribute attr, int amount )
		{
			if ( attr == AosAttribute.SpellChanneling && attrs.SpellChanneling == 0 )
			{
				attrs[attr] = 1;
				attrs.CastSpeed--;
			}
			else
				attrs[attr] += amount;
		}
		
		public static void ApplyAttribute( AosArmorAttributes attrs, AosArmorAttribute attr, int amount )
		{
			if ( attr == AosArmorAttribute.MageArmor && attrs.MageArmor == 0 )
				attrs[attr] = 1;
			else
				attrs[attr] += amount;
		}
		
		public static void ApplyAttribute( AosWeaponAttributes attrs, AosWeaponAttribute attr, int amount )
		{
			attrs[attr] += amount;
		}
		
		public static void ApplyAttribute( AosElementAttributes attrs, AosElementAttribute attr, int amount )
		{
			attrs[attr] += amount;
		}
		
		public static void ApplyElementDamages( BaseWeapon weapon, int cold, int nrgy, int fire, int pois, int chaos, int direct )
		{
			
			int weapPhys, weapCold, weapNrgy, weapFire, weapPois, weapChaos, weapDirect;
			
			weapon.GetDamageTypes( null, out weapPhys, out weapFire, out weapCold, out weapPois, out weapNrgy, out weapChaos, out weapDirect );
			
			weapPhys = weapon.ApplyAttributeElementDamage( cold, ref weapCold, weapPhys );
			weapPhys = weapon.ApplyAttributeElementDamage( nrgy, ref weapNrgy, weapPhys );
			weapPhys = weapon.ApplyAttributeElementDamage( fire, ref weapFire, weapPhys );
			weapPhys = weapon.ApplyAttributeElementDamage( pois, ref weapPois, weapPhys );
			weapPhys = weapon.ApplyAttributeElementDamage( chaos, ref weapChaos, weapPhys );
			weapPhys = weapon.ApplyAttributeElementDamage( direct, ref weapDirect, weapPhys );
			
			weapon.AosElementDamages.Physical = weapPhys;
			weapon.AosElementDamages.Cold = weapCold;
			weapon.AosElementDamages.Energy = weapNrgy;
			weapon.AosElementDamages.Fire = weapFire;
			weapon.AosElementDamages.Poison = weapPois;
			weapon.AosElementDamages.Chaos = weapChaos;
			weapon.AosElementDamages.Direct = weapDirect;
		}
		
		public static void ApplyResistance( BaseArmor ar, ResistanceType res, int amount )
		{
			switch ( res )
			{
				case ResistanceType.Physical: ar.PhysicalBonus += amount; break;
				case ResistanceType.Fire: ar.FireBonus += amount; break;
				case ResistanceType.Cold: ar.ColdBonus += amount; break;
				case ResistanceType.Poison: ar.PoisonBonus += amount; break;
				case ResistanceType.Energy: ar.EnergyBonus += amount; break;
			}
		}
		
		public static void ApplyDurability( Item item, int amount )
		{
			if ( item is IDurability )
			{
				IDurability durableItem = (IDurability)item;
								 
				durableItem.MaxHitPoints = ( ( 100 + amount ) * durableItem.MaxHitPoints ) / 100;
								
				if ( durableItem.MaxHitPoints > 255 )
					durableItem.MaxHitPoints = 255;
								
				durableItem.HitPoints = durableItem.MaxHitPoints; // Item is repaired upon enhancing
			}
		}
		#endregion
		
		#region Remove Attributes
		public static void RemoveAttrsFrom( Item item, BonusAttribute[] attributes )
		{
			if ( attributes != null && attributes.Length > 0 )
			{
				int cold = 0, nrgy = 0, fire = 0, pois = 0, chaos = 0, direct = 0;
				
				for ( int i = 0; i < attributes.Length; i++ )
				{
					if ( attributes[i].Attribute == null )
						continue;
					
					Type attrType = attributes[i].Attribute.GetType();

					if ( attrType == typeof( AosAttribute ) )
					{
						if ( item is IAosAttributes )
							RemoveAttribute( ((IAosAttributes)item).Attributes, (AosAttribute)attributes[i].Attribute, attributes[i].Amount );
					}
					else if ( attrType == typeof( AosArmorAttribute ) )
					{
						if ( (AosArmorAttribute)attributes[i].Attribute == AosArmorAttribute.DurabilityBonus )
							RemoveDurability( item, attributes[i].Amount );
						else if ( item is BaseArmor )
							RemoveAttribute( ((BaseArmor)item).ArmorAttributes, (AosArmorAttribute)attributes[i].Attribute, attributes[i].Amount );
						else if ( item is BaseOtherEquipable )
							RemoveAttribute( ((BaseOtherEquipable)item).ArmorAttributes, (AosArmorAttribute)attributes[i].Attribute, attributes[i].Amount );
					}
					else if ( attrType == typeof( AosWeaponAttribute ) )
					{
						if ( (AosWeaponAttribute)attributes[i].Attribute == AosWeaponAttribute.DurabilityBonus )
							RemoveDurability( item, attributes[i].Amount );
						else if ( item is BaseWeapon )
							RemoveAttribute( ((BaseWeapon)item).WeaponAttributes, (AosWeaponAttribute)attributes[i].Attribute, attributes[i].Amount );
					}
					else if ( attrType == typeof( AosElementAttribute ) )
					{
						if ( item is BaseWeapon )
						{
							switch ( (AosElementAttribute)attributes[i].Attribute )
							{
								case AosElementAttribute.Cold: 		cold = attributes[i].Amount; break;
								case AosElementAttribute.Energy:	nrgy = attributes[i].Amount; break;
								case AosElementAttribute.Fire:		fire = attributes[i].Amount; break;
								case AosElementAttribute.Poison:	pois = attributes[i].Amount; break;
								case AosElementAttribute.Chaos:		chaos = attributes[i].Amount; break;
								case AosElementAttribute.Direct:	direct = attributes[i].Amount; break;
							}
						}
					}
					else if ( attrType == typeof( ResistanceType ) )
					{
						if ( item is BaseArmor )
							RemoveResistance( (BaseArmor)item, (ResistanceType)attributes[i].Attribute, attributes[i].Amount );
					}
				}
				
				if ( item is BaseWeapon )
					RemoveElementDamages( (BaseWeapon)item, cold, nrgy, fire, pois, chaos, direct );
			}
		}
		
		public static void RemoveAttribute( AosAttributes attrs, AosAttribute attr, int amount )
		{
			if ( attr == AosAttribute.SpellChanneling && attrs.SpellChanneling > 0 )
			{
				attrs[attr] = 0;
				attrs.CastSpeed++;
			}
			else
				attrs[attr] = Math.Max( attrs[attr] - amount, 0 );
		}
		
		public static void RemoveAttribute( AosArmorAttributes attrs, AosArmorAttribute attr, int amount )
		{
			if ( attr == AosArmorAttribute.MageArmor && attrs.MageArmor > 0 )
				attrs[attr] = 0;
			else
				attrs[attr] = Math.Max( attrs[attr] - amount, 0 );
		}
		
		public static void RemoveAttribute( AosWeaponAttributes attrs, AosWeaponAttribute attr, int amount )
		{
			attrs[attr] = Math.Max( attrs[attr] - amount, 0 );
		}
		
		public static void RemoveAttribute( AosElementAttributes attrs, AosElementAttribute attr, int amount )
		{
			attrs[attr] = Math.Max( attrs[attr] - amount, 0 );
		}
		
		public static void RemoveElementDamages( BaseWeapon weapon, int cold, int nrgy, int fire, int pois, int chaos, int direct )
		{
			
			int weapPhys, weapCold, weapNrgy, weapFire, weapPois, weapChaos, weapDirect;
			
			weapon.GetDamageTypes( null, out weapPhys, out weapFire, out weapCold, out weapPois, out weapNrgy, out weapChaos, out weapDirect );
			
			// Remove the element damages in reverse order they were applied in.
			
			weapPhys = weapon.RemoveAttributeElementDamage( direct, ref weapDirect, weapPhys );
			weapPhys = weapon.RemoveAttributeElementDamage( chaos, ref weapChaos, weapPhys );
			weapPhys = weapon.RemoveAttributeElementDamage( pois, ref weapPois, weapPhys );
			weapPhys = weapon.RemoveAttributeElementDamage( fire, ref weapFire, weapPhys );
			weapPhys = weapon.RemoveAttributeElementDamage( nrgy, ref weapNrgy, weapPhys );
			weapPhys = weapon.RemoveAttributeElementDamage( cold, ref weapCold, weapPhys );
			
			weapon.AosElementDamages.Physical = weapPhys;
			weapon.AosElementDamages.Cold = weapCold;
			weapon.AosElementDamages.Energy = weapNrgy;
			weapon.AosElementDamages.Fire = weapFire;
			weapon.AosElementDamages.Poison = weapPois;
			weapon.AosElementDamages.Chaos = weapChaos;
			weapon.AosElementDamages.Direct = weapDirect;
		}
		
		public static void RemoveResistance( BaseArmor ar, ResistanceType res, int amount )
		{
			switch ( res )
			{
				case ResistanceType.Physical: ar.PhysicalBonus = Math.Max( ar.PhysicalBonus - amount, 0 ); break;
				case ResistanceType.Fire: ar.FireBonus = Math.Max( ar.ColdBonus - amount, 0 ); break;
				case ResistanceType.Cold: ar.ColdBonus = Math.Max( ar.FireBonus - amount, 0 ); break;
				case ResistanceType.Poison: ar.PoisonBonus = Math.Max( ar.PoisonBonus - amount, 0 ); break;
				case ResistanceType.Energy: ar.EnergyBonus = Math.Max( ar.EnergyBonus - amount, 0 ); break;
			}
		}
		
		public static void RemoveDurability( Item item, int amount )
		{
			if ( item is IDurability )
			{
				IDurability durableItem = (IDurability)item;
								 
				durableItem.MaxHitPoints = ( ( 100 + amount ) * durableItem.MaxHitPoints ) / 100;
				durableItem.HitPoints = durableItem.MaxHitPoints;
			}
		}
		#endregion
	}

	public sealed class BonusAttribute
	{
		private object m_Attribute;
		private int m_Amount;
		
		public object Attribute{ get{ return m_Attribute; } }
		public int Amount{ get{ return m_Amount; } }
		
		#region Constructors
		public BonusAttribute( AosAttribute attr, int amount ) : this( (object)attr, amount )
		{
		}
		
		public BonusAttribute( AosArmorAttribute attr, int amount ) : this( (object)attr, amount )
		{
		}
		
		public BonusAttribute( AosWeaponAttribute attr, int amount ) : this( (object)attr, amount )
		{
		}
		
		public BonusAttribute( AosElementAttribute attr, int amount ) : this( (object)attr, amount )
		{
		}
		
		public BonusAttribute( ResistanceType attr, int amount ) : this( (object)attr, amount )
		{
		}
		
		private BonusAttribute( object attr, int amount )
		{
			m_Attribute = attr;
			m_Amount = amount;
		}
		
		public BonusAttribute( GenericReader reader )
		{
			Deserialize( reader );
		}
		#endregion
				
		public void Serialize( GenericWriter writer )
		{
			writer.Write( (int) 0 ); // version

			writer.Write( m_Attribute.GetType().ToString() ); // Enum type
			writer.Write( m_Attribute.ToString() ); // Enum value
			writer.Write( (int) m_Amount );
		}
		
		public void Deserialize( GenericReader reader )
		{
			int version = reader.ReadInt();
			
			switch ( version )
			{
				case 0: 
				{
					string type = reader.ReadString();
					string enumValue = reader.ReadString();
					
					try {
						m_Attribute = Enum.Parse( Type.GetType( type ), enumValue );
					}
					catch { 
						Console.WriteLine( "Unable to create Enum of type {0} with value of {1}.", type, enumValue );
					}
							
					m_Amount = reader.ReadInt();
					break;
				}
			}
		}
	}
}
