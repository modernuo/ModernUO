using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Factions;

[SerializationGenerator(0, false)]
public partial class FactionHorseVendor : BaseFactionVendor
{
    public FactionHorseVendor(Town town, Faction faction) : base(town, faction, "the Horse Breeder")
    {
        SetSkill(SkillName.AnimalLore, 64.0, 100.0);
        SetSkill(SkillName.AnimalTaming, 90.0, 100.0);
        SetSkill(SkillName.Veterinary, 65.0, 88.0);
    }

    public override VendorShoeType ShoeType => Female ? VendorShoeType.ThighBoots : VendorShoeType.Boots;

    public override void InitSBInfo()
    {
    }

    public override int GetShoeHue() => 0;

    public override void InitOutfit()
    {
        base.InitOutfit();

        AddItem(Utility.RandomBool() ? new QuarterStaff() : new ShepherdsCrook());
    }

    public override void VendorBuy(Mobile from)
    {
        if (Faction == null || Faction.Find(from, true) != Faction)
        {
            PrivateOverheadMessage(
                MessageType.Regular,
                0x3B2,
                1042201, // You are not in my faction, I cannot sell you a horse!
                from.NetState
            );
        }
        else if (FactionGump.Exists(from))
        {
            from.SendLocalizedMessage(1042160); // You already have a faction menu open.
        }
        else if (from is PlayerMobile mobile)
        {
            mobile.SendGump(new HorseBreederGump(mobile, Faction));
        }
    }

    public override void VendorSell(Mobile from)
    {
    }

    public override bool OnBuyItems(Mobile buyer, List<BuyItemResponse> list) => false;

    public override bool OnSellItems(Mobile seller, List<SellItemResponse> list) => false;
}
