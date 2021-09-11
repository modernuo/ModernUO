using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public abstract class CompositeNode : BehaviorTreeNode
    {
        protected Dictionary<BaseCreature, int> currentTaskCache;
        public List<BehaviorTreeNode> Children { get; protected set; }
        public CompositeNode(BehaviorTree tree) : base(tree)
        {
            currentTaskCache = new Dictionary<BaseCreature, int>();
            Children = new List<BehaviorTreeNode>();
        }
        public virtual CompositeNode AddChild(BehaviorTreeNode child)
        {
            child.Parent = this;
            Children.Add(child);
            return this;
        }
    }
}
