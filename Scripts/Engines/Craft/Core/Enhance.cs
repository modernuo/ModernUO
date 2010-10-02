using System;
using System.Collections.Generic;
using Server;
using Server.Targeting;
using Server.Items;

namespace Server.Engines.Craft
{
	public enum EnhanceResult
	{
		None,
		NotInBackpack,
		BadItem,
		BadResource,
		AlreadyEnhanced,
		Success,
		Failure,
		Broken,
		NoResources,
		NoSkill
	}

	public class Enhance
	{
		public static EnhanceResult Invoke( Mobile from, CraftSystem craftSystem, BaseTool tool, Item item, CraftResource resource, Type resType, ref object resMessage )
		{
			if ( item == null )
				return EnhanceResult.BadItem;

			if ( !item.IsChildOf( from.Backpack ) )
				return EnhanceResult.NotInBackpack;

			if ( !(item is BaseArmor) && !(item is BaseWeapon) && !(item is BaseOtherEquipable) )
				return EnhanceResult.BadItem;

			if ( item is IArcaneEquip )
			{
				IArcaneEquip eq = (IArcaneEquip)item;
				if ( eq.IsArcane )
					return EnhanceResult.BadItem;
			}

			if ( CraftResources.IsStandard( resource ) )
				return EnhanceResult.BadResource;
			
			int num = craftSystem.CanCraft( from, tool, item.GetType() );
			
			if ( num > 0 )
			{
				resMessage = num;
				return EnhanceResult.None;
			}

			CraftItem craftItem = craftSystem.CraftItems.SearchFor( item.GetType() );

			if ( craftItem == null || craftItem.Resources.Count == 0 )
				return EnhanceResult.BadItem;

			bool allRequiredSkills = false;
			if( craftItem.GetSuccessChance( from, resType, craftSystem, false, ref allRequiredSkills ) <= 0.0 )
				return EnhanceResult.NoSkill;

			CraftResourceInfo info = CraftResources.GetInfo( resource );

			if ( info == null || info.ResourceTypes.Length == 0 )
				return EnhanceResult.BadResource;

			CraftAttributeInfo attributes = info.AttributeInfo;

			if ( attributes == null )
				return EnhanceResult.BadResource;

			int resHue = 0, maxAmount = 0;

			if ( !craftItem.ConsumeRes( from, resType, craftSystem, ref resHue, ref maxAmount, ConsumeType.None, ref resMessage ) )
				return EnhanceResult.NoResources;

			if ( craftSystem is DefBlacksmithy )
			{
				AncientSmithyHammer hammer = from.FindItemOnLayer( Layer.OneHanded ) as AncientSmithyHammer;
				if ( hammer != null )
				{
					hammer.UsesRemaining--;
					if ( hammer.UsesRemaining < 1 )
						hammer.Delete();
				}
			}

			int baseChance = 0;
			
			BonusAttribute[] bonusAttrs = null;
			BonusAttribute[] randomAttrs = null;

			if ( item is BaseWeapon )
			{
				BaseWeapon weapon = (BaseWeapon)item;

				if ( !CraftResources.IsStandard( weapon.Resource ) )
					return EnhanceResult.AlreadyEnhanced;

				baseChance = 20;
				
				CheckSkill( ref baseChance, from, craftSystem );
				
				int numOfRand = attributes.RandomAttributeCount;
				
				bonusAttrs = attributes.WeaponAttributes;
				randomAttrs = BonusAttributesHelper.GetRandomAttributes( attributes.WeaponRandomAttributes, numOfRand );
			}
			else if ( item is BaseArmor )
			{
				BaseArmor armor = (BaseArmor)item;

				if ( !CraftResources.IsStandard( armor.Resource ) )
					return EnhanceResult.AlreadyEnhanced;

				baseChance = 20;
				
				CheckSkill( ref baseChance, from, craftSystem );
				
				int numOfRand = attributes.RandomAttributeCount;
				
				if ( armor.UsesShieldAttrs )
				{
					bonusAttrs = attributes.ShieldAttributes;
					randomAttrs = BonusAttributesHelper.GetRandomAttributes( attributes.ShieldRandomAttributes, numOfRand );
				}
				else 
				{
					bonusAttrs = info.AttributeInfo.ArmorAttributes;
					randomAttrs = BonusAttributesHelper.GetRandomAttributes( attributes.ArmorRandomAttributes, numOfRand );
				}
			}
			else if ( item is BaseOtherEquipable )
			{
				BaseOtherEquipable otherEquip = (BaseOtherEquipable)item;

				if ( !CraftResources.IsStandard( otherEquip.Resource ) )
					return EnhanceResult.AlreadyEnhanced;

				baseChance = 20;
				
				CheckSkill( ref baseChance, from, craftSystem );
				
				int numOfRand = attributes.RandomAttributeCount;
				
				bonusAttrs = attributes.OtherAttributes;
				randomAttrs = BonusAttributesHelper.GetRandomAttributes( attributes.OtherRandomAttributes, numOfRand );	
			}
			
			List<BonusAttribute> attrs = new List<BonusAttribute>();
			if ( bonusAttrs != null && bonusAttrs.Length > 0 )
				attrs.AddRange( bonusAttrs );
			if ( randomAttrs != null && randomAttrs.Length > 0 )
				attrs.AddRange( randomAttrs );
			
			EnhanceResult res = EnhanceResult.Success;
				
			TryEnhance( attrs, item, baseChance, ref res );

			switch ( res )
			{
				case EnhanceResult.Broken:
				{
					if ( !craftItem.ConsumeRes( from, resType, craftSystem, ref resHue, ref maxAmount, ConsumeType.Half, ref resMessage ) )
						return EnhanceResult.NoResources;

					item.Delete();
					break;
				}
				case EnhanceResult.Success:
				{
					if ( !craftItem.ConsumeRes( from, resType, craftSystem, ref resHue, ref maxAmount, ConsumeType.All, ref resMessage ) )
						return EnhanceResult.NoResources;

					if( item is BaseWeapon )
					{
						BaseWeapon w = (BaseWeapon)item;
						
						w.RandomAttributes = randomAttrs;

						w.Resource = resource;

						w.Hue = w.GetElementalDamageHue( w.Hue );
					}
					else if( item is BaseArmor )
					{
						BaseArmor ar = (BaseArmor)item;
						
						ar.RandomAttributes = randomAttrs;
						
						((BaseArmor)item).Resource = resource;
					}
					else if( item is BaseOtherEquipable )	//Sanity
					{
						BaseOtherEquipable otherEquip = (BaseOtherEquipable)item;
						
						otherEquip.RandomAttributes = randomAttrs;
						
						((BaseOtherEquipable)item).Resource = resource;
					}

					break;
				}
				case EnhanceResult.Failure:
				{
					if ( !craftItem.ConsumeRes( from, resType, craftSystem, ref resHue, ref maxAmount, ConsumeType.Half, ref resMessage ) )
						return EnhanceResult.NoResources;

					break;
				}
			}

			return res;
		}

