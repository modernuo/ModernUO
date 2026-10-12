using System;

namespace Server.BotAI.V2;

/// <summary>Why a bot able to answer a want passed it over. Recorded on the want by whoever passed; see <see cref="BotWant.Passed"/>.</summary>
public enum BotPass
{
    /// <summary>The buyer stands past a carrier's reach (<see cref="BotCourier.Reach"/>).</summary>
    Reach,

    /// <summary>The buyer is on another land or another map: no gate joins the two.</summary>
    Land,

    /// <summary>No way through to the buyer, or the carrier's ledger is wary of the buyer's place.</summary>
    Sealed,

    /// <summary>The buyer could not pay for one at its own offer.</summary>
    Poor,

    /// <summary>The delivery was worth less than <see cref="BotCourier.LeastGold"/>.</summary>
    Petty,

    /// <summary>A maker with the recipe was short of the material.</summary>
    Material,

    /// <summary>A trader found nowhere to have it.</summary>
    Source,

    /// <summary>A trader found a source, and no plan that paid: margin, the buyer's purse, its pack, or the road.</summary>
    Margin,

    /// <summary>A trader judged the buyer's ground or the road to it too dangerous.</summary>
    Peril,

    /// <summary>The buyer was dead when a carrier holding the goods looked.</summary>
    Dead,

    /// <summary>
    /// The straight road from the carrier to the buyer asks more strength than the carrier brings (<see cref="BotDanger.Road"/>), the
    /// same question its auction would refuse the walk on (06.10.2026, needs15). Asked by the carrier before it offers the walk, so
    /// it offers the next want it can walk instead of one its own auction vetoes.
    /// </summary>
    Road,

    /// <summary>
    /// The buyer's pack refused a hand-over lately and shows no room yet (<see cref="BotRoom"/>, 06.10.2026, deliver16): no carrier
    /// walks to a pack that will refuse it again.
    /// </summary>
    Room
}

/// <summary>How the units of a line reached the buyer. A line met by more than one route is counted under each.</summary>
[Flags]
public enum BotRoute
{
    None = 0,

    /// <summary>A carrier's walk (<see cref="BotDeliver"/>).</summary>
    Carrier = 1,

    /// <summary>A trader's book (<c>BotHaul</c>).</summary>
    Trader = 2,

    /// <summary>The buyer at a bot's shop by the bank (<see cref="BotShopkeep"/>).</summary>
    Shop = 4
}

/// <summary>Why a buyer took its own want back. See <see cref="BotMarket.Withdrawn"/>.</summary>
public enum BotWithdraw
{
    /// <summary>It has the thing now, some other way: its own bank box, its own make, a book, a counter.</summary>
    Have,

    /// <summary>It waited for a carrier, nobody came, and it walks to a counter instead.</summary>
    Walked,

    /// <summary>It could not pay at the meeting.</summary>
    Unpaid,

    /// <summary>A trader's gathering at the bank closed.</summary>
    Closed,

    Other
}

/// <summary>Where a line stands.</summary>
public enum BotLineState
{
    /// <summary>Inside its hour and not yet met.</summary>
    Open,

    /// <summary>Met to the last unit inside its hour (<see cref="BotLines.WindowMs"/>).</summary>
    Met,

    /// <summary>Not met inside its hour, or ended after its first ten minutes without being met.</summary>
    Failed,

    /// <summary>Left out of the share, and printed: the buyer gone, or the line taken back or unpaid inside its first ten minutes.</summary>
    KeptOut
}

