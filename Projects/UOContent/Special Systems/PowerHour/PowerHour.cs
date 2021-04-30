using System;
using System.IO;
using Server;

namespace Server.Misc
{
    public class PowerHour
    {
        static TimeSpan StartTime = new TimeSpan(19, 00, 00);
        static TimeSpan EndTime = new TimeSpan(20, 00, 00);

        public static void Initialize()
        {
            Timer t = Timer.DelayCall(
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(500),
                delegate()
                {
                    TimeSpan CurrentTimestamp = DateTime.Now.TimeOfDay;

                    if (CurrentTimestamp < StartTime - new TimeSpan(0, 15, 0))
                        World.Broadcast(0x22, true, "Power hour will start in 15 minutes.");
                    if (CurrentTimestamp == StartTime)
                        World.Broadcast(0x22, true, "Power hour is now active!!");
                    if (CurrentTimestamp > StartTime + new TimeSpan(0, 15, 0))
                        World.Broadcast(0x22, true, "Power hour is currently active, with 45 minutes remaining!!");
                    if (CurrentTimestamp > StartTime + new TimeSpan(0, 30, 0))
                        World.Broadcast(0x22, true, "Power hour is currently active, with 30 minutes remaining!!");
                    if (CurrentTimestamp > StartTime + new TimeSpan(0, 45, 0))
                        World.Broadcast(0x22, true, "Power hour is currently active, with 15 minutes remaining!!");
                    if (CurrentTimestamp == EndTime)
                    {
                        World.Broadcast(0x22, true, "Power hour has now ended!!");
                        StartTime = StartTime + new TimeSpan(1, 0, 0);
                        EndTime = EndTime + new TimeSpan(1, 0, 0);
                    }


                }
            );
        }


        public static bool IsActive
        {
            get { return ((StartTime <= DateTime.Now.TimeOfDay) && (DateTime.Now.TimeOfDay <= EndTime)); }
        }
    }
}
