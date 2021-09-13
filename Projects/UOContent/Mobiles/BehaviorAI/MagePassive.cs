namespace Server.Mobiles.BehaviorAI
{
    public class MagePassive : OwnerCondition
    {
        public MagePassive(BehaviorTree tree) : base(tree, canActivate)
        {
            AddChild(new Wander(tree, 2, 0.5));
        }
        public static bool canActivate(BehaviorTreeContext context)
        {
            return context.Mobile.Combatant == null;
        }
    }
}
