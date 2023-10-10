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

        if (qs is HaochisTrialsQuest)
        {
            if (HaochisTrialsQuest.HasLostHaochisKatana(player))
            {
                qs.AddConversation(new LostSwordConversation());
                return;
            }

            QuestObjective obj = qs.FindObjective<FindHaochiObjective>();

            if (obj?.Completed == false)
            {
                obj.Complete();
                return;
            }

            obj = qs.FindObjective<FirstTrialReturnObjective>();

            if (obj?.Completed == false)
            {
                player.AddToBackpack(new LeatherDo());
                obj.Complete();
                return;
            }

            obj = qs.FindObjective<SecondTrialReturnObjective>();

            if (obj?.Completed == false)
            {
                if (((SecondTrialReturnObjective)obj).Dragon)
                {
                    player.AddToBackpack(new LeatherSuneate());
                }

                obj.Complete();
                return;
            }

            obj = qs.FindObjective<ThirdTrialReturnObjective>();

            if (obj?.Completed == false)
            {
                player.AddToBackpack(new LeatherHiroSode());
                obj.Complete();
                return;
            }

            obj = qs.FindObjective<FourthTrialReturnObjective>();

            if (obj?.Completed == false)
            {
                if (!((FourthTrialReturnObjective)obj).KilledCat)
                {
                    var cont = GetNewContainer();
                    cont.DropItem(new LeatherHiroSode());
                    cont.DropItem(new JinBaori());
                    player.AddToBackpack(cont);
                }

                obj.Complete();
                return;
            }

            obj = qs.FindObjective<FifthTrialReturnObjective>();

            if (obj?.Completed == false)
            {
                var katana = player.Backpack?.FindItemByType<HaochisKatana>();
                if (katana == null)
                {
                    return;
                }

                katana.Delete();
                obj.Complete();

                obj = qs.FindObjective<FifthTrialIntroObjective>();
                if (((FifthTrialIntroObjective)obj)?.StolenTreasure == true)
                {
                    qs.AddConversation(new SixthTrialIntroConversation(true));
                }
                else
                {
                    qs.AddConversation(new SixthTrialIntroConversation(false));
                }
            }

            obj = qs.FindObjective<SixthTrialReturnObjective>();

            if (obj?.Completed == false)
            {
                obj.Complete();
                return;
            }

            obj = qs.FindObjective<SeventhTrialReturnObjective>();

            if (obj?.Completed == false)
            {
                BaseWeapon weapon = new Daisho();
                BaseRunicTool.ApplyAttributesTo(weapon, Utility.Random(1, 3), 10, 30);
                player.AddToBackpack(weapon);

                BaseArmor armor = new LeatherDo();
                BaseRunicTool.ApplyAttributesTo(armor, Utility.Random(1, 3), 10, 20);
                player.AddToBackpack(armor);

                obj.Complete();
            }
        }
    }
}
