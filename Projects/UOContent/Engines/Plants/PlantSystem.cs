using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Misc;

namespace Server.Engines.Plants
{
    public enum PlantHealth
    {
        Dying,
        Wilted,
        Healthy,
        Vibrant
    }

    public enum PlantGrowthIndicator
    {
        None,
        InvalidLocation,
        NotHealthy,
        Delay,
        Grown,
        DoubleGrown
    }

    [SerializationGenerator(3, false)]
    public partial class PlantSystem
    {
        public static readonly TimeSpan CheckDelay = TimeSpan.FromHours(23.0);

        [DirtyTrackingEntity]
        private PlantItem _plant;

        [SerializableField(0)]
        private bool _fertileDirt;

        [SerializableFieldSaveFlag(0)]
        private bool ShouldSerializeFertileDirt() => _fertileDirt;

        [SerializableField(1)]
        private DateTime _nextGrowth;

        [SerializableField(2, setter: "private")]
        private PlantGrowthIndicator _growthIndicator;

        [SerializableFieldSaveFlag(2)]
        private bool ShouldSerializeGrowthIndicator() => _growthIndicator != PlantGrowthIndicator.None;

        [SerializableField(13)]
        private bool _pollinated;

        [SerializableFieldSaveFlag(13)]
        private bool ShouldSerializePollinated() => _pollinated;

        public PlantSystem(PlantItem plant)
        {
            _plant = plant;

            _nextGrowth = Core.Now + CheckDelay;
            _growthIndicator = PlantGrowthIndicator.None;
            _hits = MaxHits;
            _leftSeeds = 8;
            _leftResources = 8;
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            _fertileDirt = reader.ReadBool();
            NextGrowth = reader.ReadDateTime();

            GrowthIndicator = (PlantGrowthIndicator)reader.ReadInt();

            _water = reader.ReadInt();

            _hits = reader.ReadInt();
            _infestation = reader.ReadInt();
            _fungus = reader.ReadInt();
            _poison = reader.ReadInt();
            _disease = reader.ReadInt();
            _poisonPotion = reader.ReadInt();
            _curePotion = reader.ReadInt();
            _healPotion = reader.ReadInt();
            _strengthPotion = reader.ReadInt();

            Pollinated = reader.ReadBool();
            _seedType = (PlantType)reader.ReadInt();
            _seedHue = (PlantHue)reader.ReadInt();
            _availableSeeds = reader.ReadInt();
            _leftSeeds = reader.ReadInt();

            _availableResources = reader.ReadInt();
            _leftResources = reader.ReadInt();
        }

        public PlantItem Plant => _plant;

        public bool IsFullWater => _water >= 4;

