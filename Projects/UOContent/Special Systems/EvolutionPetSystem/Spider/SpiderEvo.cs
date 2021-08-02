using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Xanthos.Interfaces;

namespace EvolutionPetSystem
{
	[CorpseName( "a spider corpse" )]
	public class Spider : BaseEvo, IEvoCreature
	{
		public override BaseEvoSpec GetEvoSpec()
		{
			return SpiderSpec.Instance;
		}

		public override BaseEvoEgg GetEvoEgg()
		{
			return new SpiderEgg();
		}

		public override bool AddPointsOnDamage { get { return true; } }
		public override bool AddPointsOnMelee { get { return false; } }
		public override Type GetEvoDustType() { return typeof( SpiderDust ); }

		public override bool HasBreath{ get{ return false; } }

		public Spider( string name ) : base( name, AIType.AI_Mage, 0.2 )
		{
		}

		public Spider( Serial serial ) : base( serial )
		{
		}
		
		public override void Serialize(IGenericWriter writer)
		{
			base.Serialize(writer);
			writer.Write( (int)0 );			
		}

		public override void Deserialize(IGenericReader reader)
		{
			base.Deserialize(reader);
			int version = reader.ReadInt();
		}
	}
}
