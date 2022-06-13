using System;
using System.Collections;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Definitions;
using Server.Engines.MLQuests.Objectives;
using Server.Items;
using Server.Misc;
using Server.Regions;
using EDI = Server.Mobiles.EscortDestinationInfo;

namespace Server.Mobiles
{
    public class BaseEscortable : BaseCreature
    {
        public static readonly TimeSpan EscortDelay = TimeSpan.FromMinutes(5.0);

        public static readonly TimeSpan AbandonDelay =
            MLQuestSystem.Enabled ? TimeSpan.FromMinutes(1.0) : TimeSpan.FromMinutes(2.0);

        public static readonly TimeSpan DeleteTime =
            MLQuestSystem.Enabled ? TimeSpan.FromSeconds(100) : TimeSpan.FromSeconds(30);

        // Classic list
        // Used when: !MLQuestSystem.Enabled && !Core.ML
        private static readonly string[] m_TownNames =
        {
            "Cove", "Britain", "Jhelom",
            "Minoc", "Ocllo", "Trinsic",
            "Vesper", "Yew", "Skara Brae",
            "Nujel'm", "Moonglow", "Magincia"
        };

        // ML list, pre-ML quest system
        // Used when: !MLQuestSystem.Enabled && Core.ML
        private static readonly string[] m_MLTownNames =
        {
            "Cove", "Serpent's Hold", "Jhelom",
            "Nujel'm"
        };

        // ML quest system general list
        // Used when: MLQuestSystem.Enabled && !Region.IsPartOf( "Haven Island" )
        private static readonly Type[] m_MLQuestTypes =
        {
            typeof(EscortToYew),
            typeof(EscortToVesper),
            typeof(EscortToTrinsic),
            typeof(EscortToSkaraBrae),
            typeof(EscortToSerpentsHold),
            typeof(EscortToNujelm),
            typeof(EscortToMoonglow),
            typeof(EscortToMinoc),
            typeof(EscortToMagincia),
            typeof(EscortToJhelom),
            typeof(EscortToCove),
            typeof(EscortToBritain)
            // Ocllo was removed in pub 56
            // typeof( EscortToOcllo )
        };

        // ML quest system New Haven list
        // Used when: MLQuestSystem.Enabled && Region.IsPartOf( "Haven Island" )
        private static readonly Type[] m_MLQuestTypesNH =
        {
            typeof(EscortToNHAlchemist),
            typeof(EscortToNHBard),
            typeof(EscortToNHWarrior),
            typeof(EscortToNHTailor),
            typeof(EscortToNHCarpenter),
            typeof(EscortToNHMapmaker),
            typeof(EscortToNHMage),
            typeof(EscortToNHInn),
            // Farm destination was removed
            // typeof( EscortToNHFarm ),
            typeof(EscortToNHDocks),
            typeof(EscortToNHBowyer),
            typeof(EscortToNHBank)
        };

        private bool m_DeleteCorpse;

        private DateTime m_DeleteTime;
        private Timer m_DeleteTimer;

        private EDI m_Destination;
        private string m_DestinationString;

        private DateTime m_LastSeenEscorter;

        private MLQuest m_MLQuest;

        [Constructible]
        public BaseEscortable() : base(AIType.AI_Melee, FightMode.Aggressor, 22)
        {
            InitBody();
            InitOutfit();

            Fame = 200;
            Karma = 4000;

            SetSpeed(0.2, 1.0);
        }

        public BaseEscortable(Serial serial)
            : base(serial)
        {
        }

        public override bool StaticMLQuester => false; // Suppress automatic quest registration on creation/deserialization

        public override bool CanShout => !Controlled && !IsBeingDeleted;

        public bool IsBeingDeleted => m_DeleteTimer != null;

        public override bool Commandable => false; // Our master cannot boss us around!
        public override bool DeleteCorpseOnDeath => m_DeleteCorpse;

        [CommandProperty(AccessLevel.GameMaster)]
        public string Destination
        {
            get => m_Destination?.Name;
            set
            {
                m_DestinationString = value;
                m_Destination = EDI.Find(value);
            }
        }

