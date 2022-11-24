using ModernUO.Serialization;
using Server.Guilds;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ChaosShield : BaseShield
{
    [Constructible]
    public ChaosShield() : base(0x1BC3)
    {
        if (!Core.AOS)
        {
            LootType = LootType.Newbied;
        }

        Weight = 5.0;
    }

    public override int BasePhysicalResistance => 1;
    public override int BaseFireResistance => 0;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 0;

    public override int InitMinHits => 100;
    public override int InitMaxHits => 125;

    public override int AosStrReq => 95;

    public override int ArmorBase => 32;

    public override bool OnEquip(Mobile from) => Validate(from) && base.OnEquip(from);

    public override void OnSingleClick(Mobile from)
    {
        if (Validate(Parent as Mobile))
        {
            base.OnSingleClick(from);
        }
    }

    public virtual bool Validate(Mobile m)
    {
        if (Core.AOS || m is not { Player: true, AccessLevel: AccessLevel.Player })
        {
            return true;
        }

        if (m.Guild is not Guild { Type: GuildType.Chaos })
        {
            m.FixedEffect(0x3728, 10, 13);
            Delete();

            return false;
        }

        return true;
    }
}
