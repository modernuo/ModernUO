using System;
using System.Runtime.CompilerServices;
using ModernUO.Serialization;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Multis;

namespace Server.Items;

[SerializationGenerator(1)]
public partial class TreeStump : BaseAddon, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _logs;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private DateTime _lastChecked;

    [Constructible]
    public TreeStump(int itemID)
    {
        AddComponent(new AddonComponent(itemID), 0, 0, 0);
        _lastChecked = Core.Now;
    }

    public override BaseAddonDeed Deed =>
        new TreeStumpDeed
        {
            IsRewardItem = _isRewardItem,
            Logs = _logs
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateLogs()
    {
        var now = Core.Now;
        var daysSince = (now - _lastChecked).Days;
        _logs = Math.Min(100, _logs + daysSince * 10);
        _lastChecked = now;
    }

    public override void OnComponentUsed(AddonComponent c, Mobile from)
    {
        var house = BaseHouse.FindHouseAt(this);

        /*
         * Unique problems have unique solutions.  OSI does not have a problem with 1000s of mining carts
         * due to the fact that they have only a miniscule fraction of the number of 10 year vets that a
         * typical RunUO shard will have (RunUO's scaled down account aging system makes this a unique problem),
         * and the "freeness" of free accounts. We also dont have mitigating factors like inactive (unpaid)
         * accounts not gaining veteran time.
         *
         * The lack of high end vets and vet rewards on OSI has made testing the *exact* ranging/stacking
         * behavior of these things all but impossible, so either way its just an estimation.
         *
         * If youd like your shard's carts/stumps to work the way they did before, simply replace the check
         * below with this line of code:
         *
         * if (!from.InRange(GetWorldLocation(), 2)
         *
         * However, I am sure these checks are more accurate to OSI than the former version was.
         *
         */

        if (!from.InRange(GetWorldLocation(), 2) || !from.InLOS(this) || !(from.Z - Z > -3 && from.Z - Z < 3))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        if (house?.HasSecureAccess(from, SecureLevel.Friends) != true)
        {
            from.SendLocalizedMessage(1061637); // You are not allowed to access this.
            return;
        }

        UpdateLogs();

        if (_logs <= 0)
        {
            from.SendLocalizedMessage(1094720); // There are no more logs available.
            return;
        }

        var logs = Utility.Random(7) switch
        {
            0 => new Log(),
            1 => new AshLog(),
            2 => new OakLog(),
            3 => new YewLog(),
            4 => new HeartwoodLog(),
            5 => new BloodwoodLog(),
            _ => new FrostwoodLog()
        };

        var amount = Math.Min(10, _logs);
        logs.Amount = amount;

        if (!from.PlaceInBackpack(logs))
        {
            logs.Delete();
            from.SendLocalizedMessage(1078837); // Your backpack is full! Please make room and try again.
        }
        else
        {
            _logs -= amount;
            PublicOverheadMessage(MessageType.Regular, 0, 1094719, _logs.ToString()); // Logs: ~1_COUNT~
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _isRewardItem = reader.ReadBool();
        _logs = reader.ReadInt();

        var next = reader.ReadDateTime();

        if (next < Core.Now)
        {
            next = Core.Now;
        }

        _lastChecked = next - TimeSpan.FromDays(1);
    }
}

[SerializationGenerator(0)]
public partial class TreeStumpDeed : BaseAddonDeed, IRewardItem, IRewardOption
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _logs;

    private int _itemID;

    [Constructible]
    public TreeStumpDeed() => LootType = LootType.Blessed;

    public override int LabelNumber => 1080406; // a deed for a tree stump decoration

    public override BaseAddon Addon =>
        new TreeStump(_itemID)
        {
            IsRewardItem = _isRewardItem,
            Logs = _logs
        };

    public void GetOptions(RewardOptionList list)
    {
        list.Add(1, 1080403); // Tree Stump with Axe West
        list.Add(2, 1080404); // Tree Stump with Axe North
        list.Add(3, 1080401); // Tree Stump East
        list.Add(4, 1080402); // Tree Stump South
    }

    public void OnOptionSelected(Mobile from, int option)
    {
        _itemID = option switch
        {
            1 => 0xE56,
            2 => 0xE58,
            3 => 0xE57,
            4 => 0xE59,
            _ => _itemID
        };

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1076223); // 7th Year Veteran Reward
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
}
