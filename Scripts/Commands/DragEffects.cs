using System;
using Server;

namespace Server.Commands
{
	/*
	 * Temporary system to allow on-the-fly enabling and disabling
	 * of the drag effects, for investigating client crashes.
	 *
	 * By default the effects will be disabled.
	 */
	public static class DragEffects
	{
		public static void Initialize()
		{
			// Disable effects
			Mobile.DragEffects = false;

			CommandSystem.Register( "DragEffects", AccessLevel.Administrator, new CommandEventHandler( DragEffects_OnCommand ) );
		}

		[Usage( "DragEffects [enable=false]" )]
		[Description( "Enables or disables the item drag and drop effects." )]
		public static void DragEffects_OnCommand( CommandEventArgs e )
		{
			if ( e.Length == 0 )
			{
				e.Mobile.SendMessage( "Drag effects are currently {0}.", Mobile.DragEffects ? "enabled" : "disabled" );
			}
			else
			{
				Mobile.DragEffects = e.GetBoolean( 0 );

				e.Mobile.SendMessage( "Drag effects have been {0}.", Mobile.DragEffects ? "enabled" : "disabled" );
			}
		}
	}
}
