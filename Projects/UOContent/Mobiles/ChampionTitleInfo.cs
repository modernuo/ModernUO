using System;
using System.Linq;
using Server.Engines.CannedEvil;

namespace Server.Mobiles
{
    [PropertyObject]
    public class ChampionTitleInfo
    {
        public const int LossAmount = 90;
        public static TimeSpan LossDelay = TimeSpan.FromDays(1.0);

        private TitleInfo[] m_Values;

        public ChampionTitleInfo()
        {
        }

        public ChampionTitleInfo(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        Harrower = reader.ReadEncodedInt();

                        var length = reader.ReadEncodedInt();
                        m_Values = new TitleInfo[length];

                        for (var i = 0; i < length; i++)
                        {
                            m_Values[i] = new TitleInfo(reader);
                        }

                        if (m_Values.Length != ChampionSpawnInfo.Table.Length)
                        {
                            var oldValues = m_Values;
                            m_Values = new TitleInfo[ChampionSpawnInfo.Table.Length];

                            for (var i = 0; i < m_Values.Length && i < oldValues.Length; i++)
                            {
                                m_Values[i] = oldValues[i];
                            }
                        }

                        break;
                    }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Pestilence
        {
            get => GetValue(ChampionSpawnType.Pestilence);
            set => SetValue(ChampionSpawnType.Pestilence, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Abyss
        {
            get => GetValue(ChampionSpawnType.Abyss);
            set => SetValue(ChampionSpawnType.Abyss, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Arachnid
        {
            get => GetValue(ChampionSpawnType.Arachnid);
            set => SetValue(ChampionSpawnType.Arachnid, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ColdBlood
        {
            get => GetValue(ChampionSpawnType.ColdBlood);
            set => SetValue(ChampionSpawnType.ColdBlood, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ForestLord
        {
            get => GetValue(ChampionSpawnType.ForestLord);
            set => SetValue(ChampionSpawnType.ForestLord, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SleepingDragon
        {
            get => GetValue(ChampionSpawnType.SleepingDragon);
            set => SetValue(ChampionSpawnType.SleepingDragon, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int UnholyTerror
        {
            get => GetValue(ChampionSpawnType.UnholyTerror);
            set => SetValue(ChampionSpawnType.UnholyTerror, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int VerminHorde
        {
            get => GetValue(ChampionSpawnType.VerminHorde);
            set => SetValue(ChampionSpawnType.VerminHorde, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Harrower { get; set; }

        public int GetValue(ChampionSpawnType type) => GetValue((int)type);

        public void SetValue(ChampionSpawnType type, int value)
        {
            SetValue((int)type, value);
        }

        public void Award(ChampionSpawnType type, int value)
        {
            Award((int)type, value);
        }

        public int GetValue(int index)
        {
            if (m_Values == null || index < 0 || index >= m_Values.Length)
            {
                return 0;
            }

            m_Values[index] ??= new TitleInfo();

            return m_Values[index].Value;
        }

        public DateTime GetLastDecay(int index)
        {
            if (m_Values == null || index < 0 || index >= m_Values.Length)
            {
                return DateTime.MinValue;
            }

            m_Values[index] ??= new TitleInfo();

            return m_Values[index].LastDecay;
        }

        public void SetValue(int index, int value)
        {
            m_Values ??= new TitleInfo[ChampionSpawnInfo.Table.Length];

            if (index < 0 || index >= m_Values.Length)
            {
                return;
            }

            m_Values[index] ??= new TitleInfo();

            m_Values[index].Value = Math.Max(value, 0);
        }

        public void Award(int index, int value)
        {
            m_Values ??= new TitleInfo[ChampionSpawnInfo.Table.Length];

            if (index < 0 || index >= m_Values.Length || value <= 0)
            {
                return;
            }

            m_Values[index] ??= new TitleInfo();

            m_Values[index].Value += value;
        }

        public void Atrophy(int index, int value)
        {
            m_Values ??= new TitleInfo[ChampionSpawnInfo.Table.Length];

            if (index < 0 || index >= m_Values.Length || value <= 0)
            {
                return;
            }

            m_Values[index] ??= new TitleInfo();

            var before = m_Values[index].Value;

            m_Values[index].Value -= Math.Min(value, m_Values[index].Value);

            if (before != m_Values[index].Value)
            {
                m_Values[index].LastDecay = Core.Now;
            }
        }

        public override string ToString() => "...";

        public static void Serialize(IGenericWriter writer, ChampionTitleInfo titles)
        {
            writer.WriteEncodedInt(0); // version

            writer.WriteEncodedInt(titles.Harrower);

            var length = titles.m_Values.Length;
            writer.WriteEncodedInt(length);

            for (var i = 0; i < length; i++)
            {
                titles.m_Values[i] ??= new TitleInfo();

                TitleInfo.Serialize(writer, titles.m_Values[i]);
            }
        }

        public static void CheckAtrophy(PlayerMobile pm)
        {
            var t = pm.ChampionTitles;
            if (t == null)
            {
                return;
            }

            t.m_Values ??= new TitleInfo[ChampionSpawnInfo.Table.Length];

            for (var i = 0; i < t.m_Values.Length; i++)
            {
                if (t.GetLastDecay(i) + LossDelay < Core.Now)
                {
                    t.Atrophy(i, LossAmount);
                }
            }
        }

        public static void
            AwardHarrowerTitle(PlayerMobile pm) // Called when killing a harrower.  Will give a minimum of 1 point.
        {
            var t = pm.ChampionTitles;
            if (t == null)
            {
                return;
            }

            t.m_Values ??= new TitleInfo[ChampionSpawnInfo.Table.Length];

            var count = 1 + t.m_Values.Count(t1 => t1.Value > 900);

            t.Harrower = Math.Max(count, t.Harrower); // Harrower titles never decay.
        }

        private class TitleInfo
        {
            public TitleInfo()
            {
            }

            public TitleInfo(IGenericReader reader)
            {
                var version = reader.ReadEncodedInt();

                switch (version)
                {
                    case 0:
                        {
                            Value = reader.ReadEncodedInt();
                            LastDecay = reader.ReadDateTime();
                            break;
                        }
                }
            }

            public int Value { get; set; }

            public DateTime LastDecay { get; set; }

            public static void Serialize(IGenericWriter writer, TitleInfo info)
            {
                writer.WriteEncodedInt(0); // version

                writer.WriteEncodedInt(info.Value);
                writer.Write(info.LastDecay);
            }
        }
    }
}
