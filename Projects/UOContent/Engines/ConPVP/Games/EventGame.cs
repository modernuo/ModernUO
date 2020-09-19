using Server.Gumps;
using Server.Items;

namespace Server.Engines.ConPVP
{
    public abstract class EventController : Item
    {
        public EventController()
            : base(0x1B7A)
        {
            Visible = false;
            Movable = false;
        }

        public EventController(Serial serial)
            : base(serial)
        {
        }

        public abstract string Title { get; }
        public abstract EventGame Construct(DuelContext dc);

        public abstract string GetTeamName(int teamID);

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                from.SendGump(new PropertiesGump(from, this));
            }
        }
    }

    public abstract class EventGame
    {
        protected DuelContext m_Context;

        public EventGame(DuelContext context) => m_Context = context;

        public DuelContext Context => m_Context;

        public virtual bool FreeConsume => true;

        public virtual bool OnDeath(Mobile mob, Container corpse) => true;

        public virtual bool CantDoAnything(Mobile mob) => false;

        public virtual void OnStart()
        {
        }

        public virtual void OnStop()
        {
        }
    }
}