        [SerializableProperty(3)]
        public int Water
        {
            get => _water;
            set
            {
                _water = Math.Clamp(value, 0, 4);
                Plant.InvalidateProperties();
                MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(3)]
        private bool ShouldSerializeWater() => _water != 0;

        [SerializableProperty(4)]
        public int Hits
        {
            get => _hits;
            set
            {
                if (_hits == value)
                {
                    return;
                }

                _hits = Math.Clamp(value, 0, MaxHits);

                if (_hits == 0)
                {
                    Plant.Die();
                }

                Plant.InvalidateProperties();
                MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(4)]
        private bool ShouldSerializeHits() => _hits != 0;

        public int MaxHits => 10 + (int)Plant.PlantStatus * 2;

        public PlantHealth Health
        {
            get => (_hits * 100 / MaxHits) switch
            {
                < 33  => PlantHealth.Dying,
                < 66  => PlantHealth.Wilted,
                < 100 => PlantHealth.Healthy,
                _     => PlantHealth.Vibrant
            };
        }

        [SerializableProperty(5)]
        public int Infestation
        {
            get => _infestation;
            set
            {
                _infestation = Math.Clamp(value, 0, 2);
                MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(5)]
        private bool ShouldSerializeInfestation() => _infestation != 0;

        [SerializableProperty(6)]
        public int Fungus
        {
            get => _fungus;
            set
            {
                _fungus = Math.Clamp(value, 0, 2);
                MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(6)]
        private bool ShouldSerializeFungus() => _fungus != 0;

        [SerializableProperty(7)]
        public int Poison
        {
            get => _poison;
            set
            {
                _poison = Math.Clamp(value, 0, 2);
                MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(7)]
        private bool ShouldSerializePoison() => _poison != 0;

        [SerializableProperty(8)]
        public int Disease
        {
            get => _disease;
            set
            {
                _disease = Math.Clamp(value, 0, 2);
                MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(8)]
        private bool ShouldSerializeDisease() => _disease != 0;

        public bool IsFullPoisonPotion => _poisonPotion >= 2;

        [SerializableProperty(9)]
        public int PoisonPotion
        {
            get => _poisonPotion;
            set
            {
                _poisonPotion = Math.Clamp(value, 0, 2);
                MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(9)]
        private bool ShouldSerializePoisonPotion() => _poisonPotion != 0;

        public bool IsFullCurePotion => _curePotion >= 2;

        [SerializableProperty(10)]
        public int CurePotion
        {
            get => _curePotion;
            set
            {
                _curePotion = Math.Clamp(value, 0, 2);
                MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(10)]
        private bool ShouldSerializeCurePotion() => _curePotion != 0;

        public bool IsFullHealPotion => _healPotion >= 2;

        [SerializableProperty(11)]
        public int HealPotion
        {
            get => _healPotion;
            set
            {
                _healPotion = Math.Clamp(value, 0, 2);
                MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(11)]
        private bool ShouldSerializeHealPotion() => _healPotion != 0;

        public bool IsFullStrengthPotion => _strengthPotion >= 2;

        [SerializableProperty(12)]
        public int StrengthPotion
        {
            get => _strengthPotion;
            set
            {
                _strengthPotion = Math.Clamp(value, 0, 2);
                MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(12)]
        private bool ShouldSerializeStrengthPotion() => _strengthPotion != 0;

        public bool HasMaladies => Infestation > 0 || Fungus > 0 || Poison > 0 || Disease > 0 || Water != 2;

        public bool PollenProducing => Plant.IsCrossable && Plant.PlantStatus >= PlantStatus.FullGrownPlant;

        [SerializableProperty(14)]
        public PlantType SeedType
        {
            get => Pollinated ? _seedType : Plant.PlantType;
            set
            {
                _seedType = value;
                MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(14)]
        private bool ShouldSerializeSeedType() => _pollinated;

        [SerializableProperty(15)]
        public PlantHue SeedHue
        {
            get => Pollinated ? _seedHue : Plant.PlantHue;
            set
            {
                _seedHue = value;
                MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(15)]
        private bool ShouldSerializeSeedHue() => _pollinated;

        [SerializableProperty(16)]
        public int AvailableSeeds
        {
            get => _availableSeeds;
            set => _availableSeeds = Math.Max(value, 0);
        }

        [SerializableFieldSaveFlag(16)]
        private bool ShouldSerializeAvailableSeeds() => _availableSeeds != 0;

        [SerializableProperty(17)]
        public int LeftSeeds
        {
            get => _leftSeeds;
            set => _leftSeeds = Math.Max(value, 0);
        }

        [SerializableFieldSaveFlag(17)]
        private bool ShouldSerializeLeftSeeds() => _leftSeeds != 0;

        [SerializableProperty(18)]
        public int AvailableResources
        {
            get => _availableResources;
            set => _availableResources = Math.Max(value, 0);
        }

        [SerializableFieldSaveFlag(18)]
        private bool ShouldSerializeAvailableResources() => _availableResources != 0;

        [SerializableProperty(19)]
        public int LeftResources
        {
            get => _leftResources;
            set => _leftResources = Math.Max(value, 0);
        }

        [SerializableFieldSaveFlag(19)]
        private bool ShouldSerializeLeftResources() => _leftResources != 0;

        public void Reset(bool potions)
        {
            NextGrowth = Core.Now + CheckDelay;
            GrowthIndicator = PlantGrowthIndicator.None;

            Hits = MaxHits;
            Infestation = 0;
            Fungus = 0;
            Poison = 0;
            Disease = 0;

            if (potions)
            {
                PoisonPotion = 0;
                CurePotion = 0;
                HealPotion = 0;
                StrengthPotion = 0;
            }

            Pollinated = false;
            AvailableSeeds = 0;
            LeftSeeds = 8;

            AvailableResources = 0;
            LeftResources = 8;
        }

        public int GetLocalizedDirtStatus() =>
            Water switch
            {
                <= 1 => 1060826, // hard
                <= 2 => 1060827, // soft
                <= 3 => 1060828, // squishy
                _    => 1060829  // sappy wet
            };

        public int GetLocalizedHealth()
        {
            return Health switch
            {
                PlantHealth.Dying   => 1060825, // dying
                PlantHealth.Wilted  => 1060824, // wilted
                PlantHealth.Healthy => 1060823, // healthy
                _                   => 1060822  // vibrant
            };
        }

        public static void Configure()
        {
            EventSink.WorldLoad += EventSink_WorldLoad;

            if (!AutoRestart.Enabled)
            {
                EventSink.WorldSave += EventSink_WorldSave;
            }

            EventSink.Login += EventSink_Login;
        }

        private static void EventSink_Login(Mobile from)
        {
            Container cont = from.Backpack;
            if (cont != null)
            {
                foreach (var plant in cont.FindItemsByType<PlantItem>())
                {
                    if (plant.IsGrowable)
                    {
                        plant.PlantSystem.DoGrowthCheck();
                    }
                }
            }

            cont = from.FindBankNoCreate();
            if (cont != null)
            {
                foreach (var plant in cont.FindItemsByType<PlantItem>())
                {
                    if (plant.IsGrowable)
                    {
                        plant.PlantSystem.DoGrowthCheck();
                    }
                }
            }
        }

        public static void GrowAll()
        {
            var plants = PlantItem.Plants;
            var now = Core.Now;

            for (var i = plants.Count - 1; i >= 0; --i)
            {
                var plant = plants[i];

                if (plant.IsGrowable && plant.RootParent is not Mobile && now >= plant.PlantSystem.NextGrowth)
                {
                    plant.PlantSystem.DoGrowthCheck();
                }
            }
        }

        private static void EventSink_WorldLoad()
        {
            GrowAll();
        }

        private static void EventSink_WorldSave()
        {
            GrowAll();
        }

        public void DoGrowthCheck()
        {
            if (!Plant.IsGrowable)
            {
                return;
            }

            var now = Core.Now;

            if (now < NextGrowth)
            {
                GrowthIndicator = PlantGrowthIndicator.Delay;
                return;
            }

            NextGrowth = now + CheckDelay;

            if (!Plant.ValidGrowthLocation)
            {
                GrowthIndicator = PlantGrowthIndicator.InvalidLocation;
                return;
            }

            if (Plant.PlantStatus == PlantStatus.BowlOfDirt)
            {
                if (Water > 2 || Utility.RandomDouble() < 0.9)
                {
                    Water--;
                }

                return;
            }

            ApplyBeneficialEffects();

            if (!ApplyMaladiesEffects()) // Dead
            {
                return;
            }

            Grow();

            UpdateMaladies();
        }

        private void ApplyBeneficialEffects()
        {
            if (PoisonPotion >= Infestation)
            {
                PoisonPotion -= Infestation;
                Infestation = 0;
            }
            else
            {
                Infestation -= PoisonPotion;
                PoisonPotion = 0;
            }

            if (CurePotion >= Fungus)
            {
                CurePotion -= Fungus;
                Fungus = 0;
            }
            else
            {
                Fungus -= CurePotion;
                CurePotion = 0;
            }

            if (HealPotion >= Poison)
            {
                HealPotion -= Poison;
                Poison = 0;
            }
            else
            {
                Poison -= HealPotion;
                HealPotion = 0;
            }

            if (HealPotion >= Disease)
            {
                HealPotion -= Disease;
                Disease = 0;
            }
            else
            {
                Disease -= HealPotion;
                HealPotion = 0;
            }

            if (!HasMaladies)
            {
                if (HealPotion > 0)
                {
                    Hits += HealPotion * 7;
                }
                else
                {
                    Hits += 2;
                }
            }

            HealPotion = 0;
        }

        private bool ApplyMaladiesEffects()
        {
            var damage = 0;

            if (Infestation > 0)
            {
                damage += Infestation * Utility.RandomMinMax(3, 6);
            }

            if (Fungus > 0)
            {
                damage += Fungus * Utility.RandomMinMax(3, 6);
            }

            if (Poison > 0)
            {
                damage += Poison * Utility.RandomMinMax(3, 6);
            }

            if (Disease > 0)
            {
                damage += Disease * Utility.RandomMinMax(3, 6);
            }

            if (Water > 2)
            {
                damage += (Water - 2) * Utility.RandomMinMax(3, 6);
            }
            else if (Water < 2)
            {
                damage += (2 - Water) * Utility.RandomMinMax(3, 6);
            }

            Hits -= damage;

            return Plant.IsGrowable && Plant.PlantStatus != PlantStatus.BowlOfDirt;
        }

        private void Grow()
        {
            if (Health < PlantHealth.Healthy)
            {
                GrowthIndicator = PlantGrowthIndicator.NotHealthy;
            }
            else if (FertileDirt && Plant.PlantStatus <= PlantStatus.Stage5 && Utility.RandomDouble() < 0.1)
            {
                var curStage = (int)Plant.PlantStatus;
                Plant.PlantStatus = (PlantStatus)(curStage + 2);

                GrowthIndicator = PlantGrowthIndicator.DoubleGrown;
            }
            else if (Plant.PlantStatus < PlantStatus.Stage9)
            {
                var curStage = (int)Plant.PlantStatus;
                Plant.PlantStatus = (PlantStatus)(curStage + 1);

                GrowthIndicator = PlantGrowthIndicator.Grown;
            }
            else
            {
                if (Pollinated && LeftSeeds > 0 && Plant.Reproduces)
                {
                    LeftSeeds--;
                    AvailableSeeds++;
                }

                if (LeftResources > 0 && PlantResourceInfo.GetInfo(Plant.PlantType, Plant.PlantHue) != null)
                {
                    LeftResources--;
                    AvailableResources++;
                }

                GrowthIndicator = PlantGrowthIndicator.Grown;
            }

            if (Plant.PlantStatus >= PlantStatus.Stage9 && !Pollinated)
            {
                Pollinated = true;
                SeedType = Plant.PlantType;
                SeedHue = Plant.PlantHue;
            }
        }

        private void UpdateMaladies()
        {
            var infestationChance = 0.30 - StrengthPotion * 0.075 + (Water - 2) * 0.10;

            var typeInfo = PlantTypeInfo.GetInfo(Plant.PlantType);
            if (typeInfo.Flowery)
            {
                infestationChance += 0.10;
            }

            if (PlantHueInfo.IsBright(Plant.PlantHue))
            {
                infestationChance += 0.10;
            }

            if (Utility.RandomDouble() < infestationChance)
            {
                Infestation++;
            }

            var fungusChance = 0.15 - StrengthPotion * 0.075 + (Water - 2) * 0.10;

            if (Utility.RandomDouble() < fungusChance)
            {
                Fungus++;
            }

            if (Water > 2 || Utility.RandomDouble() < 0.9)
            {
                Water--;
            }

            if (PoisonPotion > 0)
            {
                Poison += PoisonPotion;
                PoisonPotion = 0;
            }

            if (CurePotion > 0)
            {
                Disease += CurePotion;
                CurePotion = 0;
            }

            StrengthPotion = 0;
        }
    }
}
