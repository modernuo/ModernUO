using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class EquipItemNode : ActionNode
    {
        private Type itemType;
        public EquipItemNode(BehaviorTree tree, Type type) : base(tree)
        {
            itemType = type;
        }
        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            Item item = mob.Backpack.FindItemByType(itemType);

            if (item == null || !mob.EquipItem(item))
            {
                return Result.Failure;
            }

            return Result.Success;
        }
    }
}
