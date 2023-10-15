using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModernUO.Serialization;
using Server.Accounting;
using Server.ContextMenus;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Spells;
using Server.Targeting;

namespace Server.Mobiles;

public enum StatueType
{
    Marble,
    Jade,
    Bronze
}

public enum StatuePose
{
    Ready,
    Casting,
    Salute,
    AllPraiseMe,
    Fighting,
    HandsOnHips
}

public enum StatueMaterial
{
    Antique,
    Dark,
    Medium,
    Light
}

[SerializationGenerator(1)]
public partial class CharacterStatue : Mobile, IRewardItem
{
    private int m_Animation;
    private int m_Frames;

    [InvalidateProperties]
    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _sculptedBy;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _sculptedOn;

    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CharacterStatuePlinth _plinth;

    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    public CharacterStatue(Mobile from, StatueType statueType)
    {
        _statueType = statueType;
        _pose = StatuePose.Ready;
        _material = StatueMaterial.Antique;

        Direction = Direction.South;
        AccessLevel = AccessLevel.Counselor;
        Hits = HitsMax;
        Blessed = true;
        Frozen = true;

        CloneBody(from);
        CloneClothes(from);
        InvalidateHues();
    }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public StatueType StatueType
    {
        get => _statueType;
        set
        {
            _statueType = value;
            InvalidateHues();
            InvalidatePose();
            this.MarkDirty();
        }
    }

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public StatuePose Pose
    {
        get => _pose;
        set
        {
            _pose = value;
            InvalidatePose();
            this.MarkDirty();
        }
    }

