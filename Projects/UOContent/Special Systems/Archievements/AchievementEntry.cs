using Server.ContextMenus;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Mythik.Systems.Achievements
{
    /// <summary>
    /// If you want to add this context entry you will need to add a cliloc in the 300**** range, or
    /// use an existing one
    /// </summary>
    public class AchievementMenuEntry : ContextMenuEntry
    {
        private PlayerMobile _from;
        private PlayerMobile _target;

        public AchievementMenuEntry(PlayerMobile from,PlayerMobile target)
            : base(6252, -1) // View Achievements // 3006252
        {
            _from = from;
            _target = target;
        }

        public override void OnClick()
        {
            AchievementSystem.OpenGump(_from, _target);
        }
    }

}
