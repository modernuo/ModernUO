using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Accounting;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Network;

namespace Server.Misc
{
    public static class CharacterCreation
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(CharacterCreation));

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
            SkillName.Inscribe,
            SkillName.ItemID,
            SkillName.Lockpicking,
            SkillName.Lumberjacking,
            SkillName.Macing,
            SkillName.Magery,
            SkillName.Meditation,
            SkillName.Mining,
            SkillName.Musicianship,
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
            SkillName.Tinkering,
            SkillName.Tracking,
            SkillName.Veterinary,
            SkillName.Wrestling
        };

        private static readonly TimeSpan BadStartMessageDelay = TimeSpan.FromSeconds(3.5);

        private static readonly CityInfo _newHavenInfo =
            new("New Haven", "The Bountiful Harvest Inn", 3503, 2574, 14, Map.Trammel);

        private static Mobile m_Mobile;

        public static void Initialize()
        {
            // Register our event handler
            EventSink.CharacterCreated += EventSink_CharacterCreated;
        }

        private static void AddBackpack(Mobile m)
        {
            var pack = m.Backpack;

            if (pack == null)
            {
                pack = new Backpack();
                pack.Movable = false;

                m.AddItem(pack);
            }

            PackItem(new RedBook("a book", m.Name, 20, true));
            PackItem(new Gold(1000)); // Starting gold can be customized here
            PackItem(new Dagger());
            PackItem(new Candle());
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
                logger.Information("Login: {0}: Character creation failed, account full", state);
                return;
            }

            args.Mobile = newChar;
            m_Mobile = newChar;

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

            AddBackpack(newChar);

            SetStats(newChar, state, args.Str, args.Dex, args.Int);
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
            if (Core.ML)
            {
                return _newHavenInfo; // We don't get the client Version until AFTER Character creation
            }

            var useHaven = isYoung;

            var flags = args.State?.Flags ?? ClientFlags.None;
            var m = args.Mobile;

            var profession = ProfessionInfo.Professions[args.Profession];

            switch (profession.Name.ToLowerInvariant())
            {
                case "necromancer":
                    {
                        if ((flags & ClientFlags.Malas) != 0)
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
                        return _newHavenInfo;
                    }
                case "samurai":
                    {
                        if ((flags & ClientFlags.Tokuno) != 0)
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
                        if ((flags & ClientFlags.Tokuno) != 0)
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

            return useHaven ? _newHavenInfo : args.City;
        }

        private static void FixStats(ref int str, ref int dex, ref int intel, int max)
        {
            var vMax = max - 30;

            var vStr = Math.Max(str - 10, 0);
            var vDex = Math.Max(dex - 10, 0);
            var vInt = Math.Max(intel - 10, 0);

            var total = vStr + vDex + vInt;

            if (total == 0 || total == vMax)
            {
                return;
            }

            var scalar = vMax / (double)total;

            vStr = (int)(vStr * scalar);
            vDex = (int)(vDex * scalar);
            vInt = (int)(vInt * scalar);

            FixStat(ref vStr, vStr + vDex + vInt - vMax, vMax);
            FixStat(ref vDex, vStr + vDex + vInt - vMax, vMax);
            FixStat(ref vInt, vStr + vDex + vInt - vMax, vMax);

            str = vStr + 10;
            dex = vDex + 10;
            intel = vInt + 10;
        }

        private static void FixStat(ref int stat, int diff, int max)
        {
            stat = Math.Clamp(stat + diff, 0, max);
        }

        private static void SetStats(Mobile m, NetState state, int str, int dex, int intel)
        {
            var max = state.NewCharacterCreation ? 90 : 80;

            FixStats(ref str, ref dex, ref intel, max);

            if (str is < 10 or > 60 || dex is < 10 or > 60 || intel is < 10 or > 60 || str + dex + intel != max)
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

        private static bool ValidSkills(SkillNameValue[] skills)
        {
            var total = 0;

            for (var i = 0; i < skills.Length; ++i)
            {
                var (name, value) = skills[i];

                if (value is < 0 or > 50)
                {
                    return false;
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
                skills = ProfessionInfo.Professions[prof].Skills;
            }
            else if (!ValidSkills(skills)) // This does not check for skills that are not allowed by expansion
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

                        PackItem(regs);

                        EquipItem(new BoneHelm());

                        if (elf)
                        {
                            EquipItem(new ElvenMachete());
                            EquipItem(NecroHue(new LeafChest()));
                            EquipItem(NecroHue(new LeafArms()));
                            EquipItem(NecroHue(new LeafGloves()));
                            EquipItem(NecroHue(new LeafGorget()));
                            EquipItem(NecroHue(new LeafLegs()));
                            EquipItem(new ElvenBoots());
                        }
                        else if (gargoyle)
                        {
                            EquipItem(new GlassSword());
                            EquipItem(NecroHue(m.Female ? new GargishLeatherChestType2() : new GargishLeatherChestType1()));
                            EquipItem(NecroHue(m.Female ? new GargishLeatherArmsType2() : new GargishLeatherArmsType1()));
                            EquipItem(NecroHue(m.Female ? new GargishLeatherKiltType2() : new GargishLeatherKiltType1()));
                            EquipItem(NecroHue(m.Female ? new GargishLeatherLegsType2() : new GargishLeatherLegsType1()));
                        }
                        else
                        {
                            EquipItem(new BoneHarvester());
                            EquipItem(NecroHue(new LeatherChest()));
                            EquipItem(NecroHue(new LeatherArms()));
                            EquipItem(NecroHue(new LeatherGloves()));
                            EquipItem(NecroHue(new LeatherGorget()));
                            EquipItem(NecroHue(new LeatherLegs()));
                            EquipItem(NecroHue(new Skirt()));
                            EquipItem(new Sandals(0x8FD));
                        }

                        // animate dead, evil omen, pain spike, summon familiar, wraith form
                        PackItem(new NecromancerSpellbook(0x8981ul) { LootType = LootType.Blessed });

                        addSkillItems = false;

                        break;
                    }
                case "paladin":
                    {
                        if (elf)
                        {
                            EquipItem(new ElvenMachete());
                            EquipItem(new WingedHelm());
                            EquipItem(new LeafGorget());
                            EquipItem(new LeafArms());
                            EquipItem(new LeafChest());
                            EquipItem(new LeafLegs());
                            EquipItem(new LeafGloves());
                            EquipItem(new ElvenBoots()); // Verify hue
                        }
                        else if (gargoyle)
                        {
                            EquipItem(new GlassSword());
                            EquipItem(m.Female ? new GargishStoneChestType2() : new GargishStoneChestType1());
                            EquipItem(m.Female ? new GargishStoneArmsType2() : new GargishStoneArmsType1());
                            EquipItem(m.Female ? new GargishStoneKiltType2() : new GargishStoneKiltType1());
                            EquipItem(m.Female ? new GargishStoneLegsType2() : new GargishStoneLegsType1());
                        }
                        else
                        {
                            EquipItem(new Broadsword());
                            EquipItem(new Helmet());
                            EquipItem(new PlateGorget());
                            EquipItem(new RingmailArms());
                            EquipItem(new RingmailChest());
                            EquipItem(new RingmailLegs());
                            EquipItem(new RingmailGloves());
                            EquipItem(new ThighBoots(0x748));
                            EquipItem(new Cloak(0xCF));
                            EquipItem(new BodySash(0xCF));
                        }

                        PackItem(new BookOfChivalry { LootType = LootType.Blessed });

                        addSkillItems = false;

                        break;
                    }
                case "samurai":
                    {
                        addSkillItems = false;

                        if (elf)
                        {
                            EquipItem(new RavenHelm());
                            EquipItem(new HakamaShita(0x2C3));
                            EquipItem(new Hakama(0x2C3));
                            EquipItem(new SamuraiTabi(0x2C3));
                            EquipItem(new TattsukeHakama(0x22D));
                            EquipItem(new Bokuto());
                        }
                        else if (gargoyle)
                        {
                            EquipItem(new GargishTalwar());
                            EquipItem(m.Female ? new GargishLeatherChestType2() : new GargishLeatherChestType1());
                            EquipItem(m.Female ? new GargishLeatherArmsType2() : new GargishLeatherArmsType1());
                            EquipItem(m.Female ? new GargishLeatherKiltType2() : new GargishLeatherKiltType1());
                            EquipItem(m.Female ? new GargishLeatherLegsType2() : new GargishLeatherLegsType1());
                        }
                        else
                        {
                            EquipItem(new LeatherJingasa());
                            EquipItem(new HakamaShita(0x2C3));
                            EquipItem(new Hakama(0x2C3));
                            EquipItem(new SamuraiTabi(0x2C3));
                            EquipItem(new TattsukeHakama(0x22D));
                            EquipItem(new Bokuto());
                        }

                        PackItem(new Scissors());
                        PackItem(new Bandage(50));

                        Spellbook book = new BookOfBushido();
                        PackItem(book);

                        break;
                    }
                case "ninja":
                    {
                        addSkillItems = false;

                        int[] hues = { 0x1A8, 0xEC, 0x99, 0x90, 0xB5, 0x336, 0x89 };
                        // TODO: Verify that's ALL the hues for that above.

                        if (elf)
                        {
                            EquipItem(new AssassinSpike());
                            EquipItem(new TattsukeHakama(hues.RandomElement()));
                            EquipItem(new HakamaShita(0x2C3));
                            EquipItem(new NinjaTabi(0x2C3));
                            EquipItem(new Kasa());
                        }
                        else if (gargoyle)
                        {
                            //EquipItem(new DualPointedSpear()); //IMPLEMENTATION NEEDED
                            EquipItem(m.Female ? new GargishLeatherChestType2() : new GargishLeatherChestType1());
                            EquipItem(m.Female ? new GargishLeatherArmsType2() : new GargishLeatherArmsType1());
                            EquipItem(m.Female ? new GargishLeatherKiltType2() : new GargishLeatherKiltType1());
                            EquipItem(m.Female ? new GargishLeatherLegsType2() : new GargishLeatherLegsType1());
                        }
                        else
                        {
                            EquipItem(new Tekagi());
                            EquipItem(new TattsukeHakama(hues.RandomElement()));
                            EquipItem(new HakamaShita(0x2C3));
                            EquipItem(new NinjaTabi(0x2C3));
                            EquipItem(new Kasa());
                        }

                        PackItem(new SmokeBomb());
                        PackItem(new SmokeBomb());
                        PackItem(new SmokeBomb());
                        PackItem(new SmokeBomb());
                        PackItem(new SmokeBomb());
                        PackItem(new BookOfNinjitsu());
                        break;
                    }
                case "swordsman":
                case "fencer":
                case "warrior":
                case "mace fighter":
                    {
                        if (elf)
                        {
                            EquipItem(new Circlet());
                            EquipItem(new HideGorget());
                            EquipItem(new HideChest());
                            EquipItem(new HidePauldrons());
                            EquipItem(new HideGloves());
                            EquipItem(new HidePants());
                            EquipItem(new ElvenBoots());
                        }
                        else if (gargoyle)
                        {
                            EquipItem(m.Female ? new GargishLeatherChestType2() : new GargishLeatherChestType1());
                            EquipItem(m.Female ? new GargishLeatherArmsType2() : new GargishLeatherArmsType1());
                            EquipItem(m.Female ? new GargishLeatherKiltType2() : new GargishLeatherKiltType1());
                            EquipItem(m.Female ? new GargishLeatherLegsType2() : new GargishLeatherLegsType1());
                        }
                        else
                        {
                            EquipItem(new Bascinet());
                            EquipItem(new StuddedGorget());
                            EquipItem(new StuddedChest());
                            EquipItem(new StuddedArms());
                            EquipItem(new StuddedGloves());
                            EquipItem(new StuddedLegs());
                            EquipItem(new ThighBoots());
                        }
                        break;
                    }
            }

            if (addSkillItems)
            {
                AddShirt(m, shirtHue);
                AddPants(m, pantsHue);
                AddShoes(m);
            }

            for (var i = 0; i < skills.Length; ++i)
            {
                var (name, value) = skills[i];

                if (value > 0 && (prof > 0 || _allowedStartingSkills.Contains(name)))
                {
                    var skill = m.Skills[name];

                    if (skill != null)
                    {
                        skill.BaseFixedPoint = value * 10;

                        if (addSkillItems)
                        {
                            AddSkillItems(name, m);
                        }
                    }
                }
            }
        }

        private static void EquipItem(Item item, bool mustEquip = false)
        {
            if (item == null)
            {
                return;
            }

            if (!Core.AOS)
            {
                item.LootType = LootType.Newbied;
            }

            if (m_Mobile?.EquipItem(item) == true)
            {
                return;
            }

            var pack = m_Mobile?.Backpack;

            if (!mustEquip && pack != null)
            {
                pack.DropItem(item);
            }
            else
            {
                item.Delete();
            }
        }

        private static void PackItem(Item item)
        {
            if (!Core.AOS)
            {
                item.LootType = LootType.Newbied;
            }

            var pack = m_Mobile.Backpack;

            if (pack != null)
            {
                pack.DropItem(item);
            }
            else
            {
                item.Delete();
            }
        }

        private static void AddShirt(Mobile m, int shirtHue)
        {
            var hue = Utility.ClipDyedHue(shirtHue & 0x3FFF);

            if (m.Race == Race.Elf)
            {
                EquipItem(new ElvenShirt(hue), true);
            }
            else
            {
                Item shirt = Utility.Random(3) switch
                {
                    0 => new Shirt(hue),
                    1 => new FancyShirt(hue),
                    _ => new Doublet(hue)
                };

                EquipItem(shirt, true);
            }
        }

        private static void AddPants(Mobile m, int pantsHue)
        {
            var hue = Utility.ClipDyedHue(pantsHue & 0x3FFF);

            if (m.Race == Race.Elf)
            {
                EquipItem(new ElvenPants(hue), true);
            }
            else
            {
                var female = m.Female;
                Item pants = Utility.RandomBool() switch
                {
                    true when female  => new Skirt(hue),
                    true              => new LongPants(hue),
                    false when female => new Kilt(hue),
                    false             => new ShortPants(hue)
                };

                EquipItem(pants, true);
            }
        }

        private static void AddShoes(Mobile m)
        {
            if (m.Race == Race.Elf)
            {
                EquipItem(new ElvenBoots(), true);
            }
            else
            {
                EquipItem(new Shoes(Utility.RandomYellowHue()), true);
            }
        }

        private static void PackInstrument()
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

            PackItem(instrument);
        }

        private static void PackScroll(int circle)
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

            PackItem(item);
        }

        private static Item NecroHue(Item item)
        {
            item.Hue = 0x2C3;

            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EquipRobe(Mobile m, int hue)
        {
            if (Race.IsAllowedRace(m.Race, Race.AllowElvesOnly))
            {
                EquipItem(m.Female ? new FemaleElvenRobe(hue) : new MaleElvenRobe(hue));
                return;
            }

            EquipItem(new Robe(hue));
        }

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
                Race.AllowGargoylesOnly => null, // new DiscMace(),
                _                       => new Club()
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Item FencingWeapon(int raceFlag) =>
            raceFlag switch
            {
                Race.AllowElvesOnly     => new Leafblade(),
                Race.AllowGargoylesOnly => null, // new BloodBlade(),
                _                       => new Kryss()
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Item StaffWeapon(int raceFlag) =>
            raceFlag switch
            {
                Race.AllowElvesOnly     => new WildStaff(),
                Race.AllowGargoylesOnly => null, // new SerpentStoneStaff(),
                _                       => new GnarledStaff()
            };

        private static void AddSkillItems(SkillName skill, Mobile m)
        {
            var raceFlag = m.Race.RaceFlag;
            var human = m.Race == Race.Human;
            var elf = m.Race == Race.Elf;
            var gargoyle = m.Race == Race.Gargoyle;
            var elfOrHuman = elf || human;

            switch (skill)
            {
                case SkillName.Alchemy:
                    {
                        PackItem(new Bottle(4));
                        PackItem(new MortarPestle());
                        EquipRobe(m, Utility.RandomPinkHue());

                        break;
                    }
                case SkillName.Anatomy:
                    {
                        PackItem(new Bandage(3));
                        EquipRobe(m, Utility.RandomYellowHue());

                        break;
                    }
                case SkillName.AnimalLore:
                    {
                        EquipItem(elf ? new WildStaff() : new ShepherdsCrook());
                        EquipRobe(m, Utility.RandomBlueHue());

                        break;
                    }
                case SkillName.Archery:
                    {
                        PackItem(new Arrow(25));

                        if (elf)
                        {
                            EquipItem(new ElvenCompositeLongbow());
                        }
                        else if (human)
                        {
                            EquipItem(new Bow());
                        }

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
                        EquipItem(item);

                        break;
                    }
                case SkillName.Begging:
                    {
                        EquipItem(StaffWeapon(raceFlag));
                        break;
                    }
                case SkillName.Blacksmith:
                    {
                        PackItem(new Tongs());
                        PackItem(new Pickaxe());
                        PackItem(new Pickaxe());
                        PackItem(new IronIngot(50));
                        EquipItem(new HalfApron(Utility.RandomYellowHue()));
                        break;
                    }
                case SkillName.Bushido:
                    {
                        if (elfOrHuman)
                        {
                            EquipItem(new Hakama());
                            EquipItem(new Kasa());
                        }

                        EquipItem(new BookOfBushido());
                        break;
                    }
                case SkillName.Fletching:
                    {
                        PackItem(new Board(14));
                        PackItem(new Feather(5));
                        PackItem(new Shaft(5));
                        break;
                    }
                case SkillName.Camping:
                    {
                        PackItem(new Bedroll());
                        PackItem(new Kindling(5));
                        break;
                    }
                case SkillName.Carpentry:
                    {
                        PackItem(new Board(10));
                        PackItem(new Saw());

                        if (elfOrHuman)
                        {
                            EquipItem(new HalfApron(Utility.RandomYellowHue()));
                        }

                        break;
                    }
                case SkillName.Cartography:
                    {
                        PackItem(new BlankMap());
                        PackItem(new BlankMap());
                        PackItem(new BlankMap());
                        PackItem(new BlankMap());
                        PackItem(new Sextant());
                        break;
                    }
                case SkillName.Cooking:
                    {
                        PackItem(new Kindling(2));
                        PackItem(new RawLambLeg());
                        PackItem(new RawChickenLeg());
                        PackItem(new RawFishSteak());
                        PackItem(new SackFlour());
                        PackItem(new Pitcher(BeverageType.Water));
                        break;
                    }
                case SkillName.Chivalry:
                    {
                        if (Core.ML)
                        {
                            PackItem(new BookOfChivalry());
                        }

                        break;
                    }
                case SkillName.DetectHidden:
                    {
                        if (elfOrHuman)
                        {
                            EquipItem(new Cloak(0x455));
                        }

                        break;
                    }
                case SkillName.Discordance:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Fencing:
                    {
                        EquipItem(FencingWeapon(raceFlag));
                        break;
                    }
                case SkillName.Fishing:
                    {
                        EquipItem(new FishingPole());

                        var hue = Utility.RandomYellowHue();
                        if (elf)
                        {
                            EquipItem(new Circlet { Hue = hue });

                        }
                        else if (human)
                        {
                            EquipItem(new FloppyHat(hue));
                        }

                        break;
                    }
                case SkillName.Healing:
                    {
                        PackItem(new Bandage(50));
                        PackItem(new Scissors());
                        break;
                    }
                case SkillName.Herding:
                    {
                        EquipItem(new ShepherdsCrook());
                        break;
                    }
                case SkillName.Hiding:
                    {
                        if (elfOrHuman)
                        {
                            EquipItem(new Cloak(0x455));
                        }

                        break;
                    }
                case SkillName.Inscribe:
                    {
                        PackItem(new BlankScroll(2));
                        PackItem(new BlueBook());
                        break;
                    }
                case SkillName.ItemID:
                    {
                        EquipItem(StaffWeapon(raceFlag));
                        break;
                    }
                case SkillName.Lockpicking:
                    {
                        PackItem(new Lockpick(20));
                        break;
                    }
                case SkillName.Lumberjacking:
                    {
                        EquipItem(elfOrHuman ? new Hatchet() : null); // new DualShortAxes()
                        break;
                    }
                case SkillName.Macing:
                    {
                        EquipItem(MacingWeapon(raceFlag));
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

                        PackItem(regs);

                        PackScroll(0);
                        PackScroll(1);
                        PackScroll(2);

                        EquipItem(new Spellbook(0x382A8C38ul) { LootType = LootType.Blessed });
                        EquipRobe(m, Utility.RandomBlueHue());

                        if (elf)
                        {
                            EquipItem(new Circlet());
                        }
                        else if (human)
                        {
                            EquipItem(new WizardsHat());
                        }
                        break;
                    }
                case SkillName.Mining:
                    {
                        PackItem(new Pickaxe());
                        break;
                    }
                case SkillName.Musicianship:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Necromancy:
                    {
                        if (Core.ML)
                        {
                            PackItem(new BagOfNecroReagents { LootType = LootType.Regular });
                        }

                        break;
                    }
                case SkillName.Ninjitsu:
                    {
                        if (elfOrHuman)
                        {
                            EquipItem(new Hakama(0x2C3)); // Only ninjas get the hued one.
                            EquipItem(new Kasa());
                        }

                        EquipItem(new BookOfNinjitsu());
                        break;
                    }
                case SkillName.Parry:
                    {
                        if (gargoyle)
                        {
                            // EquipItem(new GargishWoodenShield());
                        }
                        else
                        {
                            EquipItem(new WoodenShield());
                        }

                        break;
                    }
                case SkillName.Peacemaking:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Poisoning:
                    {
                        PackItem(new LesserPoisonPotion());
                        PackItem(new LesserPoisonPotion());
                        break;
                    }
                case SkillName.Provocation:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Stealing:
                case SkillName.Snooping:
                    {
                        PackItem(new Lockpick(20));
                        break;
                    }
                case SkillName.SpiritSpeak:
                    {
                        EquipItem(new Cloak(0x455));
                        break;
                    }
                case SkillName.Tactics:
                case SkillName.Swords:
                    {
                        EquipItem(SwordsWeapon(raceFlag));

                        break;
                    }
                case SkillName.Tailoring:
                    {
                        PackItem(new BoltOfCloth());
                        PackItem(new SewingKit());
                        break;
                    }
                case SkillName.Tinkering:
                    {
                        PackItem(new TinkerTools());
                        PackItem(new IronIngot(50));
                        EquipItem(new HalfApron(Utility.RandomYellowHue()));
                        break;
                    }
                case SkillName.Tracking:
                    {
                        if (elfOrHuman)
                        {
                            // Delete shoes
                            m_Mobile?.FindItemOnLayer(Layer.Shoes)?.Delete();

                            var hue = Utility.RandomYellowHue();
                            EquipItem(elf ? new ElvenBoots(hue) : new Boots(hue));
                        }

                        EquipItem(new SkinningKnife());
                        break;
                    }
                case SkillName.Veterinary:
                    {
                        PackItem(new Bandage(5));
                        PackItem(new Scissors());
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
                        EquipItem(item);
                        break;
                    }
                case SkillName.Throwing:
                    {
                        if (gargoyle)
                        {
                            // EquipItem(new Boomerang());
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
}
