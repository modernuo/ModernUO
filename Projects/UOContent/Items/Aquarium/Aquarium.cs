using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items
{
    [SerializationGenerator(4, false)]
    public partial class Aquarium : BaseAddonContainer
    {
        public static readonly TimeSpan EvaluationInterval = TimeSpan.FromDays(1);

        private static readonly Type[] m_Decorations =
        [
            typeof(FishBones),
            typeof(WaterloggedBoots),
            typeof(CaptainBlackheartsFishingPole),
            typeof(CraftysFishingHat),
            typeof(AquariumFishNet),
            typeof(AquariumMessage),
            typeof(IslandStatue),
            typeof(Shell),
            typeof(ToyBoat)
        ];

        private bool m_EvaluateDay;

        [SerializableField(0, setter: "private")]
        private Timer _evaluateTimer;

        [DeserializeTimerField(0)]
        private void DeserializeEvaluateTimer(TimeSpan delay)
        {
            _evaluateTimer = Timer.DelayCall(delay, EvaluationInterval, Evaluate);
        }

        [SerializableField(1, setter: "private")]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _liveCreatures;

        [InvalidateProperties]
        [SerializableField(2, setter: "private")]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _vacationLeft;

        [SerializedIgnoreDupe]
        [InvalidateProperties]
        [SerializableField(3, setter: "private")]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private AquariumState _food;

        [SerializedIgnoreDupe]
        [InvalidateProperties]
        [SerializableField(4, setter: "private")]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private AquariumState _water;

        [SerializedIgnoreDupe]
        [SerializableField(5, setter: "private")]
        private List<int> _events;

        [InvalidateProperties]
        [SerializableField(6)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private bool _rewardAvailable;

        public Aquarium(int itemID) : base(itemID)
        {
            Movable = false;

            if (itemID == 0x3060)
            {
                AddComponent(new AddonContainerComponent(0x3061), -1, 0, 0);
            }

            if (itemID == 0x3062)
            {
                AddComponent(new AddonContainerComponent(0x3063), 0, -1, 0);
            }

            MaxItems = 30;

            _food = new AquariumState(this);
            _water = new AquariumState(this);

            _food.State = (int)FoodState.Full;
            _water.State = (int)WaterState.Strong;

            _food.Maintain = Utility.RandomMinMax(1, 2);
            _food.Improve = _food.Maintain + Utility.RandomMinMax(1, 2);

            _water.Maintain = Utility.RandomMinMax(1, 3);

            _events = [];

            _evaluateTimer = Timer.DelayCall(EvaluationInterval, EvaluationInterval, Evaluate);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DeadCreatures
        {
            get
            {
                var dead = 0;

                for (var i = 0; i < Items.Count; i++)
                {
                    if (Items[i] is BaseFish)
                    {
                        var fish = (BaseFish)Items[i];

                        if (fish.Dead)
                        {
                            dead += 1;
                        }
                    }
                }

                return dead;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLiveCreatures
        {
            get
            {
                var state = _food.State == (int)FoodState.Overfed ? 1 : (int)FoodState.Full - _food.State;

                state += (int)WaterState.Strong - _water.State;

                state = (int)Math.Pow(state, 1.75);

                return MaxItems - state;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsFull => Items.Count >= MaxItems;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool OptimalState => _food.State == (int)FoodState.Full && _water.State == (int)WaterState.Strong;

        public override BaseAddonContainerDeed Deed => ItemID == 0x3062 ? new AquariumEastDeed() : new AquariumNorthDeed();

        public override double DefaultWeight => 10.0;

        private static int[] FishHues =
        [
            0x1C2, 0x1C3, 0x2A3, 0x47E, 0x51D
        ];

        public override void OnDelete()
        {
            _evaluateTimer.Stop();
            _evaluateTimer = null;
        }

        public override void OnDoubleClick(Mobile from)
        {
            ExamineAquarium(from);
        }

        public virtual bool HasAccess(Mobile from) =>
            from?.Deleted == false && (
                from.AccessLevel >= AccessLevel.GameMaster ||
                BaseHouse.FindHouseAt(this)?.IsCoOwner(from) == true);

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (!HasAccess(from))
            {
                from.SendLocalizedMessage(1073821); // You do not have access to that item for use with the aquarium.
                return false;
            }

            if (_vacationLeft > 0)
            {
                from.SendLocalizedMessage(1074427); // The aquarium is in vacation mode.
                return false;
            }

            var takeItem = true;

            if (dropped is FishBowl bowl)
            {
                if (bowl.Empty || !AddFish(from, bowl.Fish))
                {
                    return false;
                }

                bowl.InvalidateProperties();

                takeItem = false;
            }
            else if (dropped is BaseFish fish)
            {
                if (!AddFish(from, fish))
                {
                    return false;
                }
            }
            else if (dropped is VacationWafer)
            {
                _vacationLeft = VacationWafer.VacationDays;
                dropped.Delete();

                from.SendLocalizedMessage(
                    1074428,
                    _vacationLeft.ToString()
                ); // The aquarium will be in vacation mode for ~1_DAYS~ days
            }
            else if (dropped is AquariumFood)
            {
                _food.Added += 1;
                dropped.Delete();

                from.SendLocalizedMessage(1074259, "1"); // ~1_NUM~ unit(s) of food have been added to the aquarium.
            }
            else if (dropped is BaseBeverage beverage)
            {
                if (beverage.IsEmpty || !beverage.Pourable || beverage.Content != BeverageType.Water)
                {
                    from.SendLocalizedMessage(500840); // Can't pour that in there.
                    return false;
                }

                _water.Added += 1;
                beverage.Quantity -= 1;

                from.PlaySound(0x4E);
                from.SendLocalizedMessage(1074260, "1"); // ~1_NUM~ unit(s) of water have been added to the aquarium.

                takeItem = false;
            }
            else if (!AddDecoration(from, dropped))
            {
                takeItem = false;
            }

            from.CloseGump<AquariumGump>();

            InvalidateProperties();

            if (takeItem)
            {
                from.PlaySound(0x42);
            }

            return takeItem;
        }

        public override void DropItemsToGround()
        {
            var loc = GetWorldLocation();

            for (var i = Items.Count - 1; i >= 0; i--)
            {
                var item = Items[i];

                item.MoveToWorld(loc, Map);

                if (item is BaseFish fish && !fish.Dead)
                {
                    fish.StartTimer();
                }
            }
        }

        public override bool CheckItemUse(Mobile from, Item item)
        {
            if (item != this)
            {
                return false;
            }

            return base.CheckItemUse(from, item);
        }

        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            if (item != this)
            {
                reject = LRReason.CannotLift;
                return false;
            }

            return base.CheckLift(from, item, ref reject);
        }

        public override void OnSingleClick(Mobile from)
        {
            if (Deleted || !from.CanSee(this))
            {
                return;
            }

            base.OnSingleClick(from);

            if (_vacationLeft > 0)
            {
                LabelTo(from, 1074430, _vacationLeft.ToString()); // Vacation days left: ~1_DAYS
            }

            if (_events.Count > 0)
            {
                LabelTo(from, 1074426, _events.Count.ToString()); // ~1_NUM~ event(s) to view!
            }

            if (_rewardAvailable)
            {
                LabelTo(from, 1074362); // A reward is available!
            }

            LabelTo(from, 1074247, $"{LiveCreatures}\t{MaxLiveCreatures}"); // Live Creatures: ~1_NUM~ / ~2_MAX~

            if (DeadCreatures > 0)
            {
                LabelTo(from, 1074248, DeadCreatures.ToString()); // Dead Creatures: ~1_NUM~
            }

            var decorations = Items.Count - LiveCreatures - DeadCreatures;

            if (decorations > 0)
            {
                LabelTo(from, 1074249, decorations.ToString()); // Decorations: ~1_NUM~
            }

            LabelTo(from, 1074250, $"#{FoodNumber()}");  // Food state: ~1_STATE~
            LabelTo(from, 1074251, $"#{WaterNumber()}"); // Water state: ~1_STATE~

            if (_food.State == (int)FoodState.Dead)
            {
                LabelTo(from, 1074577, $"{_food.Added}\t{_food.Improve}"); // Food Added: ~1_CUR~ Needed: ~2_NEED~
            }
            else if (_food.State == (int)FoodState.Overfed)
            {
                LabelTo(from, 1074577, $"{_food.Added}\t{_food.Maintain}"); // Food Added: ~1_CUR~ Needed: ~2_NEED~
            }
            else
            {
                LabelTo(
                    from,
                    1074253, // Food Added: ~1_CUR~ Feed: ~2_NEED~ Improve: ~3_GROW~
                    $"{_food.Added}\t{_food.Maintain}\t{_food.Improve}"
                );
            }

            if (_water.State == (int)WaterState.Dead)
            {
                LabelTo(from, 1074578, $"{_water.Added}\t{_water.Improve}"); // Water Added: ~1_CUR~ Needed: ~2_NEED~
            }
            else if (_water.State == (int)WaterState.Strong)
            {
                LabelTo(from, 1074578, $"{_water.Added}\t{_water.Maintain}"); // Water Added: ~1_CUR~ Needed: ~2_NEED~
            }
            else
            {
                LabelTo(
                    from,
                    1074254,
                    $"{_water.Added}\t{_water.Maintain}\t{_water.Improve}"
                ); // Water Added: ~1_CUR~ Maintain: ~2_NEED~ Improve: ~3_GROW~
            }
        }

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);

            if (_vacationLeft > 0)
            {
                list.Add(1074430, $"{_vacationLeft}"); // Vacation days left: ~1_DAYS
            }

            if (_events.Count > 0)
            {
                list.Add(1074426, $"{_events.Count}"); // ~1_NUM~ event(s) to view!
            }

            if (_rewardAvailable)
            {
                list.Add(1074362); // A reward is available!
            }

            list.Add(1074247, $"{LiveCreatures}\t{MaxLiveCreatures}"); // Live Creatures: ~1_NUM~ / ~2_MAX~

            var dead = DeadCreatures;

            if (dead > 0)
            {
                list.Add(1074248, dead); // Dead Creatures: ~1_NUM~
            }

            var decorations = Items.Count - LiveCreatures - dead;

            if (decorations > 0)
            {
                list.Add(1074249, decorations); // Decorations: ~1_NUM~
            }

            list.AddLocalized(1074250, FoodNumber());  // Food state: ~1_STATE~
            list.AddLocalized(1074251, WaterNumber()); // Water state: ~1_STATE~

            if (_food.State == (int)FoodState.Dead)
            {
                list.Add(1074577, $"{_food.Added}\t{_food.Improve}"); // Food Added: ~1_CUR~ Needed: ~2_NEED~
            }
            else if (_food.State == (int)FoodState.Overfed)
            {
                list.Add(1074577, $"{_food.Added}\t{_food.Maintain}"); // Food Added: ~1_CUR~ Needed: ~2_NEED~
            }
            else
            {
                list.Add(
                    1074253, // Food Added: ~1_CUR~ Feed: ~2_NEED~ Improve: ~3_GROW~
                    $"{_food.Added}\t{_food.Maintain}\t{_food.Improve}"
                );
            }

            if (_water.State == (int)WaterState.Dead)
            {
                list.Add(1074578, $"{_water.Added}\t{_water.Improve}"); // Water Added: ~1_CUR~ Needed: ~2_NEED~
            }
            else if (_water.State == (int)WaterState.Strong)
            {
                list.Add(1074578, $"{_water.Added}\t{_water.Maintain}"); // Water Added: ~1_CUR~ Needed: ~2_NEED~
            }
            else
            {
                list.Add(
                    1074254, // Water Added: ~1_CUR~ Maintain: ~2_NEED~ Improve: ~3_GROW~
                    $"{_water.Added}\t{_water.Maintain}\t{_water.Improve}"
                );
            }
        }

        public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, ref list);

            if (from.Alive)
            {
                list.Add(new ExamineEntry());

                if (HasAccess(from))
                {
                    if (_rewardAvailable)
                    {
                        list.Add(new CollectRewardEntry());
                    }

                    if (_events.Count > 0)
                    {
                        list.Add(new ViewEventEntry());
                    }

                    if (_vacationLeft > 0)
                    {
                        list.Add(new CancelVacationMode());
                    }
                }
            }

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new GMAddFood());
                list.Add(new GMAddWater());
                list.Add(new GMForceEvaluate());
                list.Add(new GMOpen());
                list.Add(new GMFill());
            }
        }

        public override void OnAfterDuped(Item newItem)
        {
            if (newItem is not Aquarium aquarium)
            {
                return;
            }

            aquarium.Food.Added = Food.Added;
            aquarium.Food.Improve = Food.Improve;
            aquarium.Food.Maintain = Food.Maintain;
            aquarium.Food.State = Food.State;

            aquarium.Water.Added = Water.Added;
            aquarium.Water.Improve = Water.Improve;
            aquarium.Water.Maintain = Water.Maintain;
            aquarium.Water.State = Water.State;

            aquarium.Events = [..Events];
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            switch (version)
            {
                case 3:
                case 2:
                case 1:
                    {
                        var next = reader.ReadDateTime();

                        if (next < Core.Now)
                        {
                            next = Core.Now;
                        }

                        _evaluateTimer = Timer.DelayCall(next - Core.Now, EvaluationInterval, Evaluate);

                        goto case 0;
                    }
                case 0:
                    {
                        _liveCreatures = reader.ReadInt();
                        _vacationLeft = reader.ReadInt();

                        _food = new AquariumState(this);
                        _food.Deserialize(reader);
                        _water = new AquariumState(this);
                        _water.Deserialize(reader);

                        _events = [];
                        var count = reader.ReadInt();
                        for (var i = 0; i < count; i++)
                        {
                            _events.Add(reader.ReadInt());
                        }

                        _rewardAvailable = reader.ReadBool();
                        break;
                    }
            }
        }

        public int FoodNumber() =>
            _food.State switch
            {
                (int)FoodState.Full    => 1074240,
                (int)FoodState.Overfed => 1074239,
                _                      => 1074236 + _food.State
            };

        public int WaterNumber() => 1074242 + _water.State;

        public virtual void KillFish(int amount)
        {
            using var toKill = PooledRefList<Item>.Create();

            for (var i = 0; i < Items.Count; i++)
            {
                if (Items[i] is BaseFish { Dead: false } fish)
                {
                    toKill.Add(fish);
                }
            }

            toKill.Shuffle();
            if (amount > toKill.Count)
            {
                amount = toKill.Count;
            }

            for (var i = 0; i < amount; i++)
            {
                (toKill[i] as BaseFish)!.Kill();

                amount -= 1;
                LiveCreatures = Math.Max(LiveCreatures - 1, 0);

                // An unfortunate accident has left a creature floating upside-down.  It is starting to smell.
                AddToEvents(1074366);
            }
        }

        public virtual void Evaluate()
        {
            if (_vacationLeft > 0)
            {
                _vacationLeft -= 1;
            }
            else if (m_EvaluateDay)
            {
                // reset events
                ClearEvents();

                // food events
                if (
                    _food.Added < _food.Maintain && _food.State != (int)FoodState.Overfed &&
                    _food.State != (int)FoodState.Dead ||
                    _food.Added >= _food.Improve && _food.State == (int)FoodState.Full)
                {
                    AddToEvents(1074368); // The tank looks worse than it did yesterday.
                }

                if (
                    _food.Added >= _food.Improve && _food.State != (int)FoodState.Full &&
                    _food.State != (int)FoodState.Overfed ||
                    _food.Added < _food.Maintain && _food.State == (int)FoodState.Overfed)
                {
                    AddToEvents(1074367); // The tank looks healthier today.
                }

                // water events
                if (_water.Added < _water.Maintain && _water.State != (int)WaterState.Dead)
                {
                    AddToEvents(1074370); // This tank can use more water.
                }

                if (_water.Added >= _water.Improve && _water.State != (int)WaterState.Strong)
                {
                    AddToEvents(1074369); // The water looks clearer today.
                }

                UpdateFoodState();
                UpdateWaterState();

                // reward
                if (_liveCreatures > 0)
                {
                    RewardAvailable = true;
                }
            }
            else
            {
                // new fish
                if (OptimalState && _liveCreatures < MaxLiveCreatures)
                {
                    if (Utility.RandomDouble() < 0.005 * _liveCreatures)
                    {
                        BaseFish fish;
                        int message;

                        switch (Utility.Random(6))
                        {
                            case 0:
                                {
                                    message = 1074371; // Brine shrimp have hatched overnight in the tank.
                                    fish = new BrineShrimp();
                                    break;
                                }
                            case 1:
                                {
                                    message = 1074365; // A new creature has hatched overnight in the tank.
                                    fish = new Coral();
                                    break;
                                }
                            case 2:
                                {
                                    message = 1074365; // A new creature has hatched overnight in the tank.
                                    fish = new FullMoonFish();
                                    break;
                                }
                            case 3:
                                {
                                    message = 1074373; // A sea horse has hatched overnight in the tank.
                                    fish = new SeaHorseFish();
                                    break;
                                }
                            case 4:
                                {
                                    message = 1074365; // A new creature has hatched overnight in the tank.
                                    fish = new StrippedFlakeFish();
                                    break;
                                }
                            default: // 5
                                {
                                    message = 1074365; // A new creature has hatched overnight in the tank.
                                    fish = new StrippedSosarianSwill();
                                    break;
                                }
                        }

                        if (Utility.RandomDouble() < 0.05)
                        {
                            fish.Hue = FishHues.RandomElement();
                        }
                        else if (Utility.RandomBool())
                        {
                            fish.Hue = Utility.RandomMinMax(0x100, 0x3E5);
                        }

                        if (AddFish(fish))
                        {
                            AddToEvents(message);
                        }
                        else
                        {
                            fish.Delete();
                        }
                    }
                }

                // kill fish *grins*
                if (_liveCreatures < MaxLiveCreatures)
                {
                    if (Utility.RandomDouble() < 0.01)
                    {
                        KillFish(1);
                    }
                }
                else
                {
                    KillFish(_liveCreatures - MaxLiveCreatures);
                }
            }

            m_EvaluateDay = !m_EvaluateDay;
            InvalidateProperties();
        }

        public virtual void GiveReward(Mobile to)
        {
            if (!_rewardAvailable)
            {
                return;
            }

            var max = (int)((double)_liveCreatures / 30 * m_Decorations.Length);

            var random = max <= 0 ? 0 : Utility.Random(max);

            if (random >= m_Decorations.Length)
            {
                random = m_Decorations.Length - 1;
            }

            Item item;

            try
            {
                item = m_Decorations[random].CreateInstance<Item>();
            }
            catch
            {
                return;
            }

            if (item == null)
            {
                return;
            }

            if (!to.PlaceInBackpack(item))
            {
                item.Delete();
                to.SendLocalizedMessage(1074361); // The reward could not be given.  Make sure you have room in your pack.
                return;
            }

            to.SendLocalizedMessage(1074360, $"#{item.LabelNumber}"); // You receive a reward: ~1_REWARD~
            to.PlaySound(0x5A3);

            RewardAvailable = false;

            InvalidateProperties();
        }

        public virtual void UpdateFoodState()
        {
            if (_food.Added < _food.Maintain)
            {
                _food.State = _food.State <= 0 ? 0 : _food.State - 1;
            }
            else if (_food.Added >= _food.Improve)
            {
                _food.State = _food.State >= (int)FoodState.Overfed ? (int)FoodState.Overfed : _food.State + 1;
            }

            _food.Maintain = Utility.Random((int)FoodState.Overfed + 1 - _food.State, 2);

            if (_food.State == (int)FoodState.Overfed)
            {
                _food.Improve = 0;
            }
            else
            {
                _food.Improve = _food.Maintain + 2;
            }

            _food.Added = 0;
        }

        public virtual void UpdateWaterState()
        {
            if (_water.Added < _water.Maintain)
            {
                _water.State = _water.State <= 0 ? 0 : _water.State - 1;
            }
            else if (_water.Added >= _water.Improve)
            {
                _water.State = _water.State >= (int)WaterState.Strong ? (int)WaterState.Strong : _water.State + 1;
            }

            _water.Maintain = Utility.Random((int)WaterState.Strong + 2 - _water.State, 2);

            if (_water.State == (int)WaterState.Strong)
            {
                _water.Improve = 0;
            }
            else
            {
                _water.Improve = _water.Maintain + 2;
            }

            _water.Added = 0;
        }

        public virtual bool RemoveItem(Mobile from, int at)
        {
            if (at < 0 || at >= Items.Count)
            {
                return false;
            }

            var item = Items[at];

            if (item.IsLockedDown) // for legacy aquariums
            {
                from.SendLocalizedMessage(1010449); // You may not use this object while it is locked down.
                return false;
            }

            if (item is BaseFish fish)
            {
                FishBowl bowl;

                if ((bowl = GetEmptyBowl(from)) != null)
                {
                    bowl.AddItem(fish);

                    from.SendLocalizedMessage(1074511); // You put the creature into a fish bowl.
                }
                else
                {
                    if (!from.PlaceInBackpack(fish))
                    {
                        from.SendLocalizedMessage(1074514); // You have no place to put it.
                        return false;
                    }

                    from.SendLocalizedMessage(1074512); // You put the gasping creature into your pack.
                }

                if (!fish.Dead)
                {
                    LiveCreatures -= 1;
                }
            }
            else
            {
                if (!from.PlaceInBackpack(item))
                {
                    from.SendLocalizedMessage(1074514); // You have no place to put it.
                    return false;
                }

                from.SendLocalizedMessage(1074513); // You put the item into your pack.
            }

            InvalidateProperties();
            return true;
        }

        public virtual void ExamineAquarium(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            from.SendGump(new AquariumGump(this, HasAccess(from)), true);

            from.PlaySound(0x5A4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AddFish(BaseFish fish) => AddFish(null, fish);

        public virtual bool AddFish(Mobile from, BaseFish fish)
        {
            if (fish == null)
            {
                return false;
            }

            if (IsFull || _liveCreatures >= MaxLiveCreatures || fish.Dead)
            {
                from?.SendLocalizedMessage(1073633); // The aquarium can not hold the creature.

                return false;
            }

            AddItem(fish);
            fish.StopTimer();

            LiveCreatures += 1;

            // You add the following creature to your aquarium: ~1_FISH~
            from?.SendLocalizedMessage(1073632, $"#{fish.LabelNumber}");

            InvalidateProperties();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AddDecoration(Item item) => AddDecoration(null, item);

        public virtual bool AddDecoration(Mobile from, Item item)
        {
            if (item == null)
            {
                return false;
            }

            if (IsFull)
            {
                from?.SendLocalizedMessage(1073636); // The decoration will not fit in the aquarium.

                return false;
            }

            if (!Accepts(item))
            {
                from?.SendLocalizedMessage(1073822); // The aquarium can not hold that item.

                return false;
            }

            AddItem(item);

            // You add the following decoration to your aquarium: ~1_NAME~
            from?.SendLocalizedMessage(1073635, item.LabelNumber != 0 ? $"#{item.LabelNumber}" : item.Name);

            InvalidateProperties();
            return true;
        }

        public static FishBowl GetEmptyBowl(Mobile from)
        {
            if (from.Backpack == null)
            {
                return null;
            }

            foreach (var bowl in from.Backpack.FindItemsByType<FishBowl>())
            {
                if (bowl.Empty)
                {
                    return bowl;
                }
            }

            return null;
        }

        public static bool Accepts(Item item)
        {
            if (item == null)
            {
                return false;
            }

            var type = item.GetType();

            for (var i = 0; i < m_Decorations.Length; i++)
            {
                if (type == m_Decorations[i])
                {
                    return true;
                }
            }

            return false;
        }

        private class ExamineEntry : ContextMenuEntry
        {
            public ExamineEntry() : base(6235, 2)
            {
            }

            public override void OnClick(Mobile from, IEntity target)
            {
                if (!from.Alive || target is not Aquarium aquarium || aquarium.Deleted)
                {
                    return;
                }

                aquarium.ExamineAquarium(from);
            }
        }

        private class CollectRewardEntry : ContextMenuEntry
        {
            public CollectRewardEntry() : base(6237, 2)
            {
            }

            public override void OnClick(Mobile from, IEntity target)
            {
                if (!from.Alive || target is not Aquarium aquarium || aquarium.Deleted)
                {
                    return;
                }

                aquarium.GiveReward(from);
            }
        }

        private class ViewEventEntry : ContextMenuEntry
        {
            public ViewEventEntry() : base(6239, 2)
            {
            }

            public override void OnClick(Mobile from, IEntity target)
            {
                if (!from.Alive || target is not Aquarium aquarium || aquarium.Deleted || aquarium.HasAccess(from) ||
                    aquarium._events.Count == 0)
                {
                    return;
                }

                var firstEvent = aquarium.Events[0];
                from.SendLocalizedMessage(firstEvent);

                if (firstEvent == 1074366)
                {
                    from.PlaySound(0x5A2);
                }

                aquarium.RemoveFromEventsAt(0);
                aquarium.InvalidateProperties();
            }
        }

        private class CancelVacationMode : ContextMenuEntry
        {
            public CancelVacationMode() : base(6240, 2)
            {
            }

            public override void OnClick(Mobile from, IEntity target)
            {
                if (!from.Alive || target is not Aquarium aquarium || aquarium.Deleted || aquarium.HasAccess(from))
                {
                    return;
                }

                from.SendLocalizedMessage(1074429); // Vacation mode has been cancelled.
                aquarium.VacationLeft = 0;
                aquarium.InvalidateProperties();
            }
        }

        // GM context entries
        private class GMAddFood : ContextMenuEntry
        {
            public GMAddFood() : base(6231)
            {
            }

            public override void OnClick(Mobile from, IEntity target)
            {
                if (from.AccessLevel < AccessLevel.GameMaster || target is not Aquarium aquarium || aquarium.Deleted)
                {
                    return;
                }

                aquarium.Food.Added += 1;
                aquarium.InvalidateProperties();
            }
        }

        private class GMAddWater : ContextMenuEntry
        {
            public GMAddWater() : base(6232)
            {
            }

            public override void OnClick(Mobile from, IEntity target)
            {
                if (from.AccessLevel < AccessLevel.GameMaster || target is not Aquarium aquarium || aquarium.Deleted)
                {
                    return;
                }

                aquarium.Water.Added += 1;
                aquarium.InvalidateProperties();
            }
        }

        private class GMForceEvaluate : ContextMenuEntry
        {
            public GMForceEvaluate() : base(6233)
            {
            }

            public override void OnClick(Mobile from, IEntity target)
            {
                if (from.AccessLevel < AccessLevel.GameMaster || target is not Aquarium aquarium || aquarium.Deleted)
                {
                    return;
                }

                aquarium.Evaluate();
            }
        }

        private class GMOpen : ContextMenuEntry
        {
            public GMOpen() : base(6234)
            {
            }

            public override void OnClick(Mobile from, IEntity target)
            {
                if (from.AccessLevel < AccessLevel.GameMaster || target is not Aquarium aquarium || aquarium.Deleted)
                {
                    return;
                }

                from.SendGump(new AquariumGump(aquarium, true));
            }
        }

        private class GMFill : ContextMenuEntry
        {
            public GMFill() : base(6236)
            {
            }

            public override void OnClick(Mobile from, IEntity target)
            {
                if (from.AccessLevel < AccessLevel.GameMaster || target is not Aquarium aquarium || aquarium.Deleted)
                {
                    return;
                }

                aquarium.Food.Added = aquarium.Food.Maintain;
                aquarium.Water.Added = aquarium.Water.Maintain;
                aquarium.InvalidateProperties();
            }
        }
    }

    [SerializationGenerator(0, false)]
    public partial class AquariumEastDeed : BaseAddonContainerDeed
    {
        [Constructible]
        public AquariumEastDeed()
        {
        }

        public override BaseAddonContainer Addon => new Aquarium(0x3062);
        public override int LabelNumber => 1074501; // Large Aquarium (east)
    }

    [SerializationGenerator(0, false)]
    public partial class AquariumNorthDeed : BaseAddonContainerDeed
    {
        [Constructible]
        public AquariumNorthDeed()
        {
        }

        public override BaseAddonContainer Addon => new Aquarium(0x3060);
        public override int LabelNumber => 1074497; // Large Aquarium (north)
    }
}
