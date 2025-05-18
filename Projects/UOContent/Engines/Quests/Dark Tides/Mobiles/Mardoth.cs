using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Necro;

[SerializationGenerator(0, false)]
public partial class Mardoth : BaseQuester
{
    [Constructible]
    public Mardoth() : base("the Ancient Necromancer")
    {
    }

    public override string DefaultName => "Mardoth";

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = 0x8849;
        Body = 0x190;
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (dropped is not DarkTidesHorn horn || from is not PlayerMobile { Quest: DarkTidesQuest } player)
        {
            return base.OnDragDrop(from, dropped);
        }

        if (player.Young)
        {
            if (horn.Charges < 10)
            {
                SayTo(from, 1049384); // I have recharged the item for you.
                horn.Charges = 10;
            }
            else
            {
                SayTo(from, 1049385); // That doesn't need recharging yet.
            }
        }
        else
        {
            player.SendLocalizedMessage(1114333); // You must be young to have this item recharged.
        }

        return false;

    }

    public override void InitOutfit()
    {
        AddItem(new Sandals(0x1));
        AddItem(new Robe(0x66D));
        AddItem(new BlackStaff());
        AddItem(new WizardsHat(0x1));

        FacialHairItemID = 0x2041;
        FacialHairHue = 0x482;

        HairItemID = 0x203C;
        HairHue = 0x482;

        AddItem(new BoneGloves
        {
            Hue = 0x66D
        });

        AddItem(new PlateGorget
        {
            Hue = 0x1
        });
    }

    public override int GetAutoTalkRange(PlayerMobile m) => 3;

    public override bool CanTalkTo(PlayerMobile to)
    {
        if (to.Quest is not DarkTidesQuest qs)
        {
            return to.Quest == null && QuestSystem.CanOfferQuest(to, typeof(DarkTidesQuest));
        }

        return qs.FindObjective<FindMardothAboutVaultObjective>() != null;
    }

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        var qs = player.Quest;

        if (qs == null && QuestSystem.CanOfferQuest(player, typeof(DarkTidesQuest)))
        {
            new DarkTidesQuest(player).SendOffer();
            return;
        }

        if (qs is not DarkTidesQuest)
        {
            return;
        }

        if (DarkTidesQuest.HasLostCallingScroll(player))
        {
            qs.AddConversation(new LostCallingScrollConversation(true));
            return;
        }

        if (qs.FindObjective<FindMardothAboutVaultObjective>() is { Completed: false } obj1)
        {
            obj1.Complete();
            return;
        }

        if (qs.FindObjective<FindMardothAboutKronusObjective>() is { Completed: false } obj2)
        {
            obj2.Complete();
            return;
        }

        if (qs.FindObjective<FindMardothEndObjective>() is { Completed: false } obj3)
        {
            var cont = GetNewContainer();

            cont.DropItem(new PigIron(20));
            cont.DropItem(new NoxCrystal(20));
            cont.DropItem(new BatWing(25));
            cont.DropItem(new DaemonBlood(20));
            cont.DropItem(new GraveDust(20));

            BaseWeapon weapon = new BoneHarvester();

            weapon.Slayer = SlayerName.OrcSlaying;

            if (Core.AOS)
            {
                BaseRunicTool.ApplyAttributesTo(weapon, 3, 20, 40);
            }
            else
            {
                weapon.DamageLevel = (WeaponDamageLevel)RandomMinMaxScaled(2, 4);
                weapon.AccuracyLevel = (WeaponAccuracyLevel)RandomMinMaxScaled(2, 4);
                weapon.DurabilityLevel = (WeaponDurabilityLevel)RandomMinMaxScaled(2, 4);
            }

            cont.DropItem(weapon);

            cont.DropItem(new BankCheck(2000));
            cont.DropItem(new EnchantedSextant());

            if (!player.PlaceInBackpack(cont))
            {
                cont.Delete();
                // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                player.SendLocalizedMessage(1046260);
            }
            else
            {
                obj3.Complete();
            }
        }
        else if (contextMenu)
        {
            FocusTo(player);
            player.SendLocalizedMessage(1061821); // Mardoth has nothing more for you at this time.
        }
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        base.OnMovement(m, oldLocation);

        if (m is not PlayerMobile || m.Frozen || m.Alive || !InRange(m, 4) || InRange(oldLocation, 4) || !InLOS(m))
        {
            return;
        }

        if (m.Map?.CanFit(m.Location, 16, false, false) != true)
        {
            m.SendLocalizedMessage(502391); // Thou can not be resurrected there!
        }
        else
        {
            Direction = GetDirectionTo(m);

            m.PlaySound(0x214);
            m.FixedEffect(0x376A, 10, 16);

            m.SendGump(new ResurrectGump(m, ResurrectMessage.Healer));
        }
    }
}
