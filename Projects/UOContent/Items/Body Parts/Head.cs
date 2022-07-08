using ModernUO.Serialization;

namespace Server.Items
{
    public enum HeadType
    {
        Regular,
        Duel,
        Tournament
    }

    [SerializationGenerator(1, false)]
    public partial class Head : Item
    {
        [InvalidateProperties]
        [SerializableField(0)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private string _playerName;

        [InvalidateProperties]
        [SerializableField(1)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private HeadType _headType;

        [Constructible]
        public Head(string playerName) : this(HeadType.Regular, playerName)
        {
        }

        [Constructible]
        public Head(HeadType headType = HeadType.Regular, string playerName = null) : base(0x1DA0)
        {
            _headType = headType;
            _playerName = playerName;
        }

        public override string DefaultName
        {
            get
            {
                if (_playerName == null)
                {
                    return base.DefaultName;
                }

                return _headType switch
                {
                    HeadType.Duel       => $"the head of {_playerName}, taken in a duel",
                    HeadType.Tournament => $"the head of {_playerName}, taken in a tournament",
                    _                   => $"the head of {_playerName}"
                };
            }
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            _playerName = reader.ReadString();
            _headType = (HeadType)reader.ReadEncodedInt();
        }
    }
}
