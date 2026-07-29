using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Factions;

public class PlayerState : IComparable<PlayerState>
{
    private Town m_Finance;

    private int m_KillPoints;
    private MerchantTitle m_MerchantTitle;
    private RankDefinition m_Rank;
    private int m_RankIndex = -1;

    private Town m_Sheriff;

    public PlayerState(Mobile mob, Faction faction, List<PlayerState> owner)
    {
        Mobile = mob;
        Faction = faction;
        Owner = owner;

        // Owner does not contain this state yet, so the count is short by one; the caller ranks it
        // after inserting.
        SeedLowestRank();

        Attach();
        Invalidate();
    }

    public PlayerState(IGenericReader reader, Faction faction, List<PlayerState> owner)
    {
        Faction = faction;
        Owner = owner;

        var version = reader.ReadEncodedInt();

        switch (version)
        {
            case 1:
                {
                    IsActive = reader.ReadBool();
                    LastHonorTime = reader.ReadDateTime();
                    goto case 0;
                }
            case 0:
                {
                    Mobile = reader.ReadEntity<Mobile>();

                    m_KillPoints = reader.ReadEncodedInt();
                    m_MerchantTitle = (MerchantTitle)reader.ReadEncodedInt();

                    Leaving = reader.ReadDateTime();

                    break;
                }
        }

        // Members are still being read; FactionState ranks everyone once the ordering settles.
        SeedLowestRank();

        Attach();
    }

    public Mobile Mobile { get; }

    public Faction Faction { get; }

    public List<PlayerState> Owner { get; }

    public MerchantTitle MerchantTitle
    {
        get => m_MerchantTitle;
        set
        {
            m_MerchantTitle = value;
            Invalidate();
        }
    }

    public Town Sheriff
    {
        get => m_Sheriff;
        set
        {
            m_Sheriff = value;
            Invalidate();
        }
    }

    public Town Finance
    {
        get => m_Finance;
        set
        {
            m_Finance = value;
            Invalidate();
        }
    }

    public List<SilverGivenEntry> SilverGiven { get; private set; }

    public int KillPoints
    {
        get => m_KillPoints;
        set
        {
            if (m_KillPoints != value)
            {
                if (value > m_KillPoints)
                {
                    if (m_KillPoints <= 0)
                    {
                        if (value <= 0)
                        {
                            m_KillPoints = value;
                            Invalidate();
                            return;
                        }

                        Owner.Remove(this);
                        Owner.Insert(Faction.ZeroRankOffset, this);

                        // Direct, not through RankIndex: ZeroRankOffset is mid-update. The
                        // UpdateRank() at the end of this setter covers it.
                        m_RankIndex = Faction.ZeroRankOffset;
                        Faction.ZeroRankOffset++;
                    }

                    while (m_RankIndex - 1 >= 0)
                    {
                        var p = Owner[m_RankIndex - 1];
                        if (value > p.KillPoints)
                        {
                            Owner[m_RankIndex] = p;
                            Owner[m_RankIndex - 1] = this;
                            RankIndex--;
                            p.RankIndex++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    if (value <= 0)
                    {
                        if (m_KillPoints <= 0)
                        {
                            m_KillPoints = value;
                            Invalidate();
                            return;
                        }

                        while (m_RankIndex + 1 < Faction.ZeroRankOffset)
                        {
                            var p = Owner[m_RankIndex + 1];
                            Owner[m_RankIndex + 1] = this;
                            Owner[m_RankIndex] = p;
                            RankIndex++;
                            p.RankIndex--;
                        }

                        m_RankIndex = -1;
                        Faction.ZeroRankOffset--;
                    }
                    else
                    {
                        while (m_RankIndex + 1 < Faction.ZeroRankOffset)
                        {
                            var p = Owner[m_RankIndex + 1];
                            if (value < p.KillPoints)
                            {
                                Owner[m_RankIndex + 1] = this;
                                Owner[m_RankIndex] = p;
                                RankIndex++;
                                p.RankIndex--;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }

                m_KillPoints = value;
                UpdateRank();
                Invalidate();
            }
        }
    }

    public int RankIndex
    {
        get => m_RankIndex;
        set
        {
            if (m_RankIndex != value)
            {
                m_RankIndex = value;

                UpdateRank();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Read from PlayerMobile.GetProperties, so it must stay a plain field read -- recomputing or
    /// invalidating here re-enters the property list build. Maintained by <see cref="UpdateRank"/>.
    /// </summary>
    public RankDefinition Rank => m_Rank;

    // Lowest rank (Required 0): correct for an unranked member, and never null, so Rank.Title
    // cannot NRE before the first UpdateRank().
    private void SeedLowestRank()
    {
        var ranks = Faction.Definition.Ranks;

        if (ranks.Length > 0)
        {
            m_Rank = ranks[^1];
        }
    }

    /// <summary>
    /// Recomputes the cached rank. Call whenever <see cref="RankIndex"/>, the faction's
    /// ZeroRankOffset, or the member count changes -- and only once they have settled.
    /// </summary>
    public void UpdateRank()
    {
        var ranks = Faction.Definition.Ranks;

        if (ranks.Length == 0)
        {
            return;
        }

        int percent;

        if (Owner.Count == 1)
        {
            percent = 1000;
        }
        else if (m_RankIndex == -1 || Faction.ZeroRankOffset <= 0)
        {
            percent = 0;
        }
        else
        {
            percent = (Faction.ZeroRankOffset - m_RankIndex) * 1000 / Faction.ZeroRankOffset;
        }

        // Ranks run Required-descending ending at 0, so anything >= 0 matches below. A negative
        // percent (RankIndex out of sync with ZeroRankOffset) would otherwise leave it null.
        m_Rank = ranks[^1];

        for (var i = 0; i < ranks.Length; i++)
        {
            var check = ranks[i];

            if (percent >= check.Required)
            {
                m_Rank = check;
                break;
            }
        }
    }

    public DateTime LastHonorTime { get; set; }

    public DateTime Leaving { get; set; }

    public bool IsLeaving => Leaving > DateTime.MinValue;

    public bool IsActive { get; set; }

    public int CompareTo(PlayerState ps) => (ps?.m_KillPoints ?? 0) - m_KillPoints;

    public bool CanGiveSilverTo(Mobile mob)
    {
        for (var i = 0; i < SilverGiven?.Count; ++i)
        {
            var sge = SilverGiven[i];

            if (sge.IsExpired)
            {
                SilverGiven.RemoveAt(i--);
            }
            else if (sge.GivenTo == mob)
            {
                return false;
            }
        }

        return true;
    }

    public void OnGivenSilverTo(Mobile mob)
    {
        SilverGiven ??= [];

        SilverGiven.Add(new SilverGivenEntry(mob));
    }

    public void Invalidate()
    {
        (Mobile as PlayerMobile)?.InvalidateProperties();
    }

    public void Attach()
    {
        if (Mobile is PlayerMobile mobile)
        {
            mobile.FactionPlayerState = this;
        }
    }

    public void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(1); // version

        writer.Write(IsActive);
        writer.Write(LastHonorTime);

        writer.Write(Mobile);

        writer.WriteEncodedInt(m_KillPoints);
        writer.WriteEncodedInt((int)m_MerchantTitle);

        writer.Write(Leaving);
    }

    public static PlayerState Find(Mobile mob) => mob is PlayerMobile mobile ? mobile.FactionPlayerState : null;
}