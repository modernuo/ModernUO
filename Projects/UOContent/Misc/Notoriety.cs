using System.Collections.Generic;
using Server.Engines.ConPVP;
using Server.Engines.PartySystem;
using Server.Factions;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.SkillHandlers;
using Server.Spells.Seventh;

namespace Server.Misc
{
    public static class NotorietyHandlers
    {
        public static void Initialize()
        {
            Notoriety.Hues[Notoriety.Innocent] = 0x59;
            Notoriety.Hues[Notoriety.Ally] = 0x3F;
            Notoriety.Hues[Notoriety.CanBeAttacked] = 0x3B2;
            Notoriety.Hues[Notoriety.Criminal] = 0x3B2;
            Notoriety.Hues[Notoriety.Enemy] = 0x90;
            Notoriety.Hues[Notoriety.Murderer] = 0x22;
            Notoriety.Hues[Notoriety.Invulnerable] = 0x35;

            Notoriety.Handler = MobileNotoriety;

            Mobile.AllowBeneficialHandler = Mobile_AllowBeneficial;
            Mobile.AllowHarmfulHandler = Mobile_AllowHarmful;
        }

        private static GuildStatus GetGuildStatus(Mobile m)
        {
            if (m.Guild == null)
            {
                return GuildStatus.None;
            }

            if (((Guild)m.Guild).Enemies.Count == 0 && m.Guild.Type == GuildType.Regular)
            {
                return GuildStatus.Peaceful;
            }

            return GuildStatus.Waring;
        }

        private static bool CheckBeneficialStatus(GuildStatus from, GuildStatus target)
        {
            if (from == GuildStatus.Waring || target == GuildStatus.Waring)
            {
                return false;
            }

            return true;
        }

        /*private static bool CheckHarmfulStatus( GuildStatus from, GuildStatus target )
        {
          if (from == GuildStatus.Waring && target == GuildStatus.Waring)
            return true;

          return false;
        }*/

        public static bool Mobile_AllowBeneficial(Mobile from, Mobile target)
        {
            if (from == null || target == null || from.AccessLevel > AccessLevel.Player ||
                target.AccessLevel > AccessLevel.Player)
            {
                return true;
            }

            var bcFrom = from as BaseCreature;
            var bcTarg = target as BaseCreature;
            var pmFrom = from as PlayerMobile;
            var pmTarg = target as PlayerMobile;

            if (pmFrom == null && bcFrom?.Summoned == true)
            {
                pmFrom = bcFrom.SummonMaster as PlayerMobile;
            }

            if (pmTarg == null && bcTarg?.Summoned == true)
            {
                pmTarg = bcTarg.SummonMaster as PlayerMobile;
            }

            if (pmFrom != null && pmTarg != null)
            {
                if (pmFrom.DuelContext != pmTarg.DuelContext &&
                    (pmFrom.DuelContext?.Started == true || pmTarg.DuelContext?.Started == true))
                {
                    return false;
                }

                if (pmFrom.DuelContext != null && pmFrom.DuelContext == pmTarg.DuelContext &&
                    (pmFrom.DuelContext.StartedReadyCountdown && !pmFrom.DuelContext.Started || pmFrom.DuelContext.Tied ||
                     pmFrom.DuelPlayer.Eliminated || pmTarg.DuelPlayer.Eliminated))
                {
                    return false;
                }

                if (pmFrom.DuelPlayer?.Eliminated == false && pmFrom.DuelContext?.IsSuddenDeath == true)
                {
                    return false;
                }

                if (pmFrom.DuelContext != null && pmFrom.DuelContext == pmTarg.DuelContext &&
                    pmFrom.DuelContext.m_Tournament?.IsNotoRestricted == true &&
                    pmFrom.DuelPlayer != null && pmTarg.DuelPlayer != null &&
                    pmFrom.DuelPlayer.Participant != pmTarg.DuelPlayer.Participant)
                {
                    return false;
                }

                if (pmFrom.DuelContext?.Started == true && pmFrom.DuelContext == pmTarg.DuelContext)
                {
                    return true;
                }
            }

            if (pmFrom?.DuelContext?.Started == true || pmTarg?.DuelContext?.Started == true)
            {
                return false;
            }

            if (from.Region.IsPartOf<SafeZone>() || target.Region.IsPartOf<SafeZone>())
            {
                return false;
            }

            var map = from.Map;

            var targetFaction = Faction.Find(target, true);

            if ((!Core.ML || map == Faction.Facet) && targetFaction != null && Faction.Find(from, true) != targetFaction)
            {
                return false;
            }

            if ((map?.Rules & MapRules.BeneficialRestrictions) == 0)
            {
                return true; // In felucca, anything goes
            }

            if (!from.Player)
            {
                return true; // NPCs have no restrictions
            }

            if (bcTarg?.Controlled == false)
            {
                return false; // Players cannot heal uncontrolled mobiles
            }

            if (pmFrom?.Young == true && pmTarg?.Young != true)
            {
                return false; // Young players cannot perform beneficial actions towards older players
            }

            if (from.Guild is Guild fromGuild && target.Guild is Guild targetGuild &&
                (targetGuild == fromGuild || fromGuild.IsAlly(targetGuild)))
            {
                return true; // Guild members can be beneficial
            }

            return CheckBeneficialStatus(GetGuildStatus(from), GetGuildStatus(target));
        }

