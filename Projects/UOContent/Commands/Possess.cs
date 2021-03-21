using System;
using System.Collections.Generic;
using Server.Commands.Generic;
using Server.Mobiles;

namespace Server.Commands
{
    internal static class SwapItems
    {
        static readonly List<Layer> SPECIAL_LAYERS = new() { Layer.Backpack, Layer.Bank, Layer.Mount };
        public static void swapItems(Mobile target, PlayerMobile caster)
        {
            var targetItems = new List<Item>(target.Items).FindAll(item => !SPECIAL_LAYERS.Contains(item.Layer));
            var casterItems = new List<Item>(caster.Items).FindAll(item => !SPECIAL_LAYERS.Contains(item.Layer));

            foreach (Item item in casterItems)
            {
                caster.RemoveItem(item);
            }

            foreach (Item item in targetItems)
            {
                caster.EquipItem(item);
                target.RemoveItem(item);
            }

            foreach (Item item in casterItems)
            {
                target.EquipItem(item);
            }
        }
    }

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
            Console.WriteLine("Executing possess command");

            if (e.Mobile is PlayerMobile caster && obj is Mobile target) {
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
                caster.NameMod = target.Name;
                caster.HueMod = target.Hue;
                caster.SetHairMods(target.HairItemID, target.FacialHairItemID);

                caster.Location = target.Location;
                caster.Direction = target.Direction;

                target.PossessType = PossessType.Possessed;
                caster.PossessType = PossessType.Possessing;

                SwapItems.swapItems(target, caster);

                caster.Stabled.Add(target);
                target.Internalize();

                Properties.SetValue(e.Mobile, caster, "blessed", "true");

                caster.Hidden = target.Hidden;

                BuffInfo.AddBuff(caster, new BuffInfo(BuffIcon.Incognito, 1075819, new TextDefinition("Possessing " + target.Name)));

                AddResponse("You've taken control of " + target.Name);
            }
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
            if (e.Mobile is PlayerMobile caster)
            {
                var toRelease = caster.Stabled.Find(mob => mob.PossessType == PossessType.Possessed);

                if (toRelease == null)
                {
                    AddResponse("You have nothing to unpossess!");
                    return;
                }

                caster.Hidden = true;
                caster.BodyMod = 0;
                caster.NameMod = null;
                caster.HueMod = -1;
                caster.SetHairMods(-1, -1);

                caster.Stabled.Remove(toRelease);
                caster.PossessType = PossessType.None;

                Properties.SetValue(e.Mobile, caster, "blessed", "false");

                SwapItems.swapItems(toRelease, caster);

                toRelease.MoveToWorld(caster.Location, caster.Map);
                toRelease.Direction = caster.Direction;
                toRelease.PossessType = PossessType.None;

                BuffInfo.RemoveBuff(caster, BuffIcon.Incognito);
                AddResponse("You've released control of " + toRelease.Name);
            }
        }
    }
}
