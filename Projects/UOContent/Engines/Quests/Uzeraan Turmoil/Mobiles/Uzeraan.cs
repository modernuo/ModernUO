using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Haven;

[SerializationGenerator(0, false)]
public partial class Uzeraan : BaseQuester
{
    [Constructible]
    public Uzeraan() : base("the Conjurer")
    {
    }

    public override string DefaultName => "Uzeraan";

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = 0x83F3;

        Female = false;
        Body = 0x190;
    }

    public override void InitOutfit()
    {
        AddItem(new Robe(0x4DD));
        AddItem(new WizardsHat(0x8A5));
        AddItem(new Shoes(0x8A5));

        HairItemID = 0x203C;
        HairHue = 0x455;

        FacialHairItemID = 0x203E;
        FacialHairHue = 0x455;

        var staff = new BlackStaff
        {
            Movable = false
        };

        AddItem(staff);
    }

    public override int GetAutoTalkRange(PlayerMobile pm) => 3;

    public override bool CanTalkTo(PlayerMobile to) => to.Quest is UzeraanTurmoilQuest;

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        var qs = player.Quest;

        if (qs is not UzeraanTurmoilQuest)
        {
            return;
        }

        if (UzeraanTurmoilQuest.HasLostScrollOfPower(player))
        {
            qs.AddConversation(new LostScrollOfPowerConversation(true));
            return;
        }

        if (UzeraanTurmoilQuest.HasLostFertileDirt(player))
        {
            qs.AddConversation(new LostFertileDirtConversation(true));
            return;
        }

        if (UzeraanTurmoilQuest.HasLostDaemonBlood(player))
        {
            qs.AddConversation(new LostDaemonBloodConversation());
            return;
        }

        if (UzeraanTurmoilQuest.HasLostDaemonBone(player))
        {
            qs.AddConversation(new LostDaemonBoneConversation());
            return;
        }

        if (player.Profession == 2) // magician
        {
            var backpack = player.Backpack;

            if (backpack == null
                || backpack.GetAmount(typeof(BlackPearl)) < 30
                || backpack.GetAmount(typeof(Bloodmoss)) < 30
                || backpack.GetAmount(typeof(Garlic)) < 30
                || backpack.GetAmount(typeof(Ginseng)) < 30
                || backpack.GetAmount(typeof(MandrakeRoot)) < 30
                || backpack.GetAmount(typeof(Nightshade)) < 30
                || backpack.GetAmount(typeof(SulfurousAsh)) < 30
                || backpack.GetAmount(typeof(SpidersSilk)) < 30)
            {
                qs.AddConversation(new FewReagentsConversation());
            }
        }

        if (qs.FindObjective<FindUzeraanBeginObjective>() is { Completed: false } obj1)
        {
            obj1.Complete();
            return;
        }

        if (qs.FindObjective<FindUzeraanFirstTaskObjective>() is { Completed: false } obj2)
        {
            obj2.Complete();
            return;
        }

        if (qs.FindObjective<FindUzeraanAboutReportObjective>() is { Completed: false } obj3)
        {
            var cont = GetNewContainer();

            if (player.Profession == 2) // magician
            {
                cont.DropItem(new MarkScroll(5));
                cont.DropItem(new RecallScroll(5));
                for (var i = 0; i < 5; i++)
                {
                    cont.DropItem(new RecallRune());
                }
            }
            else
            {
                cont.DropItem(new Gold(300));
                for (var i = 0; i < 6; i++)
                {
                    cont.DropItem(new NightSightPotion());
                    cont.DropItem(new LesserHealPotion());
                }
            }

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

            return;
        }

        if (qs.FindObjective<ReturnScrollOfPowerObjective>()?.Completed == false)
        {
            FocusTo(player);
            SayTo(player, 1049378); // Hand me the scroll, if you have it.
            return;
        }

        if (qs.FindObjective<ReturnFertileDirtObjective>()?.Completed == false)
        {
            FocusTo(player);
            SayTo(player, 1049381); // Hand me the Fertile Dirt, if you have it.
            return;
        }

        if (qs.FindObjective<ReturnDaemonBloodObjective>()?.Completed == false)
        {
            FocusTo(player);
            SayTo(player, 1049379); // Hand me the Vial of Blood, if you have it.
            return;
        }

        if (qs.FindObjective<ReturnDaemonBoneObjective>()?.Completed == false)
        {
            FocusTo(player);
            SayTo(player, 1049380); // Hand me the Daemon Bone, if you have it.
            return;
        }

        SayTo(player, 1049357); // I have nothing more for you at this time.
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (from is not PlayerMobile player)
        {
            return base.OnDragDrop(from, dropped);
        }

        var qs = player.Quest;

        if (qs is not UzeraanTurmoilQuest)
        {
            return base.OnDragDrop(from, dropped);
        }

        if (dropped is UzeraanTurmoilHorn horn)
        {
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

        if (dropped is SchmendrickScrollOfPower)
        {
            var obj = qs.FindObjective<ReturnScrollOfPowerObjective>();

            if (obj?.Completed == false)
            {
                var cont = GetNewContainer();

                cont.DropItem(new TreasureMap(player.Young ? 0 : 1, Map.Trammel));
                cont.DropItem(new Shovel());
                cont.DropItem(new UzeraanTurmoilHorn());

                if (!player.PlaceInBackpack(cont))
                {
                    cont.Delete();
                    // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                    player.SendLocalizedMessage(1046260);
                    return false;
                }

                dropped.Delete();
                obj.Complete();
                return true;
            }
        }
        else if (dropped is QuestFertileDirt)
        {
            var obj = qs.FindObjective<ReturnFertileDirtObjective>();

            if (obj?.Completed == false)
            {
                var cont = GetNewContainer();

                if (player.Profession == 2) // magician
                {
                    cont.DropItem(new BlackPearl(20));
                    cont.DropItem(new Bloodmoss(20));
                    cont.DropItem(new Garlic(20));
                    cont.DropItem(new Ginseng(20));
                    cont.DropItem(new MandrakeRoot(20));
                    cont.DropItem(new Nightshade(20));
                    cont.DropItem(new SulfurousAsh(20));
                    cont.DropItem(new SpidersSilk(20));

                    for (var i = 0; i < 3; i++)
                    {
                        cont.DropItem(Loot.RandomScroll(0, 23, SpellbookType.Regular));
                    }
                }
                else
                {
                    cont.DropItem(new Gold(300));
                    cont.DropItem(new Bandage(25));

                    for (var i = 0; i < 5; i++)
                    {
                        cont.DropItem(new LesserHealPotion());
                    }
                }

                if (!player.PlaceInBackpack(cont))
                {
                    cont.Delete();
                    // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                    player.SendLocalizedMessage(1046260);
                    return false;
                }

                dropped.Delete();
                obj.Complete();
                return true;
            }
        }
        else if (dropped is QuestDaemonBlood)
        {
            var obj = qs.FindObjective<ReturnDaemonBloodObjective>();

            if (obj?.Completed == false)
            {
                Item reward;

                if (player.Profession == 2) // magician
                {
                    var cont = GetNewContainer();

                    cont.DropItem(new ExplosionScroll(4));
                    cont.DropItem(new MagicWizardsHat());

                    reward = cont;
                }
                else
                {
                    var weapon = Utility.Random(6) switch
                    {
                        0 => (BaseWeapon)new Broadsword(),
                        1 => new Cutlass(),
                        2 => new Katana(),
                        3 => new Longsword(),
                        4 => new Scimitar(),
                        _ => new VikingSword()
                    };

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

                    weapon.Slayer = SlayerName.Silver;

                    reward = weapon;
                }

                if (!player.PlaceInBackpack(reward))
                {
                    reward.Delete();
                    // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                    player.SendLocalizedMessage(1046260);
                    return false;
                }

                dropped.Delete();
                obj.Complete();
                return true;
            }
        }
        else if (dropped is QuestDaemonBone)
        {
            var obj = qs.FindObjective<ReturnDaemonBoneObjective>();

            if (obj?.Completed == false)
            {
                var cont = GetNewContainer();
                cont.DropItem(new BankCheck(2000));
                cont.DropItem(new EnchantedSextant());

                if (!player.PlaceInBackpack(cont))
                {
                    cont.Delete();
                    // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                    player.SendLocalizedMessage(1046260);
                    return false;
                }

                dropped.Delete();
                obj.Complete();
                return true;
            }
        }

        return base.OnDragDrop(from, dropped);
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        base.OnMovement(m, oldLocation);

        if (m is PlayerMobile && !m.Frozen && !m.Alive && InRange(m, 4) && !InRange(oldLocation, 4) && InLOS(m))
        {
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
}
