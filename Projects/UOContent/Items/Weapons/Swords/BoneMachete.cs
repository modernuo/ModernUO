using ModernUO.Serialization;
using Server.Engines.MLQuests.Items;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class BoneMachete : ElvenMachete, ITicket
    {
        [Constructible]
        public BoneMachete() => ItemID = 0x20E;

        public override WeaponAbility PrimaryAbility => null;
        public override WeaponAbility SecondaryAbility => null;

        public override int PhysicalResistance => 1;
        public override int FireResistance => 1;
        public override int ColdResistance => 1;
        public override int PoisonResistance => 1;
        public override int EnergyResistance => 1;

        public override int InitMinHits => 5;
        public override int InitMaxHits => 5;

        public void OnTicketUsed(Mobile from)
        {
            if (Utility.RandomDouble() < 0.25)
            {
                from.SendLocalizedMessage(
                    1075007
                ); // Your bone handled machete snaps in half as you force your way through the poisonous undergrowth.
                Delete();
            }
            else
            {
                from.SendLocalizedMessage(
                    1075008
                ); // Your bone handled machete has grown dull but you still manage to force your way past the venomous branches.
            }
        }
    }
}
