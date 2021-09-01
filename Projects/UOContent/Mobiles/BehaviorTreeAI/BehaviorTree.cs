using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorTreeAI
{
    public class BehaviorTree
    {
        /*
        public static BehaviorTree Instance;
        private static BehaviorTreeTimer timer;
        public static void Initialize()
        {
            Instance = new BehaviorTree();

            var seq = new SequenceNode(Instance, new BehaviorTreeNode[3]{
                new ActionNode(Instance, () => { Console.WriteLine("ActionNode1"); return BehaviorTreeNode.Result.Success; }),
                new ActionNode(Instance, () => { Console.WriteLine("ActionNode2"); return BehaviorTreeNode.Result.Success; }),
                new SequenceNode(Instance, new BehaviorTreeNode[4]{
                    new ActionNode(Instance, () => { Console.WriteLine("ActionNode3"); return BehaviorTreeNode.Result.Success; }),
                    new ActionNode(Instance, () => { Console.WriteLine("ActionNode4"); return BehaviorTreeNode.Result.Success; }),
                    new ActionNode(Instance, () => { Console.WriteLine("ActionNode5"); return BehaviorTreeNode.Result.Success; }),
                    new SelectorNode(Instance, new BehaviorTreeNode[3]
                    {
                        new ActionNode(Instance, () => { Console.WriteLine("ActionNode6"); return BehaviorTreeNode.Result.Failure; }),
                        new ActionNode(Instance, () => { Console.WriteLine("ActionNode7"); return BehaviorTreeNode.Result.Success; }),
                        new ActionNode(Instance, () => { Console.WriteLine("ActionNode8"); return BehaviorTreeNode.Result.Success; })
                    })
                }),
            });

            Instance.AddRoot(
                new RepeaterNode(Instance, seq)
            );

            Instance.Start();
        }
        */

        private BehaviorTreeNode root;
        public Dictionary<string, object> Blackboard { get; set; }
        public BehaviorTreeNode Root { get { return root; } }

        private BehaviorTreeTimer treeTimer;

        public BaseCreature Mobile { get; private set; }

        public BehaviorTree(BaseCreature mob)
        {
            Blackboard = new Dictionary<string, object>();
            treeTimer = new BehaviorTreeTimer(this);
            Mobile = mob;
        }

        public void AddRoot(BehaviorTreeNode rootNode)
        {
            if (root == null)
            {
                root = rootNode;
            }
        }

        public void Start()
        {
            treeTimer.Start();
        }

        public void Stop()
        {
            treeTimer.Stop();
        }

        public virtual void Tick()
        {
            RunBehavior();
        }

        public virtual void RunBehavior()
        {
            Root.Execute();
        }

        private class BehaviorTreeTimer : Timer
        {
            private BehaviorTree behaviorTree;
            public BehaviorTreeTimer(BehaviorTree tree) : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
            {
                behaviorTree = tree;
            }

            protected override void OnTick()
            {
                if (behaviorTree != null)
                {
                    behaviorTree.Tick();
                }
            }
        }
    }
    /*
    public enum ControlFlowNodeType
    {
        Sequence,
        Fallback,
        Parallel,
        Decorator
    }

    public enum ExecutionNodeType
    {
        Action,
        Condition
    }

    public class BehaviorTreeNode
    {
        private readonly List<BehaviorTreeNode> _children = new List<BehaviorTreeNode>();
        public bool Root { get { return Parent == null; } }

        public BehaviorTreeNode() { }
        public BehaviorTreeNode(BehaviorTreeNode parent) : this()
        {
            Parent = parent;
        }

        public BehaviorTreeNode this[int i]
        {
            get { return _children[i]; }
        }

        public BehaviorTreeNode Parent { get; private set; }

        public ReadOnlyCollection<BehaviorTreeNode> Children
        {
            get { return _children.AsReadOnly(); }
        }

        public virtual BehaviorTreeNode AddChild(BehaviorTreeNode node)
        {
            _children.Add(node);
            return node;
        }

        public virtual bool Evaluate(BaseCreature creature)
        {
            return false;
        }

        public virtual bool Traverse(BaseCreature creature)
        {
            foreach (var child in _children)
                child.Traverse(creature);
        }
    }

    public class SequenceNode : BehaviorTreeNode {
        public override bool Evaluate(BaseCreature creature)
        {
            return Children.All((BehaviorTreeNode node) => node.Evaluate(creature));
        }

        public override bool Traverse(BaseCreature creature)
        {
            if (Evaluate(creature)) return true;

            foreach (var child in Children)
                child.Traverse(creature);
        }
    }

    public class FallbackNode : BehaviorTreeNode
    {
        public override bool Evaluate(BaseCreature creature)
        {
            return Children.Any((BehaviorTreeNode node) => node.Evaluate(creature));
        }

        public override bool Traverse(BaseCreature creature)
        {
            return Evaluate(creature);
        }
    }

    public class ParallelNode : BehaviorTreeNode
    {

    }

    public class DecoratorNode : BehaviorTreeNode
    {

    }

    public class ActionNode : BehaviorTreeNode
    {

    }

    public class ConditionNode : BehaviorTreeNode
    {

    }
    */
}
