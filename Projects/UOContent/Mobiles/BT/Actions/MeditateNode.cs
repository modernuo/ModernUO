using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class MeditateNode : CooldownNode
    {
        public MeditateNode(BehaviorTree tree, int desiredMana) : base(tree, TimeSpan.FromSeconds(12.0))
        {
            AddChild(
                new SequenceNode(tree)
                    .AddChild(new UseSkillNode(tree, SkillName.Meditation))
                    .AddChild(new InverterNode(tree, new UntilFailNode(tree, new ConditionNode(tree, (mob, board) => mob.Mana < desiredMana || mob.Meditating))))
            );
        }
    }
}
