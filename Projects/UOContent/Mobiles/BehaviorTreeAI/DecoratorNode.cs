using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorTreeAI
{
    public abstract class DecoratorNode : BehaviorTreeNode
    {
        public BehaviorTreeNode Child { get; set; }

        public DecoratorNode(BehaviorTree tree) : this(tree, null)
        {
        }
        public DecoratorNode(BehaviorTree tree, BehaviorTreeNode child) : base(tree)
        {
            Child = child;
        }
    }
}
