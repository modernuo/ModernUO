using System;
using System.Collections.Generic;

namespace Server.Mobiles;

public abstract class MonsterAbilitySingleTargetDoT : MonsterAbilitySingleTarget
{
    private Dictionary<Mobile, ExpireTimer> _table;

    public virtual TimeSpan MinDelay => TimeSpan.FromSeconds(10.0);
    public virtual TimeSpan MaxDelay => TimeSpan.FromSeconds(10.0);

    protected override void OnTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender)
    {
        _table ??= new Dictionary<Mobile, ExpireTimer>();

        var duration = Utility.RandomMinMax(MinDelay, MaxDelay);
        var count = GetCount(source, defender);
        var timer = _table[defender] = new ExpireTimer(this, source, defender, duration, count);
        timer.Start();

        StartEffect(source, defender);
    }

    protected virtual int GetCount(BaseCreature source, Mobile defender) => 1;

    protected abstract void StartEffect(BaseCreature source, Mobile defender);

    protected virtual void EffectTick(BaseCreature source, Mobile defender, out TimeSpan nextDelay)
    {
        nextDelay = Utility.RandomMinMax(MinDelay, MaxDelay);
    }

    protected abstract void EndEffect(BaseCreature source, Mobile defender);
    protected abstract void OnEffectExpired(BaseCreature source, Mobile defender);

    public bool IsUnderEffect(Mobile defender) => _table?.ContainsKey(defender) == true;

    protected bool RemoveEffect(BaseCreature source, Mobile defender)
    {
        if (_table?.Remove(defender, out var timer) == true)
        {
            timer.Stop();
            EndEffect(source, defender);
            return true;
        }

        return false;
    }

    private class ExpireTimer : Timer
    {
        private BaseCreature _source;
        private Mobile _defender;
        private MonsterAbilitySingleTargetDoT _ability;

        public ExpireTimer(
            MonsterAbilitySingleTargetDoT ability,
            BaseCreature source,
            Mobile defender,
            TimeSpan delay,
            int count
        ) : base(delay, delay, count)
        {
            _ability = ability;
            _source = source;
            _defender = defender;
        }

        protected override void OnTick()
        {
            _ability.EffectTick(_source, _defender, out var delay);
            Delay = delay;

            _ability.RemoveEffect(_source, _defender);
            _ability.OnEffectExpired(_source, _defender);
        }
    }
}
