namespace Server.Items
{
	public class GreaterHealPotion : BaseHealPotion
	{
		public override int MinHeal => (Core.AOS ? 20 : 9);
		public override int MaxHeal => (Core.AOS ? 25 : 30);
		public override double Delay => 10.0;

		[Constructible]
		public GreaterHealPotion() : base( PotionEffect.HealGreater )
		{
		}

		public GreaterHealPotion( Serial serial ) : base( serial )
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
