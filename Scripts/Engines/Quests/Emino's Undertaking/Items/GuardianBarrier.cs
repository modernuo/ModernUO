using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Ninja
{
	public class GuardianBarrier : Item
	{
		[Constructible]
		public GuardianBarrier() : base( 0x3967 )
		{
			Movable = false;
			Visible = false;
		}

		public override bool OnMoveOver( Mobile m )
		{
			if ( m.AccessLevel > AccessLevel.Player )
				return true;

			// If the mobile is to the north of the barrier, allow him to pass
			if ( this.Y >= m.Y )
				return true;

			if ( m is BaseCreature creature )
			{
				Mobile master = creature.GetMaster();

				// Allow creatures to cross from the south to the north only if their master is near to the north
				return master != null && this.Y >= master.Y && master.InRange(this, 4);
			}

			if ( m is PlayerMobile pm )
			{
				if ( pm.Quest is EminosUndertakingQuest qs )
				{
					if ( qs.FindObjective( typeof( SneakPastGuardiansObjective ) ) is SneakPastGuardiansObjective obj )
					{
						if ( m.Hidden )
							return true; // Hidden ninjas can pass

						if ( !obj.TaughtHowToUseSkills )
						{
							obj.TaughtHowToUseSkills = true;
							qs.AddConversation( new NeedToHideConversation() );
						}
					}
				}
			}

			return false;
		}

		public GuardianBarrier( Serial serial ) : base( serial )
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