        public static bool Mobile_AllowHarmful(Mobile from, Mobile target)
        {
            if (from == null || target == null || from.AccessLevel > AccessLevel.Player ||
                target.AccessLevel > AccessLevel.Player)
            {
                return true;
            }

            var bcFrom = from as BaseCreature;
            var pmFrom = from as PlayerMobile;
            var pmTarg = target as PlayerMobile;
            var bcTarg = target as BaseCreature;

            if (pmFrom == null && bcFrom?.Summoned == true)
            {
                pmFrom = bcFrom.SummonMaster as PlayerMobile;
            }

            if (pmTarg == null && bcTarg?.Summoned == true)
            {
                pmTarg = bcTarg.SummonMaster as PlayerMobile;
            }

            if (pmFrom != null && pmTarg != null)
            {
                if (pmFrom.DuelContext != pmTarg.DuelContext &&
                    (pmFrom.DuelContext?.Started == true || pmTarg.DuelContext?.Started == true))
                {
                    return false;
                }

                if (pmFrom.DuelContext != null && pmFrom.DuelContext == pmTarg.DuelContext &&
                    (pmFrom.DuelContext.StartedReadyCountdown && !pmFrom.DuelContext.Started || pmFrom.DuelContext.Tied ||
                     pmFrom.DuelPlayer.Eliminated || pmTarg.DuelPlayer.Eliminated))
                {
                    return false;
                }

                if (pmFrom.DuelContext != null && pmFrom.DuelContext == pmTarg.DuelContext &&
                    pmFrom.DuelContext.m_Tournament?.IsNotoRestricted == true &&
                    pmFrom.DuelPlayer != null && pmTarg.DuelPlayer != null &&
                    pmFrom.DuelPlayer.Participant == pmTarg.DuelPlayer.Participant)
                {
                    return false;
                }

                if (pmFrom.DuelContext?.Started == true && pmFrom.DuelContext == pmTarg.DuelContext)
                {
                    return true;
                }
            }

            if (pmFrom?.DuelContext?.Started == true || pmTarg?.DuelContext?.Started == true)
            {
                return false;
            }

            if (from.Region.IsPartOf<SafeZone>() || target.Region.IsPartOf<SafeZone>())
            {
                return false;
            }

            var map = from.Map;

            if ((map?.Rules & MapRules.HarmfulRestrictions) == 0)
            {
                return true; // In felucca, anything goes
            }

            if (!from.Player && !(from is BaseCreature bc && bc.GetMaster() != null &&
                                  bc.GetMaster().AccessLevel == AccessLevel.Player))
            {
                if (!CheckAggressor(from.Aggressors, target) && !CheckAggressed(from.Aggressed, target) &&
                    pmTarg?.CheckYoungProtection(from) == true)
                {
                    return false;
                }

                return true; // Uncontrolled NPCs are only restricted by the young system
            }

            var fromGuild = GetGuildFor(from.Guild as Guild, from);
            var targetGuild = GetGuildFor(target.Guild as Guild, target);

            if (fromGuild != null && targetGuild != null &&
                (fromGuild == targetGuild || fromGuild.IsAlly(targetGuild) || fromGuild.IsEnemy(targetGuild)))
            {
                return true; // Guild allies or enemies can be harmful
            }

            if (bcTarg?.Controlled == true
                || (bcTarg?.Summoned == true && bcTarg.SummonMaster != from && bcTarg.SummonMaster.Player))
            {
                return false; // Cannot harm other controlled mobiles from players
            }

            if (pmFrom == null && bcFrom != null && bcFrom.Summoned && target.Player)
            {
                return true; // Summons from monsters can attack players
            }

            if (target.Player)
            {
                return false; // Cannot harm other players
            }

            return bcTarg?.InitialInnocent == true || Notoriety.Compute(from, target) != Notoriety.Innocent;
        }

