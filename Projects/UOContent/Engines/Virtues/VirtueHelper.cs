using System;
using Server.Mobiles;

namespace Server
{
    public enum VirtueLevel
    {
        None,
        Seeker,
        Follower,
        Knight
    }

    public enum VirtueName
    {
        Humility,
        Sacrifice,
        Compassion,
        Spirituality,
        Valor,
        Honor,
        Justice,
        Honesty
    }

    public static class VirtueHelper
    {
        public static bool HasAny(Mobile from, VirtueName virtue) => from.Virtues.GetValue((int)virtue) > 0;

        public static bool IsHighestPath(Mobile from, VirtueName virtue) =>
            from.Virtues.GetValue((int)virtue) >= GetMaxAmount(virtue);

        public static VirtueLevel GetLevel(Mobile from, VirtueName virtue)
        {
            var v = from.Virtues.GetValue((int)virtue);
            int vl;

            if (v < 4000)
            {
                vl = 0;
            }
            else if (v >= GetMaxAmount(virtue))
            {
                vl = 3;
            }
            else
            {
                vl = (v + 9999) / 10000;
            }

            return (VirtueLevel)vl;
        }

        public static int GetMaxAmount(VirtueName virtue) =>
            virtue switch
            {
                VirtueName.Honor => 20000,
                VirtueName.Sacrifice => 22000,
                _ => 21000
            };


        public static int GetGainedLocalizedMessage(VirtueName virtue) =>
            virtue switch
            {
                VirtueName.Sacrifice => 1054160, //You have gained in sacrifice.
                VirtueName.Compassion => 1053002, //You have gained in compassion.
                VirtueName.Spirituality => 1155832, //You have gained in Spirituality.
                VirtueName.Valor => 1054030, //You have gained in Valor!
                VirtueName.Honor => 1063225, //You have gained in Honor.
                VirtueName.Justice => 1049363, //You have gained in Justice.
                VirtueName.Humility => 1052070, //You have gained in Humility.
                VirtueName.Honesty => 0, //Doesn't find the message in cliloc.enu 
                _ => 0
            };

        public static int GetGainedAPathLocalizedMessage(VirtueName virtue) =>
            virtue switch
            {
                VirtueName.Sacrifice => 1052008, //You have gained a path in Sacrifice!
                VirtueName.Compassion => 0, //Doesn't find the message in cliloc.enu
                VirtueName.Spirituality => 1155833, //"You have gained a path in Spirituality!"
                VirtueName.Valor => 1054032, //You have gained a path in Valor!
                VirtueName.Honor => 1063226, //You have gained a path in Honor!
                VirtueName.Justice => 1049367, //You have gained a path in Justice!
                VirtueName.Humility => 1155811, //You have gained a path in Humility!
                VirtueName.Honesty => 0, //Doesn't find the message in cliloc.enu
                _ => 0
            };

        public static int GetHightestPathLocalizedMessage(VirtueName virtue) =>
            virtue switch
            {
                VirtueName.Sacrifice => 0, //Doesn't find the message in cliloc.enu
                VirtueName.Compassion => 1053003, //You have achieved the highest path of compassion and can no longer gain any further.
                VirtueName.Spirituality => 0, //Doesn't find the message in cliloc.enu
                VirtueName.Valor => 1054031, //You have achieved the highest path in Valor and can no longer gain any further.
                VirtueName.Honor => 0, //Doesn't find the message in cliloc.enu
                VirtueName.Justice => 0, //Doesn't find the message in cliloc.enu
                VirtueName.Humility => 0, //Doesn't find the message in cliloc.enu
                VirtueName.Honesty => 1153771, //You have achieved the highest path in Honesty and can no longer gain any further. 
                _ => 0
            };

        public static bool Award(Mobile from, VirtueName virtue, int amount, ref bool gainedPath)
        {
            var current = from.Virtues.GetValue((int)virtue);

            var maxAmount = GetMaxAmount(virtue);

            if (current >= maxAmount)
            {
                return false;
            }

            if (current + amount >= maxAmount)
            {
                amount = maxAmount - current;
            }

            var oldLevel = GetLevel(from, virtue);

            from.Virtues.SetValue((int)virtue, current + amount);

            gainedPath = GetLevel(from, virtue) != oldLevel;

            return true;
        }

        public static bool Atrophy(Mobile from, VirtueName virtue) => Atrophy(from, virtue, 1);

        public static bool Atrophy(Mobile from, VirtueName virtue, int amount)
        {
            var current = from.Virtues.GetValue((int)virtue);

            if (current - amount >= 0)
            {
                from.Virtues.SetValue((int)virtue, current - amount);
            }
            else
            {
                from.Virtues.SetValue((int)virtue, 0);
            }

            return current > 0;
        }

        public static bool IsSeeker(Mobile from, VirtueName virtue) => GetLevel(from, virtue) >= VirtueLevel.Seeker;

        public static bool IsFollower(Mobile from, VirtueName virtue) => GetLevel(from, virtue) >= VirtueLevel.Follower;

        public static bool IsKnight(Mobile from, VirtueName virtue) => GetLevel(from, virtue) >= VirtueLevel.Knight;

        public static void AwardVirtue(PlayerMobile pm, VirtueName virtue, int amount)
        {
            if (virtue == VirtueName.Compassion)
            {
                if (pm.CompassionGains > 0 && Core.Now > pm.NextCompassionDay)
                {
                    pm.NextCompassionDay = DateTime.MinValue;
                    pm.CompassionGains = 0;
                }

                if (pm.CompassionGains >= 5)
                {
                    pm.SendLocalizedMessage(1053004); // You must wait about a day before you can gain in compassion again.
                    return;
                }
            }

            var gainedPath = false;
            var virtueName = Enum.GetName(typeof(VirtueName), virtue);

            if (Award(pm, virtue, amount, ref gainedPath))
            {
                if (gainedPath)
                {
                    var localizedMessageSerial = GetGainedAPathLocalizedMessage(virtue);
                    if (localizedMessageSerial != 0)
                    {
                        pm.SendLocalizedMessage(localizedMessageSerial);
                    }
                    else
                    {
                        pm.SendMessage("You have gained a path in {0}!", virtueName);
                    }
                }
                else
                {
                    var localizedMessageSerial = GetGainedLocalizedMessage(virtue);
                    if (localizedMessageSerial != 0)
                    {
                        pm.SendLocalizedMessage(localizedMessageSerial);
                    }
                    else
                    {
                        pm.SendMessage("You have gained in {0}.", virtueName);
                    }
                }

                if (virtue == VirtueName.Compassion)
                {
                    pm.NextCompassionDay = Core.Now + TimeSpan.FromDays(1.0);
                    ++pm.CompassionGains;

                    if (pm.CompassionGains >= 5)
                    {
                        pm.SendLocalizedMessage(
                            1053004
                        ); // You must wait about a day before you can gain in compassion again.
                    }
                }
            }
            else
            {
                var localizedMessageSerial = GetHightestPathLocalizedMessage(virtue);
                if (localizedMessageSerial != 0)
                {
                    pm.SendLocalizedMessage(localizedMessageSerial);
                }
                else
                {
                    pm.SendMessage("You have achieved the highest path of {0} and can no longer gain any further.", virtueName);
                }

            }
        }
    }
}
