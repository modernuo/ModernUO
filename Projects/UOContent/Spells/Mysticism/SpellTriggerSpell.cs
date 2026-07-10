using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells.Mysticism;

public class SpellTriggerSpell : MysticSpell
{
    public const int SpellId = 685;
    public const int MaximumStorableCircle = 6;
    public const int CooldownSeconds = 300;

    private static readonly SpellInfo _info = new(
        "Spell Trigger",
        "In Vas Ort Ex",
        230,
        9022,
        Reagent.DragonsBlood,
        Reagent.Garlic,
        Reagent.MandrakeRoot,
        Reagent.SpidersSilk
    );

    private static readonly SpellTriggerDefinition[] _definitions =
    [
        new(677, "Nether Bolt", SpellCircle.First, 0x2D9E),
        new(678, "Healing Stone", SpellCircle.First, 0x2D9F),
        new(679, "Purge Magic", SpellCircle.Second, 0x2DA0),
        new(680, "Enchant", SpellCircle.Second, 0x2DA1),
        new(682, "Eagle Strike", SpellCircle.Third, 0x2DA3),
        new(683, "Animated Weapon", SpellCircle.Fourth, 0x2DA4),
        new(684, "Stone Form", SpellCircle.Fourth, 0x2DA5),
        new(687, "Cleansing Winds", SpellCircle.Sixth, 0x2DA8),
        new(688, "Bombard", SpellCircle.Sixth, 0x2DA9)
    ];

    private static readonly Dictionary<Mobile, DateTime> _cooldowns = new();

    private TimerExecutionToken _selectionTimer;

    static SpellTriggerSpell()
    {
        EventSink.Logout += OnLogout;
    }

    public SpellTriggerSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Fifth;

    public static IReadOnlyList<SpellTriggerDefinition> Definitions => _definitions;

    public override bool CheckCast()
    {
        if (!Core.SA || !base.CheckCast())
        {
            return false;
        }

        if (Caster.Backpack == null)
        {
            Caster.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return false;
        }

        if (GetEligibleDefinitions(Caster).Count == 0)
        {
            Caster.SendMessage("You do not know any Mysticism spells that can be stored.");
            return false;
        }

        return true;
    }

    public override void OnCast()
    {
        var definitions = GetEligibleDefinitions(Caster);

        if (definitions.Count == 0 || !SpellTriggerGump.DisplayTo(Caster, this, definitions))
        {
            FinishSequence();
            return;
        }

        Timer.StartTimer(TimeSpan.FromSeconds(30), CancelSelection, out _selectionTimer);
    }

    public override void OnDisturb(DisturbType type, bool message)
    {
        Caster.CloseGump<SpellTriggerGump>();
        _selectionTimer.Cancel();
        base.OnDisturb(type, message);
    }

    public override void FinishSequence()
    {
        _selectionTimer.Cancel();
        Caster.CloseGump<SpellTriggerGump>();
        base.FinishSequence();
    }

    internal void FinishSelection(int spellId)
    {
        if (Caster.Spell != this || State != SpellState.Sequencing)
        {
            return;
        }

        try
        {
            var definition = GetDefinition(spellId);

            if (definition == null || !IsEligible(Caster, definition))
            {
                return;
            }

            var newStone = new SpellStone(definition.SpellId);

            if (!Caster.PlaceInBackpack(newStone))
            {
                newStone.Delete();
                Caster.SendMessage("There is not enough room in your backpack for a Spell Stone.");
                return;
            }

            if (!CheckSequence())
            {
                newStone.Delete();
                return;
            }

            using (var stones = Caster.Backpack.EnumerateItemsByType<SpellStone>())
            {
                foreach (var stone in stones)
                {
                    if (stone != newStone)
                    {
                        stone.Delete();
                    }
                }
            }

            Caster.PlaySound(0x659);
            Caster.SendLocalizedMessage(1080165); // A Spell Stone appears in your backpack.
        }
        finally
        {
            FinishSequence();
        }
    }

    internal void CancelSelection()
    {
        if (Caster.Spell == this && State == SpellState.Sequencing)
        {
            Caster.SendMessage("You decide not to create a Spell Stone.");
            FinishSequence();
        }
        else
        {
            _selectionTimer.Cancel();
            Caster.CloseGump<SpellTriggerGump>();
        }
    }

