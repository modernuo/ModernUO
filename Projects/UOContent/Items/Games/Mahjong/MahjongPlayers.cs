using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Network;

namespace Server.Engines.Mahjong;

[SerializationGenerator(1, false)]
public partial class MahjongPlayers
{
    [SerializableField(0, setter: "private")]
    private Mobile[] _players;

    [SerializableField(1, setter: "private")]
    private bool[] _inGame;

    [SerializableField(2, setter: "private")]
    private bool[] _publicHand;

    [SerializableField(3, setter: "private")]
    private int[] _scores;

    [SerializableField(4, setter: "private")]
    private int _dealerPosition;

    private List<Mobile> _spectators;

    [DirtyTrackingEntity]
    private readonly MahjongGame _game;

    public MahjongPlayers(MahjongGame game, int maxPlayers, int baseScore)
    {
        _game = game;
        _spectators = new List<Mobile>();

        _players = new Mobile[maxPlayers];
        _inGame = new bool[maxPlayers];
        _publicHand = new bool[maxPlayers];
        _scores = new int[maxPlayers];

        for (var i = 0; i < _scores.Length; i++)
        {
            _scores[i] = baseScore;
        }
    }

    public MahjongPlayers(MahjongGame game)
    {
        _game = game;
        _spectators = new List<Mobile>();
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        var seats = reader.ReadInt();
        _players = new Mobile[seats];
        _inGame = new bool[seats];
        _publicHand = new bool[seats];
        _scores = new int[seats];

        for (var i = 0; i < seats; i++)
        {
            _players[i] = reader.ReadEntity<Mobile>();
            _publicHand[i] = reader.ReadBool();
            _scores[i] = reader.ReadInt();
        }

        _dealerPosition = reader.ReadInt();
    }

    public int Seats => _players.Length;
    public Mobile Dealer => _players[DealerPosition];

    public Mobile GetPlayer(int index)
    {
        if (index < 0 || index >= _players.Length)
        {
            return null;
        }

        return _players[index];
    }

    public int GetPlayerIndex(Mobile mobile)
    {
        for (var i = 0; i < _players.Length; i++)
        {
            if (_players[i] == mobile)
            {
                return i;
            }
        }

        return -1;
    }

    public bool IsInGameDealer(Mobile mobile)
    {
        if (Dealer != mobile)
        {
            return false;
        }

        return _inGame[DealerPosition];
    }

    public bool IsInGamePlayer(int index)
    {
        if (index < 0 || index >= _players.Length || _players[index] == null)
        {
            return false;
        }

        return _inGame[index];
    }

    public bool IsInGamePlayer(Mobile mobile)
    {
        var index = GetPlayerIndex(mobile);

        return IsInGamePlayer(index);
    }

    public bool IsSpectator(Mobile mobile) => _spectators.Contains(mobile);

    public int GetScore(int index)
    {
        if (index < 0 || index >= _scores.Length)
        {
            return 0;
        }

        return _scores[index];
    }

    public bool IsPublic(int index)
    {
        if (index < 0 || index >= _publicHand.Length)
        {
            return false;
        }

        return _publicHand[index];
    }

    public void SetPublic(int index, bool value)
    {
        if (index < 0 || index >= _publicHand.Length || _publicHand[index] == value)
        {
            return;
        }

        _publicHand[index] = value;

        SendTilesPacket(true, !_game.SpectatorVision);

        if (IsInGamePlayer(index))
        {
            _players[index].SendLocalizedMessage(value ? 1062775 : 1062776); // Your hand is [not] publicly viewable.
        }
    }

    public List<Mobile> GetInGameMobiles(bool players, bool spectators)
    {
        var list = new List<Mobile>();

        if (players)
        {
            for (var i = 0; i < _players.Length; i++)
            {
                if (IsInGamePlayer(i))
                {
                    list.Add(_players[i]);
                }
            }
        }

        if (spectators)
        {
            list.AddRange(_spectators);
        }

        return list;
    }

