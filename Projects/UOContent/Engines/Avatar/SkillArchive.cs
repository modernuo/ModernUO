using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Avatar;

/// <summary>
/// Highest base (fixed point) ever reached in each skill across all of an Avatar's lives.
/// </summary>
[PropertyObject]
[SerializationGenerator(0)]
public partial class SkillArchive
{
    [DirtyTrackingEntity]
    [CanBeNull]
    private PlayerMobile _player;

    [SerializableField(0, setter: "private")]
    private int[] _skills;

    public SkillArchive(PlayerContext context) : this(context?.Player)
    {
    }

    public SkillArchive(PlayerMobile player)
    {
        _player = player;
        _skills = new int[SkillInfo.Table.Length];
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Alchemy { get => this[SkillName.Alchemy]; set => this[SkillName.Alchemy] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Anatomy { get => this[SkillName.Anatomy]; set => this[SkillName.Anatomy] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int AnimalLore { get => this[SkillName.AnimalLore]; set => this[SkillName.AnimalLore] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int AnimalTaming { get => this[SkillName.AnimalTaming]; set => this[SkillName.AnimalTaming] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Archery { get => this[SkillName.Archery]; set => this[SkillName.Archery] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int ArmsLore { get => this[SkillName.ArmsLore]; set => this[SkillName.ArmsLore] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Begging { get => this[SkillName.Begging]; set => this[SkillName.Begging] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Blacksmith { get => this[SkillName.Blacksmith]; set => this[SkillName.Blacksmith] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Bushido { get => this[SkillName.Bushido]; set => this[SkillName.Bushido] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Camping { get => this[SkillName.Camping]; set => this[SkillName.Camping] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Carpentry { get => this[SkillName.Carpentry]; set => this[SkillName.Carpentry] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Cartography { get => this[SkillName.Cartography]; set => this[SkillName.Cartography] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Chivalry { get => this[SkillName.Chivalry]; set => this[SkillName.Chivalry] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Cooking { get => this[SkillName.Cooking]; set => this[SkillName.Cooking] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int DetectHidden { get => this[SkillName.DetectHidden]; set => this[SkillName.DetectHidden] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Discordance { get => this[SkillName.Discordance]; set => this[SkillName.Discordance] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int EvalInt { get => this[SkillName.EvalInt]; set => this[SkillName.EvalInt] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Fencing { get => this[SkillName.Fencing]; set => this[SkillName.Fencing] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Fishing { get => this[SkillName.Fishing]; set => this[SkillName.Fishing] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Fletching { get => this[SkillName.Fletching]; set => this[SkillName.Fletching] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Focus { get => this[SkillName.Focus]; set => this[SkillName.Focus] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Forensics { get => this[SkillName.Forensics]; set => this[SkillName.Forensics] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Healing { get => this[SkillName.Healing]; set => this[SkillName.Healing] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Herding { get => this[SkillName.Herding]; set => this[SkillName.Herding] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Hiding { get => this[SkillName.Hiding]; set => this[SkillName.Hiding] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Imbuing { get => this[SkillName.Imbuing]; set => this[SkillName.Imbuing] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Inscribe { get => this[SkillName.Inscribe]; set => this[SkillName.Inscribe] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int ItemID { get => this[SkillName.ItemID]; set => this[SkillName.ItemID] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Lockpicking { get => this[SkillName.Lockpicking]; set => this[SkillName.Lockpicking] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Lumberjacking { get => this[SkillName.Lumberjacking]; set => this[SkillName.Lumberjacking] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Macing { get => this[SkillName.Macing]; set => this[SkillName.Macing] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Magery { get => this[SkillName.Magery]; set => this[SkillName.Magery] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int MagicResist { get => this[SkillName.MagicResist]; set => this[SkillName.MagicResist] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Meditation { get => this[SkillName.Meditation]; set => this[SkillName.Meditation] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Mining { get => this[SkillName.Mining]; set => this[SkillName.Mining] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Musicianship { get => this[SkillName.Musicianship]; set => this[SkillName.Musicianship] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Mysticism { get => this[SkillName.Mysticism]; set => this[SkillName.Mysticism] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Necromancy { get => this[SkillName.Necromancy]; set => this[SkillName.Necromancy] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Ninjitsu { get => this[SkillName.Ninjitsu]; set => this[SkillName.Ninjitsu] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Parry { get => this[SkillName.Parry]; set => this[SkillName.Parry] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Peacemaking { get => this[SkillName.Peacemaking]; set => this[SkillName.Peacemaking] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Poisoning { get => this[SkillName.Poisoning]; set => this[SkillName.Poisoning] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Provocation { get => this[SkillName.Provocation]; set => this[SkillName.Provocation] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int RemoveTrap { get => this[SkillName.RemoveTrap]; set => this[SkillName.RemoveTrap] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Snooping { get => this[SkillName.Snooping]; set => this[SkillName.Snooping] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int SpiritSpeak { get => this[SkillName.SpiritSpeak]; set => this[SkillName.SpiritSpeak] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Spellweaving { get => this[SkillName.Spellweaving]; set => this[SkillName.Spellweaving] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Stealing { get => this[SkillName.Stealing]; set => this[SkillName.Stealing] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Stealth { get => this[SkillName.Stealth]; set => this[SkillName.Stealth] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Swords { get => this[SkillName.Swords]; set => this[SkillName.Swords] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Tactics { get => this[SkillName.Tactics]; set => this[SkillName.Tactics] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Tailoring { get => this[SkillName.Tailoring]; set => this[SkillName.Tailoring] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int TasteID { get => this[SkillName.TasteID]; set => this[SkillName.TasteID] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Throwing { get => this[SkillName.Throwing]; set => this[SkillName.Throwing] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Tinkering { get => this[SkillName.Tinkering]; set => this[SkillName.Tinkering] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Tracking { get => this[SkillName.Tracking]; set => this[SkillName.Tracking] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Veterinary { get => this[SkillName.Veterinary]; set => this[SkillName.Veterinary] = value; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Wrestling { get => this[SkillName.Wrestling]; set => this[SkillName.Wrestling] = value; }

    public int Length => _skills.Length;

    public int this[SkillName name]
    {
        get => this[(int)name];
        set => this[(int)name] = value;
    }

    public int this[int skillID]
    {
        get => skillID >= 0 && skillID < _skills.Length ? _skills[skillID] : 0;
        set
        {
            if (skillID < 0 || skillID >= SkillInfo.Table.Length)
            {
                return;
            }

            // Saves written before the skill table grew have a shorter array
            if (skillID >= _skills.Length)
            {
                Array.Resize(ref _skills, SkillInfo.Table.Length);
            }

            if (_skills[skillID] != value)
            {
                _skills[skillID] = value;
                this.MarkDirty();
            }
        }
    }

    public override string ToString() => "...";
}
