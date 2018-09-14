using Server.Items;

namespace Server.Mobiles
{
	public class SpawnedOrcishLord : OrcishLord
	{
		public override string CorpseName => "an orcish corpse";
		[Constructible]
		public SpawnedOrcishLord()
		{
			Container pack = Backpack;

			if ( pack != null )
				pack.Delete();

			NoKillAwards = true;
		}

		public SpawnedOrcishLord( Serial serial ) : base( serial )
		{
		}

		public override void OnDeath( Container c )
		{
			base.OnDeath( c );

			c.Delete();
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
			NoKillAwards = true;
		}
	}
}
