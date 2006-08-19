using System;
using Server;
using Server.Mobiles;
using Server.Targeting;
using Server.Items;

namespace Server.Engines.Craft
{
	public class Repair
	{
		public Repair()
		{
		}

		public static void Do( Mobile from, CraftSystem craftSystem, BaseTool tool )
		{
			from.Target = new InternalTarget( craftSystem, tool );
			from.SendLocalizedMessage( 1044276 ); // Target an item to repair.
		}

		public static void Do( Mobile from, CraftSystem craftSystem, RepairDeed deed )
		{
			from.Target = new InternalTarget( craftSystem, deed );
			from.SendLocalizedMessage( 1044276 ); // Target an item to repair.
		}

		private class InternalTarget : Target
		{
			private CraftSystem m_CraftSystem;
			private BaseTool m_Tool;
			private RepairDeed m_Deed;

			public InternalTarget( CraftSystem craftSystem, BaseTool tool ) :  base ( 2, false, TargetFlags.None )
			{
				m_CraftSystem = craftSystem;
				m_Tool = tool;
			}

			public InternalTarget( CraftSystem craftSystem, RepairDeed deed ) : base( 2, false, TargetFlags.None )
			{
				m_CraftSystem = craftSystem;
				m_Deed = deed;
			}

			private static void EndGolemRepair( object state )
			{
				((Mobile)state).EndAction( typeof( Golem ) );
			}

			private int GetWeakenChance( Mobile mob, SkillName skill, int curHits, int maxHits )
			{
				// 40% - (1% per hp lost) - (1% per 10 craft skill)
				return (40 + (maxHits - curHits)) - (int)(((m_Deed != null)? m_Deed.SkillLevel : mob.Skills[skill].Value) / 10);
			}

			private bool CheckWeaken( Mobile mob, SkillName skill, int curHits, int maxHits )
			{
				return ( GetWeakenChance( mob, skill, curHits, maxHits ) > Utility.Random( 100 ) );
			}

			private int GetRepairDifficulty( int curHits, int maxHits )
			{
				return (((maxHits - curHits) * 1250) / Math.Max( maxHits, 1 )) - 250;
			}

			private bool CheckRepairDifficulty( Mobile mob, SkillName skill, int curHits, int maxHits )
			{
				double difficulty = GetRepairDifficulty( curHits, maxHits ) * 0.1;


				if( m_Deed != null )
				{
					double value = m_Deed.SkillLevel;
					double minSkill = difficulty - 25.0;
					double maxSkill = difficulty + 25;

					if( value < minSkill )
						return false; // Too difficult
					else if( value >= maxSkill )
						return true; // No challenge

					double chance = (value - minSkill) / (maxSkill - minSkill);

					return (chance >= Utility.RandomDouble());
				}
				else
				{
					return mob.CheckSkill( skill, difficulty - 25.0, difficulty + 25.0 );
				}
			}

			private bool CheckDeed( Mobile from )
			{
				if( m_Deed != null )
				{
					return m_Deed.Check( from );
				}

				return true;
			}

			private bool IsSpecialClothing( BaseClothing clothing )
			{
				// Armor repairable but not craftable

				if( m_CraftSystem is DefTailoring )
				{
					return (clothing is BearMask)
						|| (clothing is DeerMask);
				}

				return false;
			}

