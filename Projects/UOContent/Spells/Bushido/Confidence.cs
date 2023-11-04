using System;
using System.Collections.Generic;

namespace Server.Spells.Bushido;

public class Confidence : SamuraiSpell
{
    private static readonly SpellInfo _info = new(
        "Confidence",
        null,
        -1,
        9002
    );

    private static readonly Dictionary<Mobile, TimerExecutionToken> _table = new();
    private static readonly Dictionary<Mobile, TimerExecutionToken> _regenTable = new();

    public Confidence(Mobile caster, Item scroll) : base(caster, scroll, _info)
    {
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(0.25);

    public override double RequiredSkill => 25.0;
    public override int RequiredMana => 10;

    public override void OnBeginCast()
    {
        base.OnBeginCast();

        Caster.FixedEffect(0x37C4, 10, 7, 4, 3);
    }

    public override void OnCast()
    {
        if (CheckSequence())
        {
            Caster.SendLocalizedMessage(1063115); // You exude confidence.

            Caster.FixedParticles(0x375A, 1, 17, 0x7DA, 0x960, 0x3, EffectLayer.Waist);
            Caster.PlaySound(0x51A);

            OnCastSuccessful(Caster);

            BeginConfidence(Caster);
            BeginRegenerating(Caster);
        }

        FinishSequence();
    }

    public static bool IsConfident(Mobile m) => _table.ContainsKey(m);

    public static void BeginConfidence(Mobile m)
    {
        StopConfidenceTimer(m);

        Timer.StartTimer(TimeSpan.FromSeconds(30.0),
            () =>
            {
                EndConfidence(m);
                m.SendLocalizedMessage(1063116); // Your confidence wanes.
            },
            out var timerToken
        );

        _table[m] = timerToken;

        if (Core.HS)
        {
            var bushido = m.Skills.Bushido.Fixed;
            BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.Confidence, 1060596, 1153809, TimeSpan.FromSeconds(30), m,
                $"{bushido / 120}\t{bushido / 50}\t{"100"}"
            ));
        }
        else
        {
            BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.Confidence, 1060596, TimeSpan.FromSeconds(30), m));
        }
    }

    private static bool StopConfidenceTimer(Mobile m)
    {
        if (_table.Remove(m, out var timerToken))
        {
            timerToken.Cancel();
            return true;
        }

        return false;
    }

    public static void EndConfidence(Mobile m)
    {
        StopRegenerating(m);

        if (StopConfidenceTimer(m))
        {
            OnEffectEnd(m, typeof(Confidence));
            BuffInfo.RemoveBuff(m, BuffIcon.Confidence);
        }
    }

    public static bool IsRegenerating(Mobile m) => _regenTable.ContainsKey(m);

    // TODO: Move this to a central regeneration so it actually works properly
    public static void BeginRegenerating(Mobile m)
    {
        StopRegenerating(m);

        TimerExecutionToken timerToken = default;
        Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), 4,
            () =>
            {
                // ReSharper disable once AccessToModifiedClosure
                if (timerToken.RemainingCount == 0)
                {
                    StopRegenerating(m);
                }

                // RunUO says this goes for 5 seconds, but UOGuide says 4 seconds during normal regeneration
                // Divide by 4 because this is per second.
                var hits = (int)((15 + m.Skills.Bushido.Value * m.Skills.Bushido.Value / 576) / 4);

                m.Hits += hits;
            },
            out timerToken
        );

        _regenTable[m] = timerToken;
    }

    public static void StopRegenerating(Mobile m)
    {
        if (_regenTable.Remove(m, out var timer))
        {
            timer.Cancel();
        }
    }
}
