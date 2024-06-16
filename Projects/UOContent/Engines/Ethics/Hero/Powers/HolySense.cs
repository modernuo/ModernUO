using System;
using Server.Text;

namespace Server.Ethics.Hero;

public sealed class HolySense : Power
{
    public HolySense() =>
        Definition = new PowerDefinition(
            0,
            "Holy Sense",
            "Drewrok Erstok",
            ""
        );

    public override void BeginInvoke(Player from)
    {
        var opposition = Ethic.Evil;

        var enemyCount = 0;

        var maxRange = 18 + from.Power;

        Player primary = null;

        foreach (var pl in opposition.Players)
        {
            var mob = pl.Mobile;

            if (mob == null || mob.Map != from.Mobile.Map || !mob.Alive)
            {
                continue;
            }

            if (!mob.InRange(from.Mobile, Math.Max(18, maxRange - pl.Power)))
            {
                continue;
            }

            if (primary == null || pl.Power > primary.Power)
            {
                primary = pl;
            }

            ++enemyCount;
        }

        using var sb = ValueStringBuilder.Create();
        sb.Append($"You sense {(enemyCount == 0 ? "no" : enemyCount.ToString())} {(enemyCount == 1 ? "enemy" : "enemies")}");

        if (primary != null)
        {
            var direction = from.Mobile.GetDirectionTo(primary.Mobile) switch
            {
                Direction.East  => "east",
                Direction.North => "north",
                Direction.South => "south",
                Direction.Up    => "north-west",
                Direction.Down  => "south-east",
                Direction.Left  => "south-west",
                Direction.Right => "north-east",
                _               => "west"
            };

            sb.Append($", and a strong presence to the {direction}.");
        }
        else
        {
            sb.Append('.');
        }

        from.Mobile.LocalOverheadMessage(MessageType.Regular, 0x59, false, sb.ToString());

        FinishInvoke(from);
    }
}
