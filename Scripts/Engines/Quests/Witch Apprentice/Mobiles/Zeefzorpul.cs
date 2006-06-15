using System;
using Server;
using Server.Mobiles;
using Server.Items;

namespace Server.Engines.Quests.Hag
{
	public class Zeefzorpul : BaseQuester
	{
		public Zeefzorpul()
		{
		}

		public override void InitBody()
		{
			Body = 0x4A;
			Name = "Zeefzorpul";
		}

		public Zeefzorpul( Serial serial ) : base( serial )
		{
		}

		public override bool CanTalkTo( PlayerMobile to )
		{
			return false;
		}

		public override void OnTalk( PlayerMobile player, bool contextMenu )
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

			Delete();
		}
	}
}