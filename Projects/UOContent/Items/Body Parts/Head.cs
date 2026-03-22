using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items
{
    public enum HeadType
    {
        Regular,
        Duel,
        Tournament
    }

    [SerializationGenerator(2, false)]
    public partial class Head : Item
    {
        [InvalidateProperties]
        [SerializableField(0)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private string _playerName;

        [InvalidateProperties]
        [SerializableField(1)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private HeadType _headType;

        [SerializableField(2)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private PlayerMobile _bountyTarget;

        [SerializableField(3)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private DateTime _carvedTime;

        [Constructible]
        public Head(string playerName) : this(HeadType.Regular, playerName)
        {
        }

        [Constructible]
        public Head(HeadType headType = HeadType.Regular, string playerName = null) : base(0x1DA0)
        {
            _headType = headType;
            _playerName = playerName;
            _carvedTime = Core.Now;
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

        // Pre-codegen legacy data (v1 only — playerName + headType)
        private void Deserialize(IGenericReader reader, int version)
        {
            _playerName = reader.ReadString();
            _headType = (HeadType)reader.ReadEncodedInt();
            // _bountyTarget and _carvedTime default to null / DateTime.MinValue
        }

        private void MigrateFrom(V1Content content)
        {
            _playerName = content.PlayerName;
            _headType = content.HeadType;
            // _bountyTarget defaults to null
            // _carvedTime defaults to DateTime.MinValue (treated as expired)
        }

    }
}
