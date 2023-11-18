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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendMessageTo(Mobile from, TextDefinition message)
        {
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
