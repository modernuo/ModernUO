using System;
using ModernUO.CodeGeneratedEvents;
using Server.Mobiles;
using Server.Network;

namespace Server.Spells.Spellweaving
{
    public class ReaperFormSpell : ArcaneForm
    {
        private static readonly SpellInfo _info = new("Reaper Form", "Tarisstree", -1);

        public ReaperFormSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.5);

        public override double RequiredSkill => 24.0;
        public override int RequiredMana => 34;

        public override int Body => 0x11D;

        public override int FireResistOffset => -25;
        public override int PhysResistOffset => 5 + FocusLevel;
        public override int ColdResistOffset => 5 + FocusLevel;
        public override int PoisResistOffset => 5 + FocusLevel;
        public override int NrgyResistOffset => 5 + FocusLevel;

        public virtual int SwingSpeedBonus => 10 + FocusLevel;
        public virtual int SpellDamageBonus => 10 + FocusLevel;

        [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
        public static void OnLogin(PlayerMobile pm)
        {
            var context = TransformationSpellHelper.GetContext(pm);

            if (context?.Type == typeof(ReaperFormSpell))
            {
                pm.NetState.SendSpeedControl(SpeedControlSetting.Walk);
            }
        }

        public override void DoEffect(Mobile m)
        {
            m.PlaySound(0x1BA);

            m.NetState.SendSpeedControl(SpeedControlSetting.Walk);
        }

        public override void RemoveEffect(Mobile m)
        {
            m.NetState.SendSpeedControl(SpeedControlSetting.Disable);
        }
    }
}
