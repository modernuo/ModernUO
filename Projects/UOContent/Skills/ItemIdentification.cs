using System;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items;

public interface IIdentifiable
{
    bool Identified { get; set; }
}

public static class ItemIdentification
{
    public static void Initialize()
    {
        SkillInfo.Table[(int)SkillName.ItemID].Callback = OnUse;
    }

    public static TimeSpan OnUse(Mobile from)
    {
        from.SendLocalizedMessage(500343); // What do you wish to appraise and identify?
        from.Target = new InternalTarget();

        return TimeSpan.FromSeconds(1.0);
    }

    [PlayerVendorTarget]
    private class InternalTarget : Target
    {
        public InternalTarget() : base(8, false, TargetFlags.None) => AllowNonlocal = true;

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is Mobile mobile)
            {
                mobile.OnSingleClick(from);
                return;
            }

            bool identified = false;

            if (o is Item item)
            {
                if (item is IIdentifiable identifiable && from.CheckTargetSkill(SkillName.ItemID, item, 0, 100))
                {
                    identifiable.Identified = true;
                    identified = true;
                }

                if (!Core.AOS)
                {
                    item.OnSingleClick(from);
                }
            }


            if (!identified)
            {
                from.SendLocalizedMessage(500353); // You are not certain...
            }
        }
    }
}
