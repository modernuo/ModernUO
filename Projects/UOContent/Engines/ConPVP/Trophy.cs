using System;

namespace Server.Items
{
    public enum TrophyRank
    {
        Bronze,
        Silver,
        Gold
    }

    [Flippable(5020, 4647)]
    public class Trophy : Item
    {
        private TrophyRank m_Rank;

        [Constructible]
        public Trophy(string title, TrophyRank rank) : base(5020)
        {
            Title = title;
            m_Rank = rank;
            Date = Core.Now;

            LootType = LootType.Blessed;

            UpdateStyle();
        }

        public Trophy(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Title { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TrophyRank Rank
        {
            get => m_Rank;
            set
            {
                m_Rank = value;
                UpdateStyle();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime Date { get; private set; }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(Title);
            writer.Write((int)m_Rank);
            writer.Write(Owner);
            writer.Write(Date);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Title = reader.ReadString();
            m_Rank = (TrophyRank)reader.ReadInt();
            Owner = reader.ReadEntity<Mobile>();
            Date = reader.ReadDateTime();

            if (version == 0)
            {
                LootType = LootType.Blessed;
            }
        }

        public override void OnAdded(IEntity parent)
        {
            base.OnAdded(parent);

            Owner ??= RootParent as Mobile;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (Owner != null)
            {
                LabelTo(from, $"{Title} -- {Owner.RawName}");
            }
            else if (Title != null)
            {
                LabelTo(from, Title);
            }

            if (Date != DateTime.MinValue)
            {
                LabelTo(from, Date.ToString("d"));
            }
        }

        public void UpdateStyle()
        {
            Name = $"{m_Rank.ToString().ToLower()} trophy";

            Hue = m_Rank switch
            {
                TrophyRank.Gold   => 2213,
                TrophyRank.Silver => 0,
                TrophyRank.Bronze => 2206,
                _                 => Hue
            };
        }
    }
}
