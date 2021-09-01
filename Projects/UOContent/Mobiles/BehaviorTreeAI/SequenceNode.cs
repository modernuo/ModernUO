using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorTreeAI
{
    public class SequenceNode : BehaviorTreeNode
    {
        int currentTask = 0;
        public List<BehaviorTreeNode> Children { get; protected set; }
        public SequenceNode(BehaviorTree tree) : this(tree, new BehaviorTreeNode[0]) { }
        public SequenceNode(BehaviorTree tree, BehaviorTreeNode[] children) : base(tree)
        {
            Children = new List<BehaviorTreeNode>(children);
        }

        public override Result Execute()
        {
            if(currentTask < Children.Count)
            {
                var result = Children[currentTask].Execute();

                if (result == Result.Running)
                {
                    return Result.Running;
                }
                else if (result == Result.Failure)
                {
                    currentTask = 0;
                    return Result.Failure;
                }

                currentTask++;
                if(currentTask == Children.Count)
                {
                    currentTask = 0;
                    return Result.Success;
                }

                return Result.Running;
            }

            return Result.Success;
        }
    }
}