		public static void CheckResult( ref EnhanceResult res, int chance )
		{
			if ( res != EnhanceResult.Success )
				return; // we've already failed..

			int random = Utility.Random( 100 );

			if ( 10 > random )
				res = EnhanceResult.Failure;
			else if ( chance > random )
				res = EnhanceResult.Broken;
		}
		
		public static void CheckSkill( ref int baseChance, Mobile from, CraftSystem craftSystem )
		{
			int skill = from.Skills[craftSystem.MainSkill].Fixed / 10;

			if ( skill >= 100 )
				baseChance -= (skill - 90) / 10;
		}
		
		public static void TryEnhance( List<BonusAttribute> attrs, Item item, int baseChance, ref EnhanceResult res )
		{
			foreach ( BonusAttribute attr in attrs )
			{
				if ( res != EnhanceResult.Success )
					return;
				
				if ( attr.Attribute == null )
					continue;
				
				Type type = attr.Attribute.GetType();
						
				if ( type == typeof( AosAttribute ) )
				{
					AosAttribute aosAttr = (AosAttribute)attr.Attribute;
						
					if ( aosAttr == AosAttribute.SpellChanneling )
						continue;
					
					if ( item is IAosAttributes )
						CheckResult( ref res, baseChance + GetChance( aosAttr, ((IAosAttributes)item).Attributes ) );
				}
				else if ( type == typeof( AosWeaponAttribute ) )
				{
					if ( item is BaseWeapon )
						CheckResult( ref res, baseChance + GetChance( (AosWeaponAttribute)attr.Attribute, ((BaseWeapon)item).WeaponAttributes ) );
				}
				else if ( type == typeof( AosArmorAttribute ) )
				{
					if ( (AosArmorAttribute)attr.Attribute == AosArmorAttribute.MageArmor )
						continue;
					
					if ( item is BaseArmor )
						CheckResult( ref res, baseChance + GetChance( (AosArmorAttribute)attr.Attribute, ((BaseArmor)item).ArmorAttributes ) );
				}
				else if ( type == typeof( ResistanceType ) )
				{
					if ( item is BaseArmor )
					{
						BaseArmor ar = (BaseArmor)item;
						
						switch ( (ResistanceType)attr.Attribute )
						{
							case ResistanceType.Physical:
								CheckResult( ref res, baseChance + ar.PhysicalResistance );
								break;
							case ResistanceType.Fire:
								CheckResult( ref res, baseChance + ar.FireResistance );
								break;
							case ResistanceType.Cold:
								CheckResult( ref res, baseChance + ar.ColdResistance );
								break;
							case ResistanceType.Poison:
								CheckResult( ref res, baseChance + ar.PoisonResistance );
								break;
							case ResistanceType.Energy:
								CheckResult( ref res, baseChance + ar.EnergyResistance );
								break;
						}
					} 
				}
			}
		}
		