        public static Guild GetGuildFor(Guild def, Mobile m)
        {
            var g = def;

            if (m is BaseCreature c && c.Controlled && c.ControlMaster != null)
            {
                c.DisplayGuildTitle = false;

                if (c.Map != Map.Internal && (Core.AOS || Guild.NewGuildSystem || c.ControlOrder is OrderType.Attack or OrderType.Guard))
                {
                    g = (Guild)(c.Guild = c.ControlMaster.Guild);
                }
                else if (c.Map == Map.Internal || c.ControlMaster.Guild == null)
                {
                    g = (Guild)(c.Guild = null);
                }
            }

            return g;
        }

        public static int CorpseNotoriety(Mobile source, Corpse target)
        {
            if (target.AccessLevel > AccessLevel.Player)
            {
                return Notoriety.CanBeAttacked;
            }

            Body body = target.Amount;

            var sourceGuild = GetGuildFor(source.Guild as Guild, source);
            var targetGuild = GetGuildFor(target.Guild, target.Owner);

            var srcFaction = Faction.Find(source, true, true);
            var trgFaction = Faction.Find(target.Owner, true, true);
            var list = target.Aggressors;

            if (sourceGuild != null && targetGuild != null)
            {
                if (sourceGuild == targetGuild || sourceGuild.IsAlly(targetGuild))
                {
                    return Notoriety.Ally;
                }

                if (sourceGuild.IsEnemy(targetGuild))
                {
                    return Notoriety.Enemy;
                }
            }

            if (target.Owner is BaseCreature creature)
            {
                if (srcFaction != null && trgFaction != null && srcFaction != trgFaction && source.Map == Faction.Facet)
                {
                    return Notoriety.Enemy;
                }

                if (CheckHouseFlag(source, creature, target.Location, target.Map))
                {
                    return Notoriety.CanBeAttacked;
                }

                var actual = Notoriety.CanBeAttacked;

                if (target.Kills >= 5 || body.IsMonster && IsSummoned(creature) || creature.AlwaysMurderer ||
                    creature.IsAnimatedDead)
                {
                    actual = Notoriety.Murderer;
                }

                if (Core.Now >= target.TimeOfDeath + Corpse.MonsterLootRightSacrifice)
                {
                    return actual;
                }

                var sourceParty = Party.Get(source);

                for (var i = 0; i < list.Count; ++i)
                {
                    if (list[i] == source || sourceParty != null && Party.Get(list[i]) == sourceParty)
                    {
                        return actual;
                    }
                }

                return Notoriety.Innocent;
            }

            if (target.Kills >= 5 || body.IsMonster)
            {
                return Notoriety.Murderer;
            }

            if (target.Criminal && (target.Map?.Rules & MapRules.HarmfulRestrictions) == 0)
            {
                return Notoriety.Criminal;
            }

            if (srcFaction != null && trgFaction != null && srcFaction != trgFaction && source.Map == Faction.Facet)
            {
                for (var i = 0; i < list.Count; ++i)
                {
                    if (list[i] == source || list[i] is BaseFactionGuard)
                    {
                        return Notoriety.Enemy;
                    }
                }
            }

            if (CheckHouseFlag(source, target.Owner, target.Location, target.Map))
            {
                return Notoriety.CanBeAttacked;
            }

            if (target.Owner is not PlayerMobile)
            {
                return Notoriety.CanBeAttacked;
            }

            for (var i = 0; i < list.Count; ++i)
            {
                if (list[i] == source)
                {
                    return Notoriety.CanBeAttacked;
                }
            }

            return Notoriety.Innocent;
        }