/// <summary>
/// One line of a want: the units a buyer asked for at one time, with its own clock (02.10.2026, build 391; the Needs plan).
///
/// <para>
/// <b>Patrick's measure is a line, not a want.</b> "A new line is fulfilled within its first hour, by one bot or jointly", target
/// 99 %. A want is one per buyer and kind and is topped up in place, so a want's age is not a line's age: the first ask opens a
/// line, and a top-up more than <see cref="BotLines.NewLineMs"/> after the newest line opened opens another on the same want.
/// Units handed over against the want fill its lines oldest first. A line is settled once — met, failed or kept out — and a line
/// that failed its hour and is met later is counted as met after the hour, never as met.
/// </para>
///
/// <para>
/// <b>What the line remembers is what a failure needs to name its cause without asking anybody twice.</b> The want counts who
/// passed it over and why (<see cref="BotWant.Passed"/>), the walks offered for it and the carriers that took one, and how long its
/// buyer lay dead; a line keeps those counts as they stood when it opened, so the difference at its end is its own.
/// </para>
/// </summary>
public sealed class BotLine
{
    /// <summary>The most contributors told apart on one line; more than this is still "jointly".</summary>
    private const int MostHands = 4;

    private readonly Serial[] _hands = new Serial[MostHands];

    internal BotLine(BotWant want, int seq, int units, long opened, bool procurement)
    {
        Want = want;
        Seq = seq;
        Units = Math.Max(1, units);
        OpenedTick = opened;
        Procurement = procurement;

        // A real tick, never the default: see FirstFillTick.
        FirstFillTick = opened;
        Snapshot();
    }

    public BotWant Want { get; }

    /// <summary>Its number on the want: the first ask is 1.</summary>
    public int Seq { get; }

    /// <summary>"12.1": want 12, line 1. The name the log and the page use.</summary>
    public string Name => $"{Want?.Id ?? 0}.{Seq}";

    /// <summary>Units asked on this line.</summary>
    public int Units { get; private set; }

    /// <summary>Units handed to the buyer against this line.</summary>
    public int Filled { get; private set; }

    public long OpenedTick { get; private set; }

    /// <summary>When the first unit arrived; meaningful only once <see cref="AnyFill"/>.</summary>
    public long FirstFillTick { get; private set; }

    public bool AnyFill { get; private set; }

    /// <summary>A trader's want: procurement for another line, counted apart and kept out of the share (as <see cref="BotGlut"/> keeps it out of demand).</summary>
    public bool Procurement { get; }

    /// <summary>Read back from the save after a restart: its clock went on from where it stood.</summary>
    public bool Restored { get; internal set; }

    public BotLineState State { get; internal set; }

    /// <summary>Failed its hour, and met to the last unit later.</summary>
    public bool MetLate { get; internal set; }

    /// <summary>Why it failed or was kept out; null while open or met.</summary>
    public string Cause { get; internal set; }

    public BotRoute Routes { get; private set; }

    /// <summary>Distinct bots that handed units against it (up to <see cref="MostHands"/> told apart).</summary>
    public int Hands { get; private set; }

    public bool Complete => Filled >= Units;

    public bool Settled => State != BotLineState.Open;

    // The want's own counts as they stood when this line opened. See the class remarks.
    internal readonly int[] PassBase = new int[BotLines.PassKinds];

    internal int OffersBase;

    internal int TakesBase;

    internal long DeadBase;

    internal long AwayBase;

    internal void Snapshot()
    {
        if (Want == null)
        {
            return;
        }

        for (var i = 0; i < PassBase.Length; i++)
        {
            PassBase[i] = Want.PassCount(i);
        }

        OffersBase = Want.Offers;
        TakesBase = Want.Takes;
        DeadBase = Want.DeadMs;
        AwayBase = Want.AwayMs;
    }

    /// <summary>More units asked on this line, inside <see cref="BotLines.NewLineMs"/> of its opening.</summary>
    internal void More(int units)
    {
        if (units > 0)
        {
            Units += units;
        }
    }

    /// <summary>Units handed over against it, by whom and how.</summary>
    internal void Fill(int units, Mobile seller, BotRoute route, long now)
    {
        if (units <= 0)
        {
            return;
        }

        if (!AnyFill)
        {
            AnyFill = true;
            FirstFillTick = now;
        }

        Filled += units;
        Routes |= route;

        if (seller == null)
        {
            return;
        }

        var known = Math.Min(Hands, MostHands);

        for (var i = 0; i < known; i++)
        {
            if (_hands[i] == seller.Serial)
            {
                return;
            }
        }

        if (Hands < MostHands)
        {
            _hands[Hands] = seller.Serial;
        }

        Hands++;
    }

