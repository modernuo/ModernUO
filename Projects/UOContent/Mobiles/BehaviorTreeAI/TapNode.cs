using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorTreeAI
{
    public class TapNode : DecoratorNode
    {
        public Action<BaseCreature> Action { get; private set; }
        public TapNode(BehaviorTree tree, Action<BaseCreature> action, BehaviorTreeNode child) : base(tree, child)
        {
        }
        public TapNode(BehaviorTree tree, Action<BaseCreature> action) : base(tree)
        {
            Action = action;
        }
        public override Result Execute()
        {
            Action(Tree.Mobile);

            if (Child != null)
            {
                return Child.Execute();
            }

            return Result.Failure;
        }
    }
}
