namespace Server.Items
{
	public class LesserHealPotion : BaseHealPotion
	{
		public override int MinHeal => (Core.AOS ? 6 : 3);
		public override int MaxHeal => (Core.AOS ? 8 : 10);
		public override double Delay => (Core.AOS ? 3.0 : 10.0);

		[Constructible]
		public LesserHealPotion() : base( PotionEffect.HealLesser )
		{
		}

		public LesserHealPotion( Serial serial ) : base( serial )
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
