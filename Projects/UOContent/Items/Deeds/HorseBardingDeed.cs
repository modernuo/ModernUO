using System;
using Server;
using Server.Engines.Craft;
using Server.Mobiles;
using Server.Targeting;
namespace Server.Items
{
	[TypeAlias("Server.Items.HorseBarding")]
	public class HorseBardingDeed : Item, ICraftable
	{
		private bool m_Exceptional;
		private Mobile m_Crafter;
		private CraftResource m_Resource;

		public override string DefaultName
		{
			get { return "horse barding deed"; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile Crafter{ get{ return m_Crafter; } set{ m_Crafter = value; InvalidateProperties(); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public bool Exceptional{ get{ return m_Exceptional; } set{ m_Exceptional = value; InvalidateProperties(); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public CraftResource Resource{ get{ return m_Resource; } set{ m_Resource = value; Hue = CraftResources.GetHue( value ); InvalidateProperties(); } }

		public HorseBardingDeed() : base( 0x14F0 )
		{
			Weight = 1.0;
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( m_Exceptional)
				list.Add(1060636); 
			if (m_Crafter != null )
				list.Add( 1050043, m_Crafter.Name ); // crafted by ~1_NAME~
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( IsChildOf( from.Backpack ) )
			{
				from.BeginTarget( 6, false, TargetFlags.None, new TargetCallback( OnTarget ) );
				from.SendMessage( "Please target the horse you wish to armor." ); // Select the swamp dragon you wish to place the barding on.
			}
			else
			{
				from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
			}
		}

		public virtual void OnTarget( Mobile from, object obj )
		{
			if ( Deleted )
				return;

			Horse pet = obj as Horse;

			if ( pet == null || pet.HasBarding )
			{
				from.SendMessage( "That is not an unarmored horse." ); // That is not an unarmored swamp dragon.
			}
			else if ( !pet.Controlled || pet.ControlMaster != from )
			{
				from.SendMessage( "You can only bard a horse that you own." ); // You can only put barding on a tamed swamp dragon that you own.
			}
			else if ( !IsChildOf( from.Backpack ) )
			{
				from.SendLocalizedMessage( 1060640 ); // The item must be in your backpack to use it.
			}
			else
			{
				pet.BardingExceptional = this.Exceptional;
				pet.BardingCrafter = this.Crafter;
				pet.BardingHP = pet.BardingMaxHP;
				pet.BardingResource = this.Resource;
				pet.HasBarding = true;
				pet.Hue = this.Hue;

				this.Delete();

				from.SendMessage( "You have barded your horse. To remove the armor use a bladed item."); // You place the barding on your swamp dragon.  Use a bladed item on your dragon to remove the armor.
			}
		}

		public HorseBardingDeed( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( IGenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version

			writer.Write( (bool) m_Exceptional );
			writer.Write( (Mobile) m_Crafter );
			writer.Write( (int) m_Resource );
		}

		public override void Deserialize( IGenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				case 0:
				{
					m_Exceptional = reader.ReadBool();
					m_Crafter = reader.ReadEntity<Mobile>();

					if ( version < 1 )
						reader.ReadInt();

					m_Resource = (CraftResource) reader.ReadInt();
					break;
				}
			}
		}
		#region ICraftable Members

		public int OnCraft(
            int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
            CraftItem craftItem, int resHue
        )
		{
            Exceptional = quality >= 2;

            if (makersMark)
            {
                Crafter = from;
            }

            var resourceType = typeRes ?? craftItem.Resources[0].ItemType;

            Resource = CraftResources.GetFromType(resourceType);

            var context = craftSystem.GetContext(from);

            if (context?.DoNotColor == true)
            {
                Hue = 0;
            }

            return quality;
        }

        #endregion
    }
}
