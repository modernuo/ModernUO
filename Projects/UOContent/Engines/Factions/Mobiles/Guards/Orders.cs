using System.Collections.Generic;

namespace Server.Factions.AI
{
    public enum ReactionType
    {
        Ignore,
        Warn,
        Attack
    }

    public enum MovementType
    {
        Stand,
        Patrol,
        Follow
    }

    public class Reaction
    {
        public Reaction(Faction faction, ReactionType type)
        {
            Faction = faction;
            Type = type;
        }

        public Reaction(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        Faction = Faction.ReadReference(reader);
                        Type = (ReactionType)reader.ReadEncodedInt();

                        break;
                    }
            }
        }

        public Faction Faction { get; }

        public ReactionType Type { get; set; }

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            Faction.WriteReference(writer, Faction);
            writer.WriteEncodedInt((int)Type);
        }
    }

    public class Orders
    {
        private readonly List<Reaction> m_Reactions;

        public Orders(BaseFactionGuard guard)
        {
            Guard = guard;
            m_Reactions = new List<Reaction>();
            Movement = MovementType.Patrol;
        }

        public Orders(BaseFactionGuard guard, IGenericReader reader)
        {
            Guard = guard;

            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 1:
                    {
                        Follow = reader.ReadEntity<Mobile>();
                        goto case 0;
                    }
                case 0:
                    {
                        var count = reader.ReadEncodedInt();
                        m_Reactions = new List<Reaction>(count);

                        for (var i = 0; i < count; ++i)
                        {
                            m_Reactions.Add(new Reaction(reader));
                        }

                        Movement = (MovementType)reader.ReadEncodedInt();

                        break;
                    }
            }
        }

        public BaseFactionGuard Guard { get; }

        public MovementType Movement { get; set; }

        public Mobile Follow { get; set; }

        public Reaction GetReaction(Faction faction)
        {
            Reaction reaction;

            for (var i = 0; i < m_Reactions.Count; ++i)
            {
                reaction = m_Reactions[i];

                if (reaction.Faction == faction)
                {
                    return reaction;
                }
            }

            reaction = new Reaction(
                faction,
                faction == null || faction == Guard.Faction ? ReactionType.Ignore : ReactionType.Attack
            );
            m_Reactions.Add(reaction);

            return reaction;
        }

        public void SetReaction(Faction faction, ReactionType type)
        {
            var reaction = GetReaction(faction);

            reaction.Type = type;
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(1); // version

            writer.Write(Follow);

            writer.WriteEncodedInt(m_Reactions.Count);

            for (var i = 0; i < m_Reactions.Count; ++i)
            {
                m_Reactions[i].Serialize(writer);
            }

            writer.WriteEncodedInt((int)Movement);
        }
    }
}
