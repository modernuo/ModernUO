using ModernUO.CodeGeneratedEvents;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Spells;

namespace Server.Items;

[Flippable(0x1541, 0x1542)]
[SerializationGenerator(0, false)]
public partial class SpellFocusingSash : BaseMiddleTorso
{
    private const int SpellFocusingCliloc = 1150058;
    private const int BrittleCliloc = 1116209;
    private const int ResetMessage = 1150117;
    private const int TunedMessage = 1150118;
    private const int PeakMessage = 1150116;
    private const int BuffTitleCliloc = 1151391;
    private const int BuffSecondaryCliloc = 1151392;
    private const int SequenceLength = 21;

    private Mobile _spellCastTarget;
    private int _spellCastCount;
    private bool _enabled = true;

    [Constructible]
    public SpellFocusingSash() : base(0x1541)
    {
        Attributes.BonusMana = 1;
        Attributes.DefendChance = 5;
        HitPoints = MaxHitPoints = 255;
    }

    public override int LabelNumber => 1150059;
    public override double DefaultWeight => 1.0;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
    public override int AosStrReq => 10;

    [SerializableProperty(0, useField: nameof(_enabled))]
    [CommandProperty(AccessLevel.GameMaster)]
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
            {
                return;
            }

            _enabled = value;
            ResetSequence(Parent as Mobile);
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    public static void Configure()
    {
        EventSink.Logout += Clear;
    }

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);

        if (Core.SA)
        {
            list.Add(SpellFocusingCliloc);
            list.Add(BrittleCliloc);
        }
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (Core.SA && from == Parent && from.Alive)
        {
            list.Add(new ToggleSpellFocusingEntry(this));
        }
    }

    public override void OnAdded(IEntity parent)
    {
        base.OnAdded(parent);

        if (parent is Mobile mobile)
        {
            ResetSequence(mobile);
        }
    }

    public override void OnRemoved(IEntity parent)
    {
        if (parent is Mobile mobile)
        {
            ResetSequence(mobile);
        }

        base.OnRemoved(parent);
    }

    public override void OnDelete()
    {
        ResetSequence(Parent as Mobile);
        base.OnDelete();
    }

    public static bool TryGetDamageOffset(Spell spell, Mobile caster, Mobile target, out int offset)
    {
        offset = 0;

        if (!Core.SA || spell?.SpellFocusingEligible != true || caster?.Deleted != false)
        {
            return false;
        }

        if (caster.FindItemOnLayer(Layer.MiddleTorso) is not SpellFocusingSash sash || !sash.Enabled)
        {
            return false;
        }

        if (target?.Deleted != false || !target.Alive || !caster.Alive)
        {
            sash.ResetSequence(caster);
            return false;
        }

        return sash.TryGetDamageOffset(caster, target, out offset);
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeletedEvent))]
    public static void Clear(Mobile mobile)
    {
        if (mobile?.FindItemOnLayer(Layer.MiddleTorso) is SpellFocusingSash sash)
        {
            sash.ResetSequence(mobile);
        }
    }

    private bool TryGetDamageOffset(Mobile caster, Mobile target, out int offset)
    {
        offset = 0;

        if (_spellCastTarget?.Deleted != false || !_spellCastTarget.Alive)
        {
            ResetSequence(caster);
        }

        if (_spellCastTarget != target)
        {
            if (_spellCastTarget != null)
            {
                caster.SendLocalizedMessage(ResetMessage);
            }

            ResetSequence(caster);
            _spellCastTarget = target;
        }

        offset = GetDamageOffset(_spellCastCount, target.Player);
        _spellCastCount++;

        if (offset == 0)
        {
            caster.SendLocalizedMessage(TunedMessage);
        }

        if (_spellCastCount >= SequenceLength)
        {
            caster.SendLocalizedMessage(PeakMessage);
            ResetSequence(caster);
        }
        else
        {
            RefreshBuffInfo(caster, offset);
        }

        return true;
    }

    private static int GetDamageOffset(int castCount, bool playerTarget)
    {
        if (castCount < 6)
        {
            return -30 + castCount * 6;
        }

        var offset = (castCount - 5) * 2;
        return playerTarget ? System.Math.Min(offset, 20) : System.Math.Min(offset, 30);
    }

    private void ResetSequence(Mobile caster)
    {
        _spellCastTarget = null;
        _spellCastCount = 0;

        if (caster is PlayerMobile player)
        {
            player.RemoveBuff(BuffIcon.SpellFocusingBuff);
            player.RemoveBuff(BuffIcon.SpellFocusingDebuff);
        }
    }

    private void RefreshBuffInfo(Mobile caster, int offset)
    {
        if (caster is not PlayerMobile player || caster != Parent || !Enabled)
        {
            return;
        }

        player.RemoveBuff(offset < 0 ? BuffIcon.SpellFocusingBuff : BuffIcon.SpellFocusingDebuff);
        player.AddBuff(
            new BuffInfo(
                offset < 0 ? BuffIcon.SpellFocusingDebuff : BuffIcon.SpellFocusingBuff,
                BuffTitleCliloc,
                BuffSecondaryCliloc,
                args: $"{_spellCastTarget?.Name ?? "None"}\t{offset}"
            )
        );
    }

    private sealed class ToggleSpellFocusingEntry : ContextMenuEntry
    {
        private readonly SpellFocusingSash _sash;

        public ToggleSpellFocusingEntry(SpellFocusingSash sash) : base(sash.Enabled ? 3006151 : 3006150, 2)
        {
            _sash = sash;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (target is SpellFocusingSash sash && sash == _sash && from == sash.Parent && from.Alive)
            {
                sash.Enabled = !sash.Enabled;
                from.SendMessage(sash.Enabled ? "Spell Focusing enabled." : "Spell Focusing disabled.");
            }
        }
    }
}
