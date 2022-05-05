using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Engines.PartySystem;
using Server.Gumps;
using Server.Network;
using Server.Utilities;

namespace Server.Items;

[SerializationGenerator(2, false)]
public partial class TreasureMapChest : LockableContainer
{
    [SerializableField(0, setter: "private")]
    private List<Mobile> _guardians;

    [SerializableField(1)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private bool _temporary;

    [SerializableField(2)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private Mobile _owner;

    [SerializableField(3)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private int _level;

    [TimerDrift]
    [SerializableField(4)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private Timer _expireTimer;

    [DeserializeTimerField(4)]
    private void DeserializeExpireTimer(TimeSpan delay)
    {
        if (!_temporary)
        {
            _expireTimer = Timer.DelayCall(delay, Delete);
        }
    }

    [Tidy]
    [SerializableField(5, setter: "private")]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private HashSet<Item> _lifted;

    [Constructible]
    public TreasureMapChest(int level) : this(null, level)
    {
    }

    public TreasureMapChest(Mobile owner, int level, bool temporary = false) : base(0xE40)
    {
        _owner = owner;
        _level = level;

        _temporary = temporary;
        _guardians = new List<Mobile>();

        _expireTimer = Timer.DelayCall(TimeSpan.FromHours(3.0), Delete);
        Fill(this, level);
    }

    public override int LabelNumber => 3000541;

    public static Type[] Artifacts { get; } =
    {
        typeof(CandelabraOfSouls), typeof(GoldBricks), typeof(PhillipsWoodenSteed),
        typeof(ArcticDeathDealer), typeof(BlazeOfDeath), typeof(BurglarsBandana),
        typeof(CavortingClub), typeof(DreadPirateHat),
        typeof(EnchantedTitanLegBone), typeof(GwennosHarp), typeof(IolosLute),
        typeof(LunaLance), typeof(NightsKiss), typeof(NoxRangersHeavyCrossbow),
        typeof(PolarBearMask), typeof(VioletCourage), typeof(HeartOfTheLion),
        typeof(ColdBlood), typeof(AlchemistsBauble)
    };

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime DeleteTime => _expireTimer.Next;

    public override bool IsDecoContainer => false;

    private static void GetRandomAOSStats(out int attributeCount, out int min, out int max)
    {
        var rnd = Utility.Random(15);

        if (Core.SE)
        {
            if (rnd < 1)
            {
                attributeCount = Utility.RandomMinMax(3, 5);
                min = 50;
                max = 100;
            }
            else if (rnd < 3)
            {
                attributeCount = Utility.RandomMinMax(2, 5);
                min = 40;
                max = 80;
            }
            else if (rnd < 6)
            {
                attributeCount = Utility.RandomMinMax(2, 4);
                min = 30;
                max = 60;
            }
            else if (rnd < 10)
            {
                attributeCount = Utility.RandomMinMax(1, 3);
                min = 20;
                max = 40;
            }
            else
            {
                attributeCount = 1;
                min = 10;
                max = 20;
            }

            return;
        }

        if (rnd < 1)
        {
            attributeCount = Utility.RandomMinMax(2, 5);
            min = 20;
            max = 70;
        }
        else if (rnd < 3)
        {
            attributeCount = Utility.RandomMinMax(2, 4);
            min = 20;
            max = 50;
        }
        else if (rnd < 6)
        {
            attributeCount = Utility.RandomMinMax(2, 3);
            min = 20;
            max = 40;
        }
        else if (rnd < 10)
        {
            attributeCount = Utility.RandomMinMax(1, 2);
            min = 10;
            max = 30;
        }
        else
        {
            attributeCount = 1;
            min = 10;
            max = 20;
        }
    }

    public static void Fill(LockableContainer cont, int level)
    {
        cont.Movable = false;
        cont.Locked = true;

        if (level == 0)
        {
            cont.LockLevel = ILockpickable.CannotPick;

            cont.DropItem(new Gold(Utility.RandomMinMax(50, 100)));

            if (Utility.RandomDouble() < 0.75)
            {
                cont.DropItem(new TreasureMap(0, Map.Trammel));
            }
        }
        else
        {
            cont.TrapType = TrapType.ExplosionTrap;
            cont.TrapPower = level * 25;
            cont.TrapLevel = level;

            cont.RequiredSkill = level switch
            {
                1 => 36,
                2 => 76,
                3 => 84,
                4 => 92,
                5 => 100,
                _ => 100
            };

            cont.LockLevel = cont.RequiredSkill - 10;
            cont.MaxLockLevel = cont.RequiredSkill + 40;

            // Publish 67 gold change
            // if (Core.SA)
            // cont.DropItem( new Gold( level * 5000 ) );
            // else
            cont.DropItem(new Gold(level * 1000));

            for (var i = 0; i < level * 5; ++i)
            {
                cont.DropItem(Loot.RandomScroll(0, 63, SpellbookType.Regular));
            }

            var numberItems = Core.SE ? level switch
            {
                1 => 5,
                2 => 10,
                3 => 15,
                4 => 38,
                5 => 50,
                6 => 60,
                _ => 0
            } : level * 6;

            for (var i = 0; i < numberItems; ++i)
            {
                var item = Core.AOS
                    ? Loot.RandomArmorOrShieldOrWeaponOrJewelry()
                    : Loot.RandomArmorOrShieldOrWeapon();

                if (item is BaseWeapon weapon)
                {
                    if (Core.AOS)
                    {
                        GetRandomAOSStats(out var attributeCount, out var min, out var max);
                        BaseRunicTool.ApplyAttributesTo(weapon, attributeCount, min, max);
                    }
                    else
                    {
                        weapon.DamageLevel = (WeaponDamageLevel)Utility.Random(6);
                        weapon.AccuracyLevel = (WeaponAccuracyLevel)Utility.Random(6);
                        weapon.DurabilityLevel = (WeaponDurabilityLevel)Utility.Random(6);
                    }

                    cont.DropItem(weapon);
                }
                else if (item is BaseArmor armor)
                {
                    if (Core.AOS)
                    {
                        GetRandomAOSStats(out var attributeCount, out var min, out var max);
                        BaseRunicTool.ApplyAttributesTo(armor, attributeCount, min, max);
                    }
                    else
                    {
                        armor.ProtectionLevel = (ArmorProtectionLevel)Utility.Random(6);
                        armor.Durability = (ArmorDurabilityLevel)Utility.Random(6);
                    }

                    cont.DropItem(armor);
                }
                else if (item is BaseHat hat)
                {
                    if (Core.AOS)
                    {
                        GetRandomAOSStats(out var attributeCount, out var min, out var max);
                        BaseRunicTool.ApplyAttributesTo(hat, attributeCount, min, max);
                    }

                    cont.DropItem(hat);
                }
                else if (item is BaseJewel jewel)
                {
                    GetRandomAOSStats(out var attributeCount, out var min, out var max);
                    BaseRunicTool.ApplyAttributesTo(jewel, attributeCount, min, max);

                    cont.DropItem(jewel);
                }
            }
        }

        var reagents = level == 0 ? 12 : level * 3;

        for (var i = 0; i < reagents; i++)
        {
            var item = Loot.RandomPossibleReagent();
            item.Amount = Utility.RandomMinMax(40, 60);
            cont.DropItem(item);
        }

        var gems = level == 0 ? 2 : level * 3;

        for (var i = 0; i < gems; i++)
        {
            var item = Loot.RandomGem();
            cont.DropItem(item);
        }

        if (level == 6 && Core.AOS)
        {
            cont.DropItem(Artifacts.RandomElement().CreateInstance<Item>());
        }
    }

    public override bool CheckLocked(Mobile from)
    {
        if (!Locked)
        {
            return false;
        }

        if (_level == 0 && from.AccessLevel < AccessLevel.GameMaster)
        {
            foreach (var m in _guardians)
            {
                if (m.Alive)
                {
                    // You must first kill the guardians before you may open this chest.
                    from.SendLocalizedMessage(1046448);
                    return true;
                }
            }

            LockPick(from);
            return false;
        }

        return base.CheckLocked(from);
    }

    private bool CheckLoot(Mobile m, bool criminalAction)
    {
        if (_temporary)
        {
            return false;
        }

        if (m.AccessLevel >= AccessLevel.GameMaster || _owner == null || m == _owner)
        {
            return true;
        }

        if (Party.Get(_owner)?.Contains(m) == true)
        {
            return true;
        }

        var map = Map;

        if ((map?.Rules & MapRules.HarmfulRestrictions) == 0)
        {
            if (criminalAction)
            {
                m.CriminalAction(true);
            }
            else
            {
                m.SendLocalizedMessage(1010630); // Taking someone else's treasure is a criminal offense!
            }

            return true;
        }

        m.SendLocalizedMessage(1010631); // You did not discover this chest!
        return false;
    }

    public override bool CheckItemUse(Mobile from, Item item) =>
        CheckLoot(from, item != this) && base.CheckItemUse(from, item);

    public override bool CheckLift(Mobile from, Item item, ref LRReason reject) =>
        CheckLoot(from, true) && base.CheckLift(from, item, ref reject);

    public override void OnItemLifted(Mobile from, Item item)
    {
        var notYetLifted = _lifted?.Contains(item) != true; 
        from.RevealingAction();

        if (notYetLifted)
        {
            _lifted ??= new HashSet<Item>();
            _lifted.Add(item);

            if (Utility.RandomDouble() <= 0.1) // 10% chance to spawn a new monster
            {
                TreasureMap.Spawn(_level, GetWorldLocation(), Map, from, false);
            }
        }

        base.OnItemLifted(from, item);
    }

    public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
    {
        if (m.AccessLevel < AccessLevel.GameMaster)
        {
            m.SendLocalizedMessage(1048122, "", 0x8A5); // The chest refuses to be filled with treasure again.
            return false;
        }

        return base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _guardians = new List<Mobile>();

        _owner = reader.ReadEntity<Mobile>();
        _level = reader.ReadInt();
        var expireTimerNext = reader.ReadDeltaTime();
        DeserializeExpireTimer(expireTimerNext == DateTime.MinValue ? TimeSpan.MinValue : expireTimerNext - Core.Now);
        _lifted = reader.ReadEntitySet<Item>();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (_expireTimer == null)
        {
            Delete();
        }
    }

    public override void OnAfterDelete()
    {
        _expireTimer?.Stop();
        _expireTimer = null;
        base.OnAfterDelete();
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);

        if (from.Alive)
        {
            list.Add(new RemoveEntry(from, this));
        }
    }

    public void BeginRemove(Mobile from)
    {
        if (!from.Alive)
        {
            return;
        }

        from.CloseGump<RemoveGump>();
        from.SendGump(new RemoveGump(from, this));
    }

    public void EndRemove(Mobile from)
    {
        if (Deleted || from != _owner || !from.InRange(GetWorldLocation(), 3))
        {
            return;
        }

        from.SendLocalizedMessage(1048124, "", 0x8A5); // The old, rusted chest crumbles when you hit it.
        Delete();
    }

    private class RemoveGump : Gump
    {
        private readonly TreasureMapChest _chest;
        private readonly Mobile _from;

        public RemoveGump(Mobile from, TreasureMapChest chest) : base(15, 15)
        {
            _from = from;
            _chest = chest;

            Closable = false;
            Disposable = false;

            AddPage(0);

            AddBackground(30, 0, 240, 240, 2620);

            // When this treasure chest is removed, any items still inside of it will be lost.
            AddHtmlLocalized(45, 15, 200, 80, 1048125, 0xFFFFFF);
            // Are you certain you're ready to remove this chest?
            AddHtmlLocalized(45, 95, 200, 60, 1048126, 0xFFFFFF);

            AddButton(40, 153, 4005, 4007, 1);
            AddHtmlLocalized(75, 155, 180, 40, 1048127, 0xFFFFFF); // Remove the Treasure Chest

            AddButton(40, 195, 4005, 4007, 2);
            AddHtmlLocalized(75, 197, 180, 35, 1006045, 0xFFFFFF); // Cancel
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                _chest.EndRemove(_from);
            }
        }
    }

    private class RemoveEntry : ContextMenuEntry
    {
        private readonly TreasureMapChest _chest;
        private readonly Mobile _from;

        public RemoveEntry(Mobile from, TreasureMapChest chest) : base(6149, 3)
        {
            _from = from;
            _chest = chest;

            Enabled = from == chest._owner;
        }

        public override void OnClick()
        {
            if (_chest.Deleted || _from != _chest._owner || !_from.CheckAlive())
            {
                return;
            }

            _chest.BeginRemove(_from);
        }
    }
}
