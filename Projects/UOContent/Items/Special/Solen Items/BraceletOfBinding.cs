using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Factions;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;
using Server.Regions;
using Server.Spells;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(2)]
public partial class BraceletOfBinding : BaseBracelet, TranslocationItem
{
    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _inscription;

    private TransportTimer _timer;

    [Constructible]
    public BraceletOfBinding() : base(0x1086)
    {
        Hue = 0x489;
        Weight = 1.0;
    }

    [SerializableProperty(0)]
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

    [SerializableProperty(3)]
    [CommandProperty(AccessLevel.GameMaster)]
    public BraceletOfBinding Bound
    {
        get
        {
            if (_bound?.Deleted == true)
            {
                _bound = null;
            }

            return _bound;
        }
        set => _bound = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int MaxCharges => 20;

    [CommandProperty(AccessLevel.GameMaster)]
    public int MaxRecharges => 255;

    private static TextDefinition _translocationItemName = "bracelet of binding";
    public TextDefinition TranslocationItemName => _translocationItemName;

    public override void AddNameProperty(IPropertyList list)
    {
        // a bracelet of binding : ~1_val~ ~2_val~
        list.Add(1054000, $"{_charges}\t{_inscription.DefaultIfNullOrEmpty(" ")}");
    }

    public override void OnSingleClick(Mobile from)
    {
        // a bracelet of binding : ~1_val~ ~2_val~
        LabelTo(from, 1054000, $"{_charges}\t{_inscription.DefaultIfNullOrEmpty(" ")}");
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);

        if (from.Alive && IsChildOf(from))
        {
            var bound = Bound;

            list.Add(new BraceletEntry(Activate, 6170, bound != null));
            list.Add(new BraceletEntry(Search, 6171, bound != null));
            list.Add(new BraceletEntry(Bind, bound == null ? 6173 : 6174, true));
            list.Add(new BraceletEntry(Inscribe, 6175, true));
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (Bound == null)
        {
            Bind(from);
        }
        else
        {
            Activate(from);
        }
    }

    public void Activate(Mobile from)
    {
        if (Deleted || Bound == null)
        {
            return;
        }

        if (!IsChildOf(from))
        {
            from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
        }
        else if (_timer != null)
        {
            // The bracelet is already attempting contact. You decide to wait a moment.
            from.SendLocalizedMessage(1054013);
        }
        else
        {
            from.PlaySound(0xF9);
            // * You concentrate on the bracelet to summon its power *
            from.LocalOverheadMessage(MessageType.Regular, 0x5D, 1151783);

            from.Frozen = true;

            _timer = new TransportTimer(this, from);
            _timer.Start();
        }
    }

    public void Search(Mobile from)
    {
        var bound = Bound;

        if (Deleted || bound == null)
        {
            return;
        }

        if (!IsChildOf(from))
        {
            from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
        }
        else
        {
            CheckUse(from, true);
        }
    }

    private bool CheckUse(Mobile from, bool successMessage)
    {
        var bound = Bound;

        if (bound == null)
        {
            return false;
        }

        var boundRoot = bound.RootParent as Mobile;

        if (Charges == 0)
        {
            // The bracelet glows black. It must be charged before it can be used again.
            from.SendLocalizedMessage(1054005);
            return false;
        }

        if (from.FindItemOnLayer(Layer.Bracelet) != this)
        {
            from.SendLocalizedMessage(1054004); // You must equip the bracelet in order to use its power.
            return false;
        }

        if (boundRoot?.NetState == null || boundRoot.FindItemOnLayer(Layer.Bracelet) != bound)
        {
            // The bracelet emits a red glow. The bracelet's twin is not available for transport.
            from.SendLocalizedMessage(1054006);
            return false;
        }

        if (!Core.AOS && from.Map != boundRoot.Map)
        {
            from.SendLocalizedMessage(1054014); // The bracelet glows black. The bracelet's target is on another facet.
            return false;
        }

        if (Sigil.ExistsOn(from))
        {
            from.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
            return false;
        }

        if (!SpellHelper.CheckTravel(from, TravelCheckType.RecallFrom, out var failureMessage))
        {
            failureMessage.SendMessageTo(from);
            return false;
        }

        if (!SpellHelper.CheckTravel(from, boundRoot.Map, boundRoot.Location, TravelCheckType.RecallTo, out failureMessage))
        {
            failureMessage.SendMessageTo(from);
            return false;
        }

        if (boundRoot.Map == Map.Felucca && from is PlayerMobile mobile && mobile.Young)
        {
            mobile.SendLocalizedMessage(1049543); // You decide against traveling to Felucca while you are still young.
            return false;
        }

        if (from.Kills >= 5 && boundRoot.Map != Map.Felucca)
        {
            from.SendLocalizedMessage(1019004); // You are not allowed to travel there.
            return false;
        }

        if (from.Criminal)
        {
            from.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
            return false;
        }

        if (SpellHelper.CheckCombat(from))
        {
            from.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
            return false;
        }

        if (StaminaSystem.IsOverloaded(from))
        {
            from.SendLocalizedMessage(502359, "", 0x22); // Thou art too encumbered to move.
            return false;
        }

        if (from.Region.IsPartOf<JailRegion>())
        {
            from.SendLocalizedMessage(1114345, "", 0x35); // You'll need a better jailbreak plan than that!
            return false;
        }

        if (boundRoot.Region.IsPartOf<JailRegion>())
        {
            from.SendLocalizedMessage(1019004); // You are not allowed to travel there.
            return false;
        }

        if (successMessage)
        {
            from.SendLocalizedMessage(1054015); // The bracelet's twin is available for transport.
        }

        return true;
    }

    public void Bind(Mobile from)
    {
        if (Deleted)
        {
            return;
        }

        if (!IsChildOf(from))
        {
            from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
        }
        else
        {
            from.SendLocalizedMessage(1054001); // Target the bracelet of binding you wish to bind this bracelet to.
            from.Target = new BindTarget(this);
        }
    }

    public void Inscribe(Mobile from)
    {
        if (Deleted)
        {
            return;
        }

        if (!IsChildOf(from))
        {
            from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
        }
        else
        {
            from.SendLocalizedMessage(1054009); // Enter the text to inscribe upon the bracelet :
            from.Prompt = new InscribePrompt(this);
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _recharges = reader.ReadEncodedInt();
        _charges = Math.Min(reader.ReadEncodedInt(), MaxCharges);
        _inscription = reader.ReadString();
        _bound = (BraceletOfBinding)reader.ReadEntity<Item>();
    }

    private delegate void BraceletCallback(Mobile from);

    private class BraceletEntry : ContextMenuEntry
    {
        private readonly BraceletCallback _callback;

        public BraceletEntry(BraceletCallback callback, int number, bool enabled) : base(number)
        {
            _callback = callback;

            if (!enabled)
            {
                Flags |= CMEFlags.Disabled;
            }
        }

        public override void OnClick()
        {
            var from = Owner.From;

            if (from.CheckAlive())
            {
                _callback(from);
            }
        }
    }

    private class TransportTimer : Timer
    {
        private readonly BraceletOfBinding _bracelet;
        private readonly Mobile _from;

        public TransportTimer(BraceletOfBinding bracelet, Mobile from) : base(TimeSpan.FromSeconds(2.0))
        {
            _bracelet = bracelet;
            _from = from;
        }

        protected override void OnTick()
        {
            _bracelet._timer = null;
            _from.Frozen = false;

            if (_bracelet.Deleted || _from.Deleted ||
                !_bracelet.CheckUse(_from, false) ||
                _bracelet.Bound.RootParent is not Mobile boundRoot)
            {
                return;
            }

            _bracelet.Charges--;

            BaseCreature.TeleportPets(_from, boundRoot.Location, boundRoot.Map, true);

            _from.PlaySound(0x1FC);
            _from.MoveToWorld(boundRoot.Location, boundRoot.Map);
            _from.PlaySound(0x1FC);
        }
    }

    private class BindTarget : Target
    {
        private readonly BraceletOfBinding _bracelet;

        public BindTarget(BraceletOfBinding bracelet) : base(-1, false, TargetFlags.None) => _bracelet = bracelet;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_bracelet.Deleted)
            {
                return;
            }

            if (!_bracelet.IsChildOf(from))
            {
                from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
            }
            else if (targeted is BraceletOfBinding bindBracelet)
            {
                if (bindBracelet == _bracelet)
                {
                    from.SendLocalizedMessage(1054012); // You cannot bind a bracelet of binding to itself!
                }
                else if (!bindBracelet.IsChildOf(from))
                {
                    from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
                }
                else
                {
                    // You bind the bracelet to its counterpart. The bracelets glow with power.
                    from.SendLocalizedMessage(1054003);
                    from.PlaySound(0x1FA);

                    _bracelet.Bound = bindBracelet;
                }
            }
            else
            {
                from.SendLocalizedMessage(1054002); // You can only bind this bracelet to another bracelet of binding!
            }
        }
    }

    private class InscribePrompt : Prompt
    {
        private readonly BraceletOfBinding _bracelet;

        public InscribePrompt(BraceletOfBinding bracelet) => _bracelet = bracelet;

        public override void OnResponse(Mobile from, string text)
        {
            if (_bracelet.Deleted)
            {
                return;
            }

            if (!_bracelet.IsChildOf(from))
            {
                from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
            }
            else
            {
                from.SendLocalizedMessage(1054011); // You mark the bracelet with your inscription.
                _bracelet.Inscription = text;
            }
        }

        public override void OnCancel(Mobile from)
        {
            from.SendLocalizedMessage(1054010); // You decide not to inscribe the bracelet at this time.
        }
    }
}
