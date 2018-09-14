using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Engines.Craft;
using Server.Network;

namespace Server.Items
{
    public class SalvageBag : Bag
    {
		private bool m_Failure;

        public override int LabelNumber => 1079931; // Salvage Bag

        [Constructible]
        public SalvageBag()
            : this( Utility.RandomBlueHue() )
        {
        }

        [Constructible]
        public SalvageBag( int hue )
        {
            Weight = 2.0;
            Hue = hue;
			m_Failure = false;
        }

        public override void GetContextMenuEntries( Mobile from, List<ContextMenuEntry> list )
        {
            base.GetContextMenuEntries( from, list );

            if ( from.Alive )
            {
                list.Add( new SalvageIngotsEntry( this, IsChildOf( from.Backpack ) && Resmeltables() ) );
                list.Add( new SalvageClothEntry( this, IsChildOf( from.Backpack ) && Scissorables() ) );
                list.Add( new SalvageAllEntry( this, IsChildOf( from.Backpack ) && Resmeltables() && Scissorables() ) );
            }
        }

		#region Checks
		private bool Resmeltables() //Where context menu checks for metal items and dragon barding deeds
		{
			foreach (Item i in Items)
			{
				return i?.Deleted == false && (
					   i is BaseWeapon weapon && CraftResources.GetType(weapon.Resource) == CraftResourceType.Metal ||
				       i is BaseArmor armor && CraftResources.GetType(armor.Resource) == CraftResourceType.Metal ||
				       i is DragonBardingDeed);
			}

			return false;
		}

		private bool Scissorables() //Where context menu checks for Leather items and cloth items
		{
			foreach( Item i in Items )
			{
				if (!(i is IScissorable) || i.Deleted)
					continue;

				if (i is BaseClothing || i is Cloth || i is BoltOfCloth || i is Hides || i is BonePile ||
				    i is BaseArmor armor && CraftResources.GetType(armor.Resource) == CraftResourceType.Leather)
					return true;
			}

			return false;
		}
		#endregion

		#region Resmelt.cs
        private bool Resmelt( Mobile from, Item item, CraftResource resource )
        {
            try
            {
                if ( CraftResources.GetType( resource ) != CraftResourceType.Metal )
                    return false;

                CraftResourceInfo info = CraftResources.GetInfo( resource );

                if ( info == null || info.ResourceTypes.Length == 0 )
                    return false;

                CraftItem craftItem = DefBlacksmithy.CraftSystem.CraftItems.SearchFor( item.GetType() );

                if ( craftItem == null || craftItem.Resources.Count == 0 )
                    return false;

                CraftRes craftResource = craftItem.Resources.GetAt( 0 );

                if ( craftResource.Amount < 2 )
                    return false; // Not enough metal to resmelt

				double difficulty = 0.0;

				switch ( resource )
				{
					case CraftResource.DullCopper: difficulty = 65.0; break;
					case CraftResource.ShadowIron: difficulty = 70.0; break;
					case CraftResource.Copper: difficulty = 75.0; break;
					case CraftResource.Bronze: difficulty = 80.0; break;
					case CraftResource.Gold: difficulty = 85.0; break;
					case CraftResource.Agapite: difficulty = 90.0; break;
					case CraftResource.Verite: difficulty = 95.0; break;
					case CraftResource.Valorite: difficulty = 99.0; break;
				}

                Type resourceType = info.ResourceTypes[ 0 ];
                Item ingot = (Item)Activator.CreateInstance( resourceType );

                if ( item is DragonBardingDeed || ( item is BaseArmor armor && armor.PlayerConstructed ) || ( item is BaseWeapon weapon && weapon.PlayerConstructed ) || ( item is BaseClothing clothing && clothing.PlayerConstructed ) )
					{
						double mining = from.Skills[ SkillName.Mining ].Value;
						if ( mining > 100.0 )
							mining = 100.0;
						double amount = ( ( ( 4 + mining ) * craftResource.Amount - 4 ) * 0.0068 );
						if ( amount < 2 )
							ingot.Amount = 2;
						else
						ingot.Amount = (int)amount;
					}
                else
				{
                    ingot.Amount = 2;
				}

				if ( difficulty > from.Skills[ SkillName.Mining ].Value )
				{
					m_Failure = true;
					ingot.Delete();
				}
				else
					item.Delete();

                from.AddToBackpack( ingot );

                from.PlaySound( 0x2A );
                from.PlaySound( 0x240 );

                return true;
            }
            catch( Exception ex )
            {
                Console.WriteLine( ex.ToString() );
            }

            return false;
        }
		#endregion