			private bool IsSpecialWeapon( BaseWeapon weapon )
			{
				// Weapons repairable but not craftable

				if ( m_CraftSystem is DefTinkering )
				{
					return ( weapon is Cleaver )
						|| ( weapon is Hatchet )
						|| ( weapon is Pickaxe )
						|| ( weapon is ButcherKnife )
						|| ( weapon is SkinningKnife );
				}
				else if ( m_CraftSystem is DefCarpentry )
				{
					return ( weapon is Club )
						|| ( weapon is BlackStaff )
						|| ( weapon is MagicWand );
				}
				else if ( m_CraftSystem is DefBlacksmithy )
				{
					return ( weapon is Pitchfork );
				}

				return false;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				int number;

				if( !CheckDeed( from ) )
					return;


				bool usingDeed = (m_Deed != null);
				bool toDelete = false;

				//TODO: Make a IRepairable

				if ( m_CraftSystem is DefTinkering && targeted is Golem )
				{
					Golem g = (Golem)targeted;
					int damage = g.HitsMax - g.Hits;

					if ( g.IsDeadBondedPet )
					{
						number = 500426; // You can't repair that.
					}
					else if ( damage <= 0 )
					{
						number = 500423; // That is already in full repair.
					}
					else
					{
						double skillValue = (usingDeed)? m_Deed.SkillLevel : from.Skills[SkillName.Tinkering].Value;

						if ( skillValue < 60.0 )
						{
							number = 1044153; // You don't have the required skills to attempt this item.	//TODO: How does OSI handle this with deeds with golems?
						}
						else if ( !from.CanBeginAction( typeof( Golem ) ) )
						{
							number = 501789; // You must wait before trying again.
						}
						else
						{
							if ( damage > (int)(skillValue * 0.3) )
								damage = (int)(skillValue * 0.3);

							damage += 30;

							if ( !from.CheckSkill( SkillName.Tinkering, 0.0, 100.0 ) )
								damage /= 2;

							Container pack = from.Backpack;

							if ( pack != null )
							{
								int v = pack.ConsumeUpTo( typeof( IronIngot ), (damage+4)/5 );

								if ( v > 0 )
								{
									g.Hits += v*5;

									number = 1044279; // You repair the item.
									toDelete = true;

									from.BeginAction( typeof( Golem ) );
									Timer.DelayCall( TimeSpan.FromSeconds( 12.0 ), new TimerStateCallback( EndGolemRepair ), from );
								}
								else
								{
									number = 1044037; // You do not have sufficient metal to make that.
								}
							}
							else
							{
								number = 1044037; // You do not have sufficient metal to make that.
							}
						}
					}
				}
				else if ( targeted is BaseWeapon )
				{
					BaseWeapon weapon = (BaseWeapon)targeted;
					SkillName skill = m_CraftSystem.MainSkill;
					int toWeaken = 0;

					if ( Core.AOS )
					{
						toWeaken = 1;
					}
					else if ( skill != SkillName.Tailoring )
					{
						double skillLevel = (usingDeed)? m_Deed.SkillLevel : from.Skills[skill].Base;

						if ( skillLevel >= 90.0 )
							toWeaken = 1;
						else if ( skillLevel >= 70.0 )
							toWeaken = 2;
						else
							toWeaken = 3;
					}

					if ( m_CraftSystem.CraftItems.SearchForSubclass( weapon.GetType() ) == null && !IsSpecialWeapon( weapon ) )
					{
						number = (usingDeed)? 1061136 : 1044277; // That item cannot be repaired. // You cannot repair that item with this type of repair contract.
					}
					else if ( !weapon.IsChildOf( from.Backpack ) )
					{
						number = 1044275; // The item must be in your backpack to repair it.
					}
					else if ( weapon.MaxHitPoints <= 0 || weapon.HitPoints == weapon.MaxHitPoints )
					{
						number = 1044281; // That item is in full repair
					}
					else if ( weapon.MaxHitPoints <= toWeaken )
					{
						number = 1044278; // That item has been repaired many times, and will break if repairs are attempted again.
					}
					else
					{
						if ( CheckWeaken( from, skill, weapon.HitPoints, weapon.MaxHitPoints ) )
						{
							weapon.MaxHitPoints -= toWeaken;
							weapon.HitPoints = Math.Max( 0, weapon.HitPoints - toWeaken );
						}

						if ( CheckRepairDifficulty( from, skill, weapon.HitPoints, weapon.MaxHitPoints ) )
						{
							number = 1044279; // You repair the item.
							m_CraftSystem.PlayCraftEffect( from );
							weapon.HitPoints = weapon.MaxHitPoints;
						}
						else
						{
							number = (usingDeed)? 1061137 : 1044280; // You fail to repair the item. [And the contract is destroyed]
							m_CraftSystem.PlayCraftEffect( from );
						}

						toDelete = true;
					}
				}
				else if ( targeted is BaseArmor )
				{
					BaseArmor armor = (BaseArmor)targeted;
					SkillName skill = m_CraftSystem.MainSkill;
					int toWeaken = 0;

					if ( Core.AOS )
					{
						toWeaken = 1;
					}
					else if ( skill != SkillName.Tailoring )
					{
						double skillLevel = (usingDeed)? m_Deed.SkillLevel : from.Skills[skill].Base;

						if ( skillLevel >= 90.0 )
							toWeaken = 1;
						else if ( skillLevel >= 70.0 )
							toWeaken = 2;
						else
							toWeaken = 3;
					}

					if ( m_CraftSystem.CraftItems.SearchForSubclass( armor.GetType() ) == null )
					{
						number = (usingDeed)? 1061136 : 1044277; // That item cannot be repaired. // You cannot repair that item with this type of repair contract.
					}
					else if ( !armor.IsChildOf( from.Backpack ) )
					{
						number = 1044275; // The item must be in your backpack to repair it.
					}
					else if ( armor.MaxHitPoints <= 0 || armor.HitPoints == armor.MaxHitPoints )
					{
						number = 1044281; // That item is in full repair
					}
					else if ( armor.MaxHitPoints <= toWeaken )
					{
						number = 1044278; // That item has been repaired many times, and will break if repairs are attempted again.
					}
					else
					{
						if ( CheckWeaken( from, skill, armor.HitPoints, armor.MaxHitPoints ) )
						{
							armor.MaxHitPoints -= toWeaken;
							armor.HitPoints = Math.Max( 0, armor.HitPoints - toWeaken );
						}

						if ( CheckRepairDifficulty( from, skill, armor.HitPoints, armor.MaxHitPoints ) )
						{
							number = 1044279; // You repair the item.
							m_CraftSystem.PlayCraftEffect( from );
							armor.HitPoints = armor.MaxHitPoints;
						}
						else
						{
							number = (usingDeed)? 1061137 : 1044280; // You fail to repair the item. [And the contract is destroyed]
							m_CraftSystem.PlayCraftEffect( from );
						}

						toDelete = true;
					}
				}
				else if ( targeted is BaseClothing )
				{
					BaseClothing clothing = (BaseClothing)targeted;
					SkillName skill = m_CraftSystem.MainSkill;
					int toWeaken = 0;

					if ( Core.AOS )
					{
						toWeaken = 1;
					}
					else if ( skill != SkillName.Tailoring )
					{
						double skillLevel = (usingDeed) ? m_Deed.SkillLevel : from.Skills[skill].Base;

						if ( skillLevel >= 90.0 )
							toWeaken = 1;
						else if ( skillLevel >= 70.0 )
							toWeaken = 2;
						else
							toWeaken = 3;
					}

					if ( m_CraftSystem.CraftItems.SearchForSubclass( clothing.GetType() ) == null && !IsSpecialClothing( clothing ))
					{
						number = (usingDeed) ? 1061136 : 1044277; // That item cannot be repaired. // You cannot repair that item with this type of repair contract.
					}
					else if ( !clothing.IsChildOf( from.Backpack ) )
					{
						number = 1044275; // The item must be in your backpack to repair it.
					}
					else if ( clothing.MaxHitPoints <= 0 || clothing.HitPoints == clothing.MaxHitPoints )
					{
						number = 1044281; // That item is in full repair
					}
					else if ( clothing.MaxHitPoints <= toWeaken )
					{
						number = 1044278; // That item has been repaired many times, and will break if repairs are attempted again.
					}
					else
					{
						if ( CheckWeaken( from, skill, clothing.HitPoints, clothing.MaxHitPoints ) )
						{
							clothing.MaxHitPoints -= toWeaken;
							clothing.HitPoints = Math.Max( 0, clothing.HitPoints - toWeaken );
						}

						if ( CheckRepairDifficulty( from, skill, clothing.HitPoints, clothing.MaxHitPoints ) )
						{
							number = 1044279; // You repair the item.
							m_CraftSystem.PlayCraftEffect( from );
							clothing.HitPoints = clothing.MaxHitPoints;
						}
						else
						{
							number = (usingDeed) ? 1061137 : 1044280; // You fail to repair the item. [And the contract is destroyed]
							m_CraftSystem.PlayCraftEffect( from );
						}

						toDelete = true;
					}
				}
				else if( !usingDeed && targeted is BlankScroll )
				{
					SkillName skill = m_CraftSystem.MainSkill;

					if( from.Skills[skill].Value >= 50.0 )
					{
						((BlankScroll)targeted).Consume( 1 );
						RepairDeed deed = new RepairDeed( RepairDeed.GetTypeFor( m_CraftSystem ), from.Skills[skill].Value, from );
						from.AddToBackpack( deed );

						number = 500442; // You create the item and put it in your backpack.
					}
					else
						number = 1047005; // You must be at least apprentice level to create a repair service contract.
				}
				else if ( targeted is Item )
				{
					number = (usingDeed)? 1061136 : 1044277; // That item cannot be repaired. // You cannot repair that item with this type of repair contract.
				}
				else
				{
					number = 500426; // You can't repair that.
				}

				if( !usingDeed )
				{
					CraftContext context = m_CraftSystem.GetContext( from );
					from.SendGump( new CraftGump( from, m_CraftSystem, m_Tool, number ) );
				}
				else if( toDelete )
				{
					m_Deed.Delete();
				}
			}
		}
	}
}