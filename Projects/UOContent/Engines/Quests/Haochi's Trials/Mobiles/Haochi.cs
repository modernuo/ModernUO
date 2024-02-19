using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai;

[SerializationGenerator(0, false)]
public partial class Haochi : BaseQuester
{
    [Constructible]
    public Haochi() : base("the Honorable Samurai Legend")
    {
    }

    public override string DefaultName => "Daimyo Haochi";

    public override int TalkNumber => -1;

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = 0x8403;

        Female = false;
        Body = 0x190;
    }

    public override void InitOutfit()
    {
        HairItemID = 0x204A;
        HairHue = 0x901;

        AddItem(new SamuraiTabi());
        AddItem(new JinBaori());

        AddItem(new PlateHaidate());
        AddItem(new StandardPlateKabuto());
        AddItem(new PlateMempo());
        AddItem(new PlateDo());
        AddItem(new PlateHiroSode());
    }

    public override int GetAutoTalkRange(PlayerMobile pm) => 2;

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        var qs = player.Quest;

        if (qs is not HaochisTrialsQuest)
        {
            return;
        }

        if (HaochisTrialsQuest.HasLostHaochisKatana(player))
        {
            qs.AddConversation(new LostSwordConversation());
            return;
        }

        if (qs.FindObjective<FindHaochiObjective>() is { Completed: false } obj1)
        {
            obj1.Complete();
            return;
        }

        if (qs.FindObjective<FirstTrialReturnObjective>() is { Completed: false } obj2)
        {
            player.AddToBackpack(new LeatherDo());
            obj2.Complete();
            return;
        }

        if (qs.FindObjective<SecondTrialReturnObjective>() is { Completed: false } obj3)
        {
            if (obj3.Dragon)
            {
                player.AddToBackpack(new LeatherSuneate());
            }

            obj3.Complete();
            return;
        }

        if (qs.FindObjective<ThirdTrialReturnObjective>() is { Completed: false } obj4)
        {
            player.AddToBackpack(new LeatherHaidate());
            obj4.Complete();
            return;
        }

        if (qs.FindObjective<FourthTrialReturnObjective>() is { Completed: false } obj5)
        {
            if (!obj5.KilledCat)
            {
                var cont = GetNewContainer();
                cont.DropItem(new LeatherHiroSode());
                cont.DropItem(new JinBaori());
                player.AddToBackpack(cont);
            }

            obj5.Complete();
            return;
        }

        if (qs.FindObjective<FifthTrialReturnObjective>() is { Completed: false } obj6)
        {
            var katana = player.Backpack?.FindItemByType<HaochisKatana>();
            if (katana == null)
            {
                return;
            }

            katana.Delete();
            obj6.Complete();

            qs.AddConversation(
                new SixthTrialIntroConversation(qs.FindObjective<FifthTrialIntroObjective>()?.StolenTreasure == true)
            );
        }

        if (qs.FindObjective<SixthTrialReturnObjective>() is { Completed: false } obj7)
        {
            obj7.Complete();
            return;
        }

        if (qs.FindObjective<SeventhTrialReturnObjective>() is { Completed: false } obj8)
        {
            BaseWeapon weapon = new Daisho();
            BaseRunicTool.ApplyAttributesTo(weapon, Utility.Random(1, 3), 10, 30);
            player.AddToBackpack(weapon);

            BaseArmor armor = new LeatherDo();
            BaseRunicTool.ApplyAttributesTo(armor, Utility.Random(1, 3), 10, 20);
            player.AddToBackpack(armor);

            obj8.Complete();
        }
    }
}
