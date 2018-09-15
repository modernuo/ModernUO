using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai
{
  public class Haochi : BaseQuester
  {
    [Constructible]
    public Haochi() : base("the Honorable Samurai Legend")
    {
    }

    public Haochi(Serial serial) : base(serial)
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

    public override int GetAutoTalkRange(PlayerMobile pm)
    {
      return 2;
    }

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
      QuestSystem qs = player.Quest;

      if (qs is HaochisTrialsQuest)
      {
        if (HaochisTrialsQuest.HasLostHaochisKatana(player))
        {
          qs.AddConversation(new LostSwordConversation());
          return;
        }

        QuestObjective obj = qs.FindObjective(typeof(FindHaochiObjective));

        if (obj != null && !obj.Completed)
        {
          obj.Complete();
          return;
        }

        obj = qs.FindObjective(typeof(FirstTrialReturnObjective));

        if (obj != null && !obj.Completed)
        {
          player.AddToBackpack(new LeatherDo());
          obj.Complete();
          return;
        }

        obj = qs.FindObjective(typeof(SecondTrialReturnObjective));

        if (obj != null && !obj.Completed)
        {
          if (((SecondTrialReturnObjective)obj).Dragon)
            player.AddToBackpack(new LeatherSuneate());

          obj.Complete();
          return;
        }

        obj = qs.FindObjective(typeof(ThirdTrialReturnObjective));

        if (obj != null && !obj.Completed)
        {
          player.AddToBackpack(new LeatherHiroSode());
          obj.Complete();
          return;
        }

        obj = qs.FindObjective(typeof(FourthTrialReturnObjective));

        if (obj != null && !obj.Completed)
        {
          if (!((FourthTrialReturnObjective)obj).KilledCat)
          {
            Container cont = GetNewContainer();
            cont.DropItem(new LeatherHiroSode());
            cont.DropItem(new JinBaori());
            player.AddToBackpack(cont);
          }

          obj.Complete();
          return;
        }

        obj = qs.FindObjective(typeof(FifthTrialReturnObjective));

        if (obj != null && !obj.Completed)
        {
          Container pack = player.Backpack;
          Item katana = pack?.FindItemByType(typeof(HaochisKatana));
          if (katana != null)
          {
            katana.Delete();
            obj.Complete();

            obj = qs.FindObjective(typeof(FifthTrialIntroObjective));
            if (obj != null && ((FifthTrialIntroObjective)obj).StolenTreasure)
              qs.AddConversation(new SixthTrialIntroConversation(true));
            else
              qs.AddConversation(new SixthTrialIntroConversation(false));
          }

          return;
        }

        obj = qs.FindObjective(typeof(SixthTrialReturnObjective));

        if (obj != null && !obj.Completed)
        {
          obj.Complete();
          return;
        }

        obj = qs.FindObjective(typeof(SeventhTrialReturnObjective));

        if (obj != null && !obj.Completed)
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

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}