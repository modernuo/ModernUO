using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// What one bot was given, and therefore what death may not take from it.
///
/// <para>
/// <b>Owned by the bot, not by a table somewhere.</b> This is a plain object that the bot holds a
/// reference to, and that is a deliberate break with the first version, where a bot's state lived in
/// thirty-two dictionaries keyed by its serial, spread across as many files. Every one of those needed
/// a <c>Reset</c>, every one leaked when the population was torn down, and answering "what does this
/// bot own" meant reading thirty-two files. A bot that is deleted takes this with it.
/// </para>
///
/// <para>
/// <b>Not serialized, on purpose.</b> The population is rebuilt from configuration on every world
/// load — bots that come back from a save are purged, because the engine's entity serializer has no
/// per-entity opt-out — so a bond only ever has to survive as long as the process. If v2 ever keeps
/// bots across a restart, this is one of the things that has to start being written down.
/// </para>
/// </summary>
public sealed class BotBond
{
    /// <summary>
    /// Serials of the indivisible things this bot was given: its weapon, its staff, its tools.
    ///
    /// Serials rather than types, because "is this particular sword the one I was given" is the
    /// question the trade code has to answer. A bot that has bought a second katana may sell that one
    /// and may not sell this one, and a type check cannot tell them apart.
    /// </summary>
    public HashSet<Serial> Items { get; } = [];

    /// <summary>
    /// The types this bot was issued, so that anything genuinely destroyed can be handed back.
    ///
    /// Kept alongside <see cref="Items"/> rather than derived from it because a serial whose item is
    /// gone no longer says what it used to be.
    /// </summary>
    public List<Type> Issued { get; } = [];

    /// <summary>
    /// Ammunition, and how much of it is bound: the amount granted at birth, per type.
    ///
    /// This is the whole reason a ledger exists at all. Stacks merge, so a hundred bound arrows and
    /// fifty bought ones become one stack of a hundred and fifty carrying a single loot flag — and
    /// whichever flag the merge kept is wrong for half the stack. A remembered number has no such
    /// problem, and it expresses the rule exactly: what death gives back is
    /// <c>min(carried, granted)</c>, a ceiling rather than a refill.
    /// </summary>
    public Dictionary<Type, int> Ammunition { get; } = [];

    /// <summary>Which weapon the birth roll settled on, and the skill that swings it.</summary>
    public BotWeaponOption? Weapon { get; set; }

    /// <summary>
    /// Writes the bond, so that a bot coming back from a save is not handed a second kit.
    ///
    /// <para>
    /// The bound items are written as serials because the engine restores an item under the serial it was saved
    /// with: the dyed weapon in the bot's hand after a restart is the same object, not a replacement. The roll —
    /// which of the class's weapons this bot actually got — is written because it is made once at birth and
    /// nothing else records it, and a bot that loses it can never buy another of what it had.
    /// </para>
    /// </summary>
    public void Save(IGenericWriter writer)
    {
        writer.Write(Weapon.HasValue);

        if (Weapon.HasValue)
        {
            var weapon = Weapon.Value;

            writer.Write(weapon.Weapon);
            writer.Write((int)weapon.Skill);
            writer.Write(weapon.Target);
            writer.Write(weapon.Ammunition);
            writer.Write(weapon.AmmunitionCount);
        }

        writer.Write(Items.Count);

        foreach (var serial in Items)
        {
            writer.Write(serial);
        }

        writer.Write(Issued.Count);

        for (var i = 0; i < Issued.Count; i++)
        {
            writer.Write(Issued[i]);
        }

        writer.Write(Ammunition.Count);

        foreach (var (type, many) in Ammunition)
        {
            writer.Write(type);
            writer.Write(many);
        }
    }

    /// <summary>Reads back what <see cref="Save"/> wrote.</summary>
    public void Load(IGenericReader reader)
    {
        if (reader.ReadBool())
        {
            var weapon = reader.ReadType();
            var skill = (SkillName)reader.ReadInt();
            var target = reader.ReadDouble();
            var ammunition = reader.ReadType();
            var many = reader.ReadInt();

            if (weapon != null)
            {
                Weapon = new BotWeaponOption(weapon, skill, target, ammunition, many);
            }
        }

        var count = reader.ReadInt();

        for (var i = 0; i < count; i++)
        {
            Items.Add(reader.ReadSerial());
        }

        count = reader.ReadInt();

        for (var i = 0; i < count; i++)
        {
            if (reader.ReadType() is { } type)
            {
                Issued.Add(type);
            }
        }

        count = reader.ReadInt();

        for (var i = 0; i < count; i++)
        {
            var type = reader.ReadType();
            var many = reader.ReadInt();

            if (type != null)
            {
                Ammunition[type] = many;
            }
        }
    }
}
