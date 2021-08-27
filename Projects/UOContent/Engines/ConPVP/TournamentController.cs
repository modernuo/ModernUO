using System.Collections.Generic;
using Server.ContextMenus;
using Server.Gumps;

namespace Server.Engines.ConPVP
{
    public class TournamentController : Item
    {
        private static readonly List<TournamentController> m_Instances = new();

        [Constructible]
        public TournamentController() : base(0x1B7A)
        {
            Visible = false;
            Movable = false;

            Tournament = new Tournament();
            m_Instances.Add(this);
        }

        public TournamentController(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public Tournament Tournament { get; private set; }

        public static bool IsActive
        {
            get
            {
                for (var i = 0; i < m_Instances.Count; ++i)
                {
                    var controller = m_Instances[i];

                    if (controller?.Deleted == false && controller.Tournament != null &&
                        controller.Tournament.Stage != TournamentStage.Inactive)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public override string DefaultName => "tournament controller";

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster && Tournament != null)
            {
                list.Add(new EditEntry(Tournament));

                if (Tournament.CurrentStage == TournamentStage.Inactive)
                {
                    list.Add(new StartEntry(Tournament));
                }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster && Tournament != null)
            {
                from.CloseGump<PickRulesetGump>();
                from.CloseGump<RulesetGump>();
                from.SendGump(new PickRulesetGump(from, null, Tournament.Ruleset));
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);

            Tournament.Serialize(writer);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Tournament = new Tournament(reader);
                        break;
                    }
            }

            m_Instances.Add(this);
        }

        public override void OnDelete()
        {
            base.OnDelete();

            m_Instances.Remove(this);
        }

        private class EditEntry : ContextMenuEntry
        {
            private readonly Tournament m_Tournament;

            public EditEntry(Tournament tourney) : base(5101) => m_Tournament = tourney;

            public override void OnClick()
            {
                Owner.From.SendGump(new PropertiesGump(Owner.From, m_Tournament));
            }
        }

        private class StartEntry : ContextMenuEntry
        {
            private readonly Tournament m_Tournament;

            public StartEntry(Tournament tourney) : base(5113) => m_Tournament = tourney;

            public override void OnClick()
            {
                if (m_Tournament.Stage == TournamentStage.Inactive)
                {
                    m_Tournament.SignupStart = Core.Now;
                    m_Tournament.Stage = TournamentStage.Signup;
                    m_Tournament.Participants.Clear();
                    m_Tournament.Pyramid.Levels.Clear();
                    m_Tournament.Alert(
                        "Hear ye! Hear ye!",
                        "Tournament signup has opened. You can enter by signing up with the registrar."
                    );
                }
            }
        }
    }
}
