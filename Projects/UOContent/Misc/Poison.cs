using System;
using Server.Engines.Virtues;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;

namespace Server;

public class PoisonImpl : Poison
{
    private readonly int _count;
    private readonly TimeSpan _delay;
    private readonly TimeSpan _interval;
    private readonly int _maximum;
    private readonly int _messageInterval;
    private readonly int _minimum;
    private readonly double _scalar;

    public PoisonImpl(
        string name, int index, int level, int min, int max, double percent, double delay, double interval, int count,
        int messageInterval, PoisonFamily family = PoisonFamily.Standard
    ) : base(index)
    {
        Name = name;
        Level = level;
        Family = family;
        _minimum = min;
        _maximum = max;
        _scalar = percent * 0.01;
        _delay = TimeSpan.FromSeconds(delay);
        _interval = TimeSpan.FromSeconds(interval);
        _count = count;
        _messageInterval = messageInterval;
    }

    public override string Name { get; }

    public override int Level { get; }

    public override PoisonFamily Family { get; }

    public override Timer ConstructTimer(Mobile m) => new PoisonTimer(m, this);

    public class PoisonTimer : Timer
    {
        private readonly Mobile _mobile;
        private readonly PoisonImpl _poison;
        private int _index;
        private int _lastDamage;

        public PoisonTimer(Mobile m, PoisonImpl p) : base(p._delay, p._interval)
        {
            From = m;
            _mobile = m;
            _poison = p;
        }

        public Mobile From{ get; set; }

        protected override void OnTick()
        {
            if ((Core.AOS && _poison.Level < 4 &&
                 TransformationSpellHelper.UnderTransformation(_mobile, typeof(VampiricEmbraceSpell)) ||
                 _poison.Level < 3 && OrangePetals.UnderEffect(_mobile) ||
                 AnimalForm.UnderTransformation(_mobile, typeof(Unicorn))) && _mobile.CurePoison(_mobile))
            {
                if (Core.SA)
                {
                    // * You feel yourself resisting the effects of the poison *
                    _mobile.LocalOverheadMessage(MessageType.Emote, 0x3F, 1114441);
                }
                else
                {
                    _mobile.LocalOverheadMessage(
                        MessageType.Emote,
                        0x3F,
                        true,
                        "* You feel yourself resisting the effects of the poison *"
                    );
                }

                if (Core.SA)
                {
                    // * ~1_NAME~ seems resistant to the poison *
                    _mobile.NonlocalOverheadMessage(MessageType.Emote, 0x3F, 1114442, _mobile.Name);
                }
                else
                {
                    _mobile.LocalOverheadMessage(
                        MessageType.Emote,
                        0x3F,
                        true,
                        $"* {_mobile.Name} seems resistant to the poison *"
                    );
                }

                Stop();
                return;
            }

            if (_index++ == _poison._count)
            {
                _mobile.SendLocalizedMessage(502136); // The poison seems to have worn off.
                _mobile.Poison = null;

                Stop();
                return;
            }

            int damage;

            if (!Core.AOS && _lastDamage != 0 && Utility.RandomBool())
            {
                damage = _lastDamage;
            }
            else
            {
                damage = 1 + (int)(_mobile.Hits * _poison._scalar);
                damage = Math.Clamp(damage, _poison._minimum, _poison._maximum);

                _lastDamage = damage;
            }

            // Darkglow: 10% damage boost when attacker is more than 1 tile away
            if (_poison.Family == PoisonFamily.Darkglow && From != null && From.Map == _mobile.Map &&
                !From.InRange(_mobile, 1))
            {
                damage = (int)(damage * 1.1);
                // Darkglow poison increases your damage!
                From.SendLocalizedMessage(1072850);
            }

            From?.DoHarmful(_mobile, true);

            (_mobile as IHonorTarget)?.ReceivedHonorContext?.OnTargetPoisoned();

            AOS.Damage(_mobile, From, damage, 0, 0, 0, 100, 0);

            // Parasitic: heals attacker for damage dealt when within 1 tile
            if (_poison.Family == PoisonFamily.Parasitic && From != null && From.Map == _mobile.Map &&
                From.InRange(_mobile, 1))
            {
                From.Heal(damage);
                // You have had ~1_HEALED_AMOUNT~ hit points healed.
                From.SendLocalizedMessage(1060203, damage.ToString());
            }

            // OSI: randomly revealed between first and third damage tick, guessing 60% chance
            if (Utility.RandomDouble() < 0.40)
            {
                _mobile.RevealingAction();
            }

            if (_index % _poison._messageInterval == 0)
            {
                _mobile.OnPoisoned(From, _poison, _poison);
            }
        }
    }
}
