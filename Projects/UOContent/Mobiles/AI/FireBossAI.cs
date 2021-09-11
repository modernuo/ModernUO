using Server.Mobiles.BT;

namespace Server.Mobiles.AI
{
    public class FireBossAI : BaseAI
    {
        private Blackboard blackboard;
        public BehaviorTree Tree { get; private set; }
        public FireBossAI(BaseCreature m) : base(m)
        {
            blackboard = new Blackboard();
            m.PassiveSpeed = 0.05;
            m.ActiveSpeed = 0.05;
            m.CurrentSpeed = 0.05;

            Tree = new BehaviorTree();
            Tree.TryAddRoot(
                new SelectorNode(Tree)
                    .AddChild(
                        new SequenceNode(Tree)
                            .AddChild(new ConditionNode(Tree, (mob, board) => mob.Combatant != null))
                    )
            );
        }
        public override bool Think()
        {
            if (m_Mobile.Deleted || !m_Mobile.Alive)
            {
                return false;
            }

            Tree.RunBehavior(m_Mobile, blackboard);

            return base.Think();
        }
    }
}
