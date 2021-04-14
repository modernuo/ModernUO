using System;
using System.Collections.ObjectModel;
using Server.Mobiles;

namespace Server.Ethics
{
    public class PlayerCollection : Collection<Player>
    {
    }

    [PropertyObject]
    public class Player
    {
        private DateTime m_Shield;

        public Player(Ethic ethic, Mobile mobile)
        {
            Ethic = ethic;
            Mobile = mobile;

            Power = 5;
            History = 5;
        }

        public Player(Ethic ethic, IGenericReader reader)
        {
            Ethic = ethic;

            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        Mobile = reader.ReadEntity<Mobile>();

                        Power = reader.ReadEncodedInt();
                        History = reader.ReadEncodedInt();

                        Steed = reader.ReadEntity<Mobile>();
                        Familiar = reader.ReadEntity<Mobile>();

                        m_Shield = reader.ReadDeltaTime();

                        break;
                    }
            }
        }

        public Ethic Ethic { get; }

        public Mobile Mobile { get; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int Power { get; set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int History { get; set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public Mobile Steed { get; set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public Mobile Familiar { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsShielded
        {
            get
            {
                if (m_Shield == DateTime.MinValue)
                {
                    return false;
                }

                if (Core.Now < m_Shield + TimeSpan.FromHours(1.0))
                {
                    return true;
                }

                FinishShield();
                return false;
            }
        }

        public static Player Find(Mobile mob) => Find(mob, false);

        public static Player Find(Mobile mob, bool inherit)
        {
            var pm = mob as PlayerMobile;

            if (pm == null)
            {
                if (inherit && mob is BaseCreature bc)
                {
                    if (bc.Controlled)
                    {
                        pm = bc.ControlMaster as PlayerMobile;
                    }
                    else if (bc.Summoned)
                    {
                        pm = bc.SummonMaster as PlayerMobile;
                    }
                }

                if (pm == null)
                {
                    return null;
                }
            }

            var pl = pm.EthicPlayer;

            if (pl?.Ethic.IsEligible(pl.Mobile) == false)
            {
                pm.EthicPlayer = pl = null;
            }

            return pl;
        }

        public void BeginShield() => m_Shield = Core.Now;

        public void FinishShield() => m_Shield = DateTime.MinValue;

        public void CheckAttach()
        {
            if (Ethic.IsEligible(Mobile))
            {
                Attach();
            }
        }

        public void Attach()
        {
            if (Mobile is PlayerMobile mobile)
            {
                mobile.EthicPlayer = this;
            }

            Ethic.Players.Add(this);
        }

        public void Detach()
        {
            if (Mobile is PlayerMobile mobile)
            {
                mobile.EthicPlayer = null;
            }

            Ethic.Players.Remove(this);
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(Mobile);

            writer.WriteEncodedInt(Power);
            writer.WriteEncodedInt(History);

            writer.Write(Steed);
            writer.Write(Familiar);

            writer.WriteDeltaTime(m_Shield);
        }
    }
}