		#region Salvaging
        private void SalvageIngots( Mobile from )
        {
            Item[] tools = from.Backpack.FindItemsByType( typeof( BaseTool ) );

            bool ToolFound = false;
            foreach( Item tool in tools )
            {
                if ( tool is BaseTool baseTool && baseTool.CraftSystem == DefBlacksmithy.CraftSystem )
                    ToolFound = true;
            }

            if ( !ToolFound )
            {
                from.SendLocalizedMessage( 1079822 ); // You need a blacksmithing tool in order to salvage ingots.
                return;
            }

	        DefBlacksmithy.CheckAnvilAndForge( from, 2, out _, out var forge );

            if ( !forge )
            {
                from.SendLocalizedMessage( 1044265 ); // You must be near a forge.
                return;
            }

            int salvaged = 0;
            int notSalvaged = 0;

			Container sBag = this;

			List<Item> smeltables = sBag.FindItemsByType<Item>();

	        foreach (Item item in smeltables)
	        {
		        if (item?.Deleted != false)
			        continue;

		        if (item is BaseArmor armor && Resmelt(from, armor, armor.Resource) ||
		            item is BaseWeapon weapon && Resmelt(from, weapon, weapon.Resource) ||
		            item is DragonBardingDeed)
		        {
			        salvaged++;
		        }
		        else
		        {
			        notSalvaged++;
		        }
	        }

			if ( m_Failure )
			{
				from.SendLocalizedMessage( 1079975 ); // You failed to smelt some metal for lack of skill.
				m_Failure = false;
			}
			else
				from.SendLocalizedMessage( 1079973, $"{salvaged}\t{salvaged + notSalvaged}"); // Salvaged: ~1_COUNT~/~2_NUM~ blacksmithed items
		}

        private void SalvageCloth( Mobile from )
        {
	        if ( !(from.Backpack.FindItemByType( typeof( Scissors ) ) is Scissors scissors) )
            {
                from.SendLocalizedMessage( 1079823 ); // You need scissors in order to salvage cloth.
                return;
            }

            int salvaged = 0;
            int notSalvaged = 0;

			Container sBag = this;

			List<Item> scissorables = sBag.FindItemsByType<Item>();

			for ( int i = scissorables.Count - 1; i >= 0; --i )
			{
				Item item = scissorables[i];

				if (!(item is IScissorable scissorable))
					continue;

				if ( Scissors.CanScissor( from, scissorable ) && scissorable.Scissor( from, scissors ) )
					++salvaged;
				else
					++notSalvaged;
			}

            from.SendLocalizedMessage( 1079974, $"{salvaged}\t{salvaged + notSalvaged}"); // Salvaged: ~1_COUNT~/~2_NUM~ tailored items

			foreach (Item i in FindItemsByType(typeof(Item), true))
			{
				if ( ( i is Leather ) || ( i is Cloth ) || ( i is SpinedLeather ) || ( i is HornedLeather ) || ( i is BarbedLeather ) || ( i is Bandage ) || ( i is Bone ) )
				{
					from.AddToBackpack( i );
				}
			}
        }

        private void SalvageAll( Mobile from )
        {
            SalvageIngots( from );

            SalvageCloth( from );
        }
		#endregion

        #region ContextMenuEntries
        private class SalvageAllEntry : ContextMenuEntry
        {
            private SalvageBag m_Bag;

            public SalvageAllEntry( SalvageBag bag, bool enabled )
                : base( 6276 )
            {
                m_Bag = bag;

                if ( !enabled )
                    Flags |= CMEFlags.Disabled;
            }

            public override void OnClick()
            {
                if ( m_Bag.Deleted )
                    return;

                Mobile from = Owner.From;

                if ( from.CheckAlive() )
                    m_Bag.SalvageAll( from );
            }
        }

        private class SalvageIngotsEntry : ContextMenuEntry
        {
            private SalvageBag m_Bag;

            public SalvageIngotsEntry( SalvageBag bag, bool enabled )
                : base( 6277 )
            {
                m_Bag = bag;

                if ( !enabled )
                    Flags |= CMEFlags.Disabled;
            }

            public override void OnClick()
            {
                if ( m_Bag.Deleted )
                    return;

                Mobile from = Owner.From;

                if ( from.CheckAlive() )
                    m_Bag.SalvageIngots( from );
            }
        }

        private class SalvageClothEntry : ContextMenuEntry
        {
            private SalvageBag m_Bag;

            public SalvageClothEntry( SalvageBag bag, bool enabled )
                : base( 6278 )
            {
                m_Bag = bag;

                if ( !enabled )
                    Flags |= CMEFlags.Disabled;
            }

            public override void OnClick()
            {
                if ( m_Bag.Deleted )
                    return;

                Mobile from = Owner.From;

                if ( from.CheckAlive() )
                    m_Bag.SalvageCloth( from );
            }
        }
        #endregion

        #region Serialization
        public SalvageBag( Serial serial )
            : base( serial )
        {
        }

        public override void Serialize( GenericWriter writer )
        {
            base.Serialize( writer );

            writer.WriteEncodedInt( (int)0 ); // version
        }

        public override void Deserialize( GenericReader reader )
        {
            base.Deserialize( reader );

            int version = reader.ReadEncodedInt();
        }
        #endregion
    }
}