        public static Dictionary<Mobile, BaseEscortable> EscortTable { get; } = new();

        protected override List<MLQuest> ConstructQuestList()
        {
            if (m_MLQuest == null)
            {
                var reg = Region;
                var list = reg.IsPartOf("Haven Island") ? m_MLQuestTypesNH : m_MLQuestTypes;

                var randomIdx = Utility.Random(list.Length);

                for (var i = 0; i < list.Length; ++i)
                {
                    var questType = list[randomIdx];

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
                            m_MLQuest = quest;
                            break;
                        }
                    }
                    else if (MLQuestSystem.Debug)
                    {
                        Console.WriteLine(
                            "Warning: Escortable cannot be assigned quest type '{0}', it is not registered",
                            questType.Name
                        );
                    }

                    randomIdx = (randomIdx + 1) % list.Length;
                }

                if (m_MLQuest == null)
                {
                    if (MLQuestSystem.Debug)
                    {
                        Console.WriteLine("Warning: No suitable quest found for escort {0}", Serial);
                    }

                    return null;
                }
            }

            var result = new List<MLQuest> { m_MLQuest };

            return result;
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
                Say(
                    "I am looking to go to {0}, will you take me?",
                    dest.Name == "Ocllo" && m.Map == Map.Trammel ? "Haven" : dest.Name
                );
                return true;
            }

            if (escorter == m)
            {
                Say(
                    "Lead on! Payment will be made when we arrive in {0}.",
                    dest.Name == "Ocllo" && m.Map == Map.Trammel ? "Haven" : dest.Name
                );
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

                Say("You must rest {0} minute{1} before we set out on this journey.", minutes, minutes == 1 ? "" : "s");
                return false;
            }

            if (SetControlMaster(m))
            {
                m_LastSeenEscorter = Core.Now;

                if (m is PlayerMobile playerMobile)
                {
                    playerMobile.LastEscortTime = Core.Now;
                }

                Say(
                    "Lead on! Payment will be made when we arrive in {0}.",
                    dest.Name == "Ocllo" && m.Map == Map.Trammel ? "Haven" : dest.Name
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
            m_DeleteTimer?.Stop();

            m_DeleteTimer = null;

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

                var lastSeenDelay = Core.Now - m_LastSeenEscorter;

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

            m_LastSeenEscorter = Core.Now;
            return master;
        }

        public virtual void BeginDelete()
        {
            m_DeleteTimer?.Stop();

            m_DeleteTime = Core.Now + DeleteTime;

            m_DeleteTimer = new DeleteTimer(this, m_DeleteTime - Core.Now);
            m_DeleteTimer.Start();
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
                Say(
                    1042809,
                    escorter.Name
                ); // We have arrived! I thank thee, ~1_PLAYER_NAME~! I have no further need of thy services. Here is thy pay.

                // not going anywhere
                m_Destination = null;
                m_DestinationString = null;

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
                    if (pm.CompassionGains > 0 && Core.Now > pm.NextCompassionDay)
                    {
                        pm.NextCompassionDay = DateTime.MinValue;
                        pm.CompassionGains = 0;
                    }

                    if (pm.CompassionGains >= 5) // have already gained 5 times in one day, can gain no more
                    {
                        pm.SendLocalizedMessage(
                            1053004
                        ); // You must wait about a day before you can gain in compassion again.
                    }
                    else if (VirtueHelper.Award(pm, VirtueName.Compassion, IsPrisoner ? 400 : 200, ref gainedPath))
                    {
                        if (gainedPath)
                        {
                            pm.SendLocalizedMessage(1053005); // You have achieved a path in compassion!
                        }
                        else
                        {
                            pm.SendLocalizedMessage(1053002); // You have gained in compassion.
                        }

                        pm.NextCompassionDay =
                            Core.Now + TimeSpan.FromDays(1.0); // in one day CompassionGains gets reset to 0
                        ++pm.CompassionGains;

                        if (pm.CompassionGains >= 5)
                        {
                            pm.SendLocalizedMessage(
                                1053004
                            ); // You must wait about a day before you can gain in compassion again.
                        }
                    }
                    else
                    {
                        pm.SendLocalizedMessage(
                            1053003
                        ); // You have achieved the highest path of compassion and can no longer gain any further.
                    }
                }

                return true;
            }

            return false;
        }

        public override bool OnBeforeDeath()
        {
            m_DeleteCorpse = Controlled || IsBeingDeleted;

            return base.OnBeforeDeath();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            var dest = GetDestination();

            writer.Write(dest != null);

            if (dest != null)
            {
                writer.Write(dest.Name);
            }

            writer.Write(m_DeleteTimer != null);

            if (m_DeleteTimer != null)
            {
                writer.WriteDeltaTime(m_DeleteTime);
            }

            MLQuestSystem.WriteQuestRef(writer, StaticMLQuester ? null : m_MLQuest);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (reader.ReadBool())
            {
                m_DestinationString =
                    reader.ReadString(); // NOTE: We cannot EDI.Find here, regions have not yet been loaded :-(
            }

            if (reader.ReadBool())
            {
                m_DeleteTime = reader.ReadDeltaTime();
                m_DeleteTimer = new DeleteTimer(this, m_DeleteTime - Core.Now);
                m_DeleteTimer.Start();
            }

            if (version >= 1)
            {
                var quest = MLQuestSystem.ReadQuestRef(reader);

                if (MLQuestSystem.Enabled && quest != null && !StaticMLQuester)
                {
                    m_MLQuest = quest;
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

        public virtual string[] GetPossibleDestinations() => Core.ML ? m_MLTownNames : m_TownNames;

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

            if (m_DestinationString == null && m_DeleteTimer == null)
            {
                m_DestinationString = PickRandomDestination();
            }

            if (m_Destination != null && m_Destination.Name == m_DestinationString)
            {
                return m_Destination;
            }

            if (Map.Felucca.Regions.Count > 0)
            {
                return m_Destination = EDI.Find(m_DestinationString);
            }

            return m_Destination = null;
        }

        private class DeleteTimer : Timer
        {
            private readonly Mobile m_Mobile;

            public DeleteTimer(Mobile m, TimeSpan delay)
                : base(delay)
            {
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                m_Mobile.Delete();
            }
        }
    }

    public class EscortDestinationInfo
    {
        private static Dictionary<string, EscortDestinationInfo> m_Table;

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

            m_Table = new Dictionary<string, EscortDestinationInfo>();

            foreach (Region r in list)
            {
                if (r.Name != null && r is DungeonRegion or TownRegion)
                {
                    m_Table[r.Name] = new EscortDestinationInfo(r.Name, r);
                }
            }
        }

        public static EDI Find(string name)
        {
            if (name == null || m_Table == null)
            {
                return null;
            }

            m_Table.TryGetValue(name, out var info);
            return info;
        }
    }

    public class AskDestinationEntry : ContextMenuEntry
    {
        private readonly Mobile m_From;
        private readonly BaseEscortable m_Mobile;

        public AskDestinationEntry(BaseEscortable m, Mobile from)
            : base(6100, 3)
        {
            m_Mobile = m;
            m_From = from;
        }

        public override void OnClick()
        {
            m_Mobile.SayDestinationTo(m_From);
        }
    }

    public class AcceptEscortEntry : ContextMenuEntry
    {
        private readonly Mobile m_From;
        private readonly BaseEscortable m_Mobile;

        public AcceptEscortEntry(BaseEscortable m, Mobile from)
            : base(6101, 3)
        {
            m_Mobile = m;
            m_From = from;
        }

        public override void OnClick()
        {
            m_Mobile.AcceptEscorter(m_From);
        }
    }

    public class AbandonEscortEntry : ContextMenuEntry
    {
        private readonly BaseEscortable m_Mobile;

        public AbandonEscortEntry(BaseEscortable m)
            : base(6102, 3) =>
            m_Mobile = m;

        public override void OnClick()
        {
            m_Mobile.Delete(); // OSI just seems to delete instantly
        }
    }
}