        /* Must be thread-safe */
        public static int MobileNotoriety(Mobile source, Mobile target)
        {
            var bcTarg = target as BaseCreature;

            if (Core.AOS && (target.Blessed || bcTarg?.IsInvulnerable == true || target is PlayerVendor or TownCrier))
            {
                return Notoriety.Invulnerable;
            }

            var pmFrom = source as PlayerMobile;
            var pmTarg = target as PlayerMobile;

            if (pmFrom != null && pmTarg != null)
            {
                if (pmFrom.DuelContext?.StartedBeginCountdown == true && !pmFrom.DuelContext.Finished &&
                    pmFrom.DuelContext == pmTarg.DuelContext)
                {
                    return pmFrom.DuelContext.IsAlly(pmFrom, pmTarg) ? Notoriety.Ally : Notoriety.Enemy;
                }
            }

            if (target.AccessLevel > AccessLevel.Player)
            {
                return Notoriety.CanBeAttacked;
            }

            if (source.Player && !target.Player && pmFrom != null && bcTarg != null)
            {
                var master = bcTarg.GetMaster();

                if (master?.AccessLevel > AccessLevel.Player)
                {
                    return Notoriety.CanBeAttacked;
                }

                master = bcTarg.ControlMaster;

                if (Core.ML && master != null)
                {
                    if (source == master && CheckAggressor(bcTarg.Aggressors, source) ||
                        CheckAggressor(source.Aggressors, bcTarg))
                    {
                        return Notoriety.CanBeAttacked;
                    }

                    return MobileNotoriety(source, master);
                }

                if (!bcTarg.Summoned && !bcTarg.Controlled && pmFrom.EnemyOfOneType == bcTarg.GetType())
                {
                    return Notoriety.Enemy;
                }
            }

            if (target.Kills >= 5 ||
                target.Body.IsMonster && IsSummoned(bcTarg) && target is not BaseFamiliar && target is not ArcaneFey &&
                target is not Golem || bcTarg?.AlwaysMurderer == true || bcTarg?.IsAnimatedDead == true)
            {
                return Notoriety.Murderer;
            }

            if (target.Criminal)
            {
                return Notoriety.Criminal;
            }

            var sourceGuild = GetGuildFor(source.Guild as Guild, source);
            var targetGuild = GetGuildFor(target.Guild as Guild, target);

            if (sourceGuild != null && targetGuild != null)
            {
                if (sourceGuild == targetGuild || sourceGuild.IsAlly(targetGuild))
                {
                    return Notoriety.Ally;
                }

                if (sourceGuild.IsEnemy(targetGuild))
                {
                    return Notoriety.Enemy;
                }
            }

            var srcFaction = Faction.Find(source, true, true);
            var trgFaction = Faction.Find(target, true, true);

            if (srcFaction != null && trgFaction != null && srcFaction != trgFaction && source.Map == Faction.Facet)
            {
                return Notoriety.Enemy;
            }

            if (Stealing.ClassicMode && pmTarg?.PermaFlags.Contains(source) == true)
            {
                return Notoriety.CanBeAttacked;
            }

            if (bcTarg?.AlwaysAttackable == true)
            {
                return Notoriety.CanBeAttacked;
            }

            if (CheckHouseFlag(source, target, target.Location, target.Map))
            {
                return Notoriety.CanBeAttacked;
            }

            if (bcTarg?.InitialInnocent != true)
            {
                if (!target.Body.IsHuman && !target.Body.IsGhost && !IsPet(bcTarg) && pmTarg == null ||
                    !Core.ML && !target.CanBeginAction<PolymorphSpell>())
                {
                    return Notoriety.CanBeAttacked;
                }
            }

            if (CheckAggressor(source.Aggressors, target))
            {
                return Notoriety.CanBeAttacked;
            }

            if (CheckAggressed(source.Aggressed, target))
            {
                return Notoriety.CanBeAttacked;
            }

            if (bcTarg?.Controlled == true && bcTarg.ControlOrder == OrderType.Guard &&
                bcTarg.ControlTarget == source)
            {
                return Notoriety.CanBeAttacked;
            }

            if (source is BaseCreature bc)
            {
                var master = bc.GetMaster();

                if (master != null && (CheckAggressor(master.Aggressors, target) ||
                                       MobileNotoriety(master, target) == Notoriety.CanBeAttacked || bcTarg != null))
                {
                    return Notoriety.CanBeAttacked;
                }
            }

            return Notoriety.Innocent;
        }

        public static bool CheckHouseFlag(Mobile from, Mobile m, Point3D p, Map map)
        {
            var house = BaseHouse.FindHouseAt(p, map, 16);

            if (house?.Public != false || !house.IsFriend(from))
            {
                return false;
            }

            if (m != null && house.IsFriend(m))
            {
                return false;
            }

            return m is not BaseCreature c || c.Deleted || !c.Controlled || c.ControlMaster == null ||
                   !house.IsFriend(c.ControlMaster);
        }

        public static bool IsPet(BaseCreature c) => c?.Controlled == true;

        public static bool IsSummoned(BaseCreature c) => c?.Summoned == true;

        public static bool CheckAggressor(List<AggressorInfo> list, Mobile target)
        {
            for (var i = 0; i < list.Count; ++i)
            {
                if (list[i].Attacker == target)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CheckAggressed(List<AggressorInfo> list, Mobile target)
        {
            for (var i = 0; i < list.Count; ++i)
            {
                var info = list[i];

                if (!info.CriminalAggression && info.Defender == target)
                {
                    return true;
                }
            }

            return false;
        }

        private enum GuildStatus
        {
            None,
            Peaceful,
            Waring
        }
    }
}
