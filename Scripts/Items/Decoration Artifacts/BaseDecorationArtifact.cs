using System;
using Server;

namespace Server.Items
{
	public abstract class BaseDecorationArtifact : Item
	{
		public abstract int ArtifactRarity{ get; }

		public override bool ForceShowProperties{ get{ return true; } }

		public BaseDecorationArtifact( int itemID ) : base( itemID )
		{
			Weight = 10.0;
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			list.Add( 1061078, this.ArtifactRarity.ToString() ); // artifact rarity ~1_val~
		}

		public BaseDecorationArtifact( Serial serial ) : base( serial )
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

	public abstract class BaseDecorationContainerArtifact : BaseContainer
	{
		public abstract int ArtifactRarity{ get; }

		public override bool ForceShowProperties{ get{ return true; } }

		public BaseDecorationContainerArtifact( int itemID ) : base( itemID )
		{
			Weight = 10.0;
		}

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );

			list.Add( 1061078, this.ArtifactRarity.ToString() ); // artifact rarity ~1_val~
		}

		public BaseDecorationContainerArtifact( Serial serial ) : base( serial )
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