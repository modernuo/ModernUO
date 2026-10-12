using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Random;

namespace Server.Engines.Harvest
{
    public class HarvestDefinition
    {
        private HarvestVein[] m_Veins;
        public int BankWidth { get; set; }

        public int BankHeight { get; set; }

        public int MinTotal { get; set; }

        public int MaxTotal { get; set; }

        public int[] LandTiles { get; set; }

        public int[] StaticTiles { get; set; }

        public bool RangedTiles { get; set; }

        public TimeSpan MinRespawn { get; set; }

        public TimeSpan MaxRespawn { get; set; }

        public int MaxRange { get; set; }

        public int ConsumedPerHarvest { get; set; }

        public int ConsumedPerFeluccaHarvest { get; set; }

        public bool PlaceAtFeetIfFull { get; set; }

        public SkillName Skill { get; set; }

        public int[] EffectActions { get; set; }

        public int[] EffectCounts { get; set; }

        public int[] EffectSounds { get; set; }

        public TimeSpan EffectSoundDelay { get; set; }

        public TimeSpan EffectDelay { get; set; }

        public TextDefinition NoResourcesMessage { get; set; }

        public TextDefinition OutOfRangeMessage { get; set; }

        public TextDefinition TimedOutOfRangeMessage { get; set; }

        public TextDefinition DoubleHarvestMessage { get; set; }

        public TextDefinition FailMessage { get; set; }

        public TextDefinition PackFullMessage { get; set; }

        public TextDefinition ToolBrokeMessage { get; set; }

        public HarvestResource[] Resources { get; set; }

        public HarvestVein[] Veins
        {
            get => m_Veins;
            set
            {
                m_Veins = value;
                var totalWeight = 0u;
                for (var i = 0; i < m_Veins.Length; i++)
                {
                    totalWeight += m_Veins[i].VeinChance;
                }

                VeinWeights = totalWeight;
            }
        }

        public BonusHarvestResource[] BonusResources { get; set; }

        public bool RaceBonus { get; set; }

        public bool RandomizeVeins { get; set; }

        public uint VeinWeights { get; private set; }

        public Dictionary<Map, Dictionary<Point2D, HarvestBank>> Banks { get; } = new();

        /// <summary>
        /// Raised whenever the harvest system tells a harvester something, before the message is sent.
        ///
        /// <para>
        /// <b>Why this exists.</b> The harvest system already knows exactly why a swing produced nothing —
        /// "There is no metal here to mine", "Someone has gotten to the metal before you", "You loosen some
        /// rocks but fail to find any useable ore", "Your backpack is full", "You have worn out your tool" —
        /// and says so, in as many words, through <see cref="SendMessageTo"/>. A player reads it. A bot has no
        /// <c>NetState</c>, and <c>Mobile.SendLocalizedMessage</c> is not virtual and writes straight to it,
        /// so every one of those sentences was dropped on the floor before anything could hear it. Bots were
        /// left inferring an empty vein from a run of quiet swings, which is a guess, and a wrong one about
        /// two thirds of the time.
        /// </para>
        ///
        /// <para>
        /// One event, raised before the send and changing nothing about it. Nothing in the engine subscribes;
        /// the bot assembly does. Kept deliberately narrow — this reports what was said and to whom, and does
        /// not let a listener alter it.
        /// </para>
        /// </summary>
        public static event Action<Mobile, HarvestDefinition, TextDefinition> Said;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendMessageTo(Mobile from, TextDefinition message)
        {
            // Before the send, and contained: a listener that throws must not take the harvest with it.
            if (Said != null && from != null)
            {
                try
                {
                    Said(from, this, message);
                }
                catch
                {
                    // A listener's mistake is a listener's mistake.
                }
            }

            if (message?.Number > 0)
            {
                from.SendLocalizedMessage(message.Number);
            }
            else if (!string.IsNullOrWhiteSpace(message))
            {
                from.SendMessage(message);
            }
        }

        public HarvestBank GetBank(Map map, int x, int y)
        {
            if (map == null || map == Map.Internal)
            {
                return null;
            }

            x /= BankWidth;
            y /= BankHeight;

            if (!Banks.TryGetValue(map, out var banks))
            {
                Banks[map] = banks = new Dictionary<Point2D, HarvestBank>();
            }

            var key = new Point2D(x, y);

            if (!banks.TryGetValue(key, out var bank))
            {
                banks[key] = bank = new HarvestBank(this, GetVeinAt(map, x, y));
            }

            return bank;
        }

        public HarvestVein GetVeinAt(Map map, int x, int y)
        {
            if (Veins.Length == 1)
            {
                return Veins[0];
            }

            if (RandomizeVeins)
            {
                return GetVeinFrom((uint)Utility.Random(1000));
            }

            // TODO: Introduce pulling primes from a config and writing them if they don't exist to the config
            var seed = (ulong)(x * 17 + y * 11 + map.MapID * 3);
            return GetVeinFrom((uint)StableRandom.First(seed, VeinWeights));
        }

        public HarvestVein GetVeinFrom(uint randomValue)
        {
            if (Veins.Length == 1)
            {
                return Veins[0];
            }

            for (var i = 0; i < Veins.Length; ++i)
            {
                if (randomValue <= Veins[i].VeinChance)
                {
                    return Veins[i];
                }

                randomValue -= Veins[i].VeinChance;
            }

            return null;
        }

        public BonusHarvestResource GetBonusResource()
        {
            if (BonusResources == null)
            {
                return null;
            }

            var randomValue = Utility.RandomDouble() * 100;

            for (var i = 0; i < BonusResources.Length; ++i)
            {
                if (randomValue <= BonusResources[i].Chance)
                {
                    return BonusResources[i];
                }

                randomValue -= BonusResources[i].Chance;
            }

            return null;
        }

        public bool Validate(int tileID, bool isLand)
        {
            var tiles = isLand ? LandTiles : StaticTiles;
            if (RangedTiles)
            {
                for (var i = 0; i < tiles.Length; i += 2)
                {
                    if (tileID >= tiles[i] && tileID <= tiles[i + 1])
                    {
                        return true;
                    }
                }

                return false;
            }

            for (var i = 0; i < tiles.Length; ++i)
            {
                if (tiles[i] == tileID)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