    public void CheckPlayers()
    {
        var removed = false;

        Span<byte> relievePacket = stackalloc byte[MahjongPackets.MahjongRelievePacketLength].InitializePacket();

        for (var i = 0; i < _players.Length; i++)
        {
            var player = _players[i];

            if (player == null)
            {
                continue;
            }

            if (player.Deleted)
            {
                _players[i] = null;

                SendPlayerExitMessage(player);
                UpdateDealer(true);

                removed = true;
            }
            else if (_inGame[i])
            {
                if (player.NetState == null)
                {
                    _inGame[i] = false;

                    SendPlayerExitMessage(player);
                    UpdateDealer(true);

                    removed = true;
                }
                else if (!_game.IsAccessibleTo(player) || player.Map != _game.Map ||
                         !player.InRange(_game.GetWorldLocation(), 5))
                {
                    _inGame[i] = false;

                    MahjongPackets.CreateMahjongRelieve(relievePacket, _game.Serial);
                    player.NetState?.Send(relievePacket);

                    SendPlayerExitMessage(player);
                    UpdateDealer(true);

                    removed = true;
                }
            }
        }

        for (var i = 0; i < _spectators.Count;)
        {
            var mobile = _spectators[i];

            if (mobile.NetState == null || mobile.Deleted)
            {
                _spectators.RemoveAt(i);
            }
            else if (!_game.IsAccessibleTo(mobile) || mobile.Map != _game.Map ||
                     !mobile.InRange(_game.GetWorldLocation(), 5))
            {
                _spectators.RemoveAt(i);

                MahjongPackets.CreateMahjongRelieve(relievePacket, _game.Serial);
                mobile.NetState?.Send(relievePacket);
            }
            else
            {
                i++;
            }
        }

        if (removed && !UpdateSpectators())
        {
            SendPlayersPacket(true, true);
        }
    }

    private void UpdateDealer(bool message)
    {
        if (IsInGamePlayer(DealerPosition))
        {
            return;
        }

        for (var i = DealerPosition + 1; i < _players.Length; i++)
        {
            if (IsInGamePlayer(i))
            {
                DealerPosition = i;

                if (message)
                {
                    SendDealerChangedMessage();
                }

                return;
            }
        }

        for (var i = 0; i < DealerPosition; i++)
        {
            if (IsInGamePlayer(i))
            {
                DealerPosition = i;

                if (message)
                {
                    SendDealerChangedMessage();
                }

                return;
            }
        }
    }

    private int GetNextSeat()
    {
        for (var i = DealerPosition; i < _players.Length; i++)
        {
            if (_players[i] == null)
            {
                return i;
            }
        }

        for (var i = 0; i < DealerPosition; i++)
        {
            if (_players[i] == null)
            {
                return i;
            }
        }

        return -1;
    }

    private bool UpdateSpectators()
    {
        if (_spectators.Count == 0)
        {
            return false;
        }

        var nextSeat = GetNextSeat();

        if (nextSeat >= 0)
        {
            var newPlayer = _spectators[0];

            _spectators.RemoveAt(0);

            AddPlayer(newPlayer, nextSeat, false);

            UpdateSpectators();

            return true;
        }

        return false;
    }

    private void AddPlayer(Mobile player, int index, bool sendJoinGame)
    {
        _players[index] = player;
        _inGame[index] = true;

        UpdateDealer(false);

        if (sendJoinGame)
        {
            player.NetState.SendMahjongJoinGame(_game.Serial);
        }

        SendPlayersPacket(true, true);

        player.NetState.SendMahjongGeneralInfo(_game);
        player.NetState.SendMahjongTilesInfo(_game, player);

        if (DealerPosition == index)
        {
            SendLocalizedMessage(1062773, player.Name); // ~1_name~ has entered the game as the dealer.
        }
        else
        {
            SendLocalizedMessage(1062772, player.Name); // ~1_name~ has entered the game as a player.
        }
    }

    private void AddSpectator(Mobile mobile)
    {
        if (!IsSpectator(mobile))
        {
            _spectators.Add(mobile);
        }

        mobile.NetState.SendMahjongJoinGame(_game.Serial);
        mobile.NetState.SendMahjongPlayersInfo(_game, mobile);
        mobile.NetState.SendMahjongGeneralInfo(_game);
        mobile.NetState.SendMahjongTilesInfo(_game, mobile);
    }

    public void Join(Mobile mobile)
    {
        var index = GetPlayerIndex(mobile);

        if (index >= 0)
        {
            AddPlayer(mobile, index, true);
            return;
        }

        var nextSeat = GetNextSeat();

        if (nextSeat >= 0)
        {
            AddPlayer(mobile, nextSeat, true);
        }
        else
        {
            AddSpectator(mobile);
        }
    }

    public void LeaveGame(Mobile player)
    {
        var index = GetPlayerIndex(player);
        if (index >= 0)
        {
            _inGame[index] = false;

            SendPlayerExitMessage(player);
            UpdateDealer(true);

            SendPlayersPacket(true, true);
        }
        else
        {
            _spectators.Remove(player);
        }
    }

