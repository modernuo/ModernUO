using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items
{
    [Flippable(0x13B2, 0x13B1)]
    [SerializationGenerator(0, false)]
    public partial class JukaBow : Bow
    {
        [Constructible]
        public JukaBow()
        {
        }

        public override int AosStrengthReq => 80;
        public override int AosDexterityReq => 80;

        public override int OldStrengthReq => 80;
        public override int OldDexterityReq => 80;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsModified => Hue == 0x453;

        public override void OnDoubleClick(Mobile from)
        {
            if (IsModified)
            {
                from.SendMessage("That has already been modified.");
            }
            else if (!IsChildOf(from.Backpack))
            {
                from.SendMessage("This must be in your backpack to modify it.");
            }
            else if (from.Skills.Fletching.Base < 100.0)
            {
                from.SendMessage("Only a grandmaster bowcrafter can modify this weapon.");
            }
            else
            {
                from.BeginTarget(2, false, TargetFlags.None, OnTargetGears);
                from.SendMessage("Select the gears you wish to use.");
            }
        }

        public void OnTargetGears(Mobile from, object targ)
        {
            if (targ is not Gears g || !g.IsChildOf(from.Backpack))
            {
                from.SendMessage(
                    "Those are not gears."
                ); // Apparently gears that aren't in your backpack aren't really gears at all. :-(
            }
            else if (IsModified)
            {
                from.SendMessage("That has already been modified.");
            }
            else if (!IsChildOf(from.Backpack))
            {
                from.SendMessage("This must be in your backpack to modify it.");
            }
            else if (from.Skills.Fletching.Base < 100.0)
            {
                from.SendMessage("Only a grandmaster bowcrafter can modify this weapon.");
            }
            else
            {
                g.Consume();

                Hue = 0x453;
                Slayer = (SlayerName)Utility.Random(2, 25);

                from.SendMessage("You modify it.");
            }
        }
    }
}
