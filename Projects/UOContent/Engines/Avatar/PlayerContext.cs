using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Avatar;

[PropertyObject]
[SerializationGenerator(0)]
public partial class PlayerContext
{
    public static readonly PlayerContext Default = new(null);

    [DirtyTrackingEntity]
    [CanBeNull]
    private PlayerMobile _player;

    public PlayerContext(PlayerMobile player)
    {
        _player = player;
        _skills = new SkillArchive(player);
    }

    public PlayerMobile Player => _player;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _pointsFarmed;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _pointsSaved;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _skillCapLevel;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _statCapLevel;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _skillGainRateLevel;

    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _pointGainRateLevel;

    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _improvedTemplateCount;

    [SerializableField(7)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _unlockPrimarySkillBoost;

    [SerializableField(8)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _unlockSecondarySkillBoost;

    [SerializableField(9)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _unlockRecordSkillCaps;

    [SerializableField(10, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SkillArchive _skills;

    [SerializableField(11)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _recordedSkillCapLevel;

    [SerializableField(12)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _unlockRecordRecipes;

    [SerializableField(13)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SlayerName _rivalSlayerName;

    [SerializableField(14)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _rivalBonusEnabled;

    [SerializableField(15)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _rivalBonusPoints;

    [SerializableField(16)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private AvatarStarterTemplates _selectedTemplate;

    [SerializableField(17)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _lifetimePointsGained;

    [SerializableField(18)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _lifetimeDeaths;

    [SerializableField(19)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _lifetimeEnemyFactionKills;

    [SerializableField(20)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _lifetimeGameTime;

    [SerializableField(21)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _lifetimeCreatureKills;

    [SerializableField(22, setter: "private")]
    private SafetyDepositBox _safetyDepositBox;

    [SerializableField(23)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _safetyDepositBoxLevel;

    [SerializableField(24, fieldChanged: nameof(OnUnlockFullSkillArchiveChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _unlockFullSkillArchive;

    [SerializableField(25, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _draftModeEnabled;

    [SerializableField(26, getter: "private", setter: "private")]
    [SaveFlag(nameof(ShouldSerializeDraftedSkillSet))]
    private HashSet<SkillName> _draftedSkillSet;

    [SerializableField(27, getter: "private", setter: "private")]
    [SaveFlag(nameof(ShouldSerializeDraftBannedSkillSet))]
    private HashSet<SkillName> _draftBannedSkillSet;

    [SerializableField(28)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _prestigeLevel;

    // GameTime keeps accumulating on the same mobile across lives, so each life is measured from here
    [SerializableField(29)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _runStartGameTime;

    public TimeSpan GetRunGameTime(PlayerMobile player) => player.GameTime - RunStartGameTime;

    private bool ShouldSerializeDraftedSkillSet() => _draftedSkillSet != null;

    private bool ShouldSerializeDraftBannedSkillSet() => _draftBannedSkillSet != null;

    private void OnUnlockFullSkillArchiveChanged(bool oldValue, bool newValue)
    {
        ClearRewardCache(Categories.PrimaryBoosts);
        ClearRewardCache(Categories.SecondaryBoosts);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Active => this != Default;

    [CommandProperty(AccessLevel.GameMaster)]
    public int GrandTotalPoints => LifetimePointsGained + PointsFarmed;

    public bool HasSafetyDepositBox => _safetyDepositBox?.Deleted == false;

    public SafetyDepositBox GetOrCreateSafetyDepositBox(Mobile owner)
    {
        if (!HasSafetyDepositBox)
        {
            var box = new SafetyDepositBox(owner);
            SafetyDepositBox = box;
            owner.BankBox.AddItem(box);
            box.Location = new Point3D(0, 0, 0);

            return box;
        }

        return _safetyDepositBox;
    }

    public override string ToString() => "...";
}

public partial class PlayerContext
{
    public Dictionary<Categories, List<int>> RewardCache { get; set; }

    public void ClearRewardCache(Categories category)
    {
        RewardCache?.Remove(category);
    }

    public int GetRecordedSkillCap() =>
        Math.Min(
            Constants.RECORDED_SKILL_CAP_MAX_AMOUNT,
            Constants.RECORDED_SKILL_CAP_MIN_AMOUNT + RecordedSkillCapLevel * Constants.RECORDED_SKILL_CAP_INTERVAL
        );
}

public partial class PlayerContext
{
    private static readonly SlayerName[] _rivalSlayers =
    [
        SlayerName.Silver,
        SlayerName.Repond,
        SlayerName.ReptilianDeath,
        SlayerName.Exorcism,
        SlayerName.ArachnidDoom,
        SlayerName.ElementalBan,
        SlayerName.Fey
    ];

    [CommandProperty(AccessLevel.GameMaster)]
    public bool HasRivalFaction => RivalSlayerName != SlayerName.None;

    [CommandProperty(AccessLevel.GameMaster)]
    public string RivalFactionName =>
        RivalSlayerName switch
        {
            SlayerName.None           => "None",
            SlayerName.Silver         => "The Returned",
            SlayerName.Repond         => "The Oathbreakers",
            SlayerName.ReptilianDeath => "The Scaled Ones",
            SlayerName.Exorcism       => "The Dreadwings",
            SlayerName.ArachnidDoom   => "The Doom Weavers",
            SlayerName.ElementalBan   => "The Riftborn",
            SlayerName.Fey            => "The Faeborn Circle",
            _                         => "Unknown Rival Race"
        };

    public void GenerateRivalry()
    {
        RivalSlayerName = _rivalSlayers.RandomElement();
        RivalBonusEnabled = true;
    }
}

public partial class PlayerContext
{
    public HashSet<AvatarStarterTemplates> BoostedTemplateCache { get; set; }

    public void ApplyTemplate(PlayerMobile player, AvatarStarterTemplates template)
    {
        if (!player.Avatar.Active)
        {
            return;
        }

        switch (template)
        {
            case AvatarStarterTemplates.Brute:
            case AvatarStarterTemplates.Acrobat:
            case AvatarStarterTemplates.Scholar:
                {
                    // You get NOTHING!
                    break;
                }

            default:
                {
                    if (template > AvatarStarterTemplates.DEFAULT_START && template < AvatarStarterTemplates.DEFAULT_END)
                    {
                        var skills = AvatarTemplateSkills.GetSkills(template);
                        Span<SkillName> names = stackalloc SkillName[skills.Length];
                        for (var i = 0; i < skills.Length; i++)
                        {
                            names[i] = skills[i].Name;
                        }

                        AvatarTemplateSkills.AddSkillBasedItems(player, names);
                    }

                    break;
                }
        }

        if (DraftModeEnabled)
        {
            // Draft players get items for every skill they drafted regardless of their current skill level
            Span<SkillName> drafted = stackalloc SkillName[DraftedSkills.Count];
            var i = 0;
            foreach (var skillName in DraftedSkills)
            {
                drafted[i++] = skillName;
            }

            AvatarTemplateSkills.AddSkillBasedItems(player, drafted);
        }
    }
}
