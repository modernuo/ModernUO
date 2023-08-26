using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseTrap : Item
{
    private DateTime m_NextPassiveTrigger, m_NextActiveTrigger;

    public BaseTrap(int itemID) : base(itemID) => Movable = false;

    public virtual bool PassivelyTriggered => false;
    public virtual TimeSpan PassiveTriggerDelay => TimeSpan.Zero;
    public virtual int PassiveTriggerRange => -1;
    public virtual TimeSpan ResetDelay => TimeSpan.Zero;

    public override bool HandlesOnMovement => true; // Tell the core that we implement OnMovement

    public virtual void OnTrigger(Mobile from)
    {
    }

    public virtual int GetEffectHue()
    {
        var hue = Hue & 0x3FFF;

        if (hue < 2)
        {
            return 0;
        }

        return hue - 1;
    }

    public bool CheckRange(Point3D loc, Point3D oldLoc, int range) =>
        CheckRange(loc, range) && !CheckRange(oldLoc, range);

    public bool CheckRange(Point3D loc, int range) =>
        Z + 8 >= loc.Z && loc.Z + 16 > Z
                       && Utility.InRange(GetWorldLocation(), loc, range);

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        base.OnMovement(m, oldLocation);

        if (m.Location == oldLocation)
        {
            return;
        }

        if (CheckRange(m.Location, oldLocation, 0) && Core.Now >= m_NextActiveTrigger)
        {
            m_NextActiveTrigger = m_NextPassiveTrigger = Core.Now + ResetDelay;

            OnTrigger(m);
        }
        else if (PassivelyTriggered && CheckRange(m.Location, oldLocation, PassiveTriggerRange) &&
                 Core.Now >= m_NextPassiveTrigger)
        {
            m_NextPassiveTrigger = Core.Now + PassiveTriggerDelay;

            OnTrigger(m);
        }
    }
}
