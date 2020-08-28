namespace Server.Engines.Quests.Hag
{
    public class HangoverCure : Item
    {
        [Constructible]
        public HangoverCure() : base(0xE2B)
        {
            Weight = 1.0;
            Hue = 0x2D;

            Uses = 20;
        }

        public HangoverCure(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1055060; // Grizelda's Extra Strength Hangover Cure

        [CommandProperty(AccessLevel.GameMaster)]
        public int Uses { get; set; }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                SendLocalizedMessageTo(from, 1042038); // You must have the object in your backpack to use it.
                return;
            }

            if (Uses > 0)
            {
                from.PlaySound(0x2D6);
                from.SendLocalizedMessage(501206); // An awful taste fills your mouth.

                if (from.BAC > 0)
                {
                    from.BAC = 0;
                    from.SendLocalizedMessage(501204); // You are now sober!
                }

                Uses--;
            }
            else
            {
                Delete();
                from.SendLocalizedMessage(501201); // There wasn't enough left to have any effect.
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.WriteEncodedInt(Uses);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        Uses = reader.ReadEncodedInt();
                        break;
                    }
                case 0:
                    {
                        Uses = 20;
                        break;
                    }
            }
        }
    }
}
