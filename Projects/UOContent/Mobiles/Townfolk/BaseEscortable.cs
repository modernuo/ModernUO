using System;
using System.Collections;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Buffers;
using Server.ContextMenus;
using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Definitions;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.Virtues;
using Server.Items;
using Server.Logging;
using Server.Misc;
using Server.Network;
using Server.Regions;
using EDI = Server.Mobiles.EscortDestinationInfo;

namespace Server.Mobiles;

[SerializationGenerator(2, false)]
public partial class BaseEscortable : BaseCreature
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BaseEscortable));

    public static readonly TimeSpan EscortDelay = TimeSpan.FromMinutes(5.0);

    public static readonly TimeSpan AbandonDelay =
        MLQuestSystem.Enabled ? TimeSpan.FromMinutes(1.0) : TimeSpan.FromMinutes(2.0);

    public static readonly TimeSpan DeleteTime =
        MLQuestSystem.Enabled ? TimeSpan.FromSeconds(100) : TimeSpan.FromSeconds(30);

    // Classic list
    // Used when: !MLQuestSystem.Enabled && !Core.ML
    private static readonly string[] _townNames =
    {
        "Cove", "Britain", "Jhelom",
        "Minoc", "Ocllo", "Trinsic",
        "Vesper", "Yew", "Skara Brae",
        "Nujel'm", "Moonglow", "Magincia"
    };

    // ML list, pre-ML quest system
    // Used when: !MLQuestSystem.Enabled && Core.ML
    private static readonly string[] _mlTownNames =
    {
        "Cove", "Serpent's Hold", "Jhelom", "Nujel'm"
    };

    // ML quest system general list
    // Used when: MLQuestSystem.Enabled && !Region.IsPartOf( "Haven Island" )
    private static readonly Dictionary<Type, (int[], int)> m_MLQuestTypes = new()
    {
        { typeof(EscortToYew), (Array.Empty<int>(), 0) },
        { typeof(EscortToVesper), (Array.Empty<int>(), 0) },
        { typeof(EscortToTrinsic), (Array.Empty<int>(), 0) },
        { typeof(EscortToSkaraBrae), (Array.Empty<int>(), 0) },
        { typeof(EscortToSerpentsHold), (Array.Empty<int>(), 0) },
        { typeof(EscortToNujelm), (Array.Empty<int>(), 0) },
        { typeof(EscortToMoonglow), (Array.Empty<int>(), 0) },
        { typeof(EscortToMinoc), (Array.Empty<int>(), 0) },
        { typeof(EscortToMagincia), (Array.Empty<int>(), 0) },
        { typeof(EscortToJhelom), (Array.Empty<int>(), 0) },
        { typeof(EscortToCove), (Array.Empty<int>(), 0) },
        { typeof(EscortToBritain), (Array.Empty<int>(), 0) }
        // Ocllo was removed in pub 56
        // { typeof(EscortToOcllo), (Array.Empty<int>(), 0) }
    };

    // ML quest system New Haven list
    // Used when: MLQuestSystem.Enabled && Region.IsPartOf("Haven Island")
    // TODO: Find out if these specific quest messages are for special one-off quest characters
    // that teach you where things are in a city instead of the randomly spawned ones.
    private static readonly Dictionary<Type, (int[], int)> m_MLQuestTypesNH = new()
    {
        // I am missing several components for my new potions, and I need to find the local alchemist.
        // My daughter is sick , and I need medicine. Do you know the way to the local alchemist?
        // I need some potions before I set out for a long journey. Can you take me to the alchemist in The Bottled Imp?
        // I’m looking to go to the Alchemist's shop. Will you take me?
        { typeof(EscortToNHAlchemist), (new[] { 1042767, 1042768, 1042769, 1042824 }, 1042811) },

        // I need new string for my lute, yet I do not know the way to the local music shop, could you take me?
        // I was hoping to hire a bard for my birthday party. Can you take me to one?
        // I fear my talent for music is less than my desire to learn, yet still I would like to try. Can you take me to the local music shop?
        // I’m looking to go to the music center. Will you take me?
        { typeof(EscortToNHBard), (new [] { 1042770, 1042771, 1042772, 1042825 }, 1042812) },

        // A family heirloom, our armoire, is falling apart. I need to see the local carpenter. Would you guide me to her?
        // My goat has broken through our fence, and I need new boards. Can you direct me to the local wood worker?
        // I need a hammer and nails. Never mind for what. Take me to the local carpenter or leave me be.
        // I’m looking to go to the local woodworker. Will you take me?
        { typeof(EscortToNHCarpenter), (new [] { 1042773, 1042774, 1042775, 1042829 }, 1042816) },

        //TODO: Add woodsman (camping, tracker, etc)
        // 1042776 - I have a job for the local woodsman. I have lost my dog. Can you take me to see him?
        // 1042777 - The tracker here is a good man. I know he will help me find my pet scorpion.  Do you know the way to his house?
        // 1042778 - I am hoping to learn something about camping. Can you take me to the local woodsman?
        // 1042836 - I’m looking to go to the local woodsman's. Will you take me?
        // 1042814 - Lead on! Payment will be made when we arrive at the weapon trainer's.

        // Me spouse's cookin' is too good. I fear me belly is outgrowin' me clothes. I need to find the local seamstress. Can you take me to her?
        // I want to learn how to sew. Can you take me to see the tailor?
        // I need new clothes for a party, and I was wondering if you could take me to the tailor?
        // I’m looking to go to the local tailor. Will you take me?
        { typeof(EscortToNHTailor), (new [] { 1042779, 1042780, 1042781, 1042828, }, 1042815) },

        // I need to deposit some gold. You look like a trustworthy soul, so could you direct me to the local bank?
        // A rich relative of mine said they deposited some gold in to my account. Would you be able to lead me to the bank?
        // I have a debt I need to pay off at the bank. Do you know the way there?
        // I’m looking to go to the city bank. Will you take me?
        { typeof(EscortToNHBank), (new [] { 1042782, 1042783, 1042784, 1042832 }, 1042819) },

        // I wish to travel and see the world, but I fear I need martial skills. Would you direct me to the local weapons trainer?
        // I need a sword to accompany me on a journey. Would escort me to the local fighter's union?
        // I need someone to help me rid my home of mongbats. Please take me to the local swordfighter.
        // I’m looking to go to the weapon trainer's. Will you take me?
        { typeof(EscortToNHWarrior), (new [] { 1042785, 1042786, 1042787, 1042827 }, 1042814) },

        // My new house requires blessing, but I am not a mage and I have not the scroll. Would you take me to the local mages guild?
        // I need a wizard. I can't say why. You'll take me to one, or won't you?
        // You there. Take me to see a sorcerer so I can turn a friend back in to a human. He is currently a cat and keeps demanding milk.
        // I’m looking to go to the magic shop. Will you take me?
        { typeof(EscortToNHMage), (new [] { 1042788, 1042789, 1042790, 1042833 }, 1042820) },

        // Psst - I hate to admit it, but I am lost. Can you take me to a place where they sell maps?
        // I am trying to confirm the location of dungeons around here. Would you take me to a map maker so I might buy supplies?
        // Where am I? Who am I? Do you know me? Hmmm - on second thought, I think I best stick with where I am first. Do you know where I can get a map?
        // I’m looking to go to the local Map maker's. Will you take me?
        { typeof(EscortToNHMapmaker), (new [] { 1042791, 1042792, 1042793, 1042835 }, 1042822) },

        // I am in search of a loaf of fresh bread. Do you know where I might find some?
        // I need to find some spices for my stew.  The local chef might have some for me, can you take me to see him?
        // I need something to eat. I am starving. Can you take me to the inn?
        // I’m looking to go to the New Haven Inn. Will you take me?
        { typeof(EscortToNHInn), (new [] { 1042794, 1042795, 1042796, 1042834 }, 1042821) },

        // Hey you!  I need to find a farmer because all my plants keep dying.  Please take me to one.
        // Do you know where I might find a person who sells seeds for crops?
        // I am hoping to swap soil stories with a local farmer, but I cannot find one. Can you take me to one?
        // I’m looking to go to a local farm. Will you take me?
        // { typeof(EscortToNHFarm), (new[] { 1042797, 1042798, 1042799, 1042830 }, 1042817) }, // Farm destination was remove

        // Hey! I need to visit the local fisherman to ask about what kind of bait he is using. I keep pulling up sea serpents. Where is he?
        // You know, I would really like some fish, but I do not know where a fisherman is. Do you?
        // I have heard of a magical fish that grants wishes. I bet THAT fisherman knows where the fish is. Please take me to him.
        // I’m looking to go to the fishing wharf. Will you take me?
        { typeof(EscortToNHDocks), (new [] { 1042800, 1042801, 1042802, 1042826 }, 1042813) },

        // I need arrows to hunt rabbits for me rabbit stew. Can you take me to the local archer?
        // I have a huge amount of feathers for the local Fletcher. Where might I find him?
        // You there. Do you know the way to the local archer?
        // I’m looking to go to the local archery range. Will you take me?
        { typeof(EscortToNHBowyer), (new [] { 1042803, 1042804, 1042805, 1042831 }, 1042818) },
    };

    [SerializableField(0, setter: "private")]
    private string _destinationString;

    [TimerDrift]
    [SerializableField(1)]
    private Timer _deleteTimer;

    [DeserializeTimerField(1)]
    private void DeserializeDeleteTimer(TimeSpan delay)
    {
        if (delay >= TimeSpan.Zero)
        {
            Timer.DelayCall(delay, Delete);
        }
    }

    [SerializableField(2)]
    private Type _mlQuestType;

    [SerializableField(3)]
    private TextDefinition _mlQuestDestinationMessage;

    [SerializableField(4)]
    private TextDefinition _mlQuestPaymentMessage;

    private bool _deleteCorpse;

    private EDI _destination;

    private DateTime _lastSeenEscorter;

    // Not serialized
    private List<MLQuest> _mlQuest;

    [Constructible]
    public BaseEscortable() : base(AIType.AI_Melee, FightMode.Aggressor, 22)
    {
        InitBody();
        InitOutfit();

        Fame = 200;
        Karma = 4000;

        SetSpeed(0.2, 1.0);
    }

    public override bool StaticMLQuester => false; // Suppress automatic quest registration on creation/deserialization

    public override bool CanShout => !Controlled && !IsBeingDeleted;

    public bool IsBeingDeleted => _deleteTimer != null;

    public override bool Commandable => false; // Our master cannot boss us around!
    public override bool DeleteCorpseOnDeath => _deleteCorpse;

    [CommandProperty(AccessLevel.GameMaster)]
    public string Destination
    {
        get => _destination?.Name;
        set
        {
            DestinationString = value;
            _destination = EDI.Find(value);
        }
    }

    public static Dictionary<Mobile, BaseEscortable> EscortTable { get; } = new();

    protected override List<MLQuest> ConstructQuestList()
    {
        if (_mlQuestType == null)
        {
            var reg = Region;
            var types = reg.IsPartOf("Haven Island") ? m_MLQuestTypesNH : m_MLQuestTypes;

            // Get a rented buffer
            var list = STArrayPool<Type>.Shared.Rent(types.Keys.Count);

            // Copy to the new list
            types.Keys.CopyTo(list, 0);

            // Create a span that is the right size
            var listSpan = list.AsSpan(0, types.Keys.Count); // We need a span to manipulate

            // Shuffle
            listSpan.Shuffle();

            // Sequentially choose a quest until we have a valid one.
            for (var i = 0; i < listSpan.Length; ++i)
            {
                var questType = list[i];

                var quest = MLQuestSystem.FindQuest(questType);

                if (quest != null)
                {
                    var okay = true;

                    foreach (var obj in quest.Objectives)
                    {
                        if (obj is EscortObjective objective && objective.Destination.Contains(reg))
                        {
                            okay = false; // We're already there!
                            break;
                        }
                    }

                    if (okay)
                    {
                        var (destinationMessage, paymentMessage) = types[questType];
                        _mlQuestType = questType;
                        _mlQuestDestinationMessage = destinationMessage?.Length > 0 ? destinationMessage.RandomElement() : 0;
                        _mlQuestPaymentMessage = paymentMessage;
                        // Cached by BaseCreature in m_MLQuests
                        _mlQuest = new List<MLQuest>(1) { quest };
                        break;
                    }
                }
                else if (MLQuestSystem.Debug)
                {
                    logger.Warning("Escortable cannot be assigned quest type '{Type}' because the quest was not registered.", questType);
                }
            }

            if (_mlQuest != null && MLQuestSystem.Debug)
            {
                logger.Warning("No suitable quest found for '{Serial}'", Serial);
            }

            // Return the rented array
            STArrayPool<Type>.Shared.Return(list);
        }

        return _mlQuest;
    }

    public override void Shout(PlayerMobile pm)
    {
        /*
         * 1072301 - You there!  Care to hear how to earn some easy gold?
         * 1072302 - Adventurer!  I have an offer for you.
         * 1072303 - Wait!  I have an opportunity for you to make some gold!
         */
        MLQuestSystem.Tell(this, pm, Utility.Random(1072301, 3));
    }

    public virtual void InitBody()
    {
        SetStr(90, 100);
        SetDex(90, 100);
        SetInt(15, 25);

        Hue = Race.Human.RandomSkinHue();

        if (Female = Utility.RandomBool())
        {
            Body = 401;
            Name = NameList.RandomName("female");
        }
        else
        {
            Body = 400;
            Name = NameList.RandomName("male");
        }
    }

    public virtual void InitOutfit()
    {
        AddItem(new FancyShirt(Utility.RandomNeutralHue()));
        AddItem(new ShortPants(Utility.RandomNeutralHue()));
        AddItem(new Boots(Utility.RandomNeutralHue()));

        Utility.AssignRandomHair(this);

        PackGold(200, 250);
    }

    public virtual bool SayDestinationTo(Mobile m)
    {
        var dest = GetDestination();

        if (dest == null || !m.Alive)
        {
            return false;
        }

        var escorter = GetEscorter();

        if (escorter == null)
        {
            if (_mlQuestDestinationMessage != null)
            {
                if (_mlQuestDestinationMessage.Number > 0)
                {
                    Say(_mlQuestDestinationMessage.Number);
                    return true;
                }

                if (!string.IsNullOrEmpty(_mlQuestDestinationMessage.String))
                {
                    Say(_mlQuestDestinationMessage.String);
                    return true;
                }
            }

            // I'm looking to go somewhere:
            Say(1042807, AffixType.Append, dest.Name == "Ocllo" && m.Map == Map.Trammel ? "Haven" : dest.Name, "");
            return true;
        }

        if (escorter == m)
        {
            if (_mlQuestPaymentMessage != null)
            {
                if (_mlQuestPaymentMessage.Number > 0)
                {
                    Say(_mlQuestPaymentMessage.Number);
                    return true;
                }

                if (!string.IsNullOrEmpty(_mlQuestPaymentMessage.String))
                {
                    Say(_mlQuestPaymentMessage.String);
                    return true;
                }
            }

            // Lead on! Payment will be made when we arrive at ~1_DESTINATION~!
            Say(1042806, dest.Name == "Ocllo" && m.Map == Map.Trammel ? "Haven" : dest.Name);
            return true;
        }

        return false;
    }

    public virtual bool AcceptEscorter(Mobile m)
    {
        var dest = GetDestination();

        if (dest == null)
        {
            return false;
        }

        if (GetEscorter() != null || !m.Alive)
        {
            return false;
        }

        if (EscortTable.TryGetValue(m, out var escortable) && escortable?.Deleted == false &&
            escortable.GetEscorter() == m)
        {
            Say("I see you already have an escort.");
            return false;
        }

        if (m is PlayerMobile mobile && mobile.LastEscortTime + EscortDelay >= Core.Now)
        {
            var minutes =
                (int)Math.Ceiling((mobile.LastEscortTime + EscortDelay - Core.Now).TotalMinutes);

            if (minutes == 1)
            {
                Say($"You must rest {minutes} minute before we set out on this journey.");
            }
            else
            {
                Say($"You must rest {minutes} minutes before we set out on this journey.");
            }

            return false;
        }

        if (SetControlMaster(m))
        {
            _lastSeenEscorter = Core.Now;

            if (m is PlayerMobile playerMobile)
            {
                playerMobile.LastEscortTime = Core.Now;
            }

            Say(
                $"Lead on! Payment will be made when we arrive in {(dest.Name == "Ocllo" && m.Map == Map.Trammel ? "Haven" : dest.Name)}."
            );
            EscortTable[m] = this;
            StartFollow();
            return true;
        }

        return false;
    }

    public override bool HandlesOnSpeech(Mobile from) =>
        !MLQuestSystem.Enabled && (from.InRange(Location, 3) || base.HandlesOnSpeech(from));

    public override void OnSpeech(SpeechEventArgs e)
    {
        base.OnSpeech(e);

        if (GetDestination() == null || e.Handled || !e.Mobile.InRange(Location, 3))
        {
            return;
        }

        if (e.HasKeyword(0x1D)) // *destination*
        {
            e.Handled = SayDestinationTo(e.Mobile);
        }
        else if (e.HasKeyword(0x1E)) // *i will take thee*
        {
            e.Handled = AcceptEscorter(e.Mobile);
        }
    }

    public override void OnAfterDelete()
    {
        _deleteTimer?.Stop();

        _deleteTimer = null;

        base.OnAfterDelete();
    }

    public override void OnThink()
    {
        base.OnThink();
        CheckAtDestination();
    }

    protected override bool OnMove(Direction d)
    {
        if (!base.OnMove(d))
        {
            return false;
        }

        CheckAtDestination();

        return true;
    }

    // TODO: Pre-ML methods below, might be mergeable with the ML methods in EscortObjective

    public virtual void StartFollow()
    {
        StartFollow(GetEscorter());
    }

    public virtual void StartFollow(Mobile escorter)
    {
        if (escorter == null)
        {
            return;
        }

        ActiveSpeed = 0.1;
        PassiveSpeed = 0.2;

        ControlOrder = OrderType.Follow;
        ControlTarget = escorter;

        if (IsPrisoner && CantWalk)
        {
            CantWalk = false;
        }

        CurrentSpeed = 0.1;
    }

    public virtual void StopFollow()
    {
        ActiveSpeed = 0.2;
        PassiveSpeed = 1.0;

        ControlOrder = OrderType.None;
        ControlTarget = null;

        CurrentSpeed = 1.0;
    }

    public virtual Mobile GetEscorter()
    {
        if (!Controlled)
        {
            return null;
        }

        var master = ControlMaster;

        if (MLQuestSystem.Enabled || master == null)
        {
            return master;
        }

        if (master.Deleted || master.Map != Map || !master.InRange(Location, 30) || !master.Alive)
        {
            StopFollow();

            var lastSeenDelay = Core.Now - _lastSeenEscorter;

            if (lastSeenDelay >= AbandonDelay)
            {
                master.SendLocalizedMessage(1042473); // You have lost the person you were escorting.
                Say(1005653);                         // Hmmm. I seem to have lost my master.

                SetControlMaster(null);
                EscortTable.Remove(master);

                Timer.StartTimer(TimeSpan.FromSeconds(5.0), Delete);
                return null;
            }

            ControlOrder = OrderType.Stay;
            return master;
        }

        if (ControlOrder != OrderType.Follow)
        {
            StartFollow(master);
        }

        _lastSeenEscorter = Core.Now;
        return master;
    }

    public virtual void BeginDelete()
    {
        if (_deleteTimer != null)
        {
            _deleteTimer.Stop();
            _deleteTimer.Delay = DeleteTime;
            _deleteTimer.Start();
        }
        else
        {
            _deleteTimer = Timer.DelayCall(DeleteTime, Delete);
        }
    }

    public virtual bool CheckAtDestination()
    {
        if (MLQuestSystem.Enabled)
        {
            return false;
        }

        var dest = GetDestination();

        if (dest == null)
        {
            return false;
        }

        var escorter = GetEscorter();

        if (escorter == null)
        {
            return false;
        }

        if (dest.Contains(Location))
        {
            // We have arrived! I thank thee, ~1_PLAYER_NAME~! I have no further need of thy services. Here is thy pay.
            Say(1042809, escorter.Name);

            // not going anywhere
            _destination = null;
            DestinationString = null;

            var cont = escorter.Backpack ?? escorter.BankBox;

            var gold = new Gold(500, 1000);

            if (!cont.TryDropItem(escorter, gold, false))
            {
                gold.MoveToWorld(escorter.Location, escorter.Map);
            }

            StopFollow();
            SetControlMaster(null);
            EscortTable.Remove(escorter);
            BeginDelete();

            Titles.AwardFame(escorter, 10, true);

            var gainedPath = false;

            if (escorter is PlayerMobile pm)
            {
                var virtues = VirtueSystem.GetOrCreateVirtues(pm);
                if (virtues.CompassionGains > 0 && Core.Now > virtues.NextCompassionDay)
                {
                    virtues.NextCompassionDay = DateTime.MinValue;
                    virtues.CompassionGains = 0;
                }

                if (virtues.CompassionGains >= 5) // have already gained 5 times in one day, can gain no more
                {
                    // You must wait about a day before you can gain in compassion again.
                    pm.SendLocalizedMessage(1053004);
                }
                else if (VirtueSystem.Award(pm, VirtueName.Compassion, IsPrisoner ? 400 : 200, ref gainedPath))
                {
                    if (gainedPath)
                    {
                        pm.SendLocalizedMessage(1053005); // You have achieved a path in compassion!
                    }
                    else
                    {
                        pm.SendLocalizedMessage(1053002); // You have gained in compassion.
                    }

                    // in one day CompassionGains gets reset to 0
                    virtues.NextCompassionDay = Core.Now + TimeSpan.FromDays(1.0);

                    if (++virtues.CompassionGains >= 5)
                    {
                        // You must wait about a day before you can gain in compassion again.
                        pm.SendLocalizedMessage(1053004);
                    }
                }
                else
                {
                    // You have achieved the highest path of compassion and can no longer gain any further.
                    pm.SendLocalizedMessage(1053003);
                }
            }

            return true;
        }

        return false;
    }

    public override bool OnBeforeDeath()
    {
        _deleteCorpse = Controlled || IsBeingDeleted;

        return base.OnBeforeDeath();
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        if (reader.ReadBool())
        {
            // NOTE: We cannot EDI.Find here, regions have not yet been loaded :-(
            _destinationString = reader.ReadString();
        }

        if (reader.ReadBool())
        {
            var deleteTime = reader.ReadDeltaTime();
            _deleteTimer = Timer.DelayCall(deleteTime - Core.Now, Delete);
            _deleteTimer.Start();
        }

        var quest = MLQuestSystem.ReadQuestRef(reader);

        if (MLQuestSystem.Enabled && quest != null && !StaticMLQuester)
        {
            _mlQuestType = quest.GetType();
            _mlQuest = new List<MLQuest>(1) { quest };

            // This isn't serialized before codegen
            if (m_MLQuestTypesNH.TryGetValue(_mlQuestType, out var tuple))
            {
                var (destinationMessages, paymentMessage) = tuple;
                _mlQuestDestinationMessage = destinationMessages?.Length > 0 ? destinationMessages.RandomElement() : 0;
                _mlQuestPaymentMessage = paymentMessage;
            }
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (MLQuestSystem.Enabled && !StaticMLQuester && _mlQuest != null && _mlQuestType != null)
        {
            var quest = MLQuestSystem.FindQuest(_mlQuestType);
            if (quest != null)
            {
                _mlQuest = new List<MLQuest>(1) { quest };
            }
        }
    }

    public override bool CanBeRenamedBy(Mobile from) => from.AccessLevel >= AccessLevel.GameMaster;

    public override void AddCustomContextEntries(Mobile from, List<ContextMenuEntry> list)
    {
        if (from.Alive)
        {
            var escorter = GetEscorter();

            if (!MLQuestSystem.Enabled && GetDestination() != null)
            {
                if (escorter == null || escorter == from)
                {
                    list.Add(new AskDestinationEntry(this, from));
                }

                if (escorter == null)
                {
                    list.Add(new AcceptEscortEntry(this, from));
                }
            }

            if (escorter == from)
            {
                list.Add(new AbandonEscortEntry(this));
            }
        }

        base.AddCustomContextEntries(from, list);
    }

    public virtual string[] GetPossibleDestinations() => Core.ML ? _mlTownNames : _townNames;

    public virtual string PickRandomDestination()
    {
        if (Map.Felucca.Regions.Count == 0 || Map == null || Map == Map.Internal || Location == Point3D.Zero)
        {
            return null; // Not yet fully initialized
        }

        var possible = GetPossibleDestinations();
        string picked = null;

        while (picked == null)
        {
            picked = possible.RandomElement();
            var test = EDI.Find(picked);

            if (test.Contains(Location))
            {
                picked = null;
            }
        }

        return picked;
    }

    public EDI GetDestination()
    {
        if (MLQuestSystem.Enabled)
        {
            return null;
        }

        if (_destinationString == null && _deleteTimer == null)
        {
            _destinationString = PickRandomDestination();
        }

        if (_destination != null && _destination.Name == _destinationString)
        {
            return _destination;
        }

        if (Map.Felucca.Regions.Count > 0)
        {
            return _destination = EDI.Find(_destinationString);
        }

        return _destination = null;
    }
}

