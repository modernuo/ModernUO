using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Haven;

[SerializationGenerator(0, false)]
public partial class DaemonBloodChest : MetalChest
{
    [Constructible]
    public DaemonBloodChest() => Movable = false;

    public override void OnDoubleClick(Mobile from)
    {
        if (from is not PlayerMobile player || !player.InRange(GetWorldLocation(), 2))
        {
            base.OnDoubleClick(from);
            return;
        }

        var qs = player.Quest;

        if (qs is not UzeraanTurmoilQuest)
        {
            base.OnDoubleClick(from);
            return;
        }

        var obj = qs.FindObjective<GetDaemonBloodObjective>();

        if (obj?.Completed != false && !UzeraanTurmoilQuest.HasLostDaemonBlood(player))
        {
            base.OnDoubleClick(from);
            return;
        }

        var vial = new QuestDaemonBlood();

        if (player.PlaceInBackpack(vial))
        {
            // You take a vial of blood from the chest and put it in your pack.
            player.SendLocalizedMessage(1049331, "", 0x22);

            if (obj?.Completed == false)
            {
                obj.Complete();
            }
        }
        else
        {
            // You find a vial of blood, but can't pick it up because your pack is too full.  Come back when you have more room in your pack.
            player.SendLocalizedMessage(1049338, "", 0x22);
            vial.Delete();
        }
    }
}
