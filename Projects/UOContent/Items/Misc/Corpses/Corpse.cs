using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Engines.PartySystem;
using Server.Engines.Quests.Doom;
using Server.Engines.Quests.Haven;
using Server.Guilds;
using Server.Misc;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

public interface IDevourer
{
    bool Devour(Corpse corpse);
}

[Flags]
public enum CorpseFlag
{
    None = 0x00000000,

    /// <summary>
    ///     Has this corpse been carved?
    /// </summary>
    Carved = 0x00000001,

    /// <summary>
    ///     If true, this corpse will not turn into bones
    /// </summary>
    NoBones = 0x00000002,

    /// <summary>
    ///     If true, the corpse has turned into bones
    /// </summary>
    IsBones = 0x00000004,

    /// <summary>
    ///     Has this corpse yet been visited by a taxidermist?
    /// </summary>
    VisitedByTaxidermist = 0x00000008,

    /// <summary>
    ///     Has this corpse yet been used to channel spiritual energy? (AOS Spirit Speak)
    /// </summary>
    Channeled = 0x00000010,

    /// <summary>
    ///     Was the owner criminal when he died?
    /// </summary>
    Criminal = 0x00000020,

    /// <summary>
    ///     Has this corpse been animated?
    /// </summary>
    Animated = 0x00000040,

    /// <summary>
    ///     Has this corpse been self looted?
    /// </summary>
    SelfLooted = 0x00000080
}

[SerializationGenerator(13, false)]
public partial class Corpse : Container, ICarvable
{
    public static readonly TimeSpan MonsterLootRightSacrifice = TimeSpan.FromMinutes(2.0);

    private static TimeSpan InstancedCorpseTime = TimeSpan.FromMinutes(3.0);

    private static TimeSpan _defaultDecayTime = TimeSpan.FromMinutes(7.0);
    private static TimeSpan _boneDecayTime = TimeSpan.FromMinutes(7.0);

    private IDevourer _devourer; // The creature that devoured this corpse

    private Dictionary<Item, InstancedItemInfo> _instancedItems;

    [SerializableField(0)]
    private List<Item> _restoreEquip;

    [SerializableField(1)]
    private CorpseFlag _flags;

    [DeltaDateTime]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _timeOfDeath;

    [SerializableField(3, getter: "private", setter: "private")]
    private Dictionary<Item, Point3D> _restoreTable;

    [TimerDrift]
    [SerializableField(4, getter: "private", setter: "private")]
    private Timer _decayTimer;

    [DeserializeTimerField(4)]
    private void DeserializeDecayTimer(TimeSpan delay) => BeginDecay(delay);

    [SerializableField(5, setter: "private")]
    private HashSet<Mobile> _looters;

