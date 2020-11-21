using Server.Mobiles;
using Server.Network;

namespace Server
{
    public class QuestArrow
    {
        public QuestArrow(PlayerMobile m, Mobile t)
        {
            Running = true;
            Mobile = m;
            Target = t;
        }

        public QuestArrow(PlayerMobile m, Mobile t, int x, int y) : this(m, t)
        {
            Update(x, y);
        }

        public PlayerMobile Mobile { get; }

        public Mobile Target { get; }

        public bool Running { get; private set; }

        public void Update()
        {
            Update(Target.X, Target.Y);
        }

        public void Update(int x, int y)
        {
            if (!Running)
            {
                return;
            }

            Mobile.NetState.SendSetArrow(x, y, Target.Serial);
        }

        public void Stop()
        {
            Stop(Target.X, Target.Y);
        }

        public void Stop(int x, int y)
        {
            if (!Running)
            {
                return;
            }

            Mobile.ClearQuestArrow();
            Mobile.NetState.SendCancelArrow(x, y, Target.Serial);

            Running = false;
            OnStop();
        }

        public virtual void OnStop()
        {
        }

        public virtual void OnClick(bool rightClick)
        {
        }
    }
}