    internal static int GetMaximumStorableCircle(Mobile caster)
    {
        if (caster == null)
        {
            return 0;
        }

        var skillTotal = GetBaseSkill(caster) + Math.Max(caster.Skills.Focus.Value, caster.Skills.Imbuing.Value);
        return Math.Min(MaximumStorableCircle, (int)(skillTotal / 40.0));
    }

    internal static List<SpellTriggerDefinition> GetEligibleDefinitions(Mobile caster)
    {
        var definitions = new List<SpellTriggerDefinition>();
        var maximumCircle = GetMaximumStorableCircle(caster);

        if (caster == null || caster.Deleted || caster.Backpack == null || maximumCircle == 0)
        {
            return definitions;
        }

        for (var i = 0; i < _definitions.Length; i++)
        {
            var definition = _definitions[i];

            if (definition.Rank > maximumCircle || !IsRegistered(definition.SpellId) || !IsKnown(caster, definition.SpellId))
            {
                continue;
            }

            definitions.Add(definition);
        }

        return definitions;
    }

    internal static SpellTriggerDefinition GetDefinition(int spellId)
    {
        for (var i = 0; i < _definitions.Length; i++)
        {
            if (_definitions[i].SpellId == spellId)
            {
                return _definitions[i];
            }
        }

        return null;
    }

    internal static bool TryUseStone(SpellStone stone, Mobile caster)
    {
        if (stone == null || stone.Deleted || caster == null || caster.Deleted || !caster.Alive)
        {
            return false;
        }

        if (caster.Backpack == null || !stone.IsChildOf(caster.Backpack))
        {
            caster.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return false;
        }

        if (IsOnCooldown(caster, out var remaining))
        {
            var seconds = Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds));
            caster.SendLocalizedMessage(1079263, seconds.ToString()); // You must wait ~1_seconds~ seconds before you can use this item.
            return false;
        }

        var definition = GetDefinition(stone.SpellId);

        if (definition == null || !IsRegistered(stone.SpellId))
        {
            caster.SendLocalizedMessage(502345); // This spell has been temporarily disabled.
            return false;
        }

        if (!IsKnown(caster, stone.SpellId))
        {
            caster.SendLocalizedMessage(500015); // You do not have that spell!
            return false;
        }

        var spell = SpellRegistry.NewSpell(stone.SpellId, caster, stone);

        if (spell == null || !spell.Cast())
        {
            return false;
        }

        _cooldowns[caster] = Core.Now.AddSeconds(CooldownSeconds);
        stone.Delete();
        return true;
    }

    internal static bool IsOnCooldown(Mobile caster, out TimeSpan remaining)
    {
        if (caster == null || !_cooldowns.TryGetValue(caster, out var cooldownUntil))
        {
            remaining = TimeSpan.Zero;
            return false;
        }

        remaining = cooldownUntil - Core.Now;

        if (remaining <= TimeSpan.Zero)
        {
            _cooldowns.Remove(caster);
            remaining = TimeSpan.Zero;
            return false;
        }

        return true;
    }

    internal static void ClearCooldownForTests(Mobile caster) => ClearCooldown(caster);

    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeletedEvent))]
    public static void OnCasterRemoved(Mobile caster) => ClearCooldown(caster);

    private static void OnLogout(Mobile caster) => ClearCooldown(caster);

    private static void ClearCooldown(Mobile caster)
    {
        if (caster != null)
        {
            _cooldowns.Remove(caster);
        }
    }

    private static bool IsEligible(Mobile caster, SpellTriggerDefinition definition) =>
        definition != null &&
        definition.Rank <= GetMaximumStorableCircle(caster) &&
        IsRegistered(definition.SpellId) &&
        IsKnown(caster, definition.SpellId);

    private static bool IsKnown(Mobile caster, int spellId) => Spellbook.Find(caster, spellId)?.HasSpell(spellId) == true;

    private static bool IsRegistered(int spellId) =>
        spellId >= 0 && spellId < SpellRegistry.Types.Length && SpellRegistry.Types[spellId] != null;
}

public sealed class SpellTriggerDefinition
{
    public SpellTriggerDefinition(int spellId, string name, SpellCircle circle, int itemId)
    {
        SpellId = spellId;
        Name = name;
        Circle = circle;
        ItemId = itemId;
    }

    public int SpellId { get; }

    public string Name { get; }

    public SpellCircle Circle { get; }

    public int ItemId { get; }

    public int Rank => (int)Circle + 1;
}