		public static int GetChance( AosAttribute attr, AosAttributes itemAttrs )
		{
			int chance;
			
			switch ( attr )
			{
				case AosAttribute.LowerWeight: 					chance = itemAttrs[attr] / 40; break;
				case AosAttribute.WeaponDamage:					chance = itemAttrs[attr] / 4; break;
				case AosAttribute.Luck: 						chance = 10 + itemAttrs[attr] / 2; break;
				default: 
					chance = itemAttrs[attr] / 2; break;
			}
			
			return chance;
		}
		
		public static int GetChance( AosArmorAttribute attr, AosArmorAttributes itemAttrs )
		{
			int chance;
			
			switch ( attr )
			{
				case AosArmorAttribute.DurabilityBonus: 		chance = itemAttrs[attr] / 40; break;
				case AosArmorAttribute.LowerStatReq: 			chance = itemAttrs[attr] / 4; break;
				default:
					chance = itemAttrs[attr] / 2; break;
			}
			
			return chance;
		}
		
		public static int GetChance( AosWeaponAttribute attr, AosWeaponAttributes itemAttrs )
		{
			int chance;
			
			switch ( attr )
			{
				case AosWeaponAttribute.DurabilityBonus: 		chance = itemAttrs[attr] / 40; break;
				case AosWeaponAttribute.LowerStatReq: 			chance = itemAttrs[attr] / 4; break;
				case AosWeaponAttribute.ResistPhysicalBonus:
				case AosWeaponAttribute.ResistFireBonus:
				case AosWeaponAttribute.ResistColdBonus:
				case AosWeaponAttribute.ResistPoisonBonus:
				case AosWeaponAttribute.ResistEnergyBonus:		chance = itemAttrs[attr]; break;
				default: 
					chance = itemAttrs[attr] / 2; break;
			}
			
			return chance;
		}

		public static void BeginTarget( Mobile from, CraftSystem craftSystem, BaseTool tool )
		{
			CraftContext context = craftSystem.GetContext( from );

			if ( context == null )
				return;

			int lastRes = context.LastResourceIndex;
			CraftSubResCol subRes = craftSystem.CraftSubRes;

			if ( lastRes >= 0 && lastRes < subRes.Count )
			{
				CraftSubRes res = subRes.GetAt( lastRes );

				if ( from.Skills[craftSystem.MainSkill].Value < res.RequiredSkill )
				{
					from.SendGump( new CraftGump( from, craftSystem, tool, res.Message ) );
				}
				else
				{
					CraftResource resource = CraftResources.GetFromType( res.ItemType );

					if ( resource != CraftResource.None )
					{
						from.Target = new InternalTarget( craftSystem, tool, res.ItemType, resource );
						from.SendLocalizedMessage( 1061004 ); // Target an item to enhance with the properties of your selected material.
					}
					else
					{
						from.SendGump( new CraftGump( from, craftSystem, tool, 1061010 ) ); // You must select a special material in order to enhance an item with its properties.
					}
				}
			}
			else
			{
				from.SendGump( new CraftGump( from, craftSystem, tool, 1061010 ) ); // You must select a special material in order to enhance an item with its properties.
			}

		}

		private class InternalTarget : Target
		{
			private CraftSystem m_CraftSystem;
			private BaseTool m_Tool;
			private Type m_ResourceType;
			private CraftResource m_Resource;

			public InternalTarget( CraftSystem craftSystem, BaseTool tool, Type resourceType, CraftResource resource ) :  base ( 2, false, TargetFlags.None )
			{
				m_CraftSystem = craftSystem;
				m_Tool = tool;
				m_ResourceType = resourceType;
				m_Resource = resource;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				if ( targeted is Item )
				{
					object message = null;
					EnhanceResult res = Enhance.Invoke( from, m_CraftSystem, m_Tool, (Item)targeted, m_Resource, m_ResourceType, ref message );

					switch ( res )
					{
						case EnhanceResult.NotInBackpack: message = 1061005; break; // The item must be in your backpack to enhance it.
						case EnhanceResult.AlreadyEnhanced: message = 1061012; break; // This item is already enhanced with the properties of a special material.
						case EnhanceResult.BadItem: message = 1061011; break; // You cannot enhance this type of item with the properties of the selected special material.
						case EnhanceResult.BadResource: message = 1061010; break; // You must select a special material in order to enhance an item with its properties.
						case EnhanceResult.Broken: message = 1061080; break; // You attempt to enhance the item, but fail catastrophically. The item is lost.
						case EnhanceResult.Failure: message = 1061082; break; // You attempt to enhance the item, but fail. Some material is lost in the process.
						case EnhanceResult.Success: message = 1061008; break; // You enhance the item with the properties of the special material.
						case EnhanceResult.NoSkill: message = 1044153; break; // You don't have the required skills to attempt this item.
					}

					from.SendGump( new CraftGump( from, m_CraftSystem, m_Tool, message ) );
				}
			}
		}
	}
}