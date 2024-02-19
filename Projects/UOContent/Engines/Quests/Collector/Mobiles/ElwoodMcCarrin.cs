using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Collector;

[SerializationGenerator(0, false)]
public partial class ElwoodMcCarrin : BaseQuester
{
    [Constructible]
    public ElwoodMcCarrin() : base("the well-known collector")
    {
    }

    public override string DefaultName => "Elwood McCarrin";

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = 0x83ED;

        Female = false;
        Body = 0x190;
    }

    public override void InitOutfit()
    {
        AddItem(new FancyShirt());
        AddItem(new LongPants(0x544));
        AddItem(new Shoes(0x454));
        AddItem(new JesterHat(0x4D2));
        AddItem(new FullApron(0x4D2));

        HairItemID = 0x203D; // Pony Tail
        HairHue = 0x47D;

        FacialHairItemID = 0x2040; // Goatee
        FacialHairHue = 0x47D;
    }

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        Direction = GetDirectionTo(player);

        var qs = player.Quest;

        if (qs is not CollectorQuest)
        {
            var newQuest = new CollectorQuest(player);

            if (qs == null && QuestSystem.CanOfferQuest(player, typeof(CollectorQuest)))
            {
                newQuest.SendOffer();
            }
            else
            {
                newQuest.AddConversation(new DontOfferConversation());
            }

            return;
        }

        if (qs.IsObjectiveInProgress(typeof(FishPearlsObjective)))
        {
            qs.AddConversation(new ElwoodDuringFishConversation());
            return;
        }

        if (qs.FindObjective<ReturnPearlsObjective>() is { Completed: false } obj1)
        {
            obj1.Complete();
            return;
        }

        if (qs.IsObjectiveInProgress(typeof(FindAlbertaObjective)))
        {
            qs.AddConversation(new ElwoodDuringPainting1Conversation());
            return;
        }

        if (qs.IsObjectiveInProgress(typeof(SitOnTheStoolObjective)))
        {
            qs.AddConversation(new ElwoodDuringPainting2Conversation());
            return;
        }

        if (qs.FindObjective<ReturnPaintingObjective>() is { Completed: false } obj2)
        {
            obj2.Complete();
            return;
        }

        if (qs.IsObjectiveInProgress(typeof(FindGabrielObjective)))
        {
            qs.AddConversation(new ElwoodDuringAutograph1Conversation());
            return;
        }

        if (qs.IsObjectiveInProgress(typeof(FindSheetMusicObjective)))
        {
            qs.AddConversation(new ElwoodDuringAutograph2Conversation());
            return;
        }

        if (qs.IsObjectiveInProgress(typeof(ReturnSheetMusicObjective)))
        {
            qs.AddConversation(new ElwoodDuringAutograph3Conversation());
            return;
        }

        if (qs.FindObjective<ReturnAutographObjective>() is { Completed: false } obj3)
        {
            obj3.Complete();
            return;
        }

        if (qs.IsObjectiveInProgress(typeof(FindTomasObjective)))
        {
            qs.AddConversation(new ElwoodDuringToys1Conversation());
            return;
        }

        if (qs.IsObjectiveInProgress(typeof(CaptureImagesObjective)))
        {
            qs.AddConversation(new ElwoodDuringToys2Conversation());
            return;
        }

        if (qs.IsObjectiveInProgress(typeof(ReturnImagesObjective)))
        {
            qs.AddConversation(new ElwoodDuringToys3Conversation());
            return;
        }

        if (qs.FindObjective<ReturnToysObjective>() is { Completed: false } obj4)
        {
            obj4.Complete();

            if (GiveReward(player))
            {
                qs.AddConversation(new EndConversation());
            }
            else
            {
                qs.AddConversation(new FullEndConversation(true));
            }

            return;
        }

        if (qs.FindObjective<MakeRoomObjective>() is { Completed: false } obj5)
        {
            if (GiveReward(player))
            {
                obj5.Complete();
                qs.AddConversation(new EndConversation());
            }
            else
            {
                qs.AddConversation(new FullEndConversation(false));
            }
        }
    }

    public static bool GiveReward(Mobile to)
    {
        var bag = new Bag();

        bag.DropItem(new Gold(Utility.RandomMinMax(500, 1000)));

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

            bag.DropItem(weapon);
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

            bag.DropItem(item);
        }

        bag.DropItem(new Obsidian());

        if (to.PlaceInBackpack(bag))
        {
            return true;
        }

        bag.Delete();
        return false;
    }
}
