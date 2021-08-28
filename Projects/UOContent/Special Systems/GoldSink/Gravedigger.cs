using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;
using Container = Server.Items.Container;

namespace Server.Misc
{
    class GravediggerCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("gravedigger", AccessLevel.Player, new CommandEventHandler(Gravedigger_Command));
        }
        public static void Register(string command, AccessLevel access, CommandEventHandler handler)
        {
            CommandSystem.Register(command, access, handler);
        }

        [Usage("gravedigger")]
        [Description("Summons the gravedigger to get your body")]
        public static void Gravedigger_Command(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (!from.Alive)
            {
                from.SendMessage("You are dead and cannot do that!");
                return;
            }

            Map map = from.Map;

            if (map == null)
                return;

            GraveDigger mSp = new GraveDigger();
            mSp.MoveToWorld(new Point3D(from.X, from.Y, from.Z), from.Map);

            mSp.Say("Greetings, Sire.");
            mSp.Say("I will fetch your corpse for a fee of 30,000 gold");
            mSp.Say("Hurry, I am busy and will only stay here a few minutes.");

        }


    }

}

namespace Server.Mobiles
{
    [CorpseName("A gravedigger corpse")]
    public class GraveDigger : BaseCreature
    {
        public virtual bool IsInvulnerable { get { return true; } }

        [Constructible]
        public GraveDigger() : base(AIType.AI_Animal, FightMode.None, 10, 1, 0.2, 0.4)
        {
            Name = "Hahrn the Gravedigger";
            Body = 42;
            BaseSoundID = 0xCC;
            Hidden = false;
            CantWalk = true;
            Timer.DelayCall(TimeSpan.FromMinutes(5.0), () => Delete());
            

            Blessed = true;

        }

        public GraveDigger(Serial serial) : base(serial)
        {
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return true;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (e.Mobile.InRange(this, 4))
            {
                if ((e.Speech.ToLower() == "fetch"))
                {
                    e.Mobile.SendMessage("Yes sire, as you command.");
                    GetMyCorpse(e.Mobile);
                    this.Delete();
                }
            }
            base.OnSpeech(e);

        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            Timer.DelayCall(TimeSpan.FromSeconds(1.0), () => Delete());
        }

        public static void GetMyCorpse(Mobile from)
        {

            if (!from.Alive)
            {
                from.SendMessage("But you are dead sire, I cannot help thee");
                return;
            }

            Map map = from.Map;

            if (map == null)
                return;
           
            int distchk = 0;
            int distpck = 0;

            ArrayList bodies = new ArrayList();
            ArrayList empty = new ArrayList();
            //ArrayList mice = new ArrayList();
            foreach (Item body in World.Items.Values) // change so it check sall facets... remove Inrange?
                if (body is Corpse)
                {
                    Corpse cadaver = (Corpse)body;

                    int carrying = body.GetTotal(TotalType.Items);

                    if (cadaver.Owner == from && carrying > 0)
                    {
                        distchk++;
                        bodies.Add(body);
                        //if ( GhostHelper.HowFar( from.X, from.Y, mSp.X, mSp.Y ) < TheClosest ){ TheClosest = GhostHelper.HowFar( from.X, from.Y, mSp.X, mSp.Y ); IsClosest = distchk; }
                    }
                    else if (cadaver.Owner == from && carrying < 1)
                    {
                        empty.Add(body);
                        //mice.Add( mSp ); 
                    }
                }


            for (int u = 0; u < empty.Count; ++u) { Item theEmpty = (Item)empty[u]; theEmpty.Delete(); }
            //for ( int m = 0; m < mice.Count; ++m ){ Mobile theMouse = ( Mobile )mice[ m ]; theMouse.Delete(); }
            if (distchk == 0) { from.SendMessage("You have no nearby corpse in this area!"); }
            else
            {

                int i_Bank;
                i_Bank = Banker.GetBalance(from);
                Container bank = from.FindBankNoCreate();
                if ((from.Backpack != null && from.Backpack.ConsumeTotal(typeof(Gold), 30000)) || (bank != null && bank.ConsumeTotal(typeof(Gold), 30000)))
                {
                    for (int h = 0; h < bodies.Count; ++h)
                    {
                        distpck++;
                        //if ( distpck == IsClosest )
                        //{
                        Corpse theBody = (Corpse)bodies[h];
                        theBody.MoveToWorld(new Point3D(from.X, from.Y, from.Z), from.Map);
                        //}
                    }
                }
                else
                {
                    from.SendMessage("I only work for gold, Sire.");
                    from.SendMessage("Make sure you have 30,000 gold (coins) in your pack or bank.");
                }
            }
        }


    }
}
