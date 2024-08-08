using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Haven;

[SerializationGenerator(0, false)]
public partial class MilitiaFighter : BaseCreature
{
    [Constructible]
    public MilitiaFighter() : base(AIType.AI_Melee)
    {
        InitStats(40, 30, 5);
        Title = "the Militia Fighter";

        SpeechHue = Utility.RandomDyedHue();

        Hue = Race.Human.RandomSkinHue();

        Female = false;
        Body = 0x190;
        Name = NameList.RandomName("male");

        Utility.AssignRandomHair(this);
        Utility.AssignRandomFacialHair(this, HairHue);

        AddItem(new ThighBoots(0x1BB));
        AddItem(new LeatherChest());
        AddItem(new LeatherArms());
        AddItem(new LeatherLegs());
        AddItem(new LeatherCap());
        AddItem(new LeatherGloves());
        AddItem(new LeatherGorget());

        var weapon = Utility.Random(6) switch
        {
            0 => (Item)new Broadsword(),
            1 => new Cutlass(),
            2 => new Katana(),
            3 => new Longsword(),
            4 => new Scimitar(),
            _ => new VikingSword()
        };

        weapon.Movable = false;
        AddItem(weapon);

        Item shield = new BronzeShield
        {
            Movable = false
        };

        AddItem(shield);

        SetSkill(SkillName.Swords, 20.0);
    }

    public override bool ClickTitle => false;

    public override bool IsEnemy(Mobile m)
    {
        while (!m.Player && m is not BaseVendor)
        {
            if (m is BaseCreature bc)
            {
                var master = bc.GetMaster();
                if (master != null)
                {
                    m = master;
                    continue;
                }
            }

            return m.Karma < 0;
        }

        return false;
    }
}

[SerializationGenerator(0, false)]
public partial class MilitiaFighterCorpse : Corpse
{
    public MilitiaFighterCorpse(Mobile owner, VirtualHairInfo hair, VirtualHairInfo facialhair, List<Item> equipItems) : base(
        owner,
        hair,
        facialhair,
        equipItems
    )
    {
    }

    public override void AddNameProperty(IPropertyList list)
    {
        if (ItemID == 0x2006) // Corpse form
        {
            list.Add("a human corpse");
            list.Add(1049318, Name); // the remains of ~1_NAME~ the militia fighter
        }
        else
        {
            list.Add(1049319); // the remains of a militia fighter
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        var hue = Notoriety.GetHue(NotorietyHandlers.CorpseNotoriety(from, this));

        if (ItemID == 0x2006) // Corpse form
        {
            // the remains of ~1_NAME~ the militia fighter
            from.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Label, hue, 3, 1049318, "", Name);
        }
        else
        {
            // the remains of a militia fighter
            from.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Label, hue, 3, 1049319);
        }
    }

    public override void Open(Mobile from, bool checkSelfLoot)
    {
        if (from.InRange(GetWorldLocation(), 2))
        {
            // Thinking about his sacrifice, you can't bring yourself to loot the body of this militia fighter.
            from.SendLocalizedMessage(1049661, "", 0x22);
        }
    }
}
