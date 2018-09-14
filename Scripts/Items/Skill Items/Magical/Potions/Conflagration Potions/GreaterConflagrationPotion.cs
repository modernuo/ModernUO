namespace Server.Items
{
	public class GreaterConflagrationPotion : BaseConflagrationPotion
	{
		public override int MinDamage => 4;
		public override int MaxDamage => 8;

		public override int LabelNumber => 1072098; // a Greater Conflagration potion

		[Constructible]
		public GreaterConflagrationPotion() : base( PotionEffect.ConflagrationGreater )
		{
		}

		public GreaterConflagrationPotion( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
