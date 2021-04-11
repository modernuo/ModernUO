using System;
using System.IO;
using Server;

namespace Server.Misc
{
    public class PowerHour
    {
        const int Begin = 19, End = 20;// Power Hour from 19 to 20

        public static void Initialize()
        {
            Timer t = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromMilliseconds(500), delegate()
            {
                TimeSpan nowTime = DateTime.Now.TimeOfDay;

                if (nowTime.Hours == 18 && nowTime.Minutes == 45 && nowTime.Seconds == 0)
                    World.Broadcast(0x22, true, "Power hour will start in 15 minutes.");
                if (nowTime.Hours == 19 && nowTime.Minutes == 0 && nowTime.Seconds == 0)
                    World.Broadcast(0x22, true, "Power hour is now active!!");
                if (nowTime.Hours == 19 && nowTime.Minutes == 15 && nowTime.Seconds == 0)
                    World.Broadcast(0x22, true, "Power hour is currently active, with 45 minutes remaining!!");
                if (nowTime.Hours == 19 && nowTime.Minutes == 30 && nowTime.Seconds == 0)
                    World.Broadcast(0x22, true, "Power hour is currently active, with 30 minutes remaining!!");
                if (nowTime.Hours == 19 && nowTime.Minutes == 45 && nowTime.Seconds == 0)
                    World.Broadcast(0x22, true, "Power hour is currently active, with 15 minutes remaining!!");
                if (nowTime.Hours == 20 && nowTime.Minutes == 0 && nowTime.Seconds == 0)
                    World.Broadcast(0x22, true, "Power hour has now ended!!");
            });
        }

        public static bool On
        {
            get { return ((Begin <= DateTime.Now.Hour) && (DateTime.Now.Hour <= End)); }
        }
    }
}
