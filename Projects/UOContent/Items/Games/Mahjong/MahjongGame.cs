using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;

namespace Server.Engines.Mahjong;

[SerializationGenerator(1, false)]
public partial class MahjongGame : Item, ISecurable
{
    public const int MaxPlayers = 4;
    public const int BaseScore = 30000;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [SerializableField(1, setter: "private")]
    private MahjongTile[] _tiles;

    [SerializableField(2, setter: "private")]
    private MahjongDealerIndicator _dealerIndicator;

    [SerializableField(3, setter: "private")]
    private MahjongWallBreakIndicator _wallBreakIndicator;

    [SerializableField(4, setter: "private")]
    private MahjongDices _dices;

    [SerializableField(5, setter: "private")]
    private MahjongPlayers _players;

    private DateTime _lastReset;

    [Constructible]
    public MahjongGame() : base(0xFAA)
    {
        Weight = 5.0;

        BuildWalls();
        _dealerIndicator =
            new MahjongDealerIndicator(this, new Point2D(300, 300), MahjongPieceDirection.Up, MahjongWind.North);
        _wallBreakIndicator = new MahjongWallBreakIndicator(this, new Point2D(335, 335));
        _dices = new MahjongDices(this);
        _players = new MahjongPlayers(this, MaxPlayers, BaseScore);
        _lastReset = Core.Now;
        _level = SecureLevel.CoOwners;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    [SerializableProperty(6)]
    public bool ShowScores
    {
        get => _showScores;
        set
        {
            if (_showScores == value)
            {
                return;
            }

            _showScores = value;

            if (value)
            {
                _players.SendPlayersPacket(true, true);
            }

            _players.SendGeneralPacket(true, true);
            _players.SendLocalizedMessage(value ? 1062777 : 1062778); // The dealer has enabled/disabled score display.
            this.MarkDirty();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    [SerializableProperty(7)]
    public bool SpectatorVision
    {
        get => _spectatorVision;
        set
        {
            if (_spectatorVision == value)
            {
                return;
            }

            _spectatorVision = value;

            if (_players.IsInGamePlayer(_players.DealerPosition))
            {
                _players.Dealer.NetState.SendMahjongGeneralInfo(this);
            }

            _players.SendTilesPacket(false, true);
            _players.SendLocalizedMessage(value ? 1062715 : 1062716); // The dealer has enabled/disabled Spectator Vision.
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    private void BuildHorizontalWall(
        ref int index, int x, int y, int stackLevel, MahjongPieceDirection direction,
        MahjongTileTypeGenerator typeGenerator
    )
    {
        for (var i = 0; i < 17; i++)
        {
            var position = new Point2D(x + i * 20, y);
            Tiles[index + i] = new MahjongTile(
                this,
                index + i,
                typeGenerator.Next(),
                position,
                stackLevel,
                direction,
                false
            );
        }

        index += 17;
        this.MarkDirty();
    }

    private void BuildVerticalWall(
        ref int index, int x, int y, int stackLevel, MahjongPieceDirection direction,
        MahjongTileTypeGenerator typeGenerator
    )
    {
        for (var i = 0; i < 17; i++)
        {
            var position = new Point2D(x, y + i * 20);
            Tiles[index + i] = new MahjongTile(
                this,
                index + i,
                typeGenerator.Next(),
                position,
                stackLevel,
                direction,
                false
            );
        }

        index += 17;
        this.MarkDirty();
    }

    private void BuildWalls()
    {
        Tiles = new MahjongTile[136];

        var typeGenerator = new MahjongTileTypeGenerator();

        var i = 0;

        BuildHorizontalWall(ref i, 165, 110, 0, MahjongPieceDirection.Up, typeGenerator);
        BuildHorizontalWall(ref i, 165, 115, 1, MahjongPieceDirection.Up, typeGenerator);

        BuildVerticalWall(ref i, 530, 165, 0, MahjongPieceDirection.Left, typeGenerator);
        BuildVerticalWall(ref i, 525, 165, 1, MahjongPieceDirection.Left, typeGenerator);

        BuildHorizontalWall(ref i, 165, 530, 0, MahjongPieceDirection.Down, typeGenerator);
        BuildHorizontalWall(ref i, 165, 525, 1, MahjongPieceDirection.Down, typeGenerator);

        BuildVerticalWall(ref i, 110, 165, 0, MahjongPieceDirection.Right, typeGenerator);
        BuildVerticalWall(ref i, 115, 165, 1, MahjongPieceDirection.Right, typeGenerator);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_spectatorVision)
        {
            list.Add(1062717); // Spectator Vision Enabled
        }
        else
        {
            list.Add(1062718); // Spectator Vision Disabled
        }
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);

        _players.CheckPlayers();

        if (from.Alive && IsAccessibleTo(from) && _players.GetInGameMobiles(true, false).Count == 0)
        {
            list.Add(new ResetGameEntry(this));
        }

        SetSecureLevelEntry.AddTo(from, this, list);
    }

    public override void OnDoubleClick(Mobile from)
    {
        _players.CheckPlayers();
        _players.Join(from);
    }

    public void ResetGame(Mobile from)
    {
        if (Core.Now - _lastReset < TimeSpan.FromSeconds(5.0))
        {
            return;
        }

        _lastReset = Core.Now;

        if (from != null)
        {
            _players.SendLocalizedMessage(1062771, from.Name); // ~1_name~ has reset the game.
        }

        _players.SendRelievePacket(true, true);

        BuildWalls();
        DealerIndicator =
            new MahjongDealerIndicator(this, new Point2D(300, 300), MahjongPieceDirection.Up, MahjongWind.North);
        WallBreakIndicator = new MahjongWallBreakIndicator(this, new Point2D(335, 335));
        Players = new MahjongPlayers(this, MaxPlayers, BaseScore);
    }

    public void ResetWalls(Mobile from)
    {
        if (Core.Now - _lastReset < TimeSpan.FromSeconds(5.0))
        {
            return;
        }

        _lastReset = Core.Now;

        BuildWalls();

        _players.SendTilesPacket(true, true);

        if (from != null)
        {
            _players.SendLocalizedMessage(1062696); // The dealer rebuilds the wall.
        }
    }

    public int GetStackLevel(MahjongPieceDim dim)
    {
        var level = -1;
        foreach (var tile in _tiles)
        {
            if (tile.StackLevel > level && dim.IsOverlapping(tile.Dimensions))
            {
                level = tile.StackLevel;
            }
        }

        return level;
    }

    private void Deserialize(IGenericReader reader, int number)
    {
        _level = (SecureLevel)reader.ReadInt();
        var length = reader.ReadInt();
        _tiles = new MahjongTile[length];

        for (var i = 0; i < length; i++)
        {
            var tile = _tiles[i] = new MahjongTile(this);
            tile.Deserialize(reader);
        }

        _dealerIndicator = new MahjongDealerIndicator(this);
        _dealerIndicator.Deserialize(reader);

        _wallBreakIndicator = new MahjongWallBreakIndicator(this);
        _wallBreakIndicator.Deserialize(reader);

        _dices = new MahjongDices(this);
        _dices.Deserialize(reader);

        _players = new MahjongPlayers(this);
        _players.Deserialize(reader);

        _showScores = reader.ReadBool();
        _spectatorVision = reader.ReadBool();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _lastReset = Core.Now;
    }

    private class ResetGameEntry : ContextMenuEntry
    {
        private readonly MahjongGame _game;

        public ResetGameEntry(MahjongGame game) : base(6162) => _game = game;

        public override void OnClick()
        {
            var from = Owner.From;

            if (from.CheckAlive() && !_game.Deleted && _game.IsAccessibleTo(from) &&
                _game.Players.GetInGameMobiles(true, false).Count == 0)
            {
                _game.ResetGame(from);
            }
        }
    }
}
