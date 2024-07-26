using System.Collections.Generic;
using Server.Collections;
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

        public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, ref list);

            if (from.AccessLevel >= AccessLevel.GameMaster && Tournament != null)
            {
                list.Add(new EditEntry());

                if (Tournament.CurrentStage == TournamentStage.Inactive)
                {
                    list.Add(new StartEntry());
                }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster && Tournament != null)
            {
                var gumps = from.GetGumps();

                gumps.Close<RulesetGump>();
                gumps.Close<PickRulesetGump>();
                gumps.Send(new PickRulesetGump(from, null, Tournament.Ruleset));
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
            public EditEntry() : base(5101)
            {
            }

            public override void OnClick(Mobile from, IEntity target)
            {
                if (target is not TournamentController controller)
                {
                    return;
                }

                from.SendGump(new PropertiesGump(from, controller.Tournament));
            }
        }

        private class StartEntry : ContextMenuEntry
        {
            public StartEntry() : base(5113)
            {
            }

            public override void OnClick(Mobile from, IEntity target)
            {
                if (target is not TournamentController controller || controller.Tournament.Stage != TournamentStage.Inactive)
                {
                    return;
                }

                controller.Tournament.SignupStart = Core.Now;
                controller.Tournament.Stage = TournamentStage.Signup;
                controller.Tournament.Participants.Clear();
                controller.Tournament.Pyramid.Levels.Clear();
                controller.Tournament.Alert(
                    "Hear ye! Hear ye!",
                    "Tournament signup has opened. You can enter by signing up with the registrar."
                );
            }
        }
    }
}
