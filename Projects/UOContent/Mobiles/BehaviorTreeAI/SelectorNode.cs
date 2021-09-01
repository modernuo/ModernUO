using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorTreeAI
{
    class SelectorNode : BehaviorTreeNode
    {
        int currentTask = 0;
        public List<BehaviorTreeNode> Children { get; protected set; }
        public SelectorNode(BehaviorTree tree) : this(tree, new BehaviorTreeNode[0]) { }
        public SelectorNode(BehaviorTree tree, BehaviorTreeNode[] children) : base(tree)
        {
            Children = new List<BehaviorTreeNode>(children);
        }

        public override Result Execute()
        {
            if (currentTask < Children.Count)
            {
                var result = Children[currentTask].Execute();

                if (result == Result.Success)
                {
                    currentTask = 0;
                    return Result.Success;
                }
                else if (result == Result.Running)
                {
                    return Result.Running;
                }

                currentTask++;
                if (currentTask == Children.Count)
                {
                    currentTask = 0;
                    return Result.Failure;
                }

                return Result.Running;
            }

            return Result.Success;
        }
    }
}
