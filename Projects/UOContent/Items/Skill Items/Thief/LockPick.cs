using System;
using Server.Targeting;

namespace Server.Items;

public interface ILockpickable : IPoint2D
{
    const int CannotPick = 0;
    const int MagicLock = -255;

    int LockLevel { get; set; }
    bool Locked { get; set; }
    Mobile Picker { get; set; }
    int MaxLockLevel { get; set; }
    int RequiredSkill { get; set; }

    void LockPick(Mobile from);
}

[Flippable(0x14fc, 0x14fb)]
public class Lockpick : Item
{
    [Constructible]
    public Lockpick(int amount = 1) : base(0x14FC)
    {
        Stackable = true;
        Amount = amount;
    }

    public Lockpick(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);

        writer.Write(1); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        var version = reader.ReadInt();

        if (version == 0 && Weight == 0.1)
        {
            Weight = -1;
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        from.SendLocalizedMessage(502068); // What do you want to pick?
        from.Target = new InternalTarget(this);
    }

    private class InternalTarget : Target
    {
        private readonly Lockpick m_Item;

        public InternalTarget(Lockpick item) : base(1, false, TargetFlags.None) => m_Item = item;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Item.Deleted)
            {
                return;
            }

            if (targeted is ILockpickable lockpickable)
            {
                var item = lockpickable as Item;
                from.Direction = from.GetDirectionTo(item);

                if (lockpickable.Locked)
                {
                    from.PlaySound(0x241);

                    new InternalTimer(from, lockpickable, m_Item).Start();
                }
                else
                {
                    // The door is not locked
                    from.SendLocalizedMessage(502069); // This does not appear to be locked
                }
            }
            else
            {
                from.SendLocalizedMessage(501666); // You can't unlock that!
            }
        }

        private class InternalTimer : Timer
        {
            private readonly Mobile m_From;
            private readonly ILockpickable m_Item;
            private readonly Lockpick m_Lockpick;

            public InternalTimer(Mobile from, ILockpickable item, Lockpick lockpick) : base(TimeSpan.FromSeconds(3.0))
            {
                m_From = from;
                m_Item = item;
                m_Lockpick = lockpick;
            }

            protected void BrokeLockPickTest()
            {
                // When failed, a 25% chance to break the lockpick
                if (Utility.Random(4) == 0)
                {
                    var item = (Item)m_Item;

                    // You broke the lockpick.
                    item.SendLocalizedMessageTo(m_From, 502074);

                    m_From.PlaySound(0x3A4);
                    m_Lockpick.Consume();
                }
            }

            protected override void OnTick()
            {
                var item = (Item)m_Item;

                if (!m_From.InRange(item.GetWorldLocation(), 1))
                {
                    return;
                }

                if (m_Item.LockLevel is ILockpickable.CannotPick or ILockpickable.MagicLock)
                {
                    // LockLevel of 0 means that the door can't be picklocked
                    // LockLevel of -255 means it's magic locked
                    item.SendLocalizedMessageTo(m_From, 502073); // This lock cannot be picked by normal means
                    return;
                }

                if (m_From.Skills.Lockpicking.Value < m_Item.RequiredSkill)
                {
                    /*
                    // Do some training to gain skills
                    m_From.CheckSkill( SkillName.Lockpicking, 0, m_Item.LockLevel );*/

                    // The LockLevel is higher thant the LockPicking of the player
                    item.SendLocalizedMessageTo(m_From, 502072); // You don't see how that lock can be manipulated.
                    return;
                }

                if (m_From.CheckTargetSkill(SkillName.Lockpicking, m_Item, m_Item.LockLevel, m_Item.MaxLockLevel))
                {
                    // Success! Pick the lock!
                    item.SendLocalizedMessageTo(m_From, 502076); // The lock quickly yields to your skill.
                    m_From.PlaySound(0x4A);
                    m_Item.LockPick(m_From);
                }
                else
                {
                    // The player failed to pick the lock
                    BrokeLockPickTest();
                    item.SendLocalizedMessageTo(m_From, 502075); // You are unable to pick the lock.
                }
            }
        }
    }
}
