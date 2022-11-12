using System.Collections.Generic;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Network;

namespace Server.Engines.Plants
{
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

    public class PlantItem : Item, ISecurable
    {
        private PlantHue m_PlantHue;
        private PlantStatus m_PlantStatus;
        private PlantType m_PlantType;
        private bool m_ShowType;

        // For clients older than 7.0.12.0
        private ObjectPropertyList _oldClientPropertyList;

        [Constructible]
        public PlantItem(bool fertileDirt = false) : base(0x1602)
        {
            Weight = 1.0;

            m_PlantStatus = PlantStatus.BowlOfDirt;
            PlantSystem = new PlantSystem(this, fertileDirt);
            Level = SecureLevel.Owner;

            Plants.Add(this);
        }

        public PlantItem(Serial serial) : base(serial)
        {
        }

        public PlantSystem PlantSystem { get; private set; }

        public ObjectPropertyList OldClientPropertyList =>
            _oldClientPropertyList ??= InitializePropertyList(new ObjectPropertyList(this));

        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [CommandProperty(AccessLevel.GameMaster)]
        public PlantStatus PlantStatus
        {
            get => m_PlantStatus;
            set
            {
                if (m_PlantStatus == value || value is < PlantStatus.BowlOfDirt or > PlantStatus.DeadTwigs)
                {
                    return;
                }

                double ratio;
                if (PlantSystem != null)
                {
                    ratio = (double)PlantSystem.Hits / PlantSystem.MaxHits;
                }
                else
                {
                    ratio = 1.0;
                }

                m_PlantStatus = value;

                if (m_PlantStatus >= PlantStatus.DecorativePlant)
                {
                    PlantSystem = null;
                }
                else
                {
                    PlantSystem ??= new PlantSystem(this, false);

                    var hits = (int)(PlantSystem.MaxHits * ratio);

                    if (hits == 0 && m_PlantStatus > PlantStatus.BowlOfDirt)
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

        [CommandProperty(AccessLevel.GameMaster)]
        public PlantType PlantType
        {
            get => m_PlantType;
            set
            {
                m_PlantType = value;
                Update();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlantHue PlantHue
        {
            get => m_PlantHue;
            set
            {
                m_PlantHue = value;
                Update();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowType
        {
            get => m_ShowType;
            set
            {
                m_ShowType = value;
                InvalidateProperties();
            }
        }

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
        public bool IsGrowable => m_PlantStatus >= PlantStatus.BowlOfDirt && m_PlantStatus <= PlantStatus.Stage9;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsCrossable => PlantHueInfo.IsCrossable(PlantHue) && PlantTypeInfo.IsCrossable(PlantType);

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Reproduces => PlantHueInfo.CanReproduce(PlantHue) && PlantTypeInfo.CanReproduce(PlantType);

        public static List<PlantItem> Plants { get; } = new();

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level { get; set; }

        public override void OnSingleClick(Mobile from)
        {
            if (m_PlantStatus >= PlantStatus.DeadTwigs)
            {
                LabelTo(from, LabelNumber);
            }
            else if (m_PlantStatus >= PlantStatus.DecorativePlant)
            {
                LabelTo(from, 1061924); // a decorative plant
            }
            else if (m_PlantStatus >= PlantStatus.FullGrownPlant)
            {
                LabelTo(from, PlantTypeInfo.GetInfo(m_PlantType).Name);
            }
            else
            {
                LabelTo(from, 1029913); // plant bowl
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);
            SetSecureLevelEntry.AddTo(from, this, list);
        }

        public int GetLocalizedPlantStatus()
        {
            return m_PlantStatus switch
            {
                >= PlantStatus.Plant   => 1060812, // plant
                >= PlantStatus.Sapling => 1023305, // sapling
                >= PlantStatus.Seed    => 1060810, // seed
                _                      => 1026951 // dirt
            };
        }

        public int GetLocalizedContainerType() => 1150435;

        private void Update()
        {
            if (m_PlantStatus >= PlantStatus.DeadTwigs)
            {
                ItemID = 0x1B9D;
                Hue = PlantHueInfo.GetInfo(m_PlantHue).Hue;
            }
            else if (m_PlantStatus >= PlantStatus.FullGrownPlant)
            {
                ItemID = PlantTypeInfo.GetInfo(m_PlantType).ItemID;
                Hue = PlantHueInfo.GetInfo(m_PlantHue).Hue;
            }
            else if (m_PlantStatus >= PlantStatus.Plant)
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
        }

        private ObjectPropertyList InitializePropertyList(ObjectPropertyList list)
        {
            GetProperties(list);
            AppendChildProperties(list);
            list.Terminate();
            return list;
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
            if (m_PlantStatus >= PlantStatus.DeadTwigs)
            {
                base.GetProperties(list);
                return;
            }

            var typeInfo = PlantTypeInfo.GetInfo(m_PlantType);
            var hueInfo = PlantHueInfo.GetInfo(m_PlantHue);

            if (m_PlantStatus >= PlantStatus.DecorativePlant)
            {
                list.Add(typeInfo.GetPlantLabelDecorative(hueInfo), $"{hueInfo.Name:#}\t{typeInfo.Name:#}");
                return;
            }

            var container = GetLocalizedContainerType();
            var dirt = PlantSystem.GetLocalizedDirtStatus();
            var health = PlantSystem.GetLocalizedHealth();
            var plantStatus = GetLocalizedPlantStatus();

            if (m_PlantStatus < PlantStatus.Seed)
            {
                // Clients above 7.0.12.0 use the regular PropertyList
                if (list != OldClientPropertyList)
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

            if (m_PlantStatus >= PlantStatus.FullGrownPlant)
            {
                list.Add(
                    typeInfo.GetPlantLabelFullGrown(hueInfo),
                    $"{health:#}\t{hueInfo.Name:#}\t{typeInfo.Name:#}"
                );
                return;
            }

            if (m_ShowType)
            {
                var plantNumber = m_PlantStatus == PlantStatus.Plant
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
            if (m_PlantStatus >= PlantStatus.DecorativePlant)
            {
                return;
            }

            var loc = GetWorldLocation();

            if (!from.InLOS(loc) || !from.InRange(loc, 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3E9, 1019045); // I can't reach that.
                return;
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
            if (m_PlantStatus >= PlantStatus.FullGrownPlant)
            {
                LabelTo(from, 1061919); // You must use a seed on some prepared soil!
            }
            else if (!IsUsableBy(from))
            {
                LabelTo(from, 1061921); // The bowl of dirt must be in your pack, or you must lock it down.
            }
            else if (m_PlantStatus != PlantStatus.BowlOfDirt)
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
                m_PlantType = seed.PlantType;
                m_PlantHue = seed.PlantHue;
                m_ShowType = seed.ShowType;

                seed.Consume();

                PlantStatus = PlantStatus.Seed;

                PlantSystem.Reset(false);

                LabelTo(from, 1061922); // You plant the seed in the bowl of dirt.
            }
        }

        public void Die()
        {
            if (m_PlantStatus >= PlantStatus.FullGrownPlant)
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
            if (m_PlantStatus >= PlantStatus.DeadTwigs)
            {
                return;
            }

            if (m_PlantStatus == PlantStatus.DecorativePlant)
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
            if (m_PlantStatus >= PlantStatus.DecorativePlant)
            {
                message = 1053049; // This is a decorative plant, it does not need watering!
                return false;
            }

            if (m_PlantStatus == PlantStatus.BowlOfDirt)
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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(2); // version

            writer.Write((int)Level);

            writer.Write((int)m_PlantStatus);
            writer.Write((int)m_PlantType);
            writer.Write((int)m_PlantHue);
            writer.Write(m_ShowType);

            if (m_PlantStatus < PlantStatus.DecorativePlant)
            {
                PlantSystem.Save(writer);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 2:
                case 1:
                    {
                        Level = (SecureLevel)reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 1)
                        {
                            Level = SecureLevel.CoOwners;
                        }

                        m_PlantStatus = (PlantStatus)reader.ReadInt();
                        m_PlantType = (PlantType)reader.ReadInt();
                        m_PlantHue = (PlantHue)reader.ReadInt();
                        m_ShowType = reader.ReadBool();

                        if (m_PlantStatus < PlantStatus.DecorativePlant)
                        {
                            PlantSystem = new PlantSystem(this, reader);
                        }

                        if (version < 2 && PlantHueInfo.IsCrossable(m_PlantHue))
                        {
                            m_PlantHue |= PlantHue.Reproduces;
                        }

                        break;
                    }
            }

            Plants.Add(this);
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            Plants.Remove(this);
        }
    }
}