    [SerializableField(6, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _killer;

    [SerializableField(7, setter: "private")]
    private List<Mobile> _aggressors;

    [SerializableField(8, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    // Value of the CorpseNameAttribute attached to the owner when he died -or- null if the owner had no CorpseNameAttribute; use "the remains of ~name~"
    [SerializableField(9, getter: "private", setter: "private")]
    private string _corpseName;

    [SerializableField(10, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private AccessLevel _accessLevel;

    [SerializableField(11, setter: "private")]
    private Guild _guild;

    [SerializableField(12)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _kills;

    [SerializableField(13, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private List<Item> _equipItems;

    // Why was this public?
    // public override bool IsPublicContainer => true;

    public Corpse(Mobile owner, List<Item> equipItems) : this(owner, null, null, equipItems)
    {
    }

    public Corpse(Mobile owner, HairInfo hair, FacialHairInfo facialhair, List<Item> equipItems)
        : base(0x2006)
    {
        // To suppress console warnings, stackable must be true
        Stackable = true;
        Amount = owner.Body; // protocol defines that for itemid 0x2006, amount=body
        Stackable = false;

        Movable = false;
        Hue = owner.Hue;
        Direction = owner.Direction;
        Name = owner.Name;

        _owner = owner;

        _corpseName = GetCorpseName(owner);

        _timeOfDeath = Core.Now;

        _accessLevel = owner.AccessLevel;
        _guild = owner.Guild as Guild;
        _kills = owner.Kills;
        SetFlag(CorpseFlag.Criminal, owner.Criminal);

        Hair = hair;
        FacialHair = facialhair;

        // This corpse does not turn to bones if: the owner is not a player
        SetFlag(CorpseFlag.NoBones, !owner.Player);

        _looters = new HashSet<Mobile>();
        _equipItems = equipItems;

        _aggressors = new List<Mobile>(owner.Aggressors.Count + owner.Aggressed.Count);

        var isBaseCreature = owner is BaseCreature;

        var lastTime = TimeSpan.MaxValue;

        for (var i = 0; i < owner.Aggressors.Count; ++i)
        {
            var info = owner.Aggressors[i];

            if (Core.Now - info.LastCombatTime < lastTime)
            {
                _killer = info.Attacker;
                lastTime = Core.Now - info.LastCombatTime;
            }

            if (!isBaseCreature && !info.CriminalAggression)
            {
                _aggressors.Add(info.Attacker);
            }
        }

        for (var i = 0; i < owner.Aggressed.Count; ++i)
        {
            var info = owner.Aggressed[i];

            if (Core.Now - info.LastCombatTime < lastTime)
            {
                _killer = info.Defender;
                lastTime = Core.Now - info.LastCombatTime;
            }

            if (!isBaseCreature)
            {
                _aggressors.Add(info.Defender);
            }
        }

        if (isBaseCreature)
        {
            var bc = (BaseCreature)owner;

            var master = bc.GetMaster();
            if (master != null)
            {
                _aggressors.Add(master);
            }

            var rights = BaseCreature.GetLootingRights(bc.DamageEntries, bc.HitsMax);
            for (var i = 0; i < rights.Count; ++i)
            {
                var ds = rights[i];

                if (ds.m_HasRight)
                {
                    _aggressors.Add(ds.m_Mobile);
                }
            }
        }

        BeginDecay(_defaultDecayTime);

        DevourCorpse();
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public virtual bool InstancedCorpse => Core.SE && Core.Now < TimeOfDeath + InstancedCorpseTime;

    public override bool IsDecoContainer => false;

    public override bool DisplayWeight => false;

    // Name of the first PlayerMobile who used Forensic Evaluation on the corpse
    [CommandProperty(AccessLevel.GameMaster)]
    public string Forensicist { get; set; }

    public HairInfo Hair { get; }

    public FacialHairInfo FacialHair { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsBones => GetFlag(CorpseFlag.IsBones);

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Devoured => _devourer != null;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Carved
    {
        get => GetFlag(CorpseFlag.Carved);
        set => SetFlag(CorpseFlag.Carved, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool VisitedByTaxidermist
    {
        get => GetFlag(CorpseFlag.VisitedByTaxidermist);
        set => SetFlag(CorpseFlag.VisitedByTaxidermist, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Channeled
    {
        get => GetFlag(CorpseFlag.Channeled);
        set => SetFlag(CorpseFlag.Channeled, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Animated
    {
        get => GetFlag(CorpseFlag.Animated);
        set => SetFlag(CorpseFlag.Animated, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool SelfLooted
    {
        get => GetFlag(CorpseFlag.SelfLooted);
        set => SetFlag(CorpseFlag.SelfLooted, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Criminal
    {
        get => GetFlag(CorpseFlag.Criminal);
        set => SetFlag(CorpseFlag.Criminal, value);
    }

    public override bool DisplaysContent => false;

    public void Carve(Mobile from, Item item)
    {
        if (IsCriminalAction(from) && (Map?.Rules & MapRules.HarmfulRestrictions) != 0)
        {
            if (Owner?.Player != true)
            {
                from.SendLocalizedMessage(1005035); // You did not earn the right to loot this creature!
            }
            else
            {
                from.SendLocalizedMessage(1010049); // You may not loot this corpse.
            }

            return;
        }

        var dead = Owner;

        if (GetFlag(CorpseFlag.Carved) || dead == null)
        {
            from.SendLocalizedMessage(500485); // You see nothing useful to carve from the corpse.
        }
        else if (((Body)Amount).IsHuman && ItemID == 0x2006)
        {
            new Blood(0x122D).MoveToWorld(Location, Map);

            new Torso().MoveToWorld(Location, Map);
            new LeftLeg().MoveToWorld(Location, Map);
            new LeftArm().MoveToWorld(Location, Map);
            new RightLeg().MoveToWorld(Location, Map);
            new RightArm().MoveToWorld(Location, Map);
            new Head(dead.Name).MoveToWorld(Location, Map);

            SetFlag(CorpseFlag.Carved, true);

            ProcessDelta();
            SendRemovePacket();
            ItemID = Utility.Random(0xECA, 9); // bone graphic
            Hue = 0;
            ProcessDelta();

            if (IsCriminalAction(from))
            {
                from.CriminalAction(true);
            }
        }
        else if (dead is BaseCreature creature)
        {
            creature.OnCarve(from, this, item);
        }
        else
        {
            from.SendLocalizedMessage(500485); // You see nothing useful to carve from the corpse.
        }
    }

    public override bool IsChildVisibleTo(Mobile m, Item child) =>
        !m.Player || m.AccessLevel > AccessLevel.Player || _instancedItems == null ||
        !_instancedItems.TryGetValue(child, out var info) || !InstancedCorpse && !info.Perpetual
        || info.IsOwner(m);

    private void AssignInstancedLoot()
    {
        if (_aggressors.Count == 0 || Items.Count == 0)
        {
            return;
        }

        _instancedItems ??= new Dictionary<Item, InstancedItemInfo>();

        var m_Stackables = new List<Item>();
        var m_Unstackables = new List<Item>();

        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];

            if (item.LootType != LootType.Cursed) // Don't have cursed items take up someone's item spot.. (?)
            {
                if (item.Stackable)
                {
                    m_Stackables.Add(item);
                }
                else
                {
                    m_Unstackables.Add(item);
                }
            }
        }

        var attackers = new List<Mobile>(_aggressors);

        for (var i = 1; i < attackers.Count - 1; i++) // randomize
        {
            var rand = Utility.Random(i + 1);

            (attackers[rand], attackers[i]) = (attackers[i], attackers[rand]);
        }

        // stackables first, for the remaining stackables, have those be randomly added after

        for (var i = 0; i < m_Stackables.Count; i++)
        {
            var item = m_Stackables[i];

            if (item.Amount >= attackers.Count)
            {
                var amountPerAttacker = Math.DivRem(item.Amount, attackers.Count, out var remainder);

                for (var j = 0; j < (remainder == 0 ? attackers.Count - 1 : attackers.Count); j++)
                {
                    // LiftItemDupe automagically adds it as a child item to the corpse
                    var splitItem = Mobile.LiftItemDupe(item, item.Amount - amountPerAttacker);

                    _instancedItems.Add(splitItem, new InstancedItemInfo(splitItem, attackers[j]));

                    // What happens to the remaining portion?  TEMP FOR NOW UNTIL OSI VERIFICATION:  Treat as Single Item.
                }

                if (remainder == 0)
                {
                    _instancedItems.Add(item, new InstancedItemInfo(item, attackers[^1]));
                }
                else
                {
                    m_Unstackables.Add(item);
                }
            }
            else
            {
                // What happens in this case?  TEMP FOR NOW UNTIL OSI VERIFICATION:  Treat as Single Item.
                m_Unstackables.Add(item);
            }
        }

        for (var i = 0; i < m_Unstackables.Count; i++)
        {
            var m = attackers[i % attackers.Count];
            var item = m_Unstackables[i];

            _instancedItems.Add(item, new InstancedItemInfo(item, m));
        }
    }

    public void AddCarvedItem(Item carved, Mobile carver)
    {
        DropItem(carved);

        if (InstancedCorpse)
        {
            _instancedItems ??= new Dictionary<Item, InstancedItemInfo>();

            _instancedItems.Add(carved, new InstancedItemInfo(carved, carver));
        }
    }

    public void TurnToBones()
    {
        if (Deleted)
        {
            return;
        }

        ProcessDelta();
        SendRemovePacket();
        ItemID = Utility.Random(0xECA, 9); // bone graphic
        Hue = 0;
        ProcessDelta();

        SetFlag(CorpseFlag.NoBones, true);
        SetFlag(CorpseFlag.IsBones, true);

        BeginDecay(_boneDecayTime);
    }

    public void BeginDecay(TimeSpan delay)
    {
        _decayTimer?.Stop();
        _decayTimer = new InternalTimer(this, delay);
        _decayTimer.Start();
    }

    public override void OnAfterDelete()
    {
        _decayTimer?.Stop();
        _decayTimer = null;
    }

    public static string GetCorpseName(Mobile m) => m is BaseCreature bc ? bc.CorpseNameOverride ?? bc.CorpseName : null;

    public static void Initialize()
    {
        Mobile.CreateCorpseHandler += Mobile_CreateCorpseHandler;
    }

    public static Container Mobile_CreateCorpseHandler(
        Mobile owner, HairInfo hair, FacialHairInfo facialhair,
        List<Item> initialContent, List<Item> equipItems
    )
    {
        var c = owner is MilitiaFighter
            ? new MilitiaFighterCorpse(owner, hair, facialhair, equipItems)
            : new Corpse(owner, hair, facialhair, equipItems);

        owner.Corpse = c;

        for (var i = 0; i < initialContent.Count; ++i)
        {
            var item = initialContent[i];

            if (Core.AOS && owner.Player && item.Parent == owner.Backpack)
            {
                c.AddItem(item);
            }
            else
            {
                c.DropItem(item);
            }

            if (owner.Player && Core.AOS)
            {
                c.SetRestoreInfo(item, item.Location);
            }
        }

        if (Core.SE && !owner.Player)
        {
            c.AssignInstancedLoot();
        }
        else if (Core.AOS && owner is PlayerMobile pm)
        {
            c.RestoreEquip = pm.EquipSnapshot;
        }

        var loc = owner.Location;
        var map = owner.Map;

        if (map == null || map == Map.Internal)
        {
            loc = owner.LogoutLocation;
            map = owner.LogoutMap;
        }

        c.MoveToWorld(loc, map);

        return c;
    }

    protected bool GetFlag(CorpseFlag flag) => (_flags & flag) != 0;

    protected void SetFlag(CorpseFlag flag, bool value)
    {
        if (value)
        {
            Flags |= flag;
        }
        else
        {
            Flags &= ~flag;
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        if (reader.ReadBool())
        {
            _restoreEquip = reader.ReadEntityList<Item>();
        }

        _flags = (CorpseFlag)reader.ReadInt();

        _timeOfDeath = reader.ReadDeltaTime();

        var count = reader.ReadInt();

        for (var i = 0; i < count; ++i)
        {
            var item = reader.ReadEntity<Item>();

            if (reader.ReadBool())
            {
                SetRestoreInfo(item, reader.ReadPoint3D());
            }
            else if (item != null)
            {
                SetRestoreInfo(item, item.Location);
            }
        }

        if (reader.ReadBool())
        {
            BeginDecay(reader.ReadDeltaTime() - Core.Now);
        }

        _looters = reader.ReadEntitySet<Mobile>();
        _killer = reader.ReadEntity<Mobile>();

        _aggressors = reader.ReadEntityList<Mobile>();
        _owner = reader.ReadEntity<Mobile>();

        _corpseName = reader.ReadString();

        _accessLevel = (AccessLevel)reader.ReadInt();
        reader.ReadInt(); // guild reserve
        _kills = reader.ReadInt();

        _equipItems = reader.ReadEntityList<Item>();
    }

    public bool DevourCorpse()
    {
        if (Devoured || Deleted || Killer?.Deleted != false || !Killer.Alive || Killer is not IDevourer devourer ||
            Owner?.Deleted != false)
        {
            return false;
        }

        _devourer = devourer;          // Set the devourer the killer
        return _devourer.Devour(this); // Devour the corpse if it hasn't
    }

    public override void SendInfoTo(NetState ns, ReadOnlySpan<byte> world = default)
    {
        base.SendInfoTo(ns, world);

        if (((Body)Amount).IsHuman && ItemID == 0x2006)
        {
            ns.SendCorpseContent(ns.Mobile, this);
            ns.SendCorpseEquip(ns.Mobile, this);
        }
    }

    public bool IsCriminalAction(Mobile from)
    {
        if (from == Owner || from.AccessLevel >= AccessLevel.GameMaster)
        {
            return false;
        }

        var p = Party.Get(Owner);

        if (p?.Contains(from) == true)
        {
            var pmi = p[Owner];

            if (pmi?.CanLoot == true)
            {
                return false;
            }
        }

        return NotorietyHandlers.CorpseNotoriety(from, this) == Notoriety.Innocent;
    }

    public override bool CheckItemUse(Mobile from, Item item) =>
        base.CheckItemUse(from, item) && (item == this || CanLoot(from, item));

    public override bool CheckLift(Mobile from, Item item, ref LRReason reject) =>
        base.CheckLift(from, item, ref reject) && CanLoot(from, item);

    public override void OnItemUsed(Mobile from, Item item)
    {
        base.OnItemUsed(from, item);

        if (item is Food)
        {
            from.RevealingAction();
        }

        if (item != this && IsCriminalAction(from))
        {
            from.CriminalAction(true);
        }

        this.Add(_looters, from);

        _instancedItems?.Remove(item);

    }

    public override void OnItemLifted(Mobile from, Item item)
    {
        base.OnItemLifted(from, item);

        if (item != this && from != Owner)
        {
            from.RevealingAction();
        }

        if (item != this && IsCriminalAction(from))
        {
            from.CriminalAction(true);
        }

        this.Add(_looters, from);

        _instancedItems?.Remove(item);
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);

        if (Core.AOS && _owner == from && from.Alive)
        {
            list.Add(new OpenCorpseEntry());
        }
    }

    public bool GetRestoreInfo(Item item, ref Point3D loc) =>
        item != null && _restoreTable?.TryGetValue(item, out loc) == true;

    public void SetRestoreInfo(Item item, Point3D loc)
    {
        if (item == null)
        {
            return;
        }

        _restoreTable ??= new Dictionary<Item, Point3D>();
        _restoreTable[item] = loc;
        this.MarkDirty();
    }

    public void ClearRestoreInfo(Item item)
    {
        if (_restoreTable == null || item == null)
        {
            return;
        }

        _restoreTable.Remove(item);
        if (_restoreTable.Count == 0)
        {
            RestoreTable = null;
        }
        else
        {
            this.MarkDirty();
        }
    }

    public bool CanLoot(Mobile from, Item item) =>
        !IsCriminalAction(from) || (Map.Rules & MapRules.HarmfulRestrictions) == 0;

    public bool CheckLoot(Mobile from, Item item)
    {
        if (!CanLoot(from, item))
        {
            if (Owner?.Player != true)
            {
                from.SendLocalizedMessage(1005035); // You did not earn the right to loot this creature!
            }
            else
            {
                from.SendLocalizedMessage(1010049); // You may not loot this corpse.
            }

            return false;
        }

        if (IsCriminalAction(from))
        {
            if (Owner?.Player != true)
            {
                from.SendLocalizedMessage(1005036); // Looting this monster corpse will be a criminal act!
            }
            else
            {
                from.SendLocalizedMessage(1005038); // Looting this corpse will be a criminal act!
            }
        }

        return true;
    }

    public virtual void Open(Mobile from, bool checkSelfLoot)
    {
        if (from.AccessLevel <= AccessLevel.Player && !from.InRange(GetWorldLocation(), 2))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
            return;
        }

        if (checkSelfLoot && from == Owner && !GetFlag(CorpseFlag.SelfLooted) && Items.Count != 0)
        {
            if (from.FindItemOnLayer(Layer.OuterTorso) is DeathRobe robe)
            {
                var map = from.Map;

                if (map != null && map != Map.Internal)
                {
                    robe.MoveToWorld(from.Location, map);
                }
            }

            var pack = from.Backpack;

            if (RestoreEquip != null && pack != null)
            {
                var packItems = new List<Item>(pack.Items); // Only items in the top-level pack are re-equipped

                for (var i = 0; i < packItems.Count; i++)
                {
                    var packItem = packItems[i];

                    if (RestoreEquip.Contains(packItem) && packItem.Movable)
                    {
                        from.EquipItem(packItem);
                    }
                }
            }

            var items = new List<Item>(Items);

            var didntFit = false;

            for (var i = 0; !didntFit && i < items.Count; ++i)
            {
                var item = items[i];
                var loc = item.Location;

                if (item.Layer is Layer.Hair or Layer.FacialHair || !item.Movable || !GetRestoreInfo(item, ref loc))
                {
                    continue;
                }

                if (pack?.CheckHold(from, item, false, true) == true)
                {
                    item.Location = loc;
                    pack.AddItem(item);

                    if (RestoreEquip?.Contains(item) == true)
                    {
                        from.EquipItem(item);
                    }
                }
                else
                {
                    didntFit = true;
                }
            }

            from.PlaySound(0x3E3);

            if (Items.Count != 0)
            {
                from.SendLocalizedMessage(1062472); // You gather some of your belongings. The rest remain on the corpse.
            }
            else
            {
                SetFlag(CorpseFlag.Carved, true);

                if (ItemID == 0x2006)
                {
                    ProcessDelta();
                    SendRemovePacket();
                    ItemID = Utility.Random(0xECA, 9); // bone graphic
                    Hue = 0;
                    ProcessDelta();
                }

                from.SendLocalizedMessage(1062471); // You quickly gather all of your belongings.
            }

            SetFlag(CorpseFlag.SelfLooted, true);
        }

        if (!CheckLoot(from, null))
        {
            return;
        }

        if (from is not PlayerMobile player)
        {
            return;
        }

        var qs = player.Quest;

        if (qs is UzeraanTurmoilQuest)
        {
            var obj = qs.FindObjective<GetDaemonBoneObjective>();
            if (obj?.CorpseWithBone == this && (!obj.Completed || UzeraanTurmoilQuest.HasLostDaemonBone(player)))
            {
                Item bone = new QuestDaemonBone();

                if (player.PlaceInBackpack(bone))
                {
                    obj.CorpseWithBone = null;
                    // You rummage through the bones and find a Daemon Bone!  You quickly place the item in your pack.
                    player.SendLocalizedMessage(1049341, "", 0x22);

                    if (!obj.Completed)
                    {
                        obj.Complete();
                    }
                }
                else
                {
                    bone.Delete();
                    // Rummaging through the bones you find a Daemon Bone, but can't pick it up because your pack is too full.  Come back when you have more room in your pack.
                    player.SendLocalizedMessage(1049342, "", 0x22);
                }

                return;
            }
        }
        else if (qs is TheSummoningQuest)
        {
            var obj = qs.FindObjective<VanquishDaemonObjective>();
            if (obj?.Completed == true && obj.CorpseWithSkull == this)
            {
                var sk = new GoldenSkull();

                if (player.PlaceInBackpack(sk))
                {
                    obj.CorpseWithSkull = null;
                    // For your valor in combating the devourer, you have been awarded a golden skull.
                    player.SendLocalizedMessage(1050022);
                    qs.Complete();
                }
                else
                {
                    sk.Delete();
                    // You find a golden skull, but your backpack is too full to carry it.
                    player.SendLocalizedMessage(1050023);
                }
            }
        }

        base.OnDoubleClick(from);
    }

    public override void OnDoubleClick(Mobile from)
    {
        Open(from, Core.AOS);
    }

    public override bool CheckContentDisplay(Mobile from) => false;

    public override void AddNameProperty(IPropertyList list)
    {
        if (ItemID == 0x2006) // Corpse form
        {
            if (_corpseName != null)
            {
                list.Add(_corpseName);
            }
            else
            {
                list.Add(1046414, Name); // the remains of ~1_NAME~
            }
        }
        else // Bone form
        {
            list.Add(1046414, Name); // the remains of ~1_NAME~
        }
    }

    public override void OnAosSingleClick(Mobile from)
    {
        var hue = Notoriety.GetHue(NotorietyHandlers.CorpseNotoriety(from, this));
        var opl = PropertyList;

        if (opl.Header > 0)
        {
            from.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Label, hue, 3, opl.Header, Name, opl.HeaderArgs);
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        var hue = Notoriety.GetHue(NotorietyHandlers.CorpseNotoriety(from, this));

        if (ItemID == 0x2006) // Corpse form
        {
            if (_corpseName != null)
            {
                from.NetState.SendMessage(Serial, ItemID, MessageType.Label, hue, 3, true, null, "", _corpseName);
            }
            else
            {
                from.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Label, hue, 3, 1046414, "", Name);
            }
        }
        else // Bone form
        {
            from.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Label, hue, 3, 1046414, "", Name);
        }
    }

    private class InstancedItemInfo
    {
        private Item _item;
        private Mobile _mobile;

        public InstancedItemInfo(Item i, Mobile m)
        {
            _item = i;
            _mobile = m;
        }

        public bool Perpetual { get; set; }

        public bool IsOwner(Mobile m)
        {
            if (_item.LootType == LootType.Cursed) // Cursed Items are part of everyone's instanced corpse... (?)
            {
                return true;
            }

            if (m == null)
            {
                return false; // sanity
            }

            if (_mobile == m)
            {
                return true;
            }

            var myParty = Party.Get(_mobile);

            return myParty != null && myParty == Party.Get(m);
        }
    }

    private class InternalTimer : Timer
    {
        private Corpse _corpse;

        public InternalTimer(Corpse c, TimeSpan delay) : base(delay) => _corpse = c;

        protected override void OnTick()
        {
            if (!_corpse.GetFlag(CorpseFlag.NoBones))
            {
                _corpse.TurnToBones();
            }
            else
            {
                _corpse.Delete();
            }
        }
    }

    private class OpenCorpseEntry : ContextMenuEntry
    {
        public OpenCorpseEntry() : base(6215, 2)
        {
        }

        public override void OnClick()
        {
            if (Owner.Target is Corpse corpse && Owner.From.CheckAlive())
            {
                corpse.Open(Owner.From, false);
            }
        }
    }
}
