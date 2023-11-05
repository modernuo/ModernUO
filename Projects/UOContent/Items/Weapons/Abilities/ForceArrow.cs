using Server.Spells;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class ForceArrow : WeaponAbility
    {
        public override int BaseMana => 20;

        public override bool RequiresTactics(Mobile from) => false;

        public override void OnHit(Mobile attacker, Mobile defender, int damage, WorldLocation worldLocation)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            attacker.SendLocalizedMessage(1074381); // You fire an arrow of pure force.
            defender.SendLocalizedMessage(1074382); // You are struck by a force arrow!

            if (0.4 > Utility.RandomDouble())
            {
                defender.Combatant = null;
                defender.Warmode = false;
            }

            ForceArrowInfo info = GetInfo(attacker, defender);

            if (info == null)
            {
                BeginForceArrow(attacker, defender);
            }
            else if (info.Timer.Running)
            {
                info.Timer.IncreaseExpiration();

                BuffInfo.RemoveBuff(defender, BuffIcon.ForceArrow);
                BuffInfo.AddBuff(defender, new BuffInfo(BuffIcon.ForceArrow, 1151285, 1151286, info.DefenseChanceMalus.ToString()));
            }

            if (defender.Spell is Spell spell && spell.IsCasting)
            {
                spell.Disturb(DisturbType.Hurt, false, true);
            }
        }

        private static readonly Dictionary<Mobile, List<ForceArrowInfo>> _table = new Dictionary<Mobile, List<ForceArrowInfo>>();

        public static void BeginForceArrow(Mobile attacker, Mobile defender)
        {
            ForceArrowInfo info = new ForceArrowInfo(attacker, defender);
            info.Timer = new ForceArrowTimer(info);

            if (_table.TryGetValue(attacker, out var list))
            {
                list.Add(info);
            }
            else
            {
                _table.Add(attacker, new List<ForceArrowInfo> { info });
            }

            BuffInfo.AddBuff(defender, new BuffInfo(BuffIcon.ForceArrow, 1151285, 1151286, info.DefenseChanceMalus.ToString()));
        }

        public static void EndForceArrow(ForceArrowInfo info)
        {
            if (info == null)
            {
                return;
            }

            Mobile attacker = info.Attacker;

            if (_table.TryGetValue(attacker, out var list) && list.Remove(info) && list.Count == 0)
            {
                _table.Remove(attacker);
            }

            BuffInfo.RemoveBuff(info.Defender, BuffIcon.ForceArrow);
        }

        public static bool HasForceArrow(Mobile attacker, Mobile defender)
        {
            if (_table.TryGetValue(attacker, out var list))
            {
                foreach (ForceArrowInfo info in list)
                {
                    if (info.Defender == defender)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static ForceArrowInfo GetInfo(Mobile attacker, Mobile defender)
        {
            if (_table.TryGetValue(attacker,out var list))
            {
                foreach (ForceArrowInfo info in list)
                {
                    if (info.Defender == defender)
                    {
                        return info;
                    }
                }
            }

            return null;
        }

        public class ForceArrowInfo
        {
            public Mobile Attacker { get; }
            public Mobile Defender { get; }
            public ForceArrowTimer Timer { get; set; }
            public int DefenseChanceMalus { get; set; }

            public ForceArrowInfo(Mobile attacker, Mobile defender)
            {
                Attacker = attacker;
                Defender = defender;
                DefenseChanceMalus = 10;
            }
        }

        public class ForceArrowTimer : Timer
        {
            private readonly ForceArrowInfo _info;
            private DateTime _expires;

            public ForceArrowTimer(ForceArrowInfo info)
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1))
            {
                _info = info;
                _expires = Core.Now + TimeSpan.FromSeconds(10);

                Start();
            }

            protected override void OnTick()
            {
                if (_expires < Core.Now)
                {
                    Stop();
                    EndForceArrow(_info);
                }
            }

            public void IncreaseExpiration()
            {
                _expires += TimeSpan.FromSeconds(2);
                _info.DefenseChanceMalus += 5;
            }
        }
    }
}
