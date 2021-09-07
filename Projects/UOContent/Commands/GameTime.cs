using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Commands
{
    public static class GameTime
    {
        public static void Initialize()
        {
            CommandSystem.Register("GameTime", AccessLevel.Player, GameTime_OnCommand);
        }

        [Usage("GameTime"), Description("Returns the player's game time.")]
        private static void GameTime_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                e.Mobile.SendMessage($"Your game time is: {pm.GameTime}");
            }
            
        }
    }
}
