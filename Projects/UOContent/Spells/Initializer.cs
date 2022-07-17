using System;
using Server.Spells.Bushido;
using Server.Spells.Chivalry;
using Server.Spells.Eighth;
using Server.Spells.Fifth;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Mysticism;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;
using Server.Spells.Second;
using Server.Spells.Seventh;
using Server.Spells.Sixth;
using Server.Spells.Spellweaving;
using Server.Spells.Third;

namespace Server.Spells
{
    public static class Initializer
    {
        public static void Configure()
        {
            // First circle
            Register(00, typeof(ClumsySpell));
            Register(01, typeof(CreateFoodSpell));
            Register(02, typeof(FeeblemindSpell));
            Register(03, typeof(HealSpell));
            Register(04, typeof(MagicArrowSpell));
            Register(05, typeof(NightSightSpell));
            Register(06, typeof(ReactiveArmorSpell));
            Register(07, typeof(WeakenSpell));

            // Second circle
            Register(08, typeof(AgilitySpell));
            Register(09, typeof(CunningSpell));
            Register(10, typeof(CureSpell));
            Register(11, typeof(HarmSpell));
            Register(12, typeof(MagicTrapSpell));
            Register(13, typeof(RemoveTrapSpell));
            Register(14, typeof(ProtectionSpell));
            Register(15, typeof(StrengthSpell));

            // Third circle
            Register(16, typeof(BlessSpell));
            Register(17, typeof(FireballSpell));
            Register(18, typeof(MagicLockSpell));
            Register(19, typeof(PoisonSpell));
            Register(20, typeof(TelekinesisSpell));
            Register(21, typeof(TeleportSpell));
            Register(22, typeof(UnlockSpell));
            Register(23, typeof(WallOfStoneSpell));

            // Fourth circle
            Register(24, typeof(ArchCureSpell));
            Register(25, typeof(ArchProtectionSpell));
            Register(26, typeof(CurseSpell));
            Register(27, typeof(FireFieldSpell));
            Register(28, typeof(GreaterHealSpell));
            Register(29, typeof(LightningSpell));
            Register(30, typeof(ManaDrainSpell));
            Register(31, typeof(RecallSpell));

            // Fifth circle
            Register(32, typeof(BladeSpiritsSpell));
            Register(33, typeof(DispelFieldSpell));
            Register(34, typeof(IncognitoSpell));
            Register(35, typeof(MagicReflectSpell));
            Register(36, typeof(MindBlastSpell));
            Register(37, typeof(ParalyzeSpell));
            Register(38, typeof(PoisonFieldSpell));
            Register(39, typeof(SummonCreatureSpell));

            // Sixth circle
            Register(40, typeof(DispelSpell));
            Register(41, typeof(EnergyBoltSpell));
            Register(42, typeof(ExplosionSpell));
            Register(43, typeof(InvisibilitySpell));
            Register(44, typeof(MarkSpell));
            Register(45, typeof(MassCurseSpell));
            Register(46, typeof(ParalyzeFieldSpell));
            Register(47, typeof(RevealSpell));

            // Seventh circle
            Register(48, typeof(ChainLightningSpell));
            Register(49, typeof(EnergyFieldSpell));
            Register(50, typeof(FlameStrikeSpell));
            Register(51, typeof(GateTravelSpell));
            Register(52, typeof(ManaVampireSpell));
            Register(53, typeof(MassDispelSpell));
            Register(54, typeof(MeteorSwarmSpell));
            Register(55, typeof(PolymorphSpell));

            // Eighth circle
            Register(56, typeof(EarthquakeSpell));
            Register(57, typeof(EnergyVortexSpell));
            Register(58, typeof(ResurrectionSpell));
            Register(59, typeof(AirElementalSpell));
            Register(60, typeof(SummonDaemonSpell));
            Register(61, typeof(EarthElementalSpell));
            Register(62, typeof(FireElementalSpell));
            Register(63, typeof(WaterElementalSpell));

            if (Core.AOS)
            {
                // Necromancy spells
                Register(100, typeof(AnimateDeadSpell));
                Register(101, typeof(BloodOathSpell));
                Register(102, typeof(CorpseSkinSpell));
                Register(103, typeof(CurseWeaponSpell));
                Register(104, typeof(EvilOmenSpell));
                Register(105, typeof(HorrificBeastSpell));
                Register(106, typeof(LichFormSpell));
                Register(107, typeof(MindRotSpell));
                Register(108, typeof(PainSpikeSpell));
                Register(109, typeof(PoisonStrikeSpell));
                Register(110, typeof(StrangleSpell));
                Register(111, typeof(SummonFamiliarSpell));
                Register(112, typeof(VampiricEmbraceSpell));
                Register(113, typeof(VengefulSpiritSpell));
                Register(114, typeof(WitherSpell));
                Register(115, typeof(WraithFormSpell));

                if (Core.SE)
                {
                    Register(116, typeof(ExorcismSpell));
                }

                // Paladin abilities
                Register(200, typeof(CleanseByFireSpell));
                Register(201, typeof(CloseWoundsSpell));
                Register(202, typeof(ConsecrateWeaponSpell));
                Register(203, typeof(DispelEvilSpell));
                Register(204, typeof(DivineFurySpell));
                Register(205, typeof(EnemyOfOneSpell));
                Register(206, typeof(HolyLightSpell));
                Register(207, typeof(NobleSacrificeSpell));
                Register(208, typeof(RemoveCurseSpell));
                Register(209, typeof(SacredJourneySpell));

                if (Core.SE)
                {
                    // Samurai abilities
                    Register(400, typeof(HonorableExecution));
                    Register(401, typeof(Confidence));
                    Register(402, typeof(Evasion));
                    Register(403, typeof(CounterAttack));
                    Register(404, typeof(LightningStrike));
                    Register(405, typeof(MomentumStrike));

                    // Ninja abilities
                    Register(500, typeof(FocusAttack));
                    Register(501, typeof(DeathStrike));
                    Register(502, typeof(AnimalForm));
                    Register(503, typeof(KiAttack));
                    Register(504, typeof(SurpriseAttack));
                    Register(505, typeof(Backstab));
                    Register(506, typeof(Shadowjump));
                    Register(507, typeof(MirrorImage));
                }

                if (Core.ML)
                {
                    Register(600, typeof(ArcaneCircleSpell));
                    Register(601, typeof(GiftOfRenewalSpell));
                    Register(602, typeof(ImmolatingWeaponSpell));
                    Register(603, typeof(AttuneWeaponSpell));
                    Register(604, typeof(ThunderstormSpell));
                    Register(605, typeof(NatureFurySpell));
                    Register(606, typeof(SummonFeySpell));
                    Register(607, typeof(SummonFiendSpell));
                    Register(608, typeof(ReaperFormSpell));
                    // Register(609, typeof(WildfireSpell));
                    Register(610, typeof(EssenceOfWindSpell));
                    // Register(611, typeof(DryadAllureSpell));
                    Register(612, typeof(EtherealVoyageSpell));
                    Register(613, typeof(WordOfDeathSpell));
                    Register(614, typeof(GiftOfLifeSpell));
                    // Register(615, typeof(ArcaneEmpowermentSpell));
                }

                if (Core.SA)
                {
                    // Mysticism spells
                    // Register(677, typeof(NetherBoltSpell));
                    // Register(678, typeof(HealingStoneSpell));
                    // Register(679, typeof(PurgeMagicSpell));
                    // Register(680, typeof(EnchantSpell));
                    // Register(681, typeof(SleepSpell));
                    Register(682, typeof(EagleStrikeSpell));
                    Register(683, typeof(AnimatedWeaponSpell));
                    Register(684, typeof(StoneFormSpell));
                    // Register(685, typeof(SpellTriggerSpell));
                    // Register(686, typeof(MassSleepSpell));
                    Register(687, typeof(CleansingWindsSpell));
                    Register(688, typeof(BombardSpell));
                    Register(689, typeof(SpellPlagueSpell));
                    Register(690, typeof(HailStormSpell));
                    Register(691, typeof(NetherCycloneSpell));
                    // Register(692, typeof(RisingColossusSpell));
                }
            }
        }

        public static void Register(int spellId, Type type)
        {
            SpellRegistry.Register(spellId, type);
        }
    }
}
