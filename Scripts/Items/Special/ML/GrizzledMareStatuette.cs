using System;
using Server.Mobiles;

namespace Server.Items
{
	public class GrizzledMareStatuette : BaseImprisonedMobile
	{
		public override int LabelNumber => 1074475; // Grizzled Mare Statuette
		public override BaseCreature Summon => new GrizzledMare();

		[Constructible]
		public GrizzledMareStatuette() : base( 0x2617 )
		{
			Weight = 1.0;
		}

		public GrizzledMareStatuette( Serial serial ) : base( serial )
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

namespace Server.Mobiles
{
	public class GrizzledMare : HellSteed
	{
		public override bool DeleteOnRelease => true;

		private static readonly string m_Myname = "a grizzled mare";

		[Constructible]
		public GrizzledMare()
			: base( m_Myname  )
		{
		}

		public virtual void OnAfterDeserialize_Callback()
		{
			SetStats( this );

			Name = m_Myname;
		}

		public GrizzledMare( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( version < 1 )
			{
				Timer.DelayCall( TimeSpan.FromSeconds( 0 ), new TimerCallback( OnAfterDeserialize_Callback ) );
			}
		}
	}
}
