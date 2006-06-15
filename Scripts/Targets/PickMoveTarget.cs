using System;
using Server;
using Server.Targeting;
using Server.Commands;
using Server.Commands.Generic;

namespace Server.Targets
{
	public class PickMoveTarget : Target
	{
		public PickMoveTarget() : base( -1, false, TargetFlags.None )
		{
		}

		protected override void OnTarget( Mobile from, object o )
		{
			if ( !BaseCommand.IsAccessible( from, o ) )
			{
				from.SendMessage( "That is not accessible." );
				return;
			}

			if ( o is Item || o is Mobile )
				from.Target = new MoveTarget( o );
		}
	}
}