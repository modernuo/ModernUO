using System;
using Server.Mobiles;

namespace Server.Items
{
    public class PlagueBeastMutationCore : Item, IScissorable
    {
        [Constructible]
        public PlagueBeastMutationCore() : base(0x1CF0)
        {
            Cut = true;
            Weight = 1.0;
            Hue = 0x480;
        }

        public PlagueBeastMutationCore(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Cut { get; set; }

        public override string DefaultName => "a plague beast mutation core";

        public virtual bool Scissor(Mobile from, Scissors scissors)
        {
            if (!Cut)
            {
                var owner = RootParent as PlagueBeastLord;

                Cut = true;
                Movable = true;

                from.AddToBackpack(this);
                from.LocalOverheadMessage(
                    MessageType.Regular,
                    0x34,
                    1071906
                ); // * You remove the plague mutation core from the plague beast, causing it to dissolve into a pile of goo *

                if (owner != null)
                {
                    Timer.StartTimer(TimeSpan.FromSeconds(1),
                        () =>
                        {
                            owner.Unfreeze();
                            owner.Kill();
                        }
                    );
                }

                return true;
            }

            return false;
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
