using ModernUO.Serialization;
using System;
using Server.Items;
using Server.Misc;
using Server.Multis;
using Server.Targeting;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public abstract partial class BaseMount : BaseCreature, IMount
{
    public BaseMount(
        int bodyID,
        int itemID,
        AIType aiType,
        FightMode fightMode = FightMode.Closest,
        int rangePerception = 10,
        int rangeFight = 1
    ) : base(aiType, fightMode, rangePerception, rangeFight)
    {
        Body = bodyID;

        InternalItem = new MountItem(this, itemID);
    }

    public virtual TimeSpan MountAbilityDelay => TimeSpan.Zero;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    public DateTime _nextMountAbility;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    protected Item _internalItem;

    public virtual bool AllowMaleRider => true;
    public virtual bool AllowFemaleRider => true;

    // Stamina System - 1 step per 10 seconds and 3840 steps max = 9.667 hours
    public virtual int StepsMax => 3840;
    public virtual int StepsGainedPerIdleTime => 1;
    public virtual TimeSpan IdleTimePerStepsGain => TimeSpan.FromSeconds(10);

    [Hue]
    [CommandProperty(AccessLevel.GameMaster)]
    public override int Hue
    {
        get => base.Hue;
        set
        {
            base.Hue = value;

            if (InternalItem != null)
            {
                InternalItem.Hue = value;
            }
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int ItemID
    {
        get => InternalItem?.ItemID ?? 0;
        set
        {
            if (InternalItem != null)
            {
                InternalItem.ItemID = value;
            }
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    [SerializableProperty(1)]
    public Mobile Rider
    {
        get => _rider;
        set
        {
            if (_rider == value)
            {
                return;
            }

            if (value == null)
            {
                var loc = _rider.Location;
                var map = _rider.Map;

                if (map == null || map == Map.Internal)
                {
                    loc = _rider.LogoutLocation;
                    map = _rider.LogoutMap;
                }

                Direction = _rider.Direction;
                Location = loc;
                Map = map;

                InternalItem?.Internalize();
            }
            else
            {
                if (_rider != null)
                {
                    Dismount(_rider);
                }

                Dismount(value);

                if (InternalItem != null)
                {
                    value.AddItem(InternalItem);
                }

                value.Direction = Direction;

                Internalize();

                if (value.Target is Bola.BolaTarget)
                {
                    Target.Cancel(value);
                }
            }

            _rider = value;

            if (value == null)
            {
                StaminaSystem.OnDismount(this);
            }

            this.MarkDirty();
        }
    }

    public virtual void OnRiderDamaged(int amount, Mobile from, bool willKill)
    {
        if (_rider == null)
        {
            return;
        }

        var attacker = from ?? _rider.FindMostRecentDamager(true);

        if (!(attacker == this || attacker == _rider || willKill || Core.Now < NextMountAbility)
            && DoMountAbility(amount, from))
        {
            NextMountAbility = Core.Now + MountAbilityDelay;
        }
    }

    public override bool OnBeforeDeath()
    {
        Rider = null;
        return base.OnBeforeDeath();
    }

    public override void OnAfterDelete()
    {
        InternalItem?.Delete();
        InternalItem = null;

        base.OnAfterDelete();
    }

    public override void OnDelete()
    {
        Rider = null;

        base.OnDelete();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialize()
    {
        if (InternalItem == null)
        {
            Delete();
        }
    }

    public virtual void OnDisallowedRider(Mobile m)
    {
        m.SendLocalizedMessage(1042317); // You may not ride at this time
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsDeadPet)
        {
            return;
        }

        if (from.IsBodyMod && !from.Body.IsHuman)
        {
            if (Core.AOS) // You cannot ride a mount in your current form.
            {
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1062061, from.NetState);
            }
            else
            {
                from.SendLocalizedMessage(1061628); // You can't do that while polymorphed.
            }

            return;
        }

        if (!CheckMountAllowed(from))
        {
            return;
        }

        if (from.Mounted)
        {
            from.SendLocalizedMessage(1005583); // Please dismount first.
            return;
        }

        if (from.Female ? !AllowFemaleRider : !AllowMaleRider)
        {
            OnDisallowedRider(from);
            return;
        }

        if (!DesignContext.Check(from))
        {
            return;
        }

        if (from.HasTrade)
        {
            from.SendLocalizedMessage(1042317, "", 0x41); // You may not ride at this time
            return;
        }

        if (from.InRange(this, 1))
        {
            var canAccess = from.AccessLevel >= AccessLevel.GameMaster
                            || Controlled && ControlMaster == from
                            || Summoned && SummonMaster == from;

            if (canAccess)
            {
                if (Poisoned)
                {
                    // This mount is too ill to ride.
                    PrivateOverheadMessage(MessageType.Regular,0x3B2,1049692,from.NetState);
                }
                else
                {
                    Rider = from;
                }
            }
            else if (!Controlled && !Summoned)
            {
                // That mount does not look broken! You would have to tame it to ride it.
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, 501263, from.NetState);
            }
            else
            {
                // This isn't your mount; it refuses to let you ride.
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, 501264, from.NetState);
            }
        }
        else
        {
            from.SendLocalizedMessage(500206); // That is too far away to ride.
        }
    }

    public static void Dismount(Mobile m)
    {
        var mount = m.Mount;

        if (mount != null)
        {
            mount.Rider = null;
        }
    }

    public static bool CheckMountAllowed(Mobile mob)
    {
        var result = true;

        if (mob is PlayerMobile mobile)
        {
            if (mobile.MountBlockReason != BlockMountType.None)
            {
                mobile.SendLocalizedMessage((int)mobile.MountBlockReason);
                result = false;
            }

            if (mobile.Race == Race.Gargoyle)
            {
                mobile.PrivateOverheadMessage(MessageType.Regular, mobile.SpeechHue, 1112281, mobile.NetState); // Gargoyles are unable to ride animals.
                result = false;
            }
        }

        return result;
    }

    public virtual bool DoMountAbility(int damage, Mobile attacker) => false;
}

[SerializationGenerator(0, false)]
public partial class MountItem : Item, IMountItem
{
    private BaseMount _mount;

    public MountItem(BaseMount mount, int itemID) : base(itemID)
    {
        Layer = Layer.Mount;
        Movable = false;

        _mount = mount;
    }

    public override double DefaultWeight => 0;

    [SerializableProperty(0, useField: nameof(_mount))]
    public IMount Mount => _mount;

    public override void OnAfterDelete()
    {
        _mount?.Delete();
        _mount = null;

        base.OnAfterDelete();
    }

    public override DeathMoveResult OnParentDeath(Mobile parent)
    {
        if (_mount != null)
        {
            _mount.Rider = null;
        }

        return DeathMoveResult.RemainEquipped;
    }

    [AfterDeserialization(false)]
    private void AfterDeserialize()
    {
        if (_mount == null)
        {
            Delete();
        }
    }
}
