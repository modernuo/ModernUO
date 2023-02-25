using System;

namespace Server.Items
{
    public class PlagueBeastVein : PlagueBeastComponent
    {
        private bool _cutting;

        public PlagueBeastVein(int itemID, int hue) : base(itemID, hue) => Cut = false;

        public PlagueBeastVein(Serial serial) : base(serial)
        {
        }

        public bool Cut { get; private set; }

        public override bool Scissor(Mobile from, Scissors scissors)
        {
            if (IsAccessibleTo(from))
            {
                if (!Cut && !_cutting)
                {
                    _cutting = true;
                    Timer.StartTimer(TimeSpan.FromSeconds(3), () => CuttingDone(from));
                    scissors.PublicOverheadMessage(
                        MessageType.Regular,
                        0x3B2,
                        1071899 // You begin cutting through the vein.
                    );
                    return true;
                }

                scissors.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1071900); // // This vein has already been cut.
            }

            return false;
        }

        private void CuttingDone(Mobile from)
        {
            _cutting = false;
            Cut = true;

            ItemID = ItemID == 0x1B1C ? 0x1B1B : 0x1B1C;

            Owner?.PlaySound(0x199);

            if (Organ is PlagueBeastRubbleOrgan organ)
            {
                organ.OnVeinCut(from, this);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(Cut);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            Cut = reader.ReadBool();
        }
    }
}
