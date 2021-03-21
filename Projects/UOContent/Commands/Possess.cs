using System;
using Server.Commands.Generic;

namespace Server.Commands
{
    public class PossessCommand : BaseCommand
    {
        public PossessCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.AllMobiles;
            Commands = new[] { "Possess" };
            ObjectTypes = ObjectTypes.Mobiles;
            Usage = "Possess";
            Description = "Takes control of a mobile";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            Console.WriteLine("Executing possess command " + Core.Expansion);

            var caster = e.Mobile;
            var target = (Mobile)obj;

            if (caster.PossessType == PossessType.Possessing)
            {
                AddResponse("You are already possessing a target!");
                return;
            }

            if (target.Player)
            {
                AddResponse("You can't possess a player");
                return;
            }

            caster.BodyMod = target.Body;
            caster.Location = target.Location;
            caster.Direction = target.Direction;

            target.PossessType = PossessType.Possessed;
            caster.PossessType = PossessType.Possessing;

            caster.Stabled.Add(target);
            target.Internalize();

            Properties.SetValue(e.Mobile, obj, "Invul", "true");

            if (caster.Hidden)
            {
                caster.Hidden = false;
            }
            BuffInfo.AddBuff(caster, new BuffInfo(BuffIcon.Incognito, 1075819, new TextDefinition("Possessing " + target.Name)));

            AddResponse("You've taken control of " + target.Name);
        }
    }

    public class UnpossessCommand : BaseCommand
    {
        public UnpossessCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.AllMobiles;
            Commands = new[] { "Unpossess" };
            ObjectTypes = ObjectTypes.Mobiles;
            Usage = "Unpossess";
            Description = "Release control of a mobile";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            var caster = e.Mobile;

            Mobile toRelease = null;

            foreach (Mobile mob in caster.Stabled)
            {
                if (mob.PossessType == PossessType.Possessed)
                {
                    toRelease = mob;
                }
            }

            if (toRelease == null)
            {
                AddResponse("You have nothing to unpossess!");
                return;
            }

            caster.Hidden = true;
            caster.BodyMod = 0;
            caster.Stabled.Remove(toRelease);
            caster.PossessType = PossessType.None;

            toRelease.MoveToWorld(caster.Location, caster.Map);
            toRelease.Direction = caster.Direction;
            toRelease.PossessType = PossessType.None;

            BuffInfo.RemoveBuff(caster, BuffIcon.Incognito);
            AddResponse("You've released control of " + toRelease.Name);
        }
    }
}