public class EscortDestinationInfo
{
    private static Dictionary<string, EscortDestinationInfo> _table;

    public EscortDestinationInfo(string name, Region region)
    {
        Name = name;
        Region = region;
    }

    public string Name { get; }

    public Region Region { get; }

    public bool Contains(Point3D p) => Region.Contains(p);

    public static void Initialize()
    {
        ICollection list = Map.Felucca.Regions.Values;

        if (list.Count == 0)
        {
            return;
        }

        _table = new Dictionary<string, EscortDestinationInfo>();

        foreach (Region r in list)
        {
            if (r.Name != null && r is DungeonRegion or TownRegion)
            {
                _table[r.Name] = new EscortDestinationInfo(r.Name, r);
            }
        }
    }

    public static EDI Find(string name)
    {
        if (name == null || _table == null)
        {
            return null;
        }

        _table.TryGetValue(name, out var info);
        return info;
    }
}

public class AskDestinationEntry : ContextMenuEntry
{
    private readonly Mobile _from;
    private readonly BaseEscortable _mobile;

    public AskDestinationEntry(BaseEscortable m, Mobile from) : base(6100, 3)
    {
        _mobile = m;
        _from = from;
    }

    public override void OnClick()
    {
        _mobile.SayDestinationTo(_from);
    }
}

public class AcceptEscortEntry : ContextMenuEntry
{
    private readonly Mobile _from;
    private readonly BaseEscortable _mobile;

    public AcceptEscortEntry(BaseEscortable m, Mobile from) : base(6101, 3)
    {
        _mobile = m;
        _from = from;
    }

    public override void OnClick()
    {
        _mobile.AcceptEscorter(_from);
    }
}

public class AbandonEscortEntry : ContextMenuEntry
{
    private readonly BaseEscortable _mobile;

    public AbandonEscortEntry(BaseEscortable m) : base(6102, 3) => _mobile = m;

    public override void OnClick()
    {
        _mobile.Delete(); // OSI just seems to delete instantly
    }
}
