using System;
using ModernUO.Serialization;
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
[SerializationGenerator(0, false)]
public partial class Lockpick : Item
{
    [Constructible]
    public Lockpick(int amount = 1) : base(0x14FC)
    {
        Stackable = true;
        Amount = amount;
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
                if (lockpickable is Item item && lockpickable.Locked)
                {
                    if (item.RootParent != from)
                    {
                        from.Direction = from.GetDirectionTo(item);
                    }

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
            private readonly Mobile _from;
            private readonly ILockpickable _item;
            private readonly Lockpick _lockpick;

            public InternalTimer(Mobile from, ILockpickable item, Lockpick lockpick) : base(TimeSpan.FromSeconds(3.0))
            {
                _from = from;
                _item = item;
                _lockpick = lockpick;
            }

            protected void BrokeLockPickTest()
            {
                // When failed, a 25% chance to break the lockpick
                if (Utility.Random(4) == 0)
                {
                    var item = (Item)_item;

                    // You broke the lockpick.
                    item.SendLocalizedMessageTo(_from, 502074);

                    _from.PlaySound(0x3A4);
                    _lockpick.Consume();
                }
            }

            protected override void OnTick()
            {
                var item = (Item)_item;

                if (!_from.InRange(item.GetWorldLocation(), 1))
                {
                    return;
                }

                if (_item.LockLevel is ILockpickable.CannotPick or ILockpickable.MagicLock)
                {
                    // LockLevel of 0 means that the door can't be picklocked
                    // LockLevel of -255 means it's magic locked
                    item.SendLocalizedMessageTo(_from, 502073); // This lock cannot be picked by normal means
                    return;
                }

                if (_from.Skills.Lockpicking.Value < _item.RequiredSkill)
                {
                    // The LockLevel is higher thant the LockPicking of the player
                    item.SendLocalizedMessageTo(_from, 502072); // You don't see how that lock can be manipulated.
                    return;
                }

                if (_from.CheckTargetSkill(SkillName.Lockpicking, _item, _item.LockLevel, _item.MaxLockLevel))
                {
                    // Success! Pick the lock!
                    item.SendLocalizedMessageTo(_from, 502076); // The lock quickly yields to your skill.
                    _from.PlaySound(0x4A);
                    _item.LockPick(_from);
                }
                else
                {
                    // The player failed to pick the lock
                    BrokeLockPickTest();
                    item.SendLocalizedMessageTo(_from, 502075); // You are unable to pick the lock.
                }
            }
        }
    }
}
