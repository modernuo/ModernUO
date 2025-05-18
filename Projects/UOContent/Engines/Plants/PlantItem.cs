using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Network;

namespace Server.Engines.Plants;

public enum PlantStatus
{
    BowlOfDirt = 0,
    Seed = 1,
    Sapling = 2,
    Plant = 4,
    FullGrownPlant = 7,
    DecorativePlant = 10,
    DeadTwigs = 11,

    Stage1 = 1,
    Stage2 = 2,
    Stage3 = 3,
    Stage4 = 4,
    Stage5 = 5,
    Stage6 = 6,
    Stage7 = 7,
    Stage8 = 8,
    Stage9 = 9
}

[SerializationGenerator(3, false)]
public partial class PlantItem : Item, ISecurable
{
    public static List<PlantItem> Plants { get; } = [];

    [SerializedIgnoreDupe]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [SerializableFieldSaveFlag(0)]
    private bool ShouldSerializeSecureLevel() => (int)_level != 0;

    [SerializedIgnoreDupe]
    [SerializableField(5, setter: "private")]
    private PlantSystem _plantSystem;

    [SerializableFieldSaveFlag(5)]
    private bool ShouldSerializePlantSystem() => _plantStatus < PlantStatus.DecorativePlant;

    // For clients older than 7.0.12.0
    private ObjectPropertyList _oldClientPropertyList;

    [Constructible]
    public PlantItem(bool fertileDirt = false) : base(0x1602)
    {
        Weight = 1.0;

        _plantStatus = PlantStatus.BowlOfDirt;
        _plantSystem = new PlantSystem(this)
        {
            FertileDirt = fertileDirt
        };

        _level = SecureLevel.Owner;

        Plants.Add(this);
    }

