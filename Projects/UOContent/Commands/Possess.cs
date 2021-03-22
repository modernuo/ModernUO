using System;
using System.Collections.Generic;
using Server.Commands.Generic;
using Server.Mobiles;

namespace Server.Commands
{
    internal static class Swap
    {
        private static readonly List<Layer> SPECIAL_LAYERS = new() { Layer.Backpack, Layer.Bank, Layer.Mount };

        private static List<Item> GetItemsFromLayers(IEnumerable<Item> items) =>
            new List<Item>(items).FindAll(item => !SPECIAL_LAYERS.Contains(item.Layer));
        public static void Items(Mobile target, PlayerMobile caster)
        {
            var targetItems = GetItemsFromLayers(target.Items);
            var casterItems = GetItemsFromLayers(caster.Items);

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

        public static void Titles(Mobile target, PlayerMobile caster)
        {
            var casterTitle = caster.Title;
            caster.Title = target.Title;
            target.Title = casterTitle;
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

            if (e.Mobile is PlayerMobile caster && obj is Mobile target)
            {
                var casterPossessedMobile = caster.PossessedMobile;
                if (casterPossessedMobile != null)
                {
                    AddResponse($"You are already possessing a target: {casterPossessedMobile.Name}!");
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
                caster.HairHue = target.HairHue;
                caster.FacialHairHue = target.FacialHairHue;

                caster.Location = target.Location;
                caster.Direction = target.Direction;

                target.PossessType = PossessType.Possessed;

                Swap.Items(target, caster);
                Swap.Titles(target, caster);

                caster.Stabled.Add(target);
                target.Internalize();

                Properties.SetValue(e.Mobile, caster, "blessed", "true");

                caster.Hidden = target.Hidden;

                BuffInfo.AddBuff(caster, new BuffInfo(BuffIcon.Incognito, 1075819, new TextDefinition($"Possessing {target.Name}")));

                AddResponse($"You've taken control of {target.Name}");
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
                var toRelease = caster.PossessedMobile;

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

                Swap.Items(toRelease, caster);
                Swap.Titles(toRelease, caster);

                toRelease.MoveToWorld(caster.Location, caster.Map);
                toRelease.Direction = caster.Direction;
                toRelease.PossessType = PossessType.None;

                BuffInfo.RemoveBuff(caster, BuffIcon.Incognito);
                AddResponse($"You've released control of {toRelease.Name}");
            }
        }
    }
}
