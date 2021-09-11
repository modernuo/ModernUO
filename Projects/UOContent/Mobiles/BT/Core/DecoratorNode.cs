using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public abstract class DecoratorNode : BehaviorTreeNode
    {
        public BehaviorTreeNode Child;
        public DecoratorNode(BehaviorTree tree) : base(tree)
        {
        }
        public DecoratorNode(BehaviorTree tree, BehaviorTreeNode child) : this(tree)
        {
            AddChild(child);
        }
        public virtual DecoratorNode AddChild(BehaviorTreeNode child)
        {
            if (Child == null)
            {
                Child = child;
            }
            return this;
        }
    }
}