    [SerializableProperty(2)]
    [CommandProperty(AccessLevel.GameMaster)]
    public StatueMaterial Material
    {
        get => _material;
        set
        {
            _material = value;
            InvalidateHues();
            InvalidatePose();
            this.MarkDirty();
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        DisplayPaperdollTo(from);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (!string.IsNullOrEmpty(_sculptedBy))
        {
            list.Add(1076202, _sculptedBy); // Sculpted by ~1_Name~
        }
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);

        if (from.Alive && _sculptedBy != null)
        {
            var house = BaseHouse.FindHouseAt(this);

            if (house?.IsCoOwner(from) == true || from.AccessLevel > AccessLevel.Counselor)
            {
                list.Add(new DemolishEntry(this));
            }
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (Plinth?.Deleted == false)
        {
            Plinth.Delete();
        }
    }

    protected override void OnMapChange(Map oldMap)
    {
        InvalidatePose();

        if (Plinth != null)
        {
            Plinth.Map = Map;
        }
    }

    protected override void OnLocationChange(Point3D oldLocation)
    {
        InvalidatePose();

        if (Plinth != null)
        {
            Plinth.Location = new Point3D(X, Y, Z - 5);
        }
    }

    public override bool CanBeRenamedBy(Mobile from) => false;

    public override bool CanBeDamaged() => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnRequestedAnimation(Mobile from)
    {
        from.NetState.SendStatueAnimation(Serial, 1, m_Animation, m_Frames);
    }

    public override void OnAosSingleClick(Mobile from)
    {
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _statueType = (StatueType)reader.ReadInt();
        _pose = (StatuePose)reader.ReadInt();
        _material = (StatueMaterial)reader.ReadInt();

        Timer.DelayCall(sculpter => _sculptedBy = sculpter?.RawName, reader.ReadEntity<Mobile>());
        SculptedOn = reader.ReadDateTime();

        Plinth = reader.ReadEntity<CharacterStatuePlinth>();
        IsRewardItem = reader.ReadBool();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        InvalidatePose();

        Frozen = true;

        if (_sculptedBy == null || Map == Map.Internal) // Remove preview statues
        {
            Delete();
        }
    }

    public void Sculpt(Mobile by)
    {
        if (by.ShowFameTitle && (by.Player || by.Body.IsHuman) &&
            by.Fame >= 10000)
        {
            _sculptedBy = $"{(by.Female ? "Lady" : "Lord")} {by.Name}";
        }
        else
        {
            _sculptedBy = by.RawName;
        }

        SculptedOn = Core.Now;

        InvalidateProperties();
    }

    public bool Demolish(Mobile by)
    {
        var deed = new CharacterStatueDeed(null);

        if (by.PlaceInBackpack(deed))
        {
            Delete();

            deed.Statue = this;
            deed.StatueType = _statueType;
            deed.IsRewardItem = IsRewardItem;

            Plinth?.Delete();

            return true;
        }

        by.SendLocalizedMessage(500720); // You don't have enough room in your backpack!
        deed.Delete();

        return false;
    }

    public void Restore(CharacterStatue from)
    {
        _material = from.Material;
        _pose = from.Pose;

        Direction = from.Direction;

        CloneBody(from);
        CloneClothes(from);

        InvalidateHues();
        InvalidatePose();
    }

    public void CloneBody(Mobile from)
    {
        Name = from.Name;
        Body = from.Body;
        Female = from.Female;
        HairItemID = from.HairItemID;
        FacialHairItemID = from.FacialHairItemID;
    }

    public void CloneClothes(Mobile from)
    {
        for (var i = Items.Count - 1; i >= 0; i--)
        {
            Items[i].Delete();
        }

        for (var i = from.Items.Count - 1; i >= 0; i--)
        {
            var item = from.Items[i];

            if (item.Layer != Layer.Backpack && item.Layer != Layer.Mount && item.Layer != Layer.Bank)
            {
                AddItem(CloneItem(item));
            }
        }
    }

    public Item CloneItem(Item item)
    {
        var cloned = new Item(item.ItemID)
        {
            Layer = item.Layer,
            Name = item.Name,
            Hue = item.Hue,
            Weight = item.Weight,
            Movable = false
        };

        return cloned;
    }

    public void InvalidateHues()
    {
        Hue = 0xB8F + (int)_statueType * 4 + (int)_material;

        HairHue = Hue;

        if (FacialHairItemID > 0)
        {
            FacialHairHue = Hue;
        }

        for (var i = Items.Count - 1; i >= 0; i--)
        {
            Items[i].Hue = Hue;
        }

        Plinth?.InvalidateHue();
    }

    public void InvalidatePose()
    {
        switch (_pose)
        {
            case StatuePose.Ready:
                m_Animation = 4;
                m_Frames = 0;
                break;
            case StatuePose.Casting:
                m_Animation = 16;
                m_Frames = 2;
                break;
            case StatuePose.Salute:
                m_Animation = 33;
                m_Frames = 1;
                break;
            case StatuePose.AllPraiseMe:
                m_Animation = 17;
                m_Frames = 4;
                break;
            case StatuePose.Fighting:
                m_Animation = 31;
                m_Frames = 5;
                break;
            case StatuePose.HandsOnHips:
                m_Animation = 6;
                m_Frames = 1;
                break;
        }

        if (Map == null)
        {
            return;
        }

        ProcessDelta();

        var eable = Map.GetClientsInRange(Location);
        Span<byte> animPacket = stackalloc byte[CharacterStatuePackets.StatueAnimationPacketLength].InitializePacket();

        foreach (var state in eable)
        {
            state.Mobile.ProcessDelta();
            CharacterStatuePackets.CreateStatueAnimation(animPacket, Serial, 1, m_Animation, m_Frames);
            state.Send(animPacket);
        }
    }

    private class DemolishEntry : ContextMenuEntry
    {
        private readonly CharacterStatue m_Statue;

        public DemolishEntry(CharacterStatue statue) : base(6275, 2) => m_Statue = statue;

        public override void OnClick()
        {
            if (!m_Statue.Deleted)
            {
                m_Statue.Demolish(Owner.From);
            }
        }
    }
}

[SerializationGenerator(1)]
public partial class CharacterStatueDeed : Item, IRewardItem
{
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CharacterStatue _statue;

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    public CharacterStatueDeed(CharacterStatue statue) : base(0x14F0)
    {
        Statue = statue;

        if (statue != null)
        {
            _statueType = statue.StatueType;
            _isRewardItem = statue.IsRewardItem;
        }

        LootType = LootType.Blessed;
        Weight = 1.0;
    }

    public override int LabelNumber
    {
        get
        {
            var t = _statueType;

            if (Statue != null)
            {
                t = Statue.StatueType;
            }

            return t switch
            {
                StatueType.Marble => 1076189,
                StatueType.Jade   => 1076188,
                StatueType.Bronze => 1076190,
                _                 => 1076173
            };
        }
    }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public StatueType StatueType
    {
        get => Statue?.StatueType ?? _statueType;
        set
        {
            _statueType = value;
            this.MarkDirty();
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1076222); // 6th Year Veteran Reward
        }

