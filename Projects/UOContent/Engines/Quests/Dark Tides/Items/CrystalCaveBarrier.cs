using Server.Mobiles;

namespace Server.Engines.Quests.Necro
{
    public class CrystalCaveBarrier : Item
    {
        [Constructible]
        public CrystalCaveBarrier() : base(0x3967) => Movable = false;

        public CrystalCaveBarrier(Serial serial) : base(serial)
        {
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m.AccessLevel > AccessLevel.Player)
            {
                return true;
            }

            var mob = m;

            if (m is BaseCreature creature)
            {
                mob = creature.ControlMaster;
            }

            if (!(mob is PlayerMobile pm))
            {
                return false;
            }

            var qs = pm.Quest;

            if (qs is DarkTidesQuest)
            {
                QuestObjective obj = qs.FindObjective<SpeakCavePasswordObjective>();

                if (obj?.Completed == true)
                {
                    m.SendLocalizedMessage(
                        1060648
                    ); // With Horus' permission, you are able to pass through the barrier.

                    return true;
                }
            }

            m.SendLocalizedMessage(
                1060649,
                "",
                0x66D
            ); // Without the permission of the guardian Horus, the magic of the barrier prevents your passage.

            return false;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
