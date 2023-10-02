using System;
using System.Runtime.CompilerServices;
using Server.Engines.CannedEvil;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.Virtues;

public static class ValorVirtue
{
    private const int LossAmount = 250;
    private static readonly TimeSpan LossDelay = TimeSpan.FromDays(7.0);

    public static void Initialize()
    {
        VirtueGump.Register(112, OnVirtueUsed);
    }

    public static void OnVirtueUsed(Mobile from)
    {
        if (from.Alive)
        {
            from.SendLocalizedMessage(1054034); // Target the Champion Idol of the Champion you wish to challenge!.
            from.Target = new InternalTarget();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanAtrophy(VirtueContext context) => context.LastValorLoss + LossDelay < Core.Now;

    public static void CheckAtrophy(PlayerMobile pm)
    {
        var virtues = VirtueSystem.GetVirtues(pm);
        if (virtues?.Valor > 0 && CanAtrophy(virtues))
        {
            if (VirtueSystem.Atrophy(pm, VirtueName.Valor, LossAmount))
            {
                pm.SendLocalizedMessage(1054040); // You have lost some Valor.
            }

            virtues.LastValorLoss = Core.Now;
        }
    }

    public static void Valor(PlayerMobile from, object targ)
    {
        if (targ is not IdolOfTheChampion idol || idol.Deleted || idol.Spawn?.Deleted != false)
        {
            from.SendLocalizedMessage(1054035); // You must target a Champion Idol to challenge the Champion's spawn!
        }
        else if (from.Hidden)
        {
            from.SendLocalizedMessage(1052015); // You cannot do that while hidden.
        }
        else if (idol.Spawn.HasBeenAdvanced)
        {
            from.SendLocalizedMessage(1054038); // The Champion of this region has already been challenged!
        }
        else if (idol.Spawn.Active)
        {
            if (idol.Spawn.Champion != null)
            {
                from.SendLocalizedMessage(1112470); // You may not use Valor on this Champion Idol. The Champion has already spawned.
            }

            int needed, consumed;
            switch (idol.Spawn.GetSubLevel())
            {
                case 0:
                    {
                        needed = consumed = 2500;
                        break;
                    }
                case 1:
                    {
                        needed = consumed = 5000;
                        break;
                    }
                case 2:
                    {
                        needed = 10000;
                        consumed = 7500;
                        break;
                    }
                default:
                    {
                        needed = 20000;
                        consumed = 10000;
                        break;
                    }
            }

            if (VirtueSystem.GetVirtues(from)?.GetValue((int)VirtueName.Valor) >= needed)
            {
                VirtueSystem.Atrophy(from, VirtueName.Valor, consumed);
                // Your challenge is heard by the Champion of this region! Beware its wrath!
                from.SendLocalizedMessage(1054037);
                idol.Spawn.HasBeenAdvanced = true;
                idol.Spawn.AdvanceLevel();
            }
            else
            {
                // The Champion of this region ignores your challenge. You must further prove your valor.
                from.SendLocalizedMessage(1054039);
            }
        }
        else if (VirtueSystem.GetLevel(from, VirtueName.Valor) == VirtueLevel.Knight)
        {
            VirtueSystem.Atrophy(from, VirtueName.Valor, 11000);
            // Your challenge is heard by the Champion of this region! Beware its wrath!
            from.SendLocalizedMessage(1054037);
            idol.Spawn.Start();
            idol.Spawn.ActivatedByValor = true;
            // Uncomment to not allow advancing level after starting with valor.
            // idol.Spawn.HasBeenAdvanced = true;
        }
        else
        {
            // You must be a Knight of Valor to summon the champion's spawn in this manner!
            from.SendLocalizedMessage(1054036);
        }
    }

    private class InternalTarget : Target
    {
        public InternalTarget() : base(14, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (from is PlayerMobile pm)
            {
                Valor(pm, targeted);
            }
        }
    }
}
