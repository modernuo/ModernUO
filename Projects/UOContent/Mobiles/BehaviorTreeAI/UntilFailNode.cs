using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorTreeAI
{
    public class UntilFailNode : DecoratorNode
    {
        public UntilFailNode(BehaviorTree tree) : base(tree)
        {
        }

        public override Result Execute()
        {
            if (Child != null)
            {
                var result = Child.Execute();

                if (result == Result.Failure)
                {
                    return Result.Failure;
                }
            }
            return Result.Running;
        }
    }
}
