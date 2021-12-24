using System;

namespace Server.Factions
{
    public interface IFactionItem
    {
        FactionItem FactionItemState { get; set; }
    }

    public class FactionItem
    {
        public static readonly TimeSpan ExpirationPeriod = TimeSpan.FromDays(21.0);

        public FactionItem(Item item, Faction faction)
        {
            Item = item;
            Faction = faction;
        }

        public FactionItem(IGenericReader reader, Faction faction)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        Item = reader.ReadEntity<Item>();
                        Expiration = reader.ReadDateTime();
                        break;
                    }
            }

            Faction = faction;
        }

        public Item Item { get; }

        public Faction Faction { get; }

        public DateTime Expiration { get; private set; }

        public bool HasExpired
        {
            get
            {
                if (Item?.Deleted != false)
                {
                    return true;
                }

                return Expiration != DateTime.MinValue && Core.Now >= Expiration;
            }
        }

        public void StartExpiration()
        {
            Expiration = Core.Now + ExpirationPeriod;
        }

        public void CheckAttach()
        {
            if (!HasExpired)
            {
                Attach();
            }
            else
            {
                Detach();
            }
        }

        public void Attach()
        {
            if (Item is IFactionItem item)
            {
                item.FactionItemState = this;
            }

            Faction?.State.FactionItems.Add(this);
        }

        public void Detach()
        {
            if (Item is IFactionItem item)
            {
                item.FactionItemState = null;
            }

            if (Faction?.State.FactionItems.Contains(this) == true)
            {
                Faction.State.FactionItems.Remove(this);
            }
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0);

            writer.Write(Item);
            writer.Write(Expiration);
        }

        public static int GetMaxWearables(Mobile mob)
        {
            var pl = PlayerState.Find(mob);

            return pl == null ? 0 :
                pl.Faction.IsCommander(mob) ? 9 : pl.Rank.MaxWearables;
        }

        public static FactionItem Find(Item item)
        {
            if (item is IFactionItem factionItem)
            {
                var state = factionItem.FactionItemState;

                if (state?.HasExpired == true)
                {
                    state.Detach();
                    state = null;
                }

                return state;
            }

            return null;
        }

        public static Item Imbue(Item item, Faction faction, bool expire, int hue)
        {
            if (item is not IFactionItem)
            {
                return item;
            }

            var state = Find(item);

            if (state == null)
            {
                state = new FactionItem(item, faction);
                state.Attach();
            }

            if (expire)
            {
                state.StartExpiration();
            }

            item.Hue = hue;
            return item;
        }
    }
}