        if (Statue != null)
        {
            list.Add(1076231, Statue.Name); // Statue of ~1_Name~
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.Account is Account acct && from.AccessLevel == AccessLevel.Player)
        {
            var time = TimeSpan.FromDays(RewardSystem.RewardInterval.TotalDays * 6) - acct.AccountAge;

            if (time > TimeSpan.Zero)
            {
                from.SendLocalizedMessage(
                    1008126,
                    true,
                    Math.Ceiling(time.TotalDays / RewardSystem.RewardInterval.TotalDays)
                        .ToString()
                ); // Your account is not old enough to use this item. Months until you can use this item :
                return;
            }
        }

        if (IsChildOf(from.Backpack))
        {
            if (!from.IsBodyMod)
            {
                from.SendLocalizedMessage(1076194); // Select a place where you would like to put your statue.
                from.Target = new CharacterStatueTarget(this, StatueType);
            }
            else
            {
                from.SendLocalizedMessage(1073648); // You may only proceed while in your original state...
            }
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }

    public override void OnDelete()
    {
        base.OnDelete();

        Statue?.Delete();
    }

    private void Deserialize(IGenericReader reader, int version)
    {

        _statueType = (StatueType)reader.ReadInt();
        _statue = reader.ReadEntity<CharacterStatue>();
        _isRewardItem = reader.ReadBool();
    }
}

public class CharacterStatueTarget : Target
{
    private readonly Item _maker;
    private readonly StatueType _statueType;

    public CharacterStatueTarget(Item maker, StatueType type) : base(-1, true, TargetFlags.None)
    {
        _maker = maker;
        _statueType = type;
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        var p = targeted as IPoint3D;
        var map = from.Map;

        if (p == null || map == null || _maker?.Deleted != false)
        {
            return;
        }

        if (!_maker.IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        SpellHelper.GetSurfaceTop(ref p);
        BaseHouse house = null;
        var loc = new Point3D(p);

        if (targeted is Item item && !item.IsLockedDown && !item.IsSecure && item is not AddonComponent)
        {
            from.SendLocalizedMessage(1076191); // Statues can only be placed in houses.
            return;
        }

        if (from.IsBodyMod)
        {
            from.SendLocalizedMessage(1073648); // You may only proceed while in your original state...
            return;
        }

        var result = CouldFit(loc, map, from, ref house);

        if (result == AddonFitResult.Valid)
        {
            var statue = new CharacterStatue(from, _statueType);
            var plinth = new CharacterStatuePlinth(statue);

            house.Addons.Add(plinth);

            if (_maker is IRewardItem rewardItem)
            {
                statue.IsRewardItem = rewardItem.IsRewardItem;
            }

            statue.Plinth = plinth;
            plinth.MoveToWorld(loc, map);
            statue.InvalidatePose();

            /*
             * TODO: Previously the maker wasn't deleted until after statue
             * customization, leading to redeeding issues. Exact OSI behavior
             * needs looking into.
             */
            _maker.Delete();
            statue.Sculpt(from);

            from.CloseGump<CharacterStatueGump>();
            from.SendGump(new CharacterStatueGump(_maker, statue, from));
            return;
        }

        if (result == AddonFitResult.Blocked)
        {
            from.SendLocalizedMessage(500269); // You cannot build that there.
            return;
        }

        if (result == AddonFitResult.NotInHouse)
        {
            // Statues can only be placed in houses where you are the owner or co-owner.
            from.SendLocalizedMessage(1076192);
            return;
        }

        if (result == AddonFitResult.DoorTooClose)
        {
            from.SendLocalizedMessage(500271); // You cannot build near the door.
        }
    }

    public static AddonFitResult CouldFit(Point3D p, Map map, Mobile from, ref BaseHouse house)
    {
        if (!map.CanFit(p.X, p.Y, p.Z, 20, true))
        {
            return AddonFitResult.Blocked;
        }

        if (!BaseAddon.CheckHouse(from, p, map, 20, out house))
        {
            return AddonFitResult.NotInHouse;
        }

        return CheckDoors(p, 20, house);
    }

    public static AddonFitResult CheckDoors(Point3D p, int height, BaseHouse house)
    {
        var doors = house.Doors;

        for (var i = 0; i < doors.Count; i++)
        {
            var door = doors[i];

            var doorLoc = door.GetWorldLocation();
            var doorHeight = door.ItemData.CalcHeight;

            if (Utility.InRange(doorLoc, p, 1) &&
                (p.Z == doorLoc.Z || p.Z + height > doorLoc.Z && doorLoc.Z + doorHeight > p.Z))
            {
                return AddonFitResult.DoorTooClose;
            }
        }

        return AddonFitResult.Valid;
    }
}
