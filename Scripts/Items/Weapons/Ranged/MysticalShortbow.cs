using System;
using Server.Items;

namespace Server.Items
{
	public class MysticalShortbow : MagicalShortbow
	{
		public override int LabelNumber => 1073511; // mystical shortbow

		[Constructible]
		public MysticalShortbow()
		{
			Attributes.SpellChanneling = 1;
			Attributes.CastSpeed = -1;
		}

		public MysticalShortbow( Serial serial ) : base( serial )
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
