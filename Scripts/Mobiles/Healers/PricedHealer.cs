using Server.Gumps;

namespace Server.Mobiles
{
	public class PricedHealer : BaseHealer
	{
		[CommandProperty( AccessLevel.GameMaster )]
		public int Price { get; set; }

		[Constructible]
		public PricedHealer() : this( 5000 )
		{
		}

		[Constructible]
		public PricedHealer( int price )
		{
			Price = price;

			if ( !Core.AOS )
				NameHue = 0x35;
		}

		public override bool IsInvulnerable => true;

		public override void InitSBInfo()
		{
		}

		public override bool HealsYoungPlayers => false;

		public override void OfferResurrection( Mobile m )
		{
			Direction = GetDirectionTo( m );

			m.PlaySound( 0x214 );
			m.FixedEffect( 0x376A, 10, 16 );

			m.CloseGump( typeof( ResurrectGump ) );
			m.SendGump( new ResurrectGump( m, this, Price ) );
		}

		public override bool CheckResurrect( Mobile m )
		{
			return true;
		}

		public PricedHealer( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( (int) Price );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					Price = reader.ReadInt();
					break;
				}
			}
		}
	}
}
