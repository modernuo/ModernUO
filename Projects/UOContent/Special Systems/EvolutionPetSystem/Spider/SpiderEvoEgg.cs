using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Xanthos.Interfaces;

namespace EvolutionPetSystem
{
	public class SpiderEgg : BaseEvoEgg
	{
		public override IEvoCreature GetEvoCreature()
		{
			return new Spider( "a spider hatchling" );
		}

		[Constructible]
		public SpiderEgg() : base()
		{
			Name = "a spider egg";
			HatchDuration = 0.25;		// 15 minutes
			Hue = 2101;
		}

        public SpiderEgg(Serial serial)
            : base(serial)
		{
		}

		public override void Serialize( IGenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int)0 );
		}

		public override void Deserialize( IGenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}
