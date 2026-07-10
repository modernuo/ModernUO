using System;
using Server.Items;
using Server.Mobiles;
using Server.Regions;

namespace Server.Spells.Mysticism;

public class RisingColossusSpell : MysticSpell, ITargetingSpell<IPoint3D>
{
    private const int ControlSlotCost = 5;

    private static readonly SpellInfo _info = new(
        "Rising Colossus",
        "Kal Vas Xen Corp Ylem",
        230,
        9022,
        Reagent.DaemonBone,
        Reagent.DragonsBlood,
        Reagent.FertileDirt,
        Reagent.Nightshade
    );

    public RisingColossusSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Eighth;

    internal static TimeSpan GetDuration(double mysticism, double supportSkill)
    {
        // Public sources describe a duration of up to about 60 seconds. Capping the
        // combined-skill formula at that value avoids importing ServUO's longer
        // 120/120 duration as an undocumented RebirthUO balance decision.
        return TimeSpan.FromSeconds(Math.Min(60.0, Math.Max(0.0, mysticism + supportSkill) / 3.0));
    }

    internal static bool IsHouseLocation(Mobile caster, Point3D location, Map map)
    {
        if (Region.Find(location, map).IsPartOf<HouseRegion>())
        {
            return true;
        }

        return caster.Map != null && Region.Find(caster.Location, caster.Map).IsPartOf<HouseRegion>();
    }

    public override bool CheckCast()
    {
        if (!base.CheckCast())
        {
            return false;
        }

        if (Caster.Followers + ControlSlotCost > Caster.FollowersMax)
        {
            Caster.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
            return false;
        }

        return true;
    }

    public void Target(IPoint3D point)
    {
        if (Caster.Followers + ControlSlotCost > Caster.FollowersMax)
        {
            Caster.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
            return;
        }

        var map = Caster.Map;
        SpellHelper.GetSurfaceTop(ref point);

        if (map?.CanSpawnMobile(point.X, point.Y, point.Z) != true || IsHouseLocation(Caster, new Point3D(point), map))
        {
            Caster.SendLocalizedMessage(501942); // That location is blocked.
        }
        else if (SpellHelper.CheckTown(point, Caster) && CheckSequence())
        {
            var mysticism = GetBaseSkill(Caster);
            var supportSkill = GetDamageSkill(Caster);
            var duration = GetDuration(mysticism, supportSkill);
            var summon = new RisingColossus(Caster, mysticism, supportSkill);

            if (BaseCreature.Summon(summon, false, Caster, new Point3D(point), 0x656, duration))
            {
                Effects.SendTargetParticles(summon, 0x3728, 10, 10, 0x13AA, (EffectLayer)255);
            }
        }
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true, retryOnLos: true);
    }
}