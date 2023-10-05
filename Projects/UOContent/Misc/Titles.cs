using System;
using Server.Engines.CannedEvil;
using Server.Mobiles;
using Server.Text;

namespace Server.Misc
{
    public static class Titles
    {
        public const int MinFame = 0;
        public const int MaxFame = 15000;

        public const int MinKarma = -15000;
        public const int MaxKarma = 15000;

        private static readonly string[,] m_Levels =
        {
            { "Neophyte", "Neophyte", "Neophyte" },
            { "Novice", "Novice", "Novice" },
            { "Apprentice", "Apprentice", "Apprentice" },
            { "Journeyman", "Journeyman", "Journeyman" },
            { "Expert", "Expert", "Expert" },
            { "Adept", "Adept", "Adept" },
            { "Master", "Master", "Master" },
            { "Grandmaster", "Grandmaster", "Grandmaster" },
            { "Elder", "Tatsujin", "Shinobi" },
            { "Legendary", "Kengo", "Ka-ge" }
        };

        private static readonly FameEntry[] m_FameEntries =
        {
            new(
                1249,
                new[]
                {
                    new KarmaEntry(-10000, "The Outcast "),
                    new KarmaEntry(-5000, "The Despicable "),
                    new KarmaEntry(-2500, "The Scoundrel "),
                    new KarmaEntry(-1250, "The Unsavory "),
                    new KarmaEntry(-625, "The Rude "),
                    new KarmaEntry(624, ""),
                    new KarmaEntry(1249, "The Fair "),
                    new KarmaEntry(2499, "The Kind "),
                    new KarmaEntry(4999, "The Good "),
                    new KarmaEntry(9999, "The Honest "),
                    new KarmaEntry(10000, "The Trustworthy ")
                }
            ),
            new(
                2499,
                new[]
                {
                    new KarmaEntry(-10000, "The Wretched "),
                    new KarmaEntry(-5000, "The Dastardly "),
                    new KarmaEntry(-2500, "The Malicious "),
                    new KarmaEntry(-1250, "The Dishonorable "),
                    new KarmaEntry(-625, "The Disreputable "),
                    new KarmaEntry(624, "The Notable "),
                    new KarmaEntry(1249, "The Upstanding "),
                    new KarmaEntry(2499, "The Respectable "),
                    new KarmaEntry(4999, "The Honorable "),
                    new KarmaEntry(9999, "The Commendable "),
                    new KarmaEntry(10000, "The Estimable ")
                }
            ),
            new(
                4999,
                new[]
                {
                    new KarmaEntry(-10000, "The Nefarious "),
                    new KarmaEntry(-5000, "The Wicked "),
                    new KarmaEntry(-2500, "The Vile "),
                    new KarmaEntry(-1250, "The Ignoble "),
                    new KarmaEntry(-625, "The Notorious "),
                    new KarmaEntry(624, "The Prominent "),
                    new KarmaEntry(1249, "The Reputable "),
                    new KarmaEntry(2499, "The Proper "),
                    new KarmaEntry(4999, "The Admirable "),
                    new KarmaEntry(9999, "The Famed "),
                    new KarmaEntry(10000, "The Great ")
                }
            ),
            new(
                9999,
                new[]
                {
                    new KarmaEntry(-10000, "The Dread "),
                    new KarmaEntry(-5000, "The Evil "),
                    new KarmaEntry(-2500, "The Villainous "),
                    new KarmaEntry(-1250, "The Sinister "),
                    new KarmaEntry(-625, "The Infamous "),
                    new KarmaEntry(624, "The Renowned "),
                    new KarmaEntry(1249, "The Distinguished "),
                    new KarmaEntry(2499, "The Eminent "),
                    new KarmaEntry(4999, "The Noble "),
                    new KarmaEntry(9999, "The Illustrious "),
                    new KarmaEntry(10000, "The Glorious ")
                }
            ),
            new(
                10000,
                new[]
                {
                    new KarmaEntry(-10000, "The Dread "),
                    new KarmaEntry(-5000, "The Evil "),
                    new KarmaEntry(-2500, "The Dark "),
                    new KarmaEntry(-1250, "The Sinister "),
                    new KarmaEntry(-625, "The Dishonored "),
                    new KarmaEntry(624, ""),
                    new KarmaEntry(1249, "The Distinguished "),
                    new KarmaEntry(2499, "The Eminent "),
                    new KarmaEntry(4999, "The Noble "),
                    new KarmaEntry(9999, "The Illustrious "),
                    new KarmaEntry(10000, "The Glorious ")
                }
            )
        };

        public static void AwardFame(Mobile m, int offset, bool message)
        {
            if (offset > 0)
            {
                if (m.Fame >= MaxFame)
                {
                    return;
                }

                offset = Math.Max(offset - m.Fame / 100, 0);
            }
            else if (offset < 0)
            {
                if (m.Fame <= MinFame)
                {
                    return;
                }

                offset = Math.Min(offset - m.Fame / 100, 0);
            }

            offset = (m.Fame + offset) switch
            {
                > MaxFame => MaxFame - m.Fame,
                < MinFame => MinFame - m.Fame,
                _         => offset
            };

            m.Fame += offset;

            if (message)
            {
                if (offset > 40)
                {
                    m.SendLocalizedMessage(1019054); // You have gained a lot of fame.
                }
                else if (offset > 20)
                {
                    m.SendLocalizedMessage(1019053); // You have gained a good amount of fame.
                }
                else if (offset > 10)
                {
                    m.SendLocalizedMessage(1019052); // You have gained some fame.
                }
                else if (offset > 0)
                {
                    m.SendLocalizedMessage(1019051); // You have gained a little fame.
                }
                else if (offset < -40)
                {
                    m.SendLocalizedMessage(1019058); // You have lost a lot of fame.
                }
                else if (offset < -20)
                {
                    m.SendLocalizedMessage(1019057); // You have lost a good amount of fame.
                }
                else if (offset < -10)
                {
                    m.SendLocalizedMessage(1019056); // You have lost some fame.
                }
                else if (offset < 0)
                {
                    m.SendLocalizedMessage(1019055); // You have lost a little fame.
                }
            }
        }

        public static void AwardKarma(Mobile m, int offset, bool message)
        {
            var pm = m as PlayerMobile;

            if (offset > 0)
            {
                if (pm?.KarmaLocked == true)
                {
                    return;
                }

                if (m.Karma >= MaxKarma)
                {
                    return;
                }

                offset = Math.Max(offset - m.Karma / 100, 0);
            }
            else if (offset < 0)
            {
                if (m.Karma <= MinKarma)
                {
                    return;
                }

                offset = Math.Min(offset - m.Karma / 100, 0);
            }

            offset = (m.Karma + offset) switch
            {
                > MaxKarma => MaxKarma - m.Karma,
                < MinKarma => MinKarma - m.Karma,
                _          => offset
            };

            var wasPositiveKarma = m.Karma >= 0;

            m.Karma += offset;

            if (message)
            {
                if (offset > 40)
                {
                    m.SendLocalizedMessage(1019062); // You have gained a lot of karma.
                }
                else if (offset > 20)
                {
                    m.SendLocalizedMessage(1019061); // You have gained a good amount of karma.
                }
                else if (offset > 10)
                {
                    m.SendLocalizedMessage(1019060); // You have gained some karma.
                }
                else if (offset > 0)
                {
                    m.SendLocalizedMessage(1019059); // You have gained a little karma.
                }
                else if (offset < -40)
                {
                    m.SendLocalizedMessage(1019066); // You have lost a lot of karma.
                }
                else if (offset < -20)
                {
                    m.SendLocalizedMessage(1019065); // You have lost a good amount of karma.
                }
                else if (offset < -10)
                {
                    m.SendLocalizedMessage(1019064); // You have lost some karma.
                }
                else if (offset < 0)
                {
                    m.SendLocalizedMessage(1019063); // You have lost a little karma.
                }
            }

            if (!Core.AOS && wasPositiveKarma && m.Karma < 0 && pm?.KarmaLocked == false)
            {
                pm.KarmaLocked = true;
                // Karma is locked.  A mantra spoken at a shrine will unlock it again.
                m.SendLocalizedMessage(1042511, "", 0x22);
            }
        }

        public static string ComputeTitle(Mobile beholder, Mobile beheld)
        {
            using var title = ValueStringBuilder.Create();

            var fame = beheld.Fame;
            var karma = beheld.Karma;

            var showSkillTitle = beheld.ShowFameTitle && (beholder == beheld || fame >= 5000);

            if (beheld.ShowFameTitle || beholder == beheld)
            {
                for (var i = 0; i < m_FameEntries.Length; ++i)
                {
                    var fe = m_FameEntries[i];

                    if (fame <= fe.m_Fame || i == m_FameEntries.Length - 1)
                    {
                        var karmaEntries = fe.m_Karma;

                        for (var j = 0; j < karmaEntries.Length; ++j)
                        {
                            var ke = karmaEntries[j];

                            if (karma <= ke.m_Karma || j == karmaEntries.Length - 1)
                            {
                                if (karma >= 10000)
                                {
                                    if (beheld.Female)
                                    {
                                        title.Append($"{ke.m_Title}Lady {beheld.Name}");
                                    }
                                    else
                                    {
                                        title.Append($"{ke.m_Title}Lord {beheld.Name}");
                                    }
                                }
                                else
                                {
                                    title.Append($"{ke.m_Title}{beheld.Name}");
                                }
                                break;
                            }
                        }

                        break;
                    }
                }
            }
            else
            {
                title.Append(beheld.Name);
            }

            if (beheld is PlayerMobile mobile && mobile.DisplayChampionTitle)
            {
                var titleLabel = ChampionTitleSystem.GetChampionTitleLabel(mobile);
                if (titleLabel > 0)
                {
                    // Should this be translated to the receivers language? Prefix titles aren't?
                    title.Append($": {Localization.GetText(titleLabel)}");

                    // Do not display the skills title
                    return title.ToString();
                }
            }

            var customTitle = beheld.Title?.Trim();

            if (customTitle?.Length > 0)
            {
                title.Append($" {customTitle}");
            }
            else if (showSkillTitle && beheld.Player)
            {
                var skillTitle = GetSkillTitle(beheld);

                if (skillTitle != null)
                {
                    title.Append($", {skillTitle}");
                }
            }

            return title.ToString();
        }

        public static string GetSkillTitle(Mobile mob)
        {
            var highest = GetHighestSkill(mob); // beheld.Skills.Highest;

            if (highest?.BaseFixedPoint >= 300)
            {
                var skillLevel = GetSkillLevel(highest);
                var skillTitle = highest.Info.Title;

                if (mob.Female && skillTitle.EndsWithOrdinal("man"))
                {
                    return $"{skillLevel} {skillTitle.AsSpan(0, skillTitle.Length - 3)}woman";
                }

                return $"{skillLevel} {skillTitle}";
            }

            return null;
        }

        private static Skill GetHighestSkill(Mobile m)
        {
            var skills = m.Skills;

            if (!Core.AOS)
            {
                return skills.Highest;
            }

            Skill highest = null;

            for (var i = 0; i < m.Skills.Length; ++i)
            {
                var check = m.Skills[i];

                if (highest == null || check.BaseFixedPoint > highest.BaseFixedPoint)
                {
                    highest = check;
                }
                else if (highest.Lock != SkillLock.Up && check.Lock == SkillLock.Up &&
                         check.BaseFixedPoint == highest.BaseFixedPoint)
                {
                    highest = check;
                }
            }

            return highest;
        }

        private static string GetSkillLevel(Skill skill) => m_Levels[GetTableIndex(skill), GetTableType(skill)];

        private static int GetTableType(Skill skill)
        {
            return skill.SkillName switch
            {
                SkillName.Bushido  => 1,
                SkillName.Ninjitsu => 2,
                _                  => 0
            };
        }

        private static int GetTableIndex(Skill skill)
        {
            var fp = Math.Min(skill.BaseFixedPoint, 1200);

            return (fp - 300) / 100;
        }
    }

    public class FameEntry
    {
        public int m_Fame;
        public KarmaEntry[] m_Karma;

        public FameEntry(int fame, KarmaEntry[] karma)
        {
            m_Fame = fame;
            m_Karma = karma;
        }
    }

    public class KarmaEntry
    {
        public int m_Karma;
        public string m_Title;

        public KarmaEntry(int karma, string title)
        {
            m_Karma = karma;
            m_Title = title;
        }
    }
}
