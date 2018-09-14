namespace Server.Items
{
	public class BladeOfInsanity : Katana
	{
		public override int LabelNumber => 1061088; // Blade of Insanity
		public override int ArtifactRarity => 11;

		public override int InitMinHits => 255;
		public override int InitMaxHits => 255;

		[Constructible]
		public BladeOfInsanity()
		{
			Hue = 0x76D;
			WeaponAttributes.HitLeechStam = 100;
			Attributes.RegenStam = 2;
			Attributes.WeaponSpeed = 30;
			Attributes.WeaponDamage = 50;
		}

		public BladeOfInsanity( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( Hue == 0x44F )
				Hue = 0x76D;
		}
	}
}
