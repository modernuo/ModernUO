using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModernUO.CodeGeneratedEvents;
using Server.Accounting;
using Server.Items;
using Server.Logging;
using Server.Maps;
using Server.Misc;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.CharacterCreation;

public static partial class CharacterCreation
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(CharacterCreation));

    // Allowed skills that are not race or era specific
    private static readonly HashSet<SkillName> _allowedStartingSkills =
    [
        SkillName.Alchemy,
        SkillName.Anatomy,
        SkillName.AnimalLore,
        SkillName.AnimalTaming,
        SkillName.Archery,
        SkillName.ArmsLore,
        SkillName.Begging,
        SkillName.Blacksmith,
        SkillName.Fletching,
        SkillName.Bushido,
        SkillName.Camping,
        SkillName.Carpentry,
        SkillName.Cartography,
        SkillName.Chivalry,
        SkillName.Cooking,
        SkillName.DetectHidden,
        SkillName.Discordance,
        SkillName.EvalInt,
        SkillName.Fencing,
        SkillName.Fishing,
        SkillName.Focus,
        SkillName.Forensics,
        SkillName.Healing,
        SkillName.Herding,
        SkillName.Hiding,
        SkillName.Imbuing,
        SkillName.Inscribe,
        SkillName.ItemID,
        SkillName.Lockpicking,
        SkillName.Lumberjacking,
        SkillName.Macing,
        SkillName.Magery,
        SkillName.Meditation,
        SkillName.Mining,
        SkillName.Musicianship,
        SkillName.Mysticism,
        SkillName.Necromancy,
        SkillName.Ninjitsu,
        SkillName.Parry,
        SkillName.Peacemaking,
        SkillName.Poisoning,
        SkillName.Provocation,
        SkillName.MagicResist,
        SkillName.Snooping,
        SkillName.SpiritSpeak,
        SkillName.Stealing,
        SkillName.Swords,
        SkillName.Tactics,
        SkillName.Tailoring,
        SkillName.TasteID,
        SkillName.Throwing,
        SkillName.Tinkering,
        SkillName.Tracking,
        SkillName.Veterinary,
        SkillName.Wrestling
    ];

    private static readonly TimeSpan BadStartMessageDelay = TimeSpan.FromSeconds(3.5);

    public static readonly CityInfo[] OldHavenStartingCities =
    [
        new("Haven", "The Bountiful Harvest Inn", 3677, 2625, 0, Map.Trammel),
        new("Britain", "Sweet Dreams Inn", 1075074, 1496, 1628, 10, Map.Trammel),
        new("Magincia", "The Great Horns Tavern", 1075077, 3734, 2222, 20, Map.Trammel),
    ];

    public static readonly CityInfo[] FeluccaStartingCities =
    [
        new("Yew", "The Empath Abbey", 1075072, 633, 858, 0, Map.Felucca),
        new("Minoc", "The Barnacle", 1075073, 2476, 413, 15, Map.Felucca),
        new("Britain", "Sweet Dreams Inn", 1075074, 1496, 1628, 10, Map.Felucca),
        new("Moonglow", "The Scholars Inn", 1075075, 4408, 1168, 0, Map.Felucca),
        new("Trinsic", "The Traveler's Inn", 1075076, 1845, 2745, 0, Map.Felucca),
        new("Magincia", "The Great Horns Tavern", 1075077, 3734, 2222, 20, Map.Felucca),
        new("Jhelom", "The Mercenary Inn", 1075078, 1374, 3826, 0, Map.Felucca),
        new("Skara Brae", "The Falconer's Inn", 1075079, 618, 2234, 0, Map.Felucca),
        new("Vesper", "The Ironwood Inn", 1075080, 2771, 976, 0, Map.Felucca)
    ];

    public static readonly CityInfo[] TrammelStartingCities =
    [
        new("Yew", "The Empath Abbey", 1075072, 633, 858, 0, Map.Trammel),
        new("Minoc", "The Barnacle", 1075073, 2476, 413, 15, Map.Trammel),
        new("Moonglow", "The Scholars Inn", 1075075, 4408, 1168, 0, Map.Trammel),
        new("Trinsic", "The Traveler's Inn", 1075076, 1845, 2745, 0, Map.Trammel),
        new("Jhelom", "The Mercenary Inn", 1075078, 1374, 3826, 0, Map.Trammel),
        new("Skara Brae", "The Falconer's Inn", 1075079, 618, 2234, 0, Map.Trammel),
        new("Vesper", "The Ironwood Inn", 1075080, 2771, 976, 0, Map.Trammel),
    ];

    public static readonly CityInfo[] NewHavenStartingCities =
    [
        new("New Haven", "The Bountiful Harvest Inn", 1150168, 3503, 2574, 14, Map.Trammel),
        new("Britain", "The Wayfarer's Inn", 1075074, 1602, 1591, 20, Map.Trammel)
        // Magincia removed because it burned down.
    ];

    public static readonly CityInfo[] StartingCitiesSA =
    [
        new("Royal City", "Royal City Inn", 1150169, 738, 3486, -19, Map.TerMur)
    ];

    private static CityInfo[] _availableStartingCities;

    public static CityInfo[] GetStartingCities() =>
        _availableStartingCities ??= ConstructAvailableStartingCities();

    private static CityInfo[] ConstructAvailableStartingCities()
    {
        var pre6000ClientSupport = TileMatrix.Pre6000ClientSupport;
        var availableMaps = ExpansionInfo.CoreExpansion.MapSelectionFlags;
        var trammelAvailable = availableMaps.Includes(MapSelectionFlags.Trammel);
        var terMerAvailable = availableMaps.Includes(MapSelectionFlags.TerMur);

        if (trammelAvailable)
        {
            if (pre6000ClientSupport)
            {
                return [..OldHavenStartingCities, ..TrammelStartingCities];
            }

            if (terMerAvailable)
            {
                return [..NewHavenStartingCities, ..TrammelStartingCities, ..StartingCitiesSA];
            }

            return [..NewHavenStartingCities, ..TrammelStartingCities];
        }

        if (availableMaps.Includes(MapSelectionFlags.Felucca))
        {
            return FeluccaStartingCities;
        }

        logger.Error("No starting cities are available.");
        return [];
    }

    [GeneratedEvent(nameof(CharacterCreatedEvent))]
    public static partial void CharacterCreatedEvent(CharacterCreatedEventArgs e);

    private static void AddBackpack(this Mobile m)
    {
        var pack = m.Backpack;

        if (pack == null)
        {
            pack = new Backpack();
            pack.Movable = false;

            m.AddItem(pack);
        }

        m.PackItem(new RedBook("a book", m.Name, 20, true));
        m.PackItem(new Gold(1000)); // Starting gold can be customized here
        m.PackItem(new Dagger());
        m.PackItem(new Candle());
    }

    private static Mobile CreateMobile(Account a)
    {
        if (a.Count >= a.Limit)
        {
            return null;
        }

        for (var i = 0; i < a.Length; ++i)
        {
            if (a[i] == null)
            {
                return a[i] = new PlayerMobile();
            }
        }

        return null;
    }

    [OnEvent(nameof(CharacterCreatedEvent))]
    private static void OnCharacterCreated(CharacterCreatedEventArgs args)
    {
        if (!ProfessionInfo.GetProfession(args.Profession, out var profession))
        {
            args.Profession = 0;
        }

        var state = args.State;

        if (state == null)
        {
            return;
        }

        var newChar = CreateMobile(args.Account as Account);

        if (newChar == null)
        {
            logger.Information("Login: {NetState}: Character creation failed, account full", state);
            return;
        }

        args.Mobile = newChar;

        newChar.Player = true;
        newChar.AccessLevel = args.Account.AccessLevel;
        newChar.Female = args.Female;
        newChar.Hue = newChar.Race.ClipSkinHue(args.Hue & 0x3FFF) | 0x8000;
        newChar.Hunger = 20;

        SetName(newChar, args.Name);
        newChar.AddBackpack();

        if (newChar.AccessLevel == AccessLevel.Player)
        {
            var race = Core.Expansion >= args.Race.RequiredExpansion ? args.Race : Race.DefaultRace;
            newChar.Race = race;

            if (newChar is PlayerMobile pm)
            {
                if (((Account)pm.Account).Young)
                {
                    pm.Young = true;

                    newChar.BankBox.DropItem(new NewPlayerTicket
                    {
                        Owner = newChar
                    });
                }
            }

            SetStats(newChar, state, profession?.Stats ?? args.Stats);
            SetSkills(newChar, profession?.Skills ?? args.Skills);
            GiveProfessionItems(newChar, profession, args.ShirtHue, args.PantsHue);

            if (race.ValidateHair(newChar, args.HairID))
            {
                newChar.HairItemID = args.HairID;
                newChar.HairHue = race.ClipHairHue(args.HairHue & 0x3FFF);
            }

            if (race.ValidateFacialHair(newChar, args.BeardID))
            {
                newChar.FacialHairItemID = args.BeardID;
                newChar.FacialHairHue = race.ClipHairHue(args.BeardHue & 0x3FFF);
            }

            if (TestCenter.Enabled)
            {
                TestCenter.FillBankbox(newChar);
            }
        }
        else
        {
            newChar.Str = 100;
            newChar.Int = 100;
            newChar.Dex = 100;

            for (var i = 0; i < newChar.Skills.Length; i++)
            {
                newChar.Skills[i].BaseFixedPoint = 1000;
            }

            newChar.Race = Race.Human;
            newChar.Blessed = true;
            newChar.AddItem(new StaffRobe(newChar.AccessLevel));
        }

        var city = GetStartLocation(args);
        newChar.MoveToWorld(city.Location, city.Map);

        logger.Information(
            "Login: {0}: New character being created (account={1}, character={2}, serial={3}, started.city={4}, started.location={5}, started.map={6})",
            state,
            args.Account.Username,
            newChar.Name,
            newChar.Serial,
            city.City,
            city.Location,
            city.Map);

        new WelcomeTimer(newChar).Start();
    }

    private static CityInfo GetStartLocation(CharacterCreatedEventArgs args)
    {
        var availableMaps = ExpansionInfo.CoreExpansion.MapSelectionFlags;
        var m = args.Mobile;

        if (m.AccessLevel > AccessLevel.Player)
        {
            var map = availableMaps.Includes(MapSelectionFlags.Felucca) ? Map.Felucca : Map.Trammel;
            if (availableMaps.Includes(MapSelectionFlags.Felucca))
            {
                return new CityInfo("Green Acres", "Green Acres", 5445, 1153, 0, map);
            }
        }

        if (Core.SA)
        {
            return args.City;
        }

        var flags = args.State?.Flags ?? ClientFlags.None;
        if (ProfessionInfo.GetProfession(args.Profession, out var profession))
        {
            switch (profession.Name.ToLowerInvariant())
            {
                case "necromancer":
                    {
                        if ((flags & ClientFlags.Malas) != 0 && availableMaps.Includes(MapSelectionFlags.Malas))
                        {
                            return new CityInfo("Umbra", "Mardoth's Tower", 2114, 1301, -50, Map.Malas);
                        }

                        /*
                         * Unfortunately you are playing on a *NON-Age-Of-Shadows* game
                         * installation and cannot be transported to Malas.
                         * You will not be able to take your new player quest in Malas
                         * without an AOS client.  You are now being taken to the city of
                         * Haven on the Trammel facet.
                         */
                        Timer.StartTimer(BadStartMessageDelay, () => m.SendLocalizedMessage(1062205));
                        return GetStartingCities()[0];
                    }
                case "paladin":
                    {
                        return GetStartingCities()[0];
                    }
                case "samurai":
                    {
                        bool haotisAndTokunoAccessible =
                            (flags & ClientFlags.Tokuno) == ClientFlags.Tokuno &&
                            (flags & ClientFlags.Malas) == ClientFlags.Malas &&
                            availableMaps.Includes(MapSelectionFlags.Malas | MapSelectionFlags.Tokuno);

                        if (haotisAndTokunoAccessible)
                        {
                            return new CityInfo("Samurai DE", "Haoti's Grounds", 368, 780, -1, Map.Malas);
                        }

                        /*
                         * Unfortunately you are playing on a *NON-Samurai-Empire* game
                         * installation and cannot be transported to Tokuno.
                         * You will not be able to take your new player quest in Tokuno
                         * without an SE client. You are now being taken to the city of
                         * Haven on the Trammel facet.
                         */
                        Timer.StartTimer(BadStartMessageDelay, () => m.SendLocalizedMessage(1063487));
                        return GetStartingCities()[0];
                    }
                case "ninja":
                    {
                        bool enimosAndTokunoAccessible =
                            (flags & ClientFlags.Tokuno) == ClientFlags.Tokuno &&
                            (flags & ClientFlags.Malas) == ClientFlags.Malas &&
                            availableMaps.Includes(MapSelectionFlags.Malas | MapSelectionFlags.Tokuno);

                        if (enimosAndTokunoAccessible)
                        {
                            return new CityInfo("Ninja DE", "Enimo's Residence", 414, 823, -1, Map.Malas);
                        }

                        /*
                         * Unfortunately you are playing on a *NON-Samurai-Empire* game
                         * installation and cannot be transported to Tokuno.
                         * You will not be able to take your new player quest in Tokuno
                         * without an SE client. You are now being taken to the city of
                         * Haven on the Trammel facet.
                         */
                        Timer.StartTimer(BadStartMessageDelay, () => m.SendLocalizedMessage(1063487));
                        return GetStartingCities()[0];
                    }
            }
        }

        return args.City;
    }

    private static void SetStats(Mobile m, NetState state, byte[] stats)
    {
        var maxStats = state.NewCharacterCreation ? 90 : 80;

        var str = stats[0];
        var dex = stats[1];
        var intel = stats[2];

        if (str is < 10 or > 60 || dex is < 10 or > 60 || intel is < 10 or > 60 || str + dex + intel != maxStats)
        {
            str = 10;
            dex = 10;
            intel = 10;
        }

        m.InitStats(str, dex, intel);
    }

    private static void SetName(Mobile m, string name)
    {
        name = name.Trim();

        if (!NameVerification.ValidatePlayerName(name))
        {
            name = "Generic Player";
        }

        m.Name = name;
    }

    private static bool ValidateSkills(int raceFlag, (SkillName, byte)[] skills)
    {
        var total = 0;

        for (var i = 0; i < skills.Length; ++i)
        {
            var (name, value) = skills[i];

            if (value > 50 || !_allowedStartingSkills.Contains(name))
            {
                return false;
            }

            /**
             * Note: Change to Alchemy @ 0 skill if something invalid is chosen.
             * To avoid this, modify the client to only show the skills allowed by your shard.
             */
            switch (name)
            {
                case SkillName.Necromancy or SkillName.Chivalry or SkillName.Focus when !Core.AOS:
                case SkillName.Ninjitsu or SkillName.Bushido when !Core.SE:
                case SkillName.Throwing or SkillName.Imbuing when !Core.SA:
                case SkillName.Archery when raceFlag == Race.AllowGargoylesOnly:
                case SkillName.Throwing when raceFlag != Race.AllowGargoylesOnly:
                    {
                        skills[i] = default;
                        break;
                    }
            }

            total += value;

            // Do not allow a skill to be listed twice
            for (var j = i + 1; j < skills.Length; ++j)
            {
                var (nameCheck, valueCheck) = skills[j];

                if (valueCheck > 0 && nameCheck == name)
                {
                    return false;
                }
            }
        }

        return total is 100 or 120;
    }

    private static void SetSkills(Mobile m, (SkillName, byte)[] skills)
    {
        if (!ValidateSkills(m.Race.RaceFlag, skills))
        {
            return;
        }

        for (var i = 0; i < skills.Length; ++i)
        {
            var (name, value) = skills[i];
            if (value <= 0)
            {
                continue;
            }

            var skill = m.Skills[name];

            if (skill != null)
            {
                skill.BaseFixedPoint = value * 10;
                m.AddSkillItems(name);
            }
        }
    }

    private static void GiveProfessionItems(Mobile m, ProfessionInfo profession, int shirtHue, int pantsHue)
    {
        var elf = m.Race == Race.Elf;
        var gargoyle = m.Race == Race.Gargoyle;

        switch (profession?.Name.ToLowerInvariant())
        {
            case "necromancer":
                {
                    Container regs = new BagOfNecroReagents { LootType = LootType.Regular };

                    if (!Core.AOS)
                    {
                        foreach (var item in regs.Items)
                        {
                            item.LootType = LootType.Newbied;
                        }
                    }

                    m.PackItem(regs);

                    EquipItem(m, new BoneHelm());

                    if (elf)
                    {
                        EquipItem(m, new ElvenMachete());
                        EquipItem(m, NecroHue(new LeafChest()));
                        EquipItem(m, NecroHue(new LeafArms()));
                        EquipItem(m, NecroHue(new LeafGloves()));
                        EquipItem(m, NecroHue(new LeafGorget()));
                        EquipItem(m, NecroHue(new LeafLegs()));
                        EquipItem(m, new ElvenBoots());
                    }
                    else if (gargoyle)
                    {
                        EquipItem(m, new GlassSword());
                        EquipItem(m, NecroHue(m.Female ? new GargishLeatherChestType2() : new GargishLeatherChestType1()));
                        EquipItem(m, NecroHue(m.Female ? new GargishLeatherArmsType2() : new GargishLeatherArmsType1()));
                        EquipItem(m, NecroHue(m.Female ? new GargishLeatherKiltType2() : new GargishLeatherKiltType1()));
                        EquipItem(m, NecroHue(m.Female ? new GargishLeatherLegsType2() : new GargishLeatherLegsType1()));
                    }
                    else
                    {
                        EquipItem(m, new BoneHarvester());
                        EquipItem(m, NecroHue(new LeatherChest()));
                        EquipItem(m, NecroHue(new LeatherArms()));
                        EquipItem(m, NecroHue(new LeatherGloves()));
                        EquipItem(m, NecroHue(new LeatherGorget()));
                        EquipItem(m, NecroHue(new LeatherLegs()));
                        EquipItem(m, NecroHue(new Skirt()));
                        EquipItem(m, new Sandals(0x8FD));
                    }

                    // animate dead, evil omen, pain spike, summon familiar, wraith form
                    m.PackItem(new NecromancerSpellbook(0x8981ul) { LootType = LootType.Blessed });
                    return;
                }
            case "paladin":
                {
                    if (elf)
                    {
                        EquipItem(m, new ElvenMachete());
                        EquipItem(m, new WingedHelm());
                        EquipItem(m, new LeafGorget());
                        EquipItem(m, new LeafArms());
                        EquipItem(m, new LeafChest());
                        EquipItem(m, new LeafLegs());
                        EquipItem(m, new LeafGloves());
                        EquipItem(m, new ElvenBoots()); // Verify hue
                    }
                    else if (gargoyle)
                    {
                        EquipItem(m, new GlassSword());
                        EquipItem(m, m.Female ? new GargishStoneChestType2() : new GargishStoneChestType1());
                        EquipItem(m, m.Female ? new GargishStoneArmsType2() : new GargishStoneArmsType1());
                        EquipItem(m, m.Female ? new GargishStoneKiltType2() : new GargishStoneKiltType1());
                        EquipItem(m, m.Female ? new GargishStoneLegsType2() : new GargishStoneLegsType1());
                    }
                    else
                    {
                        EquipItem(m, new Broadsword());
                        EquipItem(m, new Helmet());
                        EquipItem(m, new PlateGorget());
                        EquipItem(m, new RingmailArms());
                        EquipItem(m, new RingmailChest());
                        EquipItem(m, new RingmailLegs());
                        EquipItem(m, new RingmailGloves());
                        EquipItem(m, new ThighBoots(0x748));
                        EquipItem(m, new Cloak(0xCF));
                        EquipItem(m, new BodySash(0xCF));
                    }

                    m.PackItem(new BookOfChivalry { LootType = LootType.Blessed });
                    return;
                }
            case "samurai":
                {
                    if (elf)
                    {
                        EquipItem(m, new RavenHelm());
                        EquipItem(m, new HakamaShita(0x2C3));
                        EquipItem(m, new Hakama(0x2C3));
                        EquipItem(m, new SamuraiTabi(0x2C3));
                        EquipItem(m, new TattsukeHakama(0x22D));
                        EquipItem(m, new Bokuto());
                    }
                    else if (gargoyle)
                    {
                        EquipItem(m, new GargishTalwar());
                        EquipItem(m, m.Female ? new GargishLeatherChestType2() : new GargishLeatherChestType1());
                        EquipItem(m, m.Female ? new GargishLeatherArmsType2() : new GargishLeatherArmsType1());
                        EquipItem(m, m.Female ? new GargishLeatherKiltType2() : new GargishLeatherKiltType1());
                        EquipItem(m, m.Female ? new GargishLeatherLegsType2() : new GargishLeatherLegsType1());
                    }
                    else
                    {
                        EquipItem(m, new LeatherJingasa());
                        EquipItem(m, new HakamaShita(0x2C3));
                        EquipItem(m, new Hakama(0x2C3));
                        EquipItem(m, new SamuraiTabi(0x2C3));
                        EquipItem(m, new TattsukeHakama(0x22D));
                        EquipItem(m, new Bokuto());
                    }

                    m.PackItem(new Scissors());
                    m.PackItem(new Bandage(50));
                    m.PackItem(new BookOfBushido());

                    return;
                }
            case "ninja":
                {
                    ReadOnlySpan<int> hues = [0x1A8, 0xEC, 0x99, 0x90, 0xB5, 0x336, 0x89];
                    // TODO: Verify that's ALL the hues for that above.

                    if (elf)
                    {
                        EquipItem(m, new AssassinSpike());
                        EquipItem(m, new TattsukeHakama(hues.RandomElement()));
                        EquipItem(m, new HakamaShita(0x2C3));
                        EquipItem(m, new NinjaTabi(0x2C3));
                        EquipItem(m, new Kasa());
                    }
                    else if (gargoyle)
                    {
                        EquipItem(m, new DualPointedSpear());
                        EquipItem(m, m.Female ? new GargishLeatherChestType2() : new GargishLeatherChestType1());
                        EquipItem(m, m.Female ? new GargishLeatherArmsType2() : new GargishLeatherArmsType1());
                        EquipItem(m, m.Female ? new GargishLeatherKiltType2() : new GargishLeatherKiltType1());
                        EquipItem(m, m.Female ? new GargishLeatherLegsType2() : new GargishLeatherLegsType1());
                    }
                    else
                    {
                        EquipItem(m, new Tekagi());
                        EquipItem(m, new TattsukeHakama(hues.RandomElement()));
                        EquipItem(m, new HakamaShita(0x2C3));
                        EquipItem(m, new NinjaTabi(0x2C3));
                        EquipItem(m, new Kasa());
                    }

                    m.PackItem(new SmokeBomb());
                    m.PackItem(new SmokeBomb());
                    m.PackItem(new SmokeBomb());
                    m.PackItem(new SmokeBomb());
                    m.PackItem(new SmokeBomb());
                    m.PackItem(new BookOfNinjitsu());

                    return;
                }
            case "swordsman":
            case "fencer":
            case "warrior":
            case "mace fighter":
                {
                    if (elf)
                    {
                        EquipItem(m, new Circlet());
                        EquipItem(m, new HideGorget());
                        EquipItem(m, new HideChest());
                        EquipItem(m, new HidePauldrons());
                        EquipItem(m, new HideGloves());
                        EquipItem(m, new HidePants());
                        EquipItem(m, new ElvenBoots());
                    }
                    else if (gargoyle)
                    {
                        EquipItem(m, m.Female ? new GargishLeatherChestType2() : new GargishLeatherChestType1());
                        EquipItem(m, m.Female ? new GargishLeatherArmsType2() : new GargishLeatherArmsType1());
                        EquipItem(m, m.Female ? new GargishLeatherKiltType2() : new GargishLeatherKiltType1());
                        EquipItem(m, m.Female ? new GargishLeatherLegsType2() : new GargishLeatherLegsType1());
                    }
                    else
                    {
                        EquipItem(m, new Bascinet());
                        EquipItem(m, new StuddedGorget());
                        EquipItem(m, new StuddedChest());
                        EquipItem(m, new StuddedArms());
                        EquipItem(m, new StuddedGloves());
                        EquipItem(m, new StuddedLegs());
                        EquipItem(m, new ThighBoots());
                    }
                    break;
                }
        }

        m.AddShirt(shirtHue);
        m.AddPants(pantsHue);
        m.AddShoes();

        // All elves get a wild staff
        if (elf)
        {
            EquipItem(m, new WildStaff());
        }
    }

    private static void EquipItem(Mobile m, Item item, bool mustEquip = false)
    {
        if (item == null)
        {
            return;
        }

        if (!Core.AOS)
        {
            item.LootType = LootType.Newbied;
        }

        if (m?.EquipItem(item) == true)
        {
            return;
        }

        var pack = m?.Backpack;

        if (!mustEquip && pack != null)
        {
            pack.DropItem(item);
        }
        else
        {
            item.Delete();
        }
    }

    private static void PackItem(this Mobile m, Item item)
    {
        if (!Core.AOS)
        {
            item.LootType = LootType.Newbied;
        }

        var pack = m.Backpack;

        if (pack != null)
        {
            pack.DropItem(item);
        }
        else
        {
            item.Delete();
        }
    }

    private static void AddShirt(this Mobile m, int shirtHue)
    {
        var hue = Utility.ClipDyedHue(shirtHue & 0x3FFF);
        var raceFlag = m.Race.RaceFlag;

        var shirt = raceFlag switch
        {
            Race.AllowElvesOnly                   => new ElvenShirt(hue),
            Race.AllowGargoylesOnly when m.Female => new GargishClothChestType2 { Hue = hue },
            Race.AllowGargoylesOnly               => new GargishClothChestType1 { Hue = hue },
            // Humans
            _ => (Item)(Utility.Random(3) switch
            {
                0 => new Shirt(hue),
                1 => new FancyShirt(hue),
                _ => new Doublet(hue)
            })
        };

        EquipItem(m, shirt);
    }

    private static void AddPants(this Mobile m, int pantsHue)
    {
        var hue = Utility.ClipDyedHue(pantsHue & 0x3FFF);
        var raceFlag = m.Race.RaceFlag;
        var female = m.Female;

        var pants = raceFlag switch
        {
            Race.AllowElvesOnly                 => new ElvenPants(hue),
            Race.AllowGargoylesOnly when female => new GargishClothLegsType2 { Hue = hue },
            Race.AllowGargoylesOnly             => new GargishClothLegsType1 { Hue = hue },
            // Humans
            _ => (Item)(Utility.RandomBool() switch
            {
                true when female  => new Skirt(hue),
                true              => new LongPants(hue),
                false when female => new Kilt(hue),
                false             => new ShortPants(hue)
            })
        };

        EquipItem(m, pants);
    }

    private static void AddShoes(this Mobile m)
    {
        if (m.Race == Race.Elf)
        {
            EquipItem(m, new ElvenBoots());
        }
        else if (m.Race == Race.Human)
        {
            EquipItem(m, new Shoes(Utility.RandomYellowHue()));
        }
    }

    private static void PackInstrument(this Mobile m)
    {
        Item instrument = Utility.Random(6) switch
        {
            0 => new Drums(),
            1 => new Harp(),
            2 => new LapHarp(),
            3 => new Lute(),
            4 => new Tambourine(),
            _ => new TambourineTassel()
        };

        m.PackItem(instrument);
    }

    private static void PackScroll(this Mobile m, int circle)
    {
        Item item = (Utility.Random(8) * (circle + 1)) switch
        {
            0  => new ClumsyScroll(),
            1  => new CreateFoodScroll(),
            2  => new FeeblemindScroll(),
            3  => new HealScroll(),
            4  => new MagicArrowScroll(),
            5  => new NightSightScroll(),
            6  => new ReactiveArmorScroll(),
            7  => new WeakenScroll(),
            8  => new AgilityScroll(),
            9  => new CunningScroll(),
            10 => new CureScroll(),
            11 => new HarmScroll(),
            12 => new MagicTrapScroll(),
            13 => new MagicUnTrapScroll(),
            14 => new ProtectionScroll(),
            15 => new StrengthScroll(),
            16 => new BlessScroll(),
            17 => new FireballScroll(),
            18 => new MagicLockScroll(),
            19 => new PoisonScroll(),
            20 => new TelekinesisScroll(),
            21 => new TeleportScroll(),
            22 => new UnlockScroll(),
            _  => new WallOfStoneScroll()
        };

        m.PackItem(item);
    }

    private static void PackTinkerPart(this Mobile m)
    {
        Item item = Utility.Random(4) switch
        {
            0 => new Axle(),
            1 => new Gears(),
            2 => new Hinge(),
            3 => new Springs()
        };

        m.PackItem(item);
    }

    private static Item NecroHue(Item item)
    {
        item.Hue = 0x2C3;

        return item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Item Robe(int raceFlag, bool female, int hue) =>
        raceFlag switch
        {
            Race.AllowElvesOnly when female => new FemaleElvenRobe(hue),
            Race.AllowElvesOnly             => new MaleElvenRobe(hue),
            _                               => new Robe(hue)
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Item SwordsWeapon(int raceFlag) =>
        raceFlag switch
        {
            Race.AllowElvesOnly     => new RuneBlade(),
            Race.AllowGargoylesOnly => new DreadSword(),
            _                       => new Katana()
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Item MacingWeapon(int raceFlag) =>
        raceFlag switch
        {
            Race.AllowElvesOnly     => new DiamondMace(),
            Race.AllowGargoylesOnly => new DiscMace(),
            _                       => new Club()
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Item FencingWeapon(int raceFlag) =>
        raceFlag switch
        {
            Race.AllowElvesOnly     => new Leafblade(),
            Race.AllowGargoylesOnly => new BloodBlade(),
            _                       => new Kryss()
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Item RangedWeapon(int raceFlag) =>
        raceFlag switch
        {
            Race.AllowElvesOnly     => new ElvenCompositeLongbow(),
            Race.AllowGargoylesOnly => new SerpentstoneStaff(),
            _                       => new Bow()
        };

    private static void AddSkillItems(this Mobile m, SkillName skill)
    {
        var raceFlag = m.Race.RaceFlag;
        var elf = m.Race == Race.Elf;
        var human = m.Race == Race.Human;
        var gargoyle = m.Race == Race.Gargoyle;
        var elfOrHuman = Race.IsAllowedRace(m.Race, Race.AllowHumanOrElves);
        var female = m.Female;

        switch (skill)
        {
            case SkillName.Alchemy:
                {
                    m.PackItem(new Bottle(4));
                    m.PackItem(new MortarPestle());
                    EquipItem(m, Robe(raceFlag, female, Utility.RandomPinkHue()));

                    break;
                }
            case SkillName.Anatomy:
                {
                    m.PackItem(new Bandage(3));
                    EquipItem(m, Robe(raceFlag, female, Utility.RandomYellowHue()));

                    break;
                }
            case SkillName.AnimalLore:
                {
                    if (elf)
                    {
                        EquipItem(m, new WildStaff());
                    }
                    else
                    {
                        EquipItem(m, new ShepherdsCrook());
                    }

                    EquipItem(m, Robe(raceFlag, female, Utility.RandomGreenHue()));

                    break;
                }
            case SkillName.AnimalTaming:
                {
                    if (human)
                    {
                        EquipItem(m, new ShepherdsCrook());
                    }

                    break;
                }
            case SkillName.Archery:
                {
                    m.PackItem(new Arrow(25));

                    EquipItem(m, RangedWeapon(raceFlag));

                    break;
                }
            case SkillName.ArmsLore:
                {
                    Item item = Utility.Random(3) switch
                    {
                        0 => FencingWeapon(raceFlag),
                        1 => MacingWeapon(raceFlag),
                        _ => SwordsWeapon(raceFlag)
                    };
                    EquipItem(m, item);

                    break;
                }
            case SkillName.Begging:
                {
                    Item staff = raceFlag switch
                    {
                        Race.AllowElvesOnly     => new WildStaff(),
                        Race.AllowGargoylesOnly => new GlassStaff(),
                        _                       => new GnarledStaff()
                    };

                    EquipItem(m, staff);
                    break;
                }
            case SkillName.Blacksmith:
                {
                    m.PackItem(new Tongs());
                    m.PackItem(new Pickaxe());
                    m.PackItem(new Pickaxe());
                    m.PackItem(new IronIngot(50));
                    EquipItem(m, new HalfApron(Utility.RandomYellowHue()));
                    break;
                }
            case SkillName.Bushido:
                {
                    if (elfOrHuman)
                    {
                        // Delete pants
                        m.FindItemOnLayer(Layer.OuterLegs)?.Delete();

                        EquipItem(m, new Hakama());
                        EquipItem(m, new Kasa());
                    }

                    EquipItem(m, new BookOfBushido());
                    break;
                }
            case SkillName.Fletching:
                {
                    m.PackItem(new Board(14));
                    m.PackItem(new Feather(5));
                    m.PackItem(new Shaft(5));
                    break;
                }
            case SkillName.Camping:
                {
                    m.PackItem(new Bedroll());
                    m.PackItem(new Kindling(5));
                    break;
                }
            case SkillName.Carpentry:
                {
                    m.PackItem(new Board(10));
                    m.PackItem(new Saw());

                    if (elfOrHuman)
                    {
                        EquipItem(m, new HalfApron(Utility.RandomYellowHue()));
                    }

                    break;
                }
            case SkillName.Cartography:
                {
                    m.PackItem(new BlankMap());
                    m.PackItem(new BlankMap());
                    m.PackItem(new BlankMap());
                    m.PackItem(new BlankMap());
                    m.PackItem(new Sextant());
                    break;
                }
            case SkillName.Cooking:
                {
                    m.PackItem(new Kindling(2));
                    m.PackItem(new RawLambLeg());
                    m.PackItem(new RawChickenLeg());
                    m.PackItem(new RawFishSteak());
                    m.PackItem(new SackFlour());
                    m.PackItem(new Pitcher(BeverageType.Water));
                    break;
                }
            case SkillName.Chivalry:
                {
                    m.PackItem(new BookOfChivalry());
                    break;
                }
            case SkillName.DetectHidden:
                {
                    if (elfOrHuman)
                    {
                        EquipItem(m, new Cloak(0x455));
                    }

                    break;
                }
            case SkillName.Discordance:
                {
                    m.PackInstrument();
                    break;
                }
            case SkillName.Fencing:
                {
                    EquipItem(m, FencingWeapon(raceFlag));
                    break;
                }
            case SkillName.Fishing:
                {
                    EquipItem(m, new FishingPole());

                    var hue = Utility.RandomYellowHue();
                    if (elf)
                    {
                        EquipItem(m, new Circlet { Hue = hue });
                    }
                    else if (human)
                    {
                        EquipItem(m, new FloppyHat(hue));
                    }

                    break;
                }
            case SkillName.Healing:
                {
                    m.PackItem(new Bandage(50));
                    m.PackItem(new Scissors());
                    break;
                }
            case SkillName.Herding:
                {
                    EquipItem(m, new ShepherdsCrook());
                    break;
                }
            case SkillName.Hiding:
                {
                    if (elfOrHuman)
                    {
                        EquipItem(m, new Cloak(0x455));
                    }

                    break;
                }
            case SkillName.Inscribe:
                {
                    m.PackItem(new BlankScroll(2));
                    m.PackItem(new BlueBook());
                    break;
                }
            case SkillName.ItemID:
                {
                    Item staff = raceFlag switch
                    {
                        Race.AllowElvesOnly     => new WildStaff(),
                        Race.AllowGargoylesOnly => new SerpentstoneStaff(),
                        _                       => new GnarledStaff()
                    };

                    EquipItem(m, staff);
                    break;
                }
            case SkillName.Lockpicking:
                {
                    m.PackItem(new Lockpick(20));
                    break;
                }
            case SkillName.Lumberjacking:
                {
                    EquipItem(m, elfOrHuman ? new Hatchet() : new DualShortAxes());
                    break;
                }
            case SkillName.Macing:
                {
                    EquipItem(m, MacingWeapon(raceFlag));
                    break;
                }
            case SkillName.Magery:
                {
                    var regs = new BagOfReagents(30) { LootType = LootType.Regular };

                    if (!Core.AOS)
                    {
                        foreach (var item in regs.Items)
                        {
                            item.LootType = LootType.Newbied;
                        }
                    }

                    m.PackItem(regs);
                    m.PackScroll(0);
                    m.PackScroll(1);
                    m.PackScroll(2);

                    EquipItem(m, new Spellbook(0x382A8C38ul) { LootType = LootType.Blessed });
                    EquipItem(m, Robe(raceFlag, female, Utility.RandomBlueHue()));

                    if (elf)
                    {
                        EquipItem(m, new Circlet());
                    }
                    else if (human)
                    {
                        EquipItem(m, new WizardsHat());
                    }

                    break;
                }
            case SkillName.Mining:
                {
                    m.PackItem(new Pickaxe());
                    break;
                }
            case SkillName.Musicianship:
                {
                    m.PackInstrument();
                    break;
                }
            case SkillName.Necromancy:
                {
                    if (Core.ML)
                    {
                        m.PackItem(new BagOfNecroReagents { LootType = LootType.Regular });
                    }

                    break;
                }
            case SkillName.Ninjitsu:
                {
                    if (elfOrHuman)
                    {
                        // Delete pants
                        m.FindItemOnLayer(Layer.OuterLegs)?.Delete();

                        EquipItem(m, new Hakama(0x2C3)); // Only ninjas get the hued one.
                        EquipItem(m, new Kasa());
                    }

                    EquipItem(m, new BookOfNinjitsu());
                    break;
                }
            case SkillName.Parry:
                {
                    Item shield = raceFlag switch
                    {
                        Race.AllowGargoylesOnly => new GargishWoodenShield(),
                        _                       => new WoodenShield()
                    };
                    EquipItem(m, shield);

                    break;
                }
            case SkillName.Peacemaking:
                {
                    m.PackInstrument();
                    break;
                }
            case SkillName.Poisoning:
                {
                    m.PackItem(new LesserPoisonPotion());
                    m.PackItem(new LesserPoisonPotion());
                    break;
                }
            case SkillName.Provocation:
                {
                    m.PackInstrument();
                    break;
                }
            case SkillName.Stealing:
            case SkillName.Snooping:
                {
                    m.PackItem(new Lockpick(20));
                    break;
                }
            case SkillName.SpiritSpeak:
                {
                    EquipItem(m, new Cloak(0x455));
                    break;
                }
            case SkillName.Tactics:
            case SkillName.Swords:
                {
                    EquipItem(m, SwordsWeapon(raceFlag));

                    break;
                }
            case SkillName.Tailoring:
                {
                    m.PackItem(new BoltOfCloth());
                    m.PackItem(new SewingKit());
                    break;
                }
            case SkillName.Tinkering:
                {
                    if (!Core.AOS)
                    {
                        m.PackTinkerPart();
                        m.PackTinkerPart();
                        m.PackTinkerPart();
                    }
                    m.PackItem(new TinkerTools());
                    break;
                }
            case SkillName.Tracking:
                {
                    if (elfOrHuman)
                    {
                        // Delete shoes
                        m.FindItemOnLayer(Layer.Shoes)?.Delete();

                        var hue = Utility.RandomYellowHue();
                        EquipItem(m, elf ? new ElvenBoots(hue) : new Boots(hue));
                    }

                    EquipItem(m, new SkinningKnife());
                    break;
                }
            case SkillName.Veterinary:
                {
                    m.PackItem(new Bandage(5));
                    m.PackItem(new Scissors());
                    break;
                }
            case SkillName.Wrestling:
                {
                    Item item = raceFlag switch
                    {
                        Race.AllowElvesOnly                   => new LeafGloves(),
                        Race.AllowGargoylesOnly when m.Female => new GargishLeatherArmsType2(),
                        Race.AllowGargoylesOnly               => new GargishLeatherArmsType1(),
                        _                                     => new LeatherGloves()
                    };
                    EquipItem(m, item);
                    break;
                }
            case SkillName.Throwing:
                {
                    if (gargoyle)
                    {
                        EquipItem(m, new Boomerang());
                    }

                    break;
                }
            case SkillName.Mysticism:
                {
                    // PackItem(new MysticBook(0xAB));
                    break;
                }
        }
    }
}
