using Server.ContextMenus;
using Server.Items;
using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class BaseHire : BaseCreature
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextPay;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _holdGold;

    [CommandProperty(AccessLevel.GameMaster)]
    public int Pay => PerDayCost();

    public int GoldOnDeath { get; set; }

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsHired
    {
        get => _isHired;
        set
        {
            _isHired = value;

            Delta(MobileDelta.Noto);
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    public BaseHire(AIType AI) : base(AI, FightMode.Aggressor)
    {
        ControlSlots = 2;
        HoldGold = 8;
    }

    public BaseHire() : base(AIType.AI_Melee, FightMode.Aggressor) => ControlSlots = 2;

    public override bool IsBondable => false;
    public override bool KeepsItemsOnDeath => true;

    // Ensure we cannot drop if we are a hireable
    public override bool CanDrop => false;

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (IsHired)
        {
            PayTimer.RegisterTimer(this);
        }
    }

    public override bool OnBeforeDeath()
    {
        GoldOnDeath = Backpack?.GetAmount(typeof(Gold)) ?? 0;
        return base.OnBeforeDeath();
    }

    public override void Delete()
    {
        base.Delete();

        PayTimer.RemoveTimer(this);
    }

    public override void OnDeath(Container c)
    {
        if (GoldOnDeath > 0)
        {
            c.DropItem(new Gold(GoldOnDeath));
        }

        base.OnDeath(c);
    }

    public virtual Mobile GetOwner()
    {
        if (!Controlled)
        {
            return null;
        }

        var owner = ControlMaster;
        IsHired = true;

        if (owner == null)
        {
            return null;
        }

        if (owner.Deleted)
        {
            Say(1005653); // Hmmm. I seem to have lost my master.
            SetControlMaster(null);
            return null;
        }

        return owner;
    }

    public virtual bool AddHire(Mobile m)
    {
        Mobile owner = GetOwner();

        if (owner != null)
        {
            m.SendLocalizedMessage(1043283, owner.Name); // I am following ~1_NAME~.
            return false;
        }

        if (SetControlMaster(m))
        {
            IsHired = true;

            return true;
        }

        return false;
    }

    public int PerDayCost() =>
        (int)(Skills[SkillName.Anatomy].Value +
              Skills[SkillName.Tactics].Value +
              Skills[SkillName.Macing].Value +
              Skills[SkillName.Swords].Value +
              Skills[SkillName.Fencing].Value +
              Skills[SkillName.Archery].Value +
              Skills[SkillName.MagicResist].Value +
              Skills[SkillName.Healing].Value +
              Skills[SkillName.Magery].Value +
              Skills[SkillName.Parry].Value) / 35 + 1;

    private bool OnHireDragDrop(Mobile from, Item item)
    {
        if (Pay == 0)
        {
            SayTo(from, 500200); // I have no need for that.
            return false;
        }

        // Is the creature already hired
        if (Controlled)
        {
            SayTo(from, 1042495); // I have already been hired.
            return false;
        }

        // Is the item the payment in gold
        if (item is not Gold)
        {
            SayTo(from, 1043268); // Tis crass of me, but I want gold
            return false;
        }

        // Is the payment in gold sufficient
        if (item.Amount < Pay)
        {
            SayHireCost();
            return false;
        }

        if (from.Followers + ControlSlots > from.FollowersMax)
        {
            SayTo(from, 500896); // I see you already have an escort.
            return false;
        }

        // Try to add the hireling as a follower
        if (!AddHire(from))
        {
            return false;
        }

        // I thank thee for paying me.  I will work for thee for ~1_NUMBER~ days.
        SayTo(from, 1043258, $"{item.Amount / Pay}"); // Stupid that they don't have "day" cliloc

        HoldGold += item.Amount;
        NextPay = Core.Now + PayTimer.GetInterval();

        PayTimer.RegisterTimer(this);
        return true;
    }

    public override bool OnDragDrop(Mobile from, Item item) => OnHireDragDrop(from, item) || base.OnDragDrop(from, item);

    internal void SayHireCost()
    {
        // I am available for hire for ~1_AMOUNT~ gold coins a day. If thou dost give me gold, I will work for thee.
        Say(1043256, $"{Pay}");
    }

    public override void OnSpeech(SpeechEventArgs e)
    {
        // Check for a greeting, a 'hire', or a 'servant'
        if (!e.Handled && e.Mobile.InRange(this, 6) && (e.HasKeyword(0x003B) || e.HasKeyword(0x0162) || e.HasKeyword(0x000C)))
        {
            if (Controlled)
            {
                Say(1042495); // I have already been hired.
            }
            else
            {
                e.Handled = true;
                SayHireCost();
            }
        }

        base.OnSpeech(e);
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        if (Deleted)
        {
            return;
        }

        if (!Controlled)
        {
            if (CanPaperdollBeOpenedBy(from))
            {
                list.Add(new PaperdollEntry(this));
            }

            list.Add(new HireEntry(this));
        }
        else
        {
            base.GetContextMenuEntries(from, list);
        }
    }

    public class PayTimer : Timer
    {
        private readonly HashSet<BaseHire> _hires = new();
        public static PayTimer Instance { get; set; }

        public PayTimer() : base(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1))
        {
        }

        public static TimeSpan GetInterval() => TimeSpan.FromMinutes(30.0);

        protected override void OnTick()
        {
            using var queue = PooledRefQueue<Mobile>.Create();
            foreach (var hire in _hires)
            {
                if (hire.NextPay > Core.Now)
                {
                    continue;
                }

                hire.NextPay = Core.Now + GetInterval();

                int pay = hire.Pay;

                if (hire.HoldGold <= pay)
                {
                    queue.Enqueue(hire);
                }
                else
                {
                    hire.HoldGold -= pay;
                }
            }

            while (queue.Count > 0)
            {
                var hire = (BaseHire)queue.Dequeue();

                hire.GetOwner(); // Sets owner to null
                hire.Say(503235); // I regret nothing!
                hire.Delete();
            }
        }

        public static void RegisterTimer(BaseHire hire)
        {
            Instance ??= new PayTimer();

            if (!Instance.Running)
            {
                Instance.Start();
            }

            Instance._hires.Add(hire);
        }

        public static void RemoveTimer(BaseHire hire)
        {
            if (Instance?._hires.Remove(hire) == true && Instance._hires.Count == 0)
            {
                Instance.Stop();
            }
        }
    }

    public class HireEntry : ContextMenuEntry
    {
        private readonly BaseHire _hire;

        public HireEntry(BaseHire hire) : base(6120, 3) => _hire = hire;

        public override void OnClick()
        {
            _hire.SayHireCost();
        }
    }
}
