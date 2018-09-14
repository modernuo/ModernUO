using System;
using Server.Network;

namespace Server.Items
{
	public abstract class BaseFruitTreeAddon : BaseAddon
	{
		public override abstract BaseAddonDeed Deed { get; }
		public abstract Item Fruit { get; }

		private int m_Fruits;

		[CommandProperty( AccessLevel.GameMaster )]
		public int Fruits
		{
			get => m_Fruits;
			set
			{
				if ( value < 0 )
					m_Fruits = 0;
				else
					m_Fruits = value;
			}
		}

		public BaseFruitTreeAddon()
		{
			Timer.DelayCall( TimeSpan.FromMinutes( 5 ), new TimerCallback( Respawn ) );
		}

		public BaseFruitTreeAddon( Serial serial ) : base( serial )
		{
		}

		public override void OnComponentUsed( AddonComponent c, Mobile from )
		{
			if ( from.InRange( c.Location, 2 ) )
			{
				if ( m_Fruits > 0 )
				{
					Item fruit = Fruit;

					if ( fruit == null )
						return;

					if ( !from.PlaceInBackpack( fruit ) )
					{
						fruit.Delete();
						from.SendLocalizedMessage( 501015 ); // There is no room in your backpack for the fruit.
					}
					else
					{
						if ( --m_Fruits == 0 )
							Timer.DelayCall( TimeSpan.FromMinutes( 30 ), new TimerCallback( Respawn ) );

						from.SendLocalizedMessage( 501016 ); // You pick some fruit and put it in your backpack.
					}
				}
				else
					from.SendLocalizedMessage( 501017 ); // There is no more fruit on this tree
			}
			else
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
		}

		private void Respawn()
		{
			m_Fruits = Utility.RandomMinMax( 1, 4 );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version

			writer.Write( (int) m_Fruits );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();

			m_Fruits = reader.ReadInt();

			if ( m_Fruits == 0 )
				Respawn();
		}
	}

	public class AppleTreeAddon : BaseFruitTreeAddon
	{
		public override BaseAddonDeed Deed => new AppleTreeDeed();
		public override Item Fruit => new Apple();

		[Constructible]
		public AppleTreeAddon()
		{
			AddComponent( new LocalizedAddonComponent( 0xD98, 1076269 ), 0, 0, 0 );
			AddComponent( new LocalizedAddonComponent( 0x3124, 1076269 ), 0, 0, 0 );
		}

		public AppleTreeAddon( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}

	public class AppleTreeDeed : BaseAddonDeed
	{
		public override BaseAddon Addon => new AppleTreeAddon();
		public override int LabelNumber => 1076269; // Apple Tree

		[Constructible]
		public AppleTreeDeed()
		{
			LootType = LootType.Blessed;
		}

		public AppleTreeDeed( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}

	public class PeachTreeAddon : BaseFruitTreeAddon
	{
		public override BaseAddonDeed Deed => new PeachTreeDeed();
		public override Item Fruit => new Peach();

		[Constructible]
		public PeachTreeAddon()
		{
			AddComponent( new LocalizedAddonComponent( 0xD9C, 1076270 ), 0, 0, 0 );
			AddComponent( new LocalizedAddonComponent( 0x3123, 1076270 ), 0, 0, 0 );
		}

		public PeachTreeAddon( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}

	public class PeachTreeDeed : BaseAddonDeed
	{
		public override BaseAddon Addon => new PeachTreeAddon();
		public override int LabelNumber => 1076270; // Peach Tree

		[Constructible]
		public PeachTreeDeed()
		{
			LootType = LootType.Blessed;
		}

		public PeachTreeDeed( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}
