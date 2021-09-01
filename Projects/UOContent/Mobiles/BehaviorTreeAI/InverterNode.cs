using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorTreeAI
{
    public class InverterNode : DecoratorNode
    {
        public InverterNode(BehaviorTree tree) : base(tree)
        {
        }

        public override Result Execute()
        {
            if (Child != null)
            {
                var result = Child.Execute();

                if (result == Result.Success)
                {
                    return Result.Failure;
                }
                else if (result == Result.Failure)
                {
                    return Result.Success;
                }

                return Result.Running;
            }
            return Result.Success;
        }
    }
}
