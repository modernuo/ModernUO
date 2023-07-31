using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Accounting;
using Server.Items;
using Server.Logging;
using Server.Maps;
using Server.Mobiles;
using Server.Network;

namespace Server.Misc;

public static class CharacterCreation
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(CharacterCreation));

    // Allowed skills that are not race or era specific
    private static readonly HashSet<SkillName> _allowedStartingSkills = new()
    {
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
    };

    private static readonly TimeSpan BadStartMessageDelay = TimeSpan.FromSeconds(3.5);

    private static readonly CityInfo _newHavenInfo =
        new("New Haven", "The Bountiful Harvest Inn", 3503, 2574, 14, Map.Trammel);

    public static void Initialize()
    {
        // Register our event handler
        EventSink.CharacterCreated += EventSink_CharacterCreated;
    }

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

    private static void EventSink_CharacterCreated(CharacterCreatedEventArgs args)
    {
        if (!VerifyProfession(args.Profession))
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
        newChar.Race = Core.Expansion >= args.Race.RequiredExpansion ? args.Race : Race.DefaultRace;
        newChar.Hue = newChar.Race.ClipSkinHue(args.Hue & 0x3FFF) | 0x8000;
        newChar.Hunger = 20;

        var young = false;

        if (newChar is PlayerMobile pm)
        {
            pm.Profession = args.Profession;

            if (pm.AccessLevel == AccessLevel.Player && ((Account)pm.Account).Young)
            {
                young = pm.Young = true;
            }
        }

        SetName(newChar, args.Name);

        newChar.AddBackpack();

        SetStats(newChar, state, args.Stats, args.Profession);
        SetSkills(newChar, args.Skills, args.Profession, args.ShirtHue, args.PantsHue);

        var race = newChar.Race;

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

        if (young)
        {
            var ticket = new NewPlayerTicket();
            ticket.Owner = newChar;
            newChar.BankBox.DropItem(ticket);
        }

        var city = GetStartLocation(args, young);
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

    public static bool VerifyProfession(int profession) =>
        profession >= 0 && profession < ProfessionInfo.Professions.Length;

    private static CityInfo GetStartLocation(CharacterCreatedEventArgs args, bool isYoung)
    {
        // We don't get the actual client version until after character creation
        var post6000Supported = !TileMatrix.Pre6000ClientSupport;
        var availableMaps = ExpansionInfo.CoreExpansion.MapSelectionFlags;

        if (Core.ML && post6000Supported && availableMaps.Includes(MapSelectionFlags.Trammel))
        {
            return _newHavenInfo;
        }

        var useHaven = isYoung;

        var flags = args.State?.Flags ?? ClientFlags.None;
        var m = args.Mobile;

        var profession = ProfessionInfo.Professions[args.Profession];

        switch (profession?.Name.ToLowerInvariant())
        {
            case "necromancer":
                {
                    if ((flags & ClientFlags.Malas) != 0 && availableMaps.Includes(MapSelectionFlags.Malas))
                    {
                        return new CityInfo("Umbra", "Mardoth's Tower", 2114, 1301, -50, Map.Malas);
                    }

                    useHaven = true;

                    /*
                     * Unfortunately you are playing on a *NON-Age-Of-Shadows* game
                     * installation and cannot be transported to Malas.
                     * You will not be able to take your new player quest in Malas
                     * without an AOS client.  You are now being taken to the city of
                     * Haven on the Trammel facet.
                     */
                    Timer.StartTimer(BadStartMessageDelay, () => m.SendLocalizedMessage(1062205));

                    break;
                }
            case "paladin":
                {
                    if (availableMaps.Includes(MapSelectionFlags.Trammel) && post6000Supported)
                    {
                        return _newHavenInfo;
                    }

                    break;
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

                    useHaven = true;

                    /*
                     * Unfortunately you are playing on a *NON-Samurai-Empire* game
                     * installation and cannot be transported to Tokuno.
                     * You will not be able to take your new player quest in Tokuno
                     * without an SE client. You are now being taken to the city of
                     * Haven on the Trammel facet.
                     */
                    Timer.StartTimer(BadStartMessageDelay, () => m.SendLocalizedMessage(1063487));

                    break;
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

                    useHaven = true;

                    /*
                     * Unfortunately you are playing on a *NON-Samurai-Empire* game
                     * installation and cannot be transported to Tokuno.
                     * You will not be able to take your new player quest in Tokuno
                     * without an SE client. You are now being taken to the city of
                     * Haven on the Trammel facet.
                     */
                    Timer.StartTimer(BadStartMessageDelay, () => m.SendLocalizedMessage(1063487));

                    break;
                }
        }

        if (post6000Supported && useHaven && availableMaps.Includes(MapSelectionFlags.Trammel))
        {
            // New Haven is supported, so put them there...
            // Note: if your server maps don't contain New Haven, this will place
            // them in the wilderness of Ocllo
            return _newHavenInfo;
        }

        if (useHaven)
        {
            // New Haven is not available, so place them in Ocllo instead, if they're aiming for Haven
            CityInfo oclloBank = new CityInfo("Ocllo", "Near the bank", 3677, 2513, -1, Map.Trammel);
            if (availableMaps.Includes(MapSelectionFlags.Trammel))
            {
                return oclloBank;
            }

            if (availableMaps.Includes(MapSelectionFlags.Felucca))
            {
                oclloBank.Map = Map.Felucca;
                return oclloBank;
            }
        }

        // They're not trying to get to Haven, so use their city selection
        // instead - adjusted according to available maps
        if (args.City.Map == Map.Trammel && !availableMaps.Includes(MapSelectionFlags.Trammel))
        {
            args.City.Map = Map.Felucca;
        }

        if (args.City.Map == Map.Felucca && !availableMaps.Includes(MapSelectionFlags.Felucca))
        {
            args.City.Map = Map.Trammel;
        }

        return args.City;
    }

    private static void SetStats(Mobile m, NetState state, StatNameValue[] stats, int prof)
    {
        var maxStats = state.NewCharacterCreation ? 90 : 80;

        var str = 0;
        var dex = 0;
        var intel = 0;

        if (prof > 0)
        {
            stats = ProfessionInfo.Professions[prof]?.Stats ?? stats;
        }

        for (var i = 0; i < stats.Length; i++)
        {
            var (statType, value) = stats[i];
            switch (statType)
            {
                case StatType.Str: str = value; break;
                case StatType.Dex: dex = value; break;
                case StatType.Int: intel = value; break;
            }
        }

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

        if (!NameVerification.Validate(name, 2, 16, true, false, true, 1, NameVerification.SpaceDashPeriodQuote))
        {
            name = "Generic Player";
        }

        m.Name = name;
    }

    private static bool ValidateSkills(int raceFlag, SkillNameValue[] skills)
    {
        var total = 0;

        for (var i = 0; i < skills.Length; ++i)
        {
            var (name, value) = skills[i];
            var notValid = value is < 0 or > 50 || !_allowedStartingSkills.Contains(name) ||
                           !Core.AOS && name is SkillName.Necromancy or SkillName.Chivalry or SkillName.Focus ||
                           !Core.SE && name is SkillName.Ninjitsu or SkillName.Bushido ||
                           Core.SA && (raceFlag == Race.AllowGargoylesOnly && name == SkillName.Archery ||
                                       raceFlag != Race.AllowGargoylesOnly && name == SkillName.Throwing) ||
                           !Core.SA && name is SkillName.Throwing or SkillName.Imbuing;

            if (notValid)
            {
                skills[i] = default;
                continue;
            }

            total += value;

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

    private static void SetSkills(Mobile m, SkillNameValue[] skills, int prof, int shirtHue, int pantsHue)
    {
        ProfessionInfo profession = null;
        if (prof > 0)
        {
            profession = ProfessionInfo.Professions[prof];
            skills = ProfessionInfo.Professions[prof]?.Skills ?? skills;
        }
        else if (!ValidateSkills(m.Race.RaceFlag, skills)) // This does not check for skills that are not allowed by expansion
        {
            return;
        }

        var addSkillItems = true;
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

                    addSkillItems = false;

                    break;
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

                    addSkillItems = false;

                    break;
                }
            case "samurai":
                {
                    addSkillItems = false;

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

                    break;
                }
            case "ninja":
                {
                    addSkillItems = false;

                    int[] hues = { 0x1A8, 0xEC, 0x99, 0x90, 0xB5, 0x336, 0x89 };
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
                    break;
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

        if (addSkillItems)
        {
            m.AddShirt(shirtHue);
            m.AddPants(pantsHue);
            m.AddShoes();

            // All elves get a wild staff
            if (elf)
            {
                EquipItem(m, new WildStaff());
            }
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
