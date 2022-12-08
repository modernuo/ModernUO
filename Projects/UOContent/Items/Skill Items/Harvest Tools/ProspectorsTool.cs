using System;
using ModernUO.Serialization;
using Server.Engines.Harvest;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ProspectorsTool : BaseBashing, IUsesRemaining
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _usesRemaining;

    [Constructible]
    public ProspectorsTool() : base(0xFB4)
    {
        Weight = 9.0;
        UsesRemaining = 50;
    }

    public override int LabelNumber => 1049065; // prospector's tool

    public override WeaponAbility PrimaryAbility => WeaponAbility.CrushingBlow;
    public override WeaponAbility SecondaryAbility => WeaponAbility.ShadowStrike;

    public override int AosStrengthReq => 40;
    public override int AosMinDamage => 13;
    public override int AosMaxDamage => 15;
    public override int AosSpeed => 33;

    public override int OldStrengthReq => 10;
    public override int OldMinDamage => 6;
    public override int OldMaxDamage => 8;
    public override int OldSpeed => 33;

    bool IUsesRemaining.ShowUsesRemaining
    {
        get => true;
        set { }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack) || Parent == from)
        {
            from.Target = new InternalTarget(this);
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }

    public void Prospect(Mobile from, object toProspect)
    {
        if (!IsChildOf(from.Backpack) && Parent != from)
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        HarvestSystem system = Mining.System;

        if (!system.GetHarvestDetails(from, this, toProspect, out var tileID, out var map, out var loc))
        {
            from.SendLocalizedMessage(1049048); // You cannot use your prospector tool on that.
            return;
        }

        var def = system.GetDefinition(tileID);

        if (def == null || def.Veins.Length <= 1)
        {
            from.SendLocalizedMessage(1049048); // You cannot use your prospector tool on that.
            return;
        }

        var bank = def.GetBank(map, loc.X, loc.Y);

        if (bank == null)
        {
            from.SendLocalizedMessage(1049048); // You cannot use your prospector tool on that.
            return;
        }

        HarvestVein vein = bank.Vein, defaultVein = bank.DefaultVein;

        if (vein == null || defaultVein == null)
        {
            from.SendLocalizedMessage(1049048); // You cannot use your prospector tool on that.
            return;
        }

        if (vein != defaultVein)
        {
            from.SendLocalizedMessage(1049049); // That ore looks to be prospected already.
            return;
        }

        var veinIndex = Array.IndexOf(def.Veins, vein);

        if (veinIndex < 0)
        {
            from.SendLocalizedMessage(1049048); // You cannot use your prospector tool on that.
        }
        else if (veinIndex >= def.Veins.Length - 1)
        {
            from.SendLocalizedMessage(1049061); // You cannot improve valorite ore through prospecting.
        }
        else
        {
            bank.Vein = def.Veins[veinIndex + 1];
            from.SendLocalizedMessage(1049050 + veinIndex);

            --UsesRemaining;

            if (UsesRemaining <= 0)
            {
                from.SendLocalizedMessage(1049062); // You have used up your prospector's tool.
                Delete();
            }
        }
    }

    private class InternalTarget : Target
    {
        private ProspectorsTool _tool;

        public InternalTarget(ProspectorsTool tool) : base(2, true, TargetFlags.None) => _tool = tool;

        protected override void OnTarget(Mobile from, object targeted)
        {
            _tool.Prospect(from, targeted);
        }
    }
}
