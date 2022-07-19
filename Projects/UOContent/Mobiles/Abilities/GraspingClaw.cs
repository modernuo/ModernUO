using System;
using System.Collections.Generic;

namespace Server.Mobiles;

public class GraspingClaw : MonsterAbility
{
    private Dictionary<Mobile, ExpireTimer> _table;

    public override MonsterAbilityType AbilityType => MonsterAbilityType.GraspingClaw;
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.GiveMeleeDamage;
    public override double ChanceToTrigger => 0.10;

    public override void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
    {
        if (_table.Remove(target, out var timer))
        {
            timer.DoExpire();
            target.SendLocalizedMessage(1070837); // The creature lands another blow in your weakened state.
        }
        else
        {
            // The blow from the creature's claws has made you more susceptible to physical attacks.
            target.SendLocalizedMessage(1070836);
        }

        /**
         * Grasping Claw
         * Start cliloc: 1070836
         * Effect: Physical resistance -15% for 5 seconds
         * Refresh Cliloc: 1070837
         * End cliloc: 1070838
         * Effect:
         * - Type: "3"
         * - From: Player
         * - To: 0x0
         * - ItemId: "0x37B9"
         * - ItemIdName: "glow"
         * - FromLocation: "(1149 808, 32)"
         * - ToLocation: "(1149 808, 32)"
         * - Speed: "10" - Duration: "5"
         * - FixedDirection: "True"
         * - Explode: "False"
         */
        var effect = -(target.PhysicalResistance * 15 / 100);

        var mod = new ResistanceMod(ResistanceType.Physical, "GraspingClaw", effect);

        target.FixedEffect(0x37B9, 10, 5);
        target.AddResistanceMod(mod);

        timer = new ExpireTimer(this, target, mod, TimeSpan.FromSeconds(5.0));
        timer.Start();

        _table ??= new Dictionary<Mobile, ExpireTimer>();
        _table[target] = timer;

        base.Trigger(trigger, source, target);
    }

    private class ExpireTimer : Timer
    {
        private GraspingClaw _graspingClaw;
        private Mobile _mobile;
        private ResistanceMod _mod;

        public ExpireTimer(GraspingClaw claw, Mobile m, ResistanceMod mod, TimeSpan delay) : base(delay)
        {
            _mobile = m;
            _mod = mod;
            _graspingClaw = claw;
        }

        public void DoExpire()
        {
            _mobile.RemoveResistanceMod(_mod);
            Stop();
            _graspingClaw._table?.Remove(_mobile);
        }

        protected override void OnTick()
        {
            _mobile.SendLocalizedMessage(1070838); // Your resistance to physical attacks has returned.
            DoExpire();
        }
    }
}
