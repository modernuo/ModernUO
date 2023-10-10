using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai;

[SerializationGenerator(0)]
public partial class Relnia : BaseQuester
{
    [Constructible]
    public Relnia() : base("the Gypsy")
    {
    }

    public override string DefaultName => "Disheveled Relnia";

    public override int TalkNumber => -1;

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = 0x83FF;

        Female = true;
        Body = 0x191;
    }

    public override void InitOutfit()
    {
        HairItemID = 0x203C;
        HairHue = 0x654;

        AddItem(new ThighBoots(0x901));
        AddItem(new FancyShirt(0x5F3));
        AddItem(new SkullCap(0x6A7));
        AddItem(new Skirt(0x544));
    }

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (from is PlayerMobile player)
        {
            var qs = player.Quest;

            if (qs is HaochisTrialsQuest)
            {
                QuestObjective obj = qs.FindObjective<FourthTrialCatsObjective>();

                if (obj?.Completed == false)
                {
                    if (dropped is Gold gold)
                    {
                        obj.Complete();
                        qs.AddObjective(new FourthTrialReturnObjective(false));

                        SayTo(from, 1063241); // I thank thee.  This gold will be a great help to me and mine!

                        gold.Consume(); // Intentional difference from OSI: don't take all the gold of poor newbies!
                        return gold.Deleted;
                    }
                }
            }
        }

        return base.OnDragDrop(from, dropped);
    }
}
