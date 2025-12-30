using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Haven;

[SerializationGenerator(0, false)]
public partial class SchmendrickApprenticeCorpse : Corpse
{
    private static int _hairHue;

    [SerializableField(0, setter: "private")]
    private Lantern _lantern;

    [Constructible]
    public SchmendrickApprenticeCorpse() : base(GetOwner(), GetHair(), GetFacialHair(), GetEquipment())
    {
        Direction = Direction.West;

        foreach (var item in EquipItems)
        {
            DropItem(item);
        }

        _lantern = new Lantern { Movable = false, Protected = true };
        _lantern.Ignite();

        Owner = null;
    }

    private static Mobile GetOwner()
    {
        var apprentice = new Mobile
        {
            Hue = Race.Human.RandomSkinHue(),
            Female = false,
            Body = 0x190,
            Name = NameList.RandomName("male")
        };

        apprentice.Delete();
        return apprentice;
    }

    private static List<Item> GetEquipment() =>
    [
        new Robe(QuestSystem.RandomBrightHue()),
        new WizardsHat(Utility.RandomNeutralHue()),
        new Shoes(Utility.RandomNeutralHue()),
        new Spellbook()
    ];

    private static VirtualHairInfo GetHair()
    {
        _hairHue = Race.Human.RandomHairHue();
        return new VirtualHairInfo(Race.Human.RandomHair(false), _hairHue);
    }

    private static VirtualHairInfo GetFacialHair()
    {
        _hairHue = Race.Human.RandomHairHue();

        return new VirtualHairInfo(Race.Human.RandomFacialHair(false), _hairHue);
    }

    public override void AddNameProperty(IPropertyList list)
    {
        if (ItemID == 0x2006) // Corpse form
        {
            list.Add("a human corpse");
            list.Add(1049144, Name); // the remains of ~1_NAME~ the apprentice
        }
        else
        {
            list.Add(1049145); // the remains of a wizard's apprentice
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        var hue = Notoriety.GetHue(NotorietyHandlers.CorpseNotoriety(from, this));

        if (ItemID == 0x2006) // Corpse form
        {
            // the remains of ~1_NAME~ the apprentice
            from.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Label, hue, 3, 1049144, "", Name);
        }
        else
        {
            // the remains of a wizard's apprentice
            from.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Label, hue, 3, 1049145);
        }
    }

    public override void Open(Mobile from, bool checkSelfLoot)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            return;
        }

        if (from is not PlayerMobile player)
        {
            return;
        }

        if (player.Quest is not UzeraanTurmoilQuest qs || qs.FindObjective<FindApprenticeObjective>() is not
                { Completed: false } obj)
        {
            // This is the corpse of a wizard's apprentice.  You can't bring yourself to search it without a good reason.
            from.SendLocalizedMessage(1049143, "", 0x22);
            return;
        }

        var scroll = new SchmendrickScrollOfPower();

        if (player.PlaceInBackpack(scroll))
        {
            player.SendLocalizedMessage(1049147, "", 0x22); // You find the scroll and put it in your pack.
            obj.Complete();
        }
        else
        {
            // You find the scroll, but can't pick it up because your pack is too full.  Come back when you have more room in your pack.
            player.SendLocalizedMessage(1049146, "", 0x22);
            scroll.Delete();
        }
    }

    public override void OnLocationChange(Point3D oldLoc)
    {
        if (_lantern?.Deleted == false)
        {
            _lantern.Location = new Point3D(X, Y + 1, Z);
        }
    }

    public override void OnMapChange()
    {
        if (_lantern?.Deleted == false)
        {
            _lantern.Map = Map;
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (_lantern?.Deleted == false)
        {
            _lantern.Delete();
        }

        Owner?.Delete();
    }
}
