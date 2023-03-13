using ModernUO.Serialization;
using Server.Engines.VeteranRewards;
using Server.Gumps;

namespace Server.Items;

public enum MinotaurStatueType
{
    AttackSouth = 100,
    AttackEast = 101,
    DefendSouth = 102,
    DefendEast = 103
}

[SerializationGenerator(0)]
public partial class MinotaurStatue : BaseAddon, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public MinotaurStatue(MinotaurStatueType type)
    {
        switch (type)
        {
            case MinotaurStatueType.AttackSouth:
                {
                    AddComponent(new AddonComponent(0x306C), 0, 0, 0);
                    AddComponent(new AddonComponent(0x306D), -1, 0, 0);
                    AddComponent(new AddonComponent(0x306E), 0, -1, 0);
                    break;
                }
            case MinotaurStatueType.AttackEast:
                {
                    AddComponent(new AddonComponent(0x3074), 0, 0, 0);
                    AddComponent(new AddonComponent(0x3075), -1, 0, 0);
                    AddComponent(new AddonComponent(0x3076), 0, -1, 0);
                    break;
                }
            case MinotaurStatueType.DefendSouth:
                {
                    AddComponent(new AddonComponent(0x3072), 0, 0, 0);
                    AddComponent(new AddonComponent(0x3073), 0, -1, 0);
                    break;
                }
            case MinotaurStatueType.DefendEast:
                {
                    AddComponent(new AddonComponent(0x306F), 0, 0, 0);
                    AddComponent(new AddonComponent(0x3070), -1, 0, 0);
                    AddComponent(new AddonComponent(0x3071), 0, -1, 0);
                    break;
                }
        }
    }

    public override BaseAddonDeed Deed =>
        new MinotaurStatueDeed
        {
            IsRewardItem = _isRewardItem
        };
}

[SerializationGenerator(0)]
public partial class MinotaurStatueDeed : BaseAddonDeed, IRewardItem, IRewardOption
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    private MinotaurStatueType _statueType;

    [Constructible]
    public MinotaurStatueDeed() => LootType = LootType.Blessed;

    public override int LabelNumber => 1080409; // Minotaur Statue Deed

    public override BaseAddon Addon =>
        new MinotaurStatue(_statueType)
        {
            IsRewardItem = _isRewardItem
        };

    public void GetOptions(RewardOptionList list)
    {
        list.Add((int)MinotaurStatueType.AttackSouth, 1080410); // Minotaur Attack South
        list.Add((int)MinotaurStatueType.AttackEast, 1080411);  // Minotaur Attack East
        list.Add((int)MinotaurStatueType.DefendSouth, 1080412); // Minotaur Defend South
        list.Add((int)MinotaurStatueType.DefendEast, 1080413);  // Minotaur Defend East
    }

    public void OnOptionSelected(Mobile from, int option)
    {
        _statueType = (MinotaurStatueType)option;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_isRewardItem && !RewardSystem.CheckIsUsableBy(from, this))
        {
            return;
        }

        if (IsChildOf(from.Backpack))
        {
            from.CloseGump<RewardOptionGump>();
            from.SendGump(new RewardOptionGump(this));
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1076218); // 2nd Year Veteran Reward
        }
    }
}
