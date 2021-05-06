using System;
using System.IO;
using Server;

namespace Server.Misc
{
    public class PowerHour
    {
        static TimeSpan StartTime = new TimeSpan(19, 00, 00);
        static TimeSpan EndTime = new TimeSpan(20, 00, 00);

        static bool Message_1 = false;
        static bool Message_2 = false;
        static bool Message_3 = false;
        static bool Message_4 = false;
        static bool Message_5 = false;


        public static void Initialize()
        {
            Timer t = Timer.DelayCall(
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(500),
                delegate()
                {
                    TimeSpan CurrentTimestamp = DateTime.Now.TimeOfDay;

                    if (CurrentTimestamp < StartTime - new TimeSpan(0, 15, 0) && !Message_1)
                    {
                        World.Broadcast(0x22, true, "Power hour will start in 15 minutes.");
                        Message_1 = true;
                    }
                    if (CurrentTimestamp == StartTime && !Message_2)
                    {
                        World.Broadcast(0x22, true, "Power hour is now active!!");
                        Message_2 = true;
                    }
                        
                    if (CurrentTimestamp > StartTime + new TimeSpan(0, 15, 0) && !Message_3)
                    {
                        World.Broadcast(0x22, true, "Power hour is currently active, with 45 minutes remaining!!");
                        Message_3 = true;
                    }
                        
                    if (CurrentTimestamp > StartTime + new TimeSpan(0, 30, 0) && !Message_4)
                    {
                        World.Broadcast(0x22, true, "Power hour is currently active, with 30 minutes remaining!!");
                        Message_4 = true;
                    }
                        
                    if (CurrentTimestamp > StartTime + new TimeSpan(0, 45, 0) && !Message_5)
                    {
                        World.Broadcast(0x22, true, "Power hour is currently active, with 15 minutes remaining!!");
                        Message_5 = true;
                    }
                        
                    if (CurrentTimestamp == EndTime)
                    {
                        World.Broadcast(0x22, true, "Power hour has now ended!!");
                        StartTime = StartTime + new TimeSpan(1, 0, 0);
                        EndTime = EndTime + new TimeSpan(1, 0, 0);
                        Message_1 = false;
                        Message_2 = false;
                        Message_3 = false;
                        Message_4 = false;
                        Message_5 = false;
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
