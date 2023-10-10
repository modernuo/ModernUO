using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai;

[SerializationGenerator(0)]
public partial class HaochisKatanaGenerator : Item
{
    [Constructible]
    public HaochisKatanaGenerator() : base(0x1B7B)
    {
        Visible = false;
        Movable = false;
    }

    public override string DefaultName => "Haochi's katana generator";

    public override bool OnMoveOver(Mobile m)
    {
        if (m is not PlayerMobile player)
        {
            return base.OnMoveOver(m);
        }

        var qs = player.Quest;

        if (qs is not HaochisTrialsQuest)
        {
            return base.OnMoveOver(m);
        }

        if (HaochisTrialsQuest.HasLostHaochisKatana(player))
        {
            Item katana = new HaochisKatana();

            if (!player.PlaceInBackpack(katana))
            {
                katana.Delete();
                // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                player.SendLocalizedMessage(1046260);
            }
        }
        else
        {
            QuestObjective obj = qs.FindObjective<FifthTrialIntroObjective>();

            if (obj?.Completed == false)
            {
                Item katana = new HaochisKatana();

                if (player.PlaceInBackpack(katana))
                {
                    obj.Complete();
                }
                else
                {
                    katana.Delete();
                    // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                    player.SendLocalizedMessage(1046260);
                }
            }
        }

        return base.OnMoveOver(m);
    }
}
