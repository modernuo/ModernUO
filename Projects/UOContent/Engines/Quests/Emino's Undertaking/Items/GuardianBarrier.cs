using Server.Mobiles;

namespace Server.Engines.Quests.Ninja
{
    public class GuardianBarrier : Item
    {
        [Constructible]
        public GuardianBarrier() : base(0x3967)
        {
            Movable = false;
            Visible = false;
        }

        public GuardianBarrier(Serial serial) : base(serial)
        {
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m.AccessLevel > AccessLevel.Player)
            {
                return true;
            }

            // If the mobile is to the north of the barrier, allow him to pass
            if (Y >= m.Y)
            {
                return true;
            }

            if (m is BaseCreature creature)
            {
                var master = creature.GetMaster();

                // Allow creatures to cross from the south to the north only if their master is near to the north
                return master != null && Y >= master.Y && master.InRange(this, 4);
            }

            if (m is PlayerMobile pm && pm.Quest is EminosUndertakingQuest qs)
            {
                var obj = qs.FindObjective<SneakPastGuardiansObjective>();
                if (obj != null)
                {
                    if (m.Hidden)
                    {
                        return true; // Hidden ninjas can pass
                    }

                    if (!obj.TaughtHowToUseSkills)
                    {
                        obj.TaughtHowToUseSkills = true;
                        qs.AddConversation(new NeedToHideConversation());
                    }
                }
            }

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
