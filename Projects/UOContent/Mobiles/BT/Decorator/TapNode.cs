using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class TapNode : DecoratorNode
    {
        public delegate void TapNodeAction(BaseCreature mob, Blackboard blackboard);
        public TapNodeAction Action { get; private set; }
        public TapNode(BehaviorTree tree, TapNodeAction action) : base(tree)
        {
            Action = action;
        }
        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            Action(mob, blackboard);

            if (Child != null)
            {
                return Child.Execute(mob, blackboard);
            }

            return Result.Failure;
        }
    }
}