    public void ResetScores(int value)
    {
        for (var i = 0; i < _scores.Length; i++)
        {
            _scores[i] = value;
        }

        SendPlayersPacket(true, _game.ShowScores);

        SendLocalizedMessage(1062697); // The dealer redistributes the score sticks evenly.
    }

    public void TransferScore(Mobile from, int toPosition, int amount)
    {
        var fromPosition = GetPlayerIndex(from);
        var to = GetPlayer(toPosition);

        if (fromPosition < 0 || to == null || _scores[fromPosition] < amount)
        {
            return;
        }

        _scores[fromPosition] -= amount;
        _scores[toPosition] += amount;

        if (_game.ShowScores)
        {
            SendPlayersPacket(true, true);
        }
        else
        {
            from.NetState.SendMahjongPlayersInfo(_game, from);
            to.NetState.SendMahjongPlayersInfo(_game, to);
        }

        // ~1_giver~ gives ~2_receiver~ ~3_number~ points.
        SendLocalizedMessage(1062774, $"{from.Name}\t{to.Name}\t{amount}");
    }

    public void OpenSeat(int index)
    {
        var player = GetPlayer(index);
        if (player == null)
        {
            return;
        }

        if (_inGame[index])
        {
            player.NetState.SendMahjongRelieve(_game.Serial);
        }

        _players[index] = null;

        SendLocalizedMessage(1062699, player.Name); // ~1_name~ is relieved from the game by the dealer.

        UpdateDealer(true);

        if (!UpdateSpectators())
        {
            SendPlayersPacket(true, true);
        }
    }

    public void AssignDealer(int index)
    {
        var to = GetPlayer(index);

        if (to == null || !_inGame[index])
        {
            return;
        }

        var oldDealer = DealerPosition;

        DealerPosition = index;

        if (IsInGamePlayer(oldDealer))
        {
            _players[oldDealer].NetState.SendMahjongPlayersInfo(_game, _players[oldDealer]);
        }

        to.NetState.SendMahjongPlayersInfo(_game, to);

        SendDealerChangedMessage();
    }

    private void SendDealerChangedMessage()
    {
        if (Dealer != null)
        {
            SendLocalizedMessage(1062698, Dealer.Name); // ~1_name~ is assigned the dealer.
        }
    }

    private void SendPlayerExitMessage(Mobile who)
    {
        SendLocalizedMessage(1062762, who.Name); // ~1_name~ has left the game.
    }

    public void SendPlayersPacket(bool players, bool spectators)
    {
        foreach (var mobile in GetInGameMobiles(players, spectators))
        {
            mobile.NetState.SendMahjongPlayersInfo(_game, mobile);
        }
    }

    public void SendGeneralPacket(bool players, bool spectators)
    {
        var mobiles = GetInGameMobiles(players, spectators);

        if (mobiles.Count == 0)
        {
            return;
        }

        Span<byte> generalInfo = stackalloc byte[MahjongPackets.MahjongGeneralInfoPacketLength].InitializePacket();

        foreach (var mobile in mobiles)
        {
            MahjongPackets.CreateMahjongGeneralInfo(generalInfo, _game);
            mobile.NetState?.Send(generalInfo);
        }
    }

    public void SendTilesPacket(bool players, bool spectators)
    {
        foreach (var mobile in GetInGameMobiles(players, spectators))
        {
            mobile.NetState.SendMahjongTilesInfo(_game, mobile);
        }
    }

    public void SendTilePacket(MahjongTile tile, bool players, bool spectators)
    {
        foreach (var mobile in GetInGameMobiles(players, spectators))
        {
            mobile.NetState.SendMahjongTileInfo(tile, mobile);
        }
    }

    public void SendRelievePacket(bool players, bool spectators)
    {
        var mobiles = GetInGameMobiles(players, spectators);

        if (mobiles.Count == 0)
        {
            return;
        }

        Span<byte> relievePacket = stackalloc byte[MahjongPackets.MahjongRelievePacketLength].InitializePacket();

        foreach (var mobile in mobiles)
        {
            MahjongPackets.CreateMahjongRelieve(relievePacket, _game.Serial);
            mobile.NetState?.Send(relievePacket);
        }
    }

    public void SendLocalizedMessage(int number)
    {
        foreach (var mobile in GetInGameMobiles(true, true))
        {
            mobile.SendLocalizedMessage(number);
        }
    }

    public void SendLocalizedMessage(int number, string args)
    {
        foreach (var mobile in GetInGameMobiles(true, true))
        {
            mobile.SendLocalizedMessage(number, args);
        }
    }
}
