using Server.Mobiles;

namespace Server.Engines.Quests.Hag
{
    public class MagicFlute : Item
    {
        [Constructible]
        public MagicFlute() : base(0x1421) => Hue = 0x8AB;

        public MagicFlute(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1055051; // magic flute

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                SendLocalizedMessageTo(from, 1042292); // You must have the object in your backpack to use it.
                return;
            }

            from.PlaySound(0x3D);

            if (from is PlayerMobile player)
            {
                var qs = player.Quest;

                if (qs is WitchApprenticeQuest)
                {
                    var obj = qs.FindObjective<FindZeefzorpulObjective>();
                    if (obj?.Completed == false)
                    {
                        if (player.Map != Map.Trammel && player.Map != Map.Felucca || !player.InRange(obj.ImpLocation, 8))
                        {
                            player.SendLocalizedMessage(
                                1055053
                            ); // Nothing happens. Zeefzorpul must not be hiding in this area.
                        }
                        else if (player.InRange(obj.ImpLocation, 4))
                        {
                            Delete();

                            obj.Complete();
                        }
                        else
                        {
                            player.SendLocalizedMessage(
                                1055052
                            ); // The flute sparkles. Zeefzorpul must be in a good hiding place nearby.
                        }
                    }
                }
            }
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
