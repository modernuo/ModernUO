using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class SetBlackboardData<T> : ActionNode
    {
        public delegate T BlackboardDataTransformation(BaseCreature mob);
        private BlackboardDataTransformation transformation;
        private string blackboardKey;
        public SetBlackboardData(BehaviorTree tree, string key, BlackboardDataTransformation fn) : base(tree)
        {
            blackboardKey = key;
            transformation = fn;
        }
        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            T value = transformation(mob);
            if(blackboard.TryGetValue(blackboardKey, out object oldValue) && (T)oldValue != null)
            {
                blackboard[blackboardKey] = (object)value;
            }
            else
            {
                blackboard.Add(blackboardKey, (object)value);
            }
            return Result.Success;
        }
    }
}
