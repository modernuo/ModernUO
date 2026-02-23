using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.Quests;
using Server.Regions;
using Server.Spells;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items;

public enum BagOfSendingHue
{
    Yellow,
    Blue,
    Red
}

[SerializationGenerator(2, false)]
public partial class BagOfSending : Item, TranslocationItem
{
    [Constructible]
    public BagOfSending() : this(RandomHue())
    {
    }

    [Constructible]
    public BagOfSending(BagOfSendingHue hue) : base(0xE76)
    {
        BagOfSendingHue = hue;
        _charges = Utility.RandomMinMax(3, 9);
    }

    public override double DefaultWeight => 2.0;

    public override int LabelNumber => 1054104; // a bag of sending

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public BagOfSendingHue BagOfSendingHue
    {
        get => _bagOfSendingHue;
        set
        {
            _bagOfSendingHue = value;

            Hue = value switch
            {
                BagOfSendingHue.Yellow => 0x8A5,
                BagOfSendingHue.Blue   => 0x8AD,
                BagOfSendingHue.Red    => 0x89B,
                _                      => Hue
            };
            this.MarkDirty();
        }
    }

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Charges
    {
        get => _charges;
        set
        {
            _charges = Math.Clamp(value, 0, MaxCharges);
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    [SerializableProperty(2)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Recharges
    {
        get => _recharges;
        set
        {
            _recharges = Math.Clamp(value, 0, MaxRecharges);
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int MaxCharges => 30;

    [CommandProperty(AccessLevel.GameMaster)]
    public int MaxRecharges => 255;


    private static TextDefinition _translocationItemName = 1150423; // bag of sending
    public TextDefinition TranslocationItemName => _translocationItemName;

    public static BagOfSendingHue RandomHue()
    {
        return Utility.Random(3) switch
        {
            0 => BagOfSendingHue.Yellow,
            1 => BagOfSendingHue.Blue,
            _ => BagOfSendingHue.Red
        };
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1060741, _charges); // charges: ~1_val~
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        LabelTo(from, 1060741, _charges.ToString()); // charges: ~1_val~
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (from.Alive)
        {
            list.Add(new UseBagEntry(Charges > 0 && IsChildOf(from.Backpack)));
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.Region.IsPartOf<JailRegion>())
        {
            from.SendMessage("You may not do that in jail.");
        }
        else if (!IsChildOf(from.Backpack))
        {
            // The bag of sending must be in your backpack.
            this.SendLocalizedMessageTo(from, 1062334, 0x59);
        }
        else if (Charges == 0)
        {
            this.SendLocalizedMessageTo(from, 1042544, 0x59); // This item is out of charges.
        }
        else
        {
            from.Target = new SendTarget(this);
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _recharges = reader.ReadEncodedInt();
        _charges = reader.ReadEncodedInt();
        _bagOfSendingHue = (BagOfSendingHue)reader.ReadEncodedInt();
    }

    private class UseBagEntry : ContextMenuEntry
    {
        public UseBagEntry(bool enabled) : base(6189) => Enabled = enabled;

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from.CheckAlive() && target is BagOfSending bag && !bag.Deleted)
            {
                bag.OnDoubleClick(from);
            }
        }
    }

    private class SendTarget : Target
    {
        private readonly BagOfSending _bag;

        public SendTarget(BagOfSending bag) : base(-1, false, TargetFlags.None) => _bag = bag;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_bag.Deleted)
            {
                return;
            }

            if (targeted is not Item item)
            {
                return;
            }

            if (from.Region.IsPartOf<JailRegion>())
            {
                from.SendMessage("You may not do that in jail.");
                return;
            }

            if (!_bag.IsChildOf(from.Backpack))
            {
                // The bag of sending must be in your backpack. 1054107 is gone from client, using generic response
                _bag.SendLocalizedMessageTo(from, 1062334, 0x59);
                return;
            }

            if (_bag.Charges == 0)
            {
                _bag.SendLocalizedMessageTo(from, 1042544, 0x59); // This item is out of charges.
                return;
            }

            var reqCharges = (int)Math.Max(1, Math.Ceiling(item.TotalWeight / 10.0));

            if (!item.IsChildOf(from.Backpack))
            {
                // You may only send items from your backpack to your bank box.
                _bag.SendLocalizedMessageTo(from, 1054152, 0x59);
            }
            else if (item is BagOfSending or Container)
            {
                // You cannot send a container through the bag of sending
                _bag.SendLocalizedMessageTo(from, 1079428, 0x59);
            }
            else if (item.LootType == LootType.Cursed)
            {
                // The bag of sending rejects the cursed item.
                _bag.SendLocalizedMessageTo(from, 1054108, 0x59);
            }
            else if (!item.VerifyMove(from) || item is QuestItem || item.Nontransferable)
            {
                // The bag of sending rejects that item.
                _bag.SendLocalizedMessageTo(from, 1054109, 0x59);
            }
            else if (SpellHelper.IsDoomGauntlet(from.Map, from.Location))
            {
                from.SendLocalizedMessage(1062089); // You cannot use that here.
            }
            else if (Core.ML && reqCharges > _bag.Charges)
            {
                from.SendLocalizedMessage(1079932); // You don't have enough charges to send that much weight
            }
            else
            {
                bool sent;

                if (item is Gold or BankCheck)
                {
                    var amount = (item as Gold)?.Amount ?? ((BankCheck)item).Worth;
                    sent = Banker.Deposit(from, amount);
                    if (sent)
                    {
                        item.Delete();
                    }
                }
                else
                {
                    sent = from.BankBox.TryDropItem(from, item, false);
                }

                if (sent)
                {
                    _bag.Charges -= Core.ML ? reqCharges : 1;

                    // The item was placed in your bank box.
                    _bag.SendLocalizedMessageTo(from, 1054150, 0x59);
                }
                else
                {
                    _bag.SendLocalizedMessageTo(from, 1054110, 0x59); // Your bank box is full.
                }
            }
        }
    }
}
