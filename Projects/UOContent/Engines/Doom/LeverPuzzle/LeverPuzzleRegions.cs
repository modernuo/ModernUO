using Server.Mobiles;
using Server.Regions;

namespace Server.Engines.Doom
{
    public class LampRoomRegion : BaseRegion
    {
        private readonly LeverPuzzleController m_Controller;

        public LampRoomRegion(LeverPuzzleController controller)
            : base(null, Map.Malas, Find(LeverPuzzleController.lr_Enter, Map.Malas), LeverPuzzleController.lr_Rect)
        {
            m_Controller = controller;
            Register();
        }

        public static void Initialize()
        {
            EventSink.Login += OnLogin;
        }

        public static void OnLogin(Mobile m)
        {
            var rect = LeverPuzzleController.lr_Rect;
            if (m.X >= rect.X && m.X <= rect.X + 10 && m.Y >= rect.Y && m.Y <= rect.Y + 10 && m.Map == Map.Internal)
            {
                Timer kick = new LeverPuzzleController.LampRoomKickTimer(m);
                kick.Start();
            }
        }

        public override void OnEnter(Mobile m)
        {
            if (m is null or WandererOfTheVoid)
            {
                return;
            }

            if (m.AccessLevel > AccessLevel.Player)
            {
                return;
            }

            if (m_Controller.Successful != null)
            {
                if (m is PlayerMobile)
                {
                    if (m == m_Controller.Successful)
                    {
                        return;
                    }
                }
                else if (m is BaseCreature bc && (bc.Controlled && bc.ControlMaster == m_Controller.Successful ||
                                                  bc.Summoned))
                {
                    return;
                }
            }

            Timer kick = new LeverPuzzleController.LampRoomKickTimer(m);
            kick.Start();
        }

        public override void OnExit(Mobile m)
        {
            if (m != null && m == m_Controller.Successful)
            {
                m_Controller.RemoveSuccessful();
            }
        }

        public override void OnDeath(Mobile m)
        {
            if (m?.Deleted != false || m is WandererOfTheVoid)
            {
                return;
            }

            Timer kick = new LeverPuzzleController.LampRoomKickTimer(m);
            kick.Start();
        }

        public override bool OnSkillUse(Mobile m, int Skill) /* just in case */ => m_Controller.Successful != null &&
            (m.AccessLevel != AccessLevel.Player || m == m_Controller.Successful);
    }

    public class LeverPuzzleRegion : BaseRegion
    {
        public Mobile m_Occupant;

        public LeverPuzzleRegion(int[] loc)
            : base(null, Map.Malas, Find(LeverPuzzleController.lr_Enter, Map.Malas), new Rectangle2D(loc[0], loc[1], 1, 1))
        {
            Register();
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Occupant => m_Occupant?.Alive == true ? m_Occupant : null;

        public override void OnEnter(Mobile m)
        {
            if (m != null && m_Occupant == null && m is PlayerMobile && m.Alive)
            {
                m_Occupant = m;
            }
        }

        public override void OnExit(Mobile m)
        {
            if (m != null && m == m_Occupant)
            {
                m_Occupant = null;
            }
        }
    }
}
