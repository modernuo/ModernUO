using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Haven;

[SerializationGenerator(0, false)]
public partial class UzeraanTurmoilTeleporter : DynamicTeleporter
{
    [Constructible]
    public UzeraanTurmoilTeleporter()
    {
    }

    public override bool GetDestination(PlayerMobile player, ref Point3D loc, ref Map map)
    {
        var qs = player.Quest;

        if (qs is not UzeraanTurmoilQuest)
        {
            return false;
        }

        if (qs.IsObjectiveInProgress(typeof(FindSchmendrickObjective))
            || qs.IsObjectiveInProgress(typeof(FindApprenticeObjective))
            || UzeraanTurmoilQuest.HasLostScrollOfPower(player))
        {
            loc = new Point3D(5222, 1858, 0);
            map = Map.Trammel;
            return true;
        }

        if (qs.IsObjectiveInProgress(typeof(FindDryadObjective))
            || UzeraanTurmoilQuest.HasLostFertileDirt(player))
        {
            loc = new Point3D(3557, 2690, 2);
            map = Map.Trammel;
            return true;
        }

        if (player.Profession != 5 // paladin
            && (qs.IsObjectiveInProgress(typeof(GetDaemonBoneObjective))
                || UzeraanTurmoilQuest.HasLostDaemonBone(player)))
        {
            loc = new Point3D(3422, 2653, 48);
            map = Map.Trammel;
            return true;
        }

        if (qs.IsObjectiveInProgress(typeof(CashBankCheckObjective)))
        {
            loc = new Point3D(3624, 2610, 0);
            map = Map.Trammel;
            return true;
        }

        return false;
    }
}
