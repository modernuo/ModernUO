using System;
using ModernUO.CodeGeneratedEvents;
using ModernUO.Serialization;
using Server.Misc;
using Server.Mobiles;

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
        [SaveFlag(nameof(ShouldSerializeFertileDirt))]
        private bool _fertileDirt;

        private bool ShouldSerializeFertileDirt() => _fertileDirt;

        [SerializableField(1)]
        private DateTime _nextGrowth;

        [SerializableField(2, setter: "private")]
        [SaveFlag(nameof(ShouldSerializeGrowthIndicator))]
        private PlantGrowthIndicator _growthIndicator;

        private bool ShouldSerializeGrowthIndicator() => _growthIndicator != PlantGrowthIndicator.None;

        [SerializableField(13)]
        [SaveFlag(nameof(ShouldSerializePollinated))]
        private bool _pollinated;

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
            _nextGrowth = reader.ReadDateTime();

            _growthIndicator = (PlantGrowthIndicator)reader.ReadInt();

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

        [SerializableField(3, fieldChanged: nameof(OnWaterChanged), allowFieldChange: nameof(AllowWaterChange))]
        [SaveFlag(nameof(ShouldSerializeWater))]
        private int _water;

        private bool AllowWaterChange(ref int value)
        {
            value = Math.Clamp(value, 0, 4);
            return true;
        }

        private void OnWaterChanged(int oldValue, int newValue)
        {
            Plant.InvalidateProperties();
        }

        private bool ShouldSerializeWater() => _water != 0;

        [SerializableField(4, fieldChanged: nameof(OnHitsChanged), allowFieldChange: nameof(AllowHitsChange))]
        [SaveFlag(nameof(ShouldSerializeHits))]
        private int _hits;

        private bool AllowHitsChange(ref int value)
        {
            value = Math.Clamp(value, 0, MaxHits);
            return true;
        }

        private void OnHitsChanged(int oldValue, int newValue)
        {
            if (_hits == 0)
            {
                Plant.Die();
            }
            Plant.InvalidateProperties();
        }

        private bool ShouldSerializeHits() => _hits != 0;

        public int MaxHits => 10 + (int)Plant.PlantStatus * 2;

        public PlantHealth Health =>
            (_hits * 100 / MaxHits) switch
            {
                < 33  => PlantHealth.Dying,
                < 66  => PlantHealth.Wilted,
                < 100 => PlantHealth.Healthy,
                _     => PlantHealth.Vibrant
            };

        [SerializableField(5, allowFieldChange: nameof(AllowInfestationChange))]
        [SaveFlag(nameof(ShouldSerializeInfestation))]
        private int _infestation;

        private bool AllowInfestationChange(ref int value)
        {
            value = Math.Clamp(value, 0, 2);
            return true;
        }

        private bool ShouldSerializeInfestation() => _infestation != 0;

        [SerializableField(6, allowFieldChange: nameof(AllowFungusChange))]
        [SaveFlag(nameof(ShouldSerializeFungus))]
        private int _fungus;

        private bool AllowFungusChange(ref int value)
        {
            value = Math.Clamp(value, 0, 2);
            return true;
        }

        private bool ShouldSerializeFungus() => _fungus != 0;

        [SerializableField(7, allowFieldChange: nameof(AllowPoisonChange))]
        [SaveFlag(nameof(ShouldSerializePoison))]
        private int _poison;

        private bool AllowPoisonChange(ref int value)
        {
            value = Math.Clamp(value, 0, 2);
            return true;
        }

        private bool ShouldSerializePoison() => _poison != 0;

        [SerializableField(8, allowFieldChange: nameof(AllowDiseaseChange))]
        [SaveFlag(nameof(ShouldSerializeDisease))]
        private int _disease;

        private bool AllowDiseaseChange(ref int value)
        {
            value = Math.Clamp(value, 0, 2);
            return true;
        }

        private bool ShouldSerializeDisease() => _disease != 0;

        public bool IsFullPoisonPotion => _poisonPotion >= 2;

        [SerializableField(9, allowFieldChange: nameof(AllowPoisonPotionChange))]
        [SaveFlag(nameof(ShouldSerializePoisonPotion))]
        private int _poisonPotion;

        private bool AllowPoisonPotionChange(ref int value)
        {
            value = Math.Clamp(value, 0, 2);
            return true;
        }

        private bool ShouldSerializePoisonPotion() => _poisonPotion != 0;

        public bool IsFullCurePotion => _curePotion >= 2;

        [SerializableField(10, allowFieldChange: nameof(AllowCurePotionChange))]
        [SaveFlag(nameof(ShouldSerializeCurePotion))]
        private int _curePotion;

        private bool AllowCurePotionChange(ref int value)
        {
            value = Math.Clamp(value, 0, 2);
            return true;
        }

        private bool ShouldSerializeCurePotion() => _curePotion != 0;

        public bool IsFullHealPotion => _healPotion >= 2;

        [SerializableField(11, allowFieldChange: nameof(AllowHealPotionChange))]
        [SaveFlag(nameof(ShouldSerializeHealPotion))]
        private int _healPotion;

        private bool AllowHealPotionChange(ref int value)
        {
            value = Math.Clamp(value, 0, 2);
            return true;
        }

        private bool ShouldSerializeHealPotion() => _healPotion != 0;

        public bool IsFullStrengthPotion => _strengthPotion >= 2;

        [SerializableField(12, allowFieldChange: nameof(AllowStrengthPotionChange))]
        [SaveFlag(nameof(ShouldSerializeStrengthPotion))]
        private int _strengthPotion;

        private bool AllowStrengthPotionChange(ref int value)
        {
            value = Math.Clamp(value, 0, 2);
            return true;
        }

        private bool ShouldSerializeStrengthPotion() => _strengthPotion != 0;

        public bool HasMaladies => Infestation > 0 || Fungus > 0 || Poison > 0 || Disease > 0 || Water != 2;

        public bool PollenProducing => Plant.IsCrossable && Plant.PlantStatus >= PlantStatus.FullGrownPlant;

        [SerializableProperty(14)]
        [SaveFlag(nameof(ShouldSerializeSeedType))]
        public PlantType SeedType
        {
            get => Pollinated ? _seedType : Plant.PlantType;
            set
            {
                _seedType = value;
                MarkDirty();
            }
        }

        private bool ShouldSerializeSeedType() => _pollinated;

        [SerializableProperty(15)]
        [SaveFlag(nameof(ShouldSerializeSeedHue))]
        public PlantHue SeedHue
        {
            get => Pollinated ? _seedHue : Plant.PlantHue;
            set
            {
                _seedHue = value;
                MarkDirty();
            }
        }

        private bool ShouldSerializeSeedHue() => _pollinated;

        [SerializableField(16, allowFieldChange: nameof(AllowAvailableSeedsChange))]
        [SaveFlag(nameof(ShouldSerializeAvailableSeeds))]
        private int _availableSeeds;

        private bool AllowAvailableSeedsChange(ref int value)
        {
            value = Math.Max(value, 0);
            return true;
        }

        private bool ShouldSerializeAvailableSeeds() => _availableSeeds != 0;

        [SerializableField(17, allowFieldChange: nameof(AllowLeftSeedsChange))]
        [SaveFlag(nameof(ShouldSerializeLeftSeeds), nameof(LeftSeedsDefaultValue))]
        private int _leftSeeds;

        private bool AllowLeftSeedsChange(ref int value)
        {
            value = Math.Max(value, 0);
            return true;
        }

        private bool ShouldSerializeLeftSeeds() => _leftSeeds != 8;

        private int LeftSeedsDefaultValue() => 8;

        [SerializableField(18, allowFieldChange: nameof(AllowAvailableResourcesChange))]
        [SaveFlag(nameof(ShouldSerializeAvailableResources))]
        private int _availableResources;

        private bool AllowAvailableResourcesChange(ref int value)
        {
            value = Math.Max(value, 0);
            return true;
        }

        private bool ShouldSerializeAvailableResources() => _availableResources != 0;

        [SerializableField(19, allowFieldChange: nameof(AllowLeftResourcesChange))]
        [SaveFlag(nameof(ShouldSerializeLeftResources), nameof(LeftResourcesDefaultValue))]
        private int _leftResources;

        private bool AllowLeftResourcesChange(ref int value)
        {
            value = Math.Max(value, 0);
            return true;
        }

        private bool ShouldSerializeLeftResources() => _leftResources != 8;

        private int LeftResourcesDefaultValue() => 8;

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

        public void OnAfterDuped(Item newItem)
        {
            if (newItem is not PlantItem plant)
            {
                return;
            }

            // Copy all properties from this.PlantSystem to plantItem.PlantSystem
            var plantSystem = plant.PlantSystem;
            plantSystem.Water = Water;
            plantSystem.Hits = Hits;
            plantSystem.Infestation = Infestation;
            plantSystem.Fungus = Fungus;
            plantSystem.PoisonPotion = PoisonPotion;
            plantSystem.Disease = Disease;
            plantSystem.PoisonPotion = PoisonPotion;
            plantSystem.CurePotion = CurePotion;
            plantSystem.HealPotion = HealPotion;
            plantSystem.StrengthPotion = StrengthPotion;

            plantSystem.Pollinated = Pollinated;

            // Do not use getter since it has computed logic
            plantSystem.SeedType = _seedType;
            plantSystem.SeedHue = _seedHue;

            plantSystem.AvailableSeeds = AvailableSeeds;
            plantSystem.LeftSeeds = LeftSeeds;
            plantSystem.AvailableResources = AvailableResources;
            plantSystem.LeftResources = LeftResources;
            plantSystem.FertileDirt = FertileDirt;
            plantSystem.NextGrowth = NextGrowth;
            plantSystem.GrowthIndicator = GrowthIndicator;
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
        }

        [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
        public static void OnLogin(PlayerMobile from)
        {
            var cont = from.Backpack;
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