    internal int PassedSince(int kind) => Want == null ? 0 : Math.Max(0, Want.PassCount(kind) - PassBase[kind]);

    internal int OffersSince => Want == null ? 0 : Math.Max(0, Want.Offers - OffersBase);

    internal int TakesSince => Want == null ? 0 : Math.Max(0, Want.Takes - TakesBase);

    // ---- Persistence: ages, never ticks (a tick belongs to the process that made it). See BotMarketStore. ----------------

    internal void Save(IGenericWriter writer, long now)
    {
        writer.WriteEncodedInt(Seq);
        writer.WriteEncodedInt(Units);
        writer.WriteEncodedInt(Filled);
        writer.Write(Math.Max(0L, now - OpenedTick));
        writer.Write(AnyFill);
        writer.Write(AnyFill ? Math.Max(0L, now - FirstFillTick) : 0L);
        writer.WriteEncodedInt((int)State);
        writer.Write(MetLate);
        writer.Write(Cause ?? "");
        writer.WriteEncodedInt((int)Routes);
        writer.Write(Procurement);

        var known = Math.Min(Hands, MostHands);

        writer.WriteEncodedInt(Hands);
        writer.WriteEncodedInt(known);

        for (var i = 0; i < known; i++)
        {
            writer.Write(_hands[i]);
        }
    }

    /// <summary>One line as the save holds it, before its want is made again. See <see cref="BotMarket.Load"/>.</summary>
    internal readonly record struct Record(
        int Seq, int Units, int Filled, long Age, bool AnyFill, long FirstAge, int State, bool MetLate, string Cause, int Routes,
        bool Procurement, int Hands, Serial[] Known
    );

    internal static Record Read(IGenericReader reader)
    {
        var seq = reader.ReadEncodedInt();
        var units = reader.ReadEncodedInt();
        var filled = reader.ReadEncodedInt();
        var age = reader.ReadLong();
        var anyFill = reader.ReadBool();
        var firstAge = reader.ReadLong();
        var state = reader.ReadEncodedInt();
        var metLate = reader.ReadBool();
        var cause = reader.ReadString();
        var routes = reader.ReadEncodedInt();
        var procurement = reader.ReadBool();
        var hands = reader.ReadEncodedInt();
        var known = reader.ReadEncodedInt();
        var serials = new Serial[Math.Max(0, known)];

        for (var i = 0; i < serials.Length; i++)
        {
            serials[i] = reader.ReadSerial();
        }

        return new Record(seq, units, filled, age, anyFill, firstAge, state, metLate, cause, routes, procurement, hands, serials);
    }

    internal static BotLine Restore(BotWant want, in Record record, long now)
    {
        var line = new BotLine(want, Math.Max(1, record.Seq), record.Units, now - Math.Max(0L, record.Age), record.Procurement)
        {
            Filled = Math.Max(0, record.Filled),
            AnyFill = record.AnyFill,
            State = Enum.IsDefined(typeof(BotLineState), record.State) ? (BotLineState)record.State : BotLineState.Open,
            MetLate = record.MetLate,
            Cause = string.IsNullOrEmpty(record.Cause) ? null : record.Cause,
            Routes = (BotRoute)(record.Routes & 7),
            Restored = true
        };

        if (record.AnyFill)
        {
            line.FirstFillTick = now - Math.Max(0L, record.FirstAge);
        }

        var known = record.Known ?? [];

        for (var i = 0; i < known.Length && i < MostHands; i++)
        {
            line._hands[i] = known[i];
        }

        line.Hands = Math.Max(record.Hands, Math.Min(known.Length, MostHands));

        return line;
    }
}