    public ObjectPropertyList OldClientPropertyList
    {
        get
        {
            InitializePropertyList(_oldClientPropertyList ??= new ObjectPropertyList(this));
            return _oldClientPropertyList;
        }
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    [CommandProperty(AccessLevel.GameMaster)]
    [SerializableProperty(1)]
    public PlantStatus PlantStatus
    {
        get => _plantStatus;
        set
        {
            if (_plantStatus == value || value is < PlantStatus.BowlOfDirt or > PlantStatus.DeadTwigs)
            {
                return;
            }

            var ratio = PlantSystem != null ? (double)PlantSystem.Hits / PlantSystem.MaxHits : 1.0;

            _plantStatus = value;

            if (_plantStatus >= PlantStatus.DecorativePlant)
            {
                PlantSystem = null;
            }
            else
            {
                PlantSystem ??= new PlantSystem(this);

                var hits = (int)(PlantSystem.MaxHits * ratio);

                if (hits == 0 && _plantStatus > PlantStatus.BowlOfDirt)
                {
                    PlantSystem.Hits = 1;
                }
                else
                {
                    PlantSystem.Hits = hits;
                }
            }

            Update();
        }
    }

    [SerializableFieldSaveFlag(1)]
    private bool ShouldSerializePlantStatus() => _plantStatus != PlantStatus.BowlOfDirt;

    [SerializableProperty(2)]
    [CommandProperty(AccessLevel.GameMaster)]
    public PlantType PlantType
    {
        get => _plantType;
        set
        {
            _plantType = value;
            Update();
        }
    }

    [SerializableFieldSaveFlag(2)]
    private bool ShouldSerializePlantType() => (int)_plantType != 0;

    [SerializableProperty(3)]
    [CommandProperty(AccessLevel.GameMaster)]
    public PlantHue PlantHue
    {
        get => _plantHue;
        set
        {
            _plantHue = value;
            Update();
        }
    }

    [SerializableFieldSaveFlag(3)]
    private bool ShouldSerializePlantHue() => _plantHue != PlantHue.None;

    [SerializableProperty(4)]
    [CommandProperty(AccessLevel.GameMaster)]
    public bool ShowType
    {
        get => _showType;
        set
        {
            _showType = value;
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    [SerializableFieldSaveFlag(4)]
    private bool ShouldSerializeShowType() => _showType;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool ValidGrowthLocation
    {
        get
        {
            if (IsLockedDown && RootParent == null)
            {
                return true;
            }

            if (RootParent is not Mobile owner)
            {
                return false;
            }

            return IsChildOf(owner.Backpack) || IsChildOf(owner.FindBankNoCreate());
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsGrowable => _plantStatus >= PlantStatus.BowlOfDirt && _plantStatus <= PlantStatus.Stage9;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsCrossable => PlantHueInfo.IsCrossable(PlantHue) && PlantTypeInfo.IsCrossable(PlantType);

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Reproduces => PlantHueInfo.CanReproduce(PlantHue) && PlantTypeInfo.CanReproduce(PlantType);

    public override void OnAfterDuped(Item newItem)
    {
        PlantSystem.OnAfterDuped(newItem);
    }

    public override void OnSingleClick(Mobile from)
    {
        if (_plantStatus >= PlantStatus.DeadTwigs)
        {
            LabelTo(from, LabelNumber);
        }
        else if (_plantStatus >= PlantStatus.DecorativePlant)
        {
            LabelTo(from, 1061924); // a decorative plant
        }
        else if (_plantStatus >= PlantStatus.FullGrownPlant)
        {
            LabelTo(from, PlantTypeInfo.GetInfo(_plantType).Name);
        }
        else
        {
            LabelTo(from, 1029913); // plant bowl
        }
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);
        SetSecureLevelEntry.AddTo(from, this, ref list);
    }

    public int GetLocalizedPlantStatus()
    {
        return _plantStatus switch
        {
            >= PlantStatus.Plant   => 1060812, // plant
            >= PlantStatus.Sapling => 1023305, // sapling
            >= PlantStatus.Seed    => 1060810, // seed
            _                      => 1026951  // dirt
        };
    }

    public static int GetLocalizedContainerType() => 1150435;

    private void Update()
    {
        if (_plantStatus >= PlantStatus.DeadTwigs)
        {
            ItemID = 0x1B9D;
            Hue = PlantHueInfo.GetInfo(_plantHue).Hue;
        }
        else if (_plantStatus >= PlantStatus.FullGrownPlant)
        {
            ItemID = PlantTypeInfo.GetInfo(_plantType).ItemID;
            Hue = PlantHueInfo.GetInfo(_plantHue).Hue;
        }
        else if (_plantStatus >= PlantStatus.Plant)
        {
            ItemID = 0x1600;
            Hue = 0;
        }
        else
        {
            ItemID = 0x1602;
            Hue = 0;
        }

        InvalidateProperties();
        this.MarkDirty();
    }

    private void InitializePropertyList(ObjectPropertyList list)
    {
        GetProperties(list);
        AppendChildProperties(list);
        list.Terminate();
    }

    // Overridden to support new and old client localization
    public override void SendOPLPacketTo(NetState ns)
    {
        if (!ObjectPropertyList.Enabled)
        {
            return;
        }

        if (ns.Version < ClientVersion.Version70120)
        {
            ns.SendOPLInfo(Serial, OldClientPropertyList.Hash);
            return;
        }

        ns.SendOPLInfo(this);
    }

    public override void SendPropertiesTo(NetState ns)
    {
        if (ns?.Version < ClientVersion.Version70120)
        {
            ns?.Send(OldClientPropertyList.Buffer);
            return;
        }

        ns?.Send(PropertyList.Buffer);
    }

    public override void ClearProperties()
    {
        base.ClearProperties();
        _oldClientPropertyList = null;
    }

    public override void InvalidateProperties()
    {
        base.InvalidateProperties();

        if (!ObjectPropertyList.Enabled)
        {
            return;
        }

        if (Map != null && Map != Map.Internal && !World.Loading)
        {
            int? oldHash;
            int newHash;
            if (_oldClientPropertyList != null)
            {
                oldHash = _oldClientPropertyList.Hash;
                _oldClientPropertyList.Reset();
                InitializePropertyList(_oldClientPropertyList);
                newHash = _oldClientPropertyList.Hash;
            }
            else
            {
                oldHash = null;
                newHash = OldClientPropertyList.Hash;
            }

            if (oldHash != newHash)
            {
                Delta(ItemDelta.Properties);
            }
        }
        else
        {
            ClearProperties();
        }
    }

    public override void OnAosSingleClick(Mobile from)
    {
        var ns = from?.NetState;

        if (ns == null)
        {
            return;
        }

        var opl = ns.Version < ClientVersion.Version70120 ? OldClientPropertyList : PropertyList;

        if (opl.Header > 0)
        {
            from.NetState.SendMessageLocalized(
                Serial,
                ItemID,
                MessageType.Label,
                0x3B2,
                3,
                opl.Header,
                Name,
                opl.HeaderArgs
            );
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        if (_plantStatus >= PlantStatus.DeadTwigs)
        {
            base.GetProperties(list);
            return;
        }

        var typeInfo = PlantTypeInfo.GetInfo(_plantType);
        var hueInfo = PlantHueInfo.GetInfo(_plantHue);

        if (_plantStatus >= PlantStatus.DecorativePlant)
        {
            list.Add(typeInfo.GetPlantLabelDecorative(hueInfo), $"{hueInfo.Name:#}\t{typeInfo.Name:#}");
            return;
        }

        var container = GetLocalizedContainerType();
        var dirt = PlantSystem.GetLocalizedDirtStatus();
        var health = PlantSystem.GetLocalizedHealth();
        var plantStatus = GetLocalizedPlantStatus();

        if (_plantStatus < PlantStatus.Seed)
        {
            // Clients above 7.0.12.0 use the regular PropertyList
            if (list != _oldClientPropertyList)
            {
                // a ~1_val~ of ~2_val~ dirt
                list.Add(1060830, $"{container:#}\t{dirt:#}");
            }
            else
            {
                // a ~1_val~ of ~2_val~ dirt
                list.Add(1060830, $"{dirt:#}");
            }

            return;
        }

        if (_plantStatus >= PlantStatus.FullGrownPlant)
        {
            list.Add(
                typeInfo.GetPlantLabelFullGrown(hueInfo),
                $"{health:#}\t{hueInfo.Name:#}\t{typeInfo.Name:#}"
            );
            return;
        }

        if (_showType)
        {
            var plantNumber = _plantStatus == PlantStatus.Plant
                ? typeInfo.GetPlantLabelPlant(hueInfo)
                : typeInfo.GetPlantLabelSeed(hueInfo);

            if (list != _oldClientPropertyList)
            {
                list.Add(
                    plantNumber,
                    $"{container:#}\t{dirt:#}\t{health:#}\t{hueInfo.Name:#}\t{typeInfo.Name:#}\t{plantStatus:#}"
                );
            }
            else
            {
                list.Add(
                    plantNumber,
                    $"{dirt:#}\t{health:#}\t{hueInfo.Name:#}\t{typeInfo.Name:#}\t{plantStatus:#}"
                );
            }
        }
        else
        {
            var category = typeInfo.PlantCategory == PlantCategory.Default ? hueInfo.Name : (int)typeInfo.PlantCategory;
            var plantNumber = hueInfo.IsBright() ? 1060832 : 1060831;
            if (list != _oldClientPropertyList)
            {
                list.Add(plantNumber, $"{container:#}\t{dirt:#}\t{health:#}\t{category:#}\t{plantStatus:#}");
            }
            else
            {
                list.Add(plantNumber, $"{dirt:#}\t{health:#}\t{category:#}\t{plantStatus:#}");
            }
        }
    }

    public bool IsUsableBy(Mobile from) =>
        IsChildOf(from.Backpack) || IsChildOf(from.FindBankNoCreate()) || IsLockedDown && IsAccessibleTo(from) ||
        RootParent is Item root && root.IsSecure && root.IsAccessibleTo(from);

    public override void OnDoubleClick(Mobile from)
    {
        if (_plantStatus >= PlantStatus.DecorativePlant)
        {
            return;
        }

        if (!IsChildOf(from))
        {
            var loc = GetWorldLocation();

            if (!from.InLOS(loc) || !from.InRange(loc, 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3E9, 1019045); // I can't reach that
                return;
            }
        }

        if (!IsUsableBy(from))
        {
            LabelTo(from, 1061856); // You must have the item in your backpack or locked down in order to use it.
            return;
        }

        from.SendGump(new MainPlantGump(this));
    }

    public void PlantSeed(Mobile from, Seed seed)
    {
        if (_plantStatus >= PlantStatus.FullGrownPlant)
        {
            LabelTo(from, 1061919); // You must use a seed on some prepared soil!
        }
        else if (!IsUsableBy(from))
        {
            LabelTo(from, 1061921); // The bowl of dirt must be in your pack, or you must lock it down.
        }
        else if (_plantStatus != PlantStatus.BowlOfDirt)
        {
            // This bowl of dirt already has a ~1_val~ in it!
            from.SendLocalizedMessage(1080389, $"#{GetLocalizedPlantStatus()}");
        }
        else if (PlantSystem.Water < 2)
        {
            LabelTo(from, 1061920); // The dirt needs to be softened first.
        }
        else
        {
            PlantType = seed.PlantType;
            PlantHue = seed.PlantHue;
            ShowType = seed.ShowType;

            seed.Consume();

            PlantStatus = PlantStatus.Seed;

            PlantSystem.Reset(false);

            LabelTo(from, 1061922); // You plant the seed in the bowl of dirt.
        }
    }

    public void Die()
    {
        if (_plantStatus >= PlantStatus.FullGrownPlant)
        {
            PlantStatus = PlantStatus.DeadTwigs;
        }
        else
        {
            PlantStatus = PlantStatus.BowlOfDirt;
            PlantSystem.Reset(true);
        }
    }

    public void Pour(Mobile from, Item item)
    {
        if (_plantStatus >= PlantStatus.DeadTwigs)
        {
            return;
        }

        if (_plantStatus == PlantStatus.DecorativePlant)
        {
            LabelTo(from, 1053049); // This is a decorative plant, it does not need watering!
            return;
        }

        if (!IsUsableBy(from))
        {
            LabelTo(from, 1061856); // You must have the item in your backpack or locked down in order to use it.
            return;
        }

        if (item is BaseBeverage beverage)
        {
            if (beverage.IsEmpty || !beverage.Pourable || beverage.Content != BeverageType.Water)
            {
                LabelTo(from, 1053069); // You can't use that on a plant!
                return;
            }

            if (!beverage.ValidateUse(from, true))
            {
                return;
            }

            beverage.Quantity--;
            PlantSystem.Water++;

            from.PlaySound(0x4E);
            LabelTo(from, 1061858); // You soften the dirt with water.
        }
        else if (item is BasePotion potion)
        {
            if (ApplyPotion(potion.PotionEffect, false, out var message))
            {
                potion.Consume();
                from.PlaySound(0x240);
                from.AddToBackpack(new Bottle());
            }

            LabelTo(from, message);
        }
        else if (item is PotionKeg keg)
        {
            if (keg.Held <= 0)
            {
                LabelTo(from, 1053069); // You can't use that on a plant!
                return;
            }

            if (ApplyPotion(keg.Type, false, out var message))
            {
                keg.Held--;
                from.PlaySound(0x240);
            }

            LabelTo(from, message);
        }
        else
        {
            LabelTo(from, 1053069); // You can't use that on a plant!
        }
    }

    public bool ApplyPotion(PotionEffect effect, bool testOnly, out int message)
    {
        if (_plantStatus >= PlantStatus.DecorativePlant)
        {
            message = 1053049; // This is a decorative plant, it does not need watering!
            return false;
        }

        if (_plantStatus == PlantStatus.BowlOfDirt)
        {
            message = 1053066; // You should only pour potions on a plant or seed!
            return false;
        }

        var full = false;

        if (effect is PotionEffect.PoisonGreater or PotionEffect.PoisonDeadly)
        {
            if (PlantSystem.IsFullPoisonPotion)
            {
                full = true;
            }
            else if (!testOnly)
            {
                PlantSystem.PoisonPotion++;
            }
        }
        else if (effect == PotionEffect.CureGreater)
        {
            if (PlantSystem.IsFullCurePotion)
            {
                full = true;
            }
            else if (!testOnly)
            {
                PlantSystem.CurePotion++;
            }
        }
        else if (effect == PotionEffect.HealGreater)
        {
            if (PlantSystem.IsFullHealPotion)
            {
                full = true;
            }
            else if (!testOnly)
            {
                PlantSystem.HealPotion++;
            }
        }
        else if (effect == PotionEffect.StrengthGreater)
        {
            if (PlantSystem.IsFullStrengthPotion)
            {
                full = true;
            }
            else if (!testOnly)
            {
                PlantSystem.StrengthPotion++;
            }
        }
        else if (effect is PotionEffect.PoisonLesser or PotionEffect.Poison or PotionEffect.CureLesser or PotionEffect.Cure or PotionEffect.HealLesser or PotionEffect.Heal or PotionEffect.Strength)
        {
            message = 1053068; // This potion is not powerful enough to use on a plant!
            return false;
        }
        else
        {
            message = 1053069; // You can't use that on a plant!
            return false;
        }

        if (full)
        {
            message = 1053065; // The plant is already soaked with this type of potion!
            return false;
        }

        message = 1053067; // You pour the potion over the plant.
        return true;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _level = (SecureLevel)reader.ReadInt();
        _plantStatus = (PlantStatus)reader.ReadInt();
        _plantType = (PlantType)reader.ReadInt();
        _plantHue = (PlantHue)reader.ReadInt();
        _showType = reader.ReadBool();

        if (_plantStatus < PlantStatus.DecorativePlant)
        {
            _plantSystem = new PlantSystem(this);
            _plantSystem.Deserialize(reader);
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Plants.Add(this);
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        Plants.Remove(this);
    }
}
