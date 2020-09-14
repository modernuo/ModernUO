using Server.Items;

namespace Server.ContextMenus
{
    public class EatEntry : ContextMenuEntry
    {
        private readonly Food m_Food;
        private readonly Mobile m_From;

        public EatEntry(Mobile from, Food food) : base(6135, 1)
        {
            m_From = from;
            m_Food = food;
        }

        public override void OnClick()
        {
            if (m_Food?.Deleted != false || !m_Food.Movable || !m_From.CheckAlive() || !m_Food.CheckItemUse(m_From))
            {
                return;
            }

            m_Food.Eat(m_From);
        }
    }
}
