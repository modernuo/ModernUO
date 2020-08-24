namespace Server.Items
{
    public enum HeadType
    {
        Regular,
        Duel,
        Tournament
    }

    public class Head : Item
    {
        [Constructible]
        public Head(string playerName) : this(HeadType.Regular, playerName)
        {
        }

        [Constructible]
        public Head(HeadType headType = HeadType.Regular, string playerName = null)
            : base(0x1DA0)
        {
            HeadType = headType;
            PlayerName = playerName;
        }

        public Head(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string PlayerName { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public HeadType HeadType { get; set; }

        public override string DefaultName
        {
            get
            {
                if (PlayerName == null)
                    return base.DefaultName;

                return HeadType switch
                {
                    HeadType.Duel => $"the head of {PlayerName}, taken in a duel",
                    HeadType.Tournament => $"the head of {PlayerName}, taken in a tournament",
                    _ => $"the head of {PlayerName}"
                };
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(PlayerName);
            writer.WriteEncodedInt((int)HeadType);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    PlayerName = reader.ReadString();
                    HeadType = (HeadType)reader.ReadEncodedInt();
                    break;

                case 0:
                    string format = Name;

                    if (format != null)
                    {
                        if (format.StartsWith("the head of "))
                            format = format.Substring("the head of ".Length);

                        if (format.EndsWith(", taken in a duel"))
                        {
                            format = format.Substring(0, format.Length - ", taken in a duel".Length);
                            HeadType = HeadType.Duel;
                        }
                        else if (format.EndsWith(", taken in a tournament"))
                        {
                            format = format.Substring(0, format.Length - ", taken in a tournament".Length);
                            HeadType = HeadType.Tournament;
                        }
                    }

                    PlayerName = format;
                    Name = null;

                    break;
            }
        }
    }
}
