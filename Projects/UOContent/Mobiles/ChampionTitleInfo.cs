using System;
using System.Runtime.CompilerServices;
using Server.Engines.CannedEvil;

namespace Server.Mobiles
{
    [PropertyObject]
    public class ChampionTitleInfo
    {
        private const int LossAmount = 90;
        private static TimeSpan LossDelay = TimeSpan.FromDays(1.0);

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

                        if (length == 0)
                        {
                            break;
                        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetValue(ChampionSpawnType type) => GetValue((int)type);

        public int GetValue(int index)
        {
            if (index < 0 || index >= m_Values?.Length)
            {
                return 0;
            }

            return m_Values?[index]?.Value ?? 0;
        }

        public DateTime GetLastDecay(int index)
        {
            if (index < 0 || index >= m_Values?.Length)
            {
                return DateTime.MinValue;
            }

            return m_Values?[index]?.LastDecay ?? DateTime.MinValue;
        }

        public void SetValue(ChampionSpawnType type, int value)
        {
            var index = (int)type;
            if (index < 0 || index >= ChampionSpawnInfo.Table.Length)
            {
                return;
            }

            m_Values ??= new TitleInfo[ChampionSpawnInfo.Table.Length];
            var title = m_Values[index];
            if (title == null)
            {
                if (value > 0)
                {
                    m_Values[index] = new TitleInfo(value, Core.Now);
                }
                return;
            }

            title.Value = Math.Max(value, 0);
            title.LastDecay = title.Value == 0 ? DateTime.MinValue : Core.Now;
        }

        public void Award(ChampionSpawnType type, int value)
        {
            var index = (int)type;
            if (value <= 0 || index < 0 || index >= ChampionSpawnInfo.Table.Length)
            {
                return;
            }

            m_Values ??= new TitleInfo[ChampionSpawnInfo.Table.Length];
            var title = m_Values[index];
            if (title == null)
            {
                m_Values[index] = new TitleInfo(value, Core.Now);
                return;
            }

            title.Value += value;
            if (title.LastDecay == DateTime.MinValue)
            {
                title.LastDecay = Core.Now;
            }
        }

        public void Atrophy(int index, int value)
        {
            if (value <= 0 || index < 0 || index >= ChampionSpawnInfo.Table.Length)
            {
                return;
            }

            var title = m_Values?[index];
            if (title == null)
            {
                return;
            }

            var before = title.Value;

            title.Value -= Math.Min(value, title.Value);

            if (before != title.Value)
            {
                title.LastDecay = Core.Now;
            }
        }

        public override string ToString() => "...";

        public static void Serialize(IGenericWriter writer, ChampionTitleInfo titles)
        {
            writer.WriteEncodedInt(0); // version

            writer.WriteEncodedInt(titles.Harrower);

            var length = titles.m_Values?.Length ?? 0;
            writer.WriteEncodedInt(length);

            for (var i = 0; i < length; i++)
            {
                titles.m_Values![i] ??= new TitleInfo();

                TitleInfo.Serialize(writer, titles.m_Values[i]);
            }
        }

        public static bool ShouldAtrophy(PlayerMobile pm)
        {
            var t = pm.ChampionTitles;
            if (t?.m_Values == null)
            {
                return false;
            }

            for (var i = 0; i < t.m_Values.Length; i++)
            {
                var decay = t.GetLastDecay(i);
                if (decay > DateTime.MinValue && decay + LossDelay < Core.Now)
                {
                    return true;
                }
            }

            return false;
        }

        public static void CheckAtrophy(PlayerMobile pm)
        {
            var t = pm.ChampionTitles;
            if (t?.m_Values == null)
            {
                return;
            }

            for (var i = 0; i < t.m_Values.Length; i++)
            {
                var decay = t.GetLastDecay(i);
                if (decay > DateTime.MinValue && decay + LossDelay < Core.Now)
                {
                    t.Atrophy(i, LossAmount);
                }
            }
        }

        // Called when killing a harrower. Will give a minimum of 1 point.
        public static void AwardHarrowerTitle(PlayerMobile pm)
        {
            var t = pm.ChampionTitles;
            if (t == null)
            {
                return;
            }

            if (t.m_Values == null)
            {
                if (t.Harrower == 0)
                {
                    t.Harrower = 1;
                }
                return;
            }

            t.m_Values ??= new TitleInfo[ChampionSpawnInfo.Table.Length];

            var count = 1;
            for (var i = 0; i < t.m_Values.Length; i++)
            {
                if (t.m_Values[i].Value > 900)
                {
                    count++;
                }
            }

            t.Harrower = Math.Max(count, t.Harrower); // Harrower titles never decay.
        }

        private class TitleInfo
        {
            public TitleInfo()
            {
            }

            public TitleInfo(int value, DateTime lastDecay)
            {
                Value = value;
                LastDecay = lastDecay;
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
