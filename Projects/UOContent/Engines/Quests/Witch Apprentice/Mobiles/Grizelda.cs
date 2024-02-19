using ModernUO.Serialization;
using Server.Engines.Virtues;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Hag;

[SerializationGenerator(0, false)]
public partial class Grizelda : BaseQuester
{
    [Constructible]
    public Grizelda() : base("the Hag")
    {
    }

    public override bool ClickTitle => true;
    public override string DefaultName => "Grizelda";

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = 0x83EA;

        Female = true;
        Body = 0x191;
    }

    public override void InitOutfit()
    {
        AddItem(new Robe(0x1));
        AddItem(new Sandals());
        AddItem(new WizardsHat(0x1));
        AddItem(new GoldBracelet());

        HairItemID = 0x203C;

        Item staff = new GnarledStaff();
        staff.Movable = false;
        AddItem(staff);
    }

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        Direction = GetDirectionTo(player);

        var qs = player.Quest;

        if (qs is not WitchApprenticeQuest)
        {
            var newQuest = new WitchApprenticeQuest(player);

            if (qs != null)
            {
                newQuest.AddConversation(new DontOfferConversation());
            }
            else if (QuestSystem.CanOfferQuest(player, typeof(WitchApprenticeQuest), out var inRestartPeriod))
            {
                PlaySound(0x20);
                PlaySound(0x206);
                newQuest.SendOffer();
            }
            else if (inRestartPeriod)
            {
                PlaySound(0x259);
                PlaySound(0x206);
                newQuest.AddConversation(new RecentlyFinishedConversation());
            }

            return;
        }

        if (qs.IsObjectiveInProgress(typeof(FindApprenticeObjective)))
        {
            PlaySound(0x259);
            PlaySound(0x206);
            qs.AddConversation(new HagDuringCorpseSearchConversation());
            return;
        }

        if (qs.FindObjective<FindGrizeldaAboutMurderObjective>() is { Completed: false } obj1)
        {
            PlaySound(0x420);
            PlaySound(0x20);
            obj1.Complete();
            return;
        }

        if (qs.IsObjectiveInProgress(typeof(KillImpsObjective))
            || qs.IsObjectiveInProgress(typeof(FindZeefzorpulObjective)))
        {
            PlaySound(0x259);
            PlaySound(0x206);
            qs.AddConversation(new HagDuringImpSearchConversation());
            return;
        }

        if (qs.FindObjective<ReturnRecipeObjective>() is { Completed: false } obj2)
        {
            PlaySound(0x258);
            PlaySound(0x41B);
            obj2.Complete();
            return;
        }

        if (qs.IsObjectiveInProgress(typeof(FindIngredientObjective)))
        {
            PlaySound(0x259);
            PlaySound(0x206);
            qs.AddConversation(new HagDuringIngredientsConversation());
            return;
        }

        if (qs.FindObjective<ReturnIngredientsObjective>() is { Completed: false } obj3)
        {
            var cont = GetNewContainer();

            cont.DropItem(new BlackPearl(30));
            cont.DropItem(new Bloodmoss(30));
            cont.DropItem(new Garlic(30));
            cont.DropItem(new Ginseng(30));
            cont.DropItem(new MandrakeRoot(30));
            cont.DropItem(new Nightshade(30));
            cont.DropItem(new SulfurousAsh(30));
            cont.DropItem(new SpidersSilk(30));

            cont.DropItem(new Cauldron());
            cont.DropItem(new MoonfireBrew());
            cont.DropItem(new TreasureMap(Utility.RandomMinMax(1, 4), Map));
            cont.DropItem(new Gold(2000, 2200));

            if (Utility.RandomBool())
            {
                var weapon = Loot.RandomWeapon();

                if (Core.AOS)
                {
                    BaseRunicTool.ApplyAttributesTo(weapon, 2, 20, 30);
                }
                else
                {
                    weapon.DamageLevel = (WeaponDamageLevel)RandomMinMaxScaled(2, 3);
                    weapon.AccuracyLevel = (WeaponAccuracyLevel)RandomMinMaxScaled(2, 3);
                    weapon.DurabilityLevel = (WeaponDurabilityLevel)RandomMinMaxScaled(2, 3);
                }

                cont.DropItem(weapon);
            }
            else
            {
                Item item;

                if (Core.AOS)
                {
                    item = Loot.RandomArmorOrShieldOrJewelry();

                    if (item is BaseArmor armor)
                    {
                        BaseRunicTool.ApplyAttributesTo(armor, 2, 20, 30);
                    }
                    else if (item is BaseJewel jewel)
                    {
                        BaseRunicTool.ApplyAttributesTo(jewel, 2, 20, 30);
                    }
                }
                else
                {
                    var armor = Loot.RandomArmorOrShield();
                    item = armor;

                    armor.ProtectionLevel = (ArmorProtectionLevel)RandomMinMaxScaled(2, 3);
                    armor.Durability = (ArmorDurabilityLevel)RandomMinMaxScaled(2, 3);
                }

                cont.DropItem(item);
            }

            if (player.BAC > 0)
            {
                cont.DropItem(new HangoverCure());
            }

            if (player.PlaceInBackpack(cont))
            {
                var gainedPath = false;

                // TODO: Check amount on OSI.
                if (VirtueSystem.Award(player, VirtueName.Sacrifice, 250, ref gainedPath))
                {
                    player.SendLocalizedMessage(1054160); // You have gained in sacrifice.
                }

                PlaySound(0x253);
                PlaySound(0x20);
                obj3.Complete();
            }
            else
            {
                cont.Delete();
                // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                player.SendLocalizedMessage(1046260);
            }
        }
    }
}
