using System;
using System.Collections.Generic;
using Server.Commands.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Commands
{
    internal static class PossessHelper
    {
        private const string _possessStatModLabel = "PossessMod";

        private static readonly HashSet<Layer> _specialLayers = new() { Layer.Backpack, Layer.Bank, Layer.Mount };

        private static readonly List<StatType> _statTypes = new() { StatType.Str, StatType.Dex, StatType.Int };

        private static List<Item> GetItemsOnMobile(Mobile m)
        {
            var items = m.Items;
            var list = new List<Item>(m.Items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!_specialLayers.Contains(item.Layer))
                {
                    list.Add(item);
                }
            }

            return list;
        }

        private static Backpack PackItems(IEnumerable<Item> items)
        {
            var backpack = new Backpack();

            foreach (Item i in items)
            {
                backpack.AddItem(i);
            }

            return backpack;
        }

        private static void OnPossessItems(Mobile target, Mobile caster)
        {
            var targetItems = GetItemsOnMobile(target);
            var casterItems = GetItemsOnMobile(caster);

            var targetBackpack = target.Backpack;
            if (targetBackpack != null)
            {
                target.RemoveItem(targetBackpack);
                caster.AddToBackpack(targetBackpack);
                caster.PossessContainer = targetBackpack;
            }

            foreach (Item item in casterItems)
            {
                caster.RemoveItem(item);
            }

            var packedCasterItems = PackItems(casterItems);
            target.PossessContainer = packedCasterItems;

            foreach (Item item in targetItems)
            {
                target.RemoveItem(item);
                caster.EquipItem(item);
            }
        }

        private static void OnUnpossessItems(Mobile target, Mobile caster)
        {
            var casterItems = GetItemsOnMobile(caster);

            foreach (Item item in casterItems)
            {
                caster.RemoveItem(item);
                target.AddItem(item);
            }

            target.AddItem(caster.PossessContainer);

            var targetPossessContainer = target.PossessContainer;

            if (targetPossessContainer != null)
            {
                var formerCasterItems = new List<Item>(targetPossessContainer.Items);
                foreach (Item item in formerCasterItems)
                {
                    caster.AddItem(item);
                }
            }

            caster.PossessContainer = null;
            target.PossessContainer = null;
        }

        private static void SwapTitles(Mobile target, Mobile caster)
        {
            var casterTitle = caster.Title;
            caster.Title = target.Title;
            target.Title = casterTitle;
        }

        private static string GetStatModLabel(StatType statType) => $"{_possessStatModLabel} {Enum.GetName(statType)}";

        private static void ApplyStatMods(Mobile target, Mobile caster)
        {
            foreach (StatType statType in _statTypes)
            {
                var casterStat = caster.GetStat(statType);
                var targetStat = target.GetStat(statType);

                if (targetStat > casterStat)
                {
                    caster.StatMods.Add(
                        new StatMod(statType, GetStatModLabel(statType), targetStat - casterStat, TimeSpan.Zero));
                }
            }

            caster.Hits = caster.HitsMax;
            caster.Mana = caster.ManaMax;
            caster.Stam = caster.StamMax;
        }

        private static void RemoveStatMods(Mobile caster)
        {
            foreach (StatType statType in _statTypes)
            {
                caster.RemoveStatMod(GetStatModLabel(statType));
            }
        }

        private static void ApplySkillMods(Mobile target, Mobile caster)
        {
            for (int i = 0; i < target.Skills.Length; i++)
            {
                var targetSkill = target.Skills[i];
                var casterSkill = caster.Skills[targetSkill.SkillName];

                if (targetSkill.Value > casterSkill.Value)
                {
                    caster.AddSkillMod(new PossessionSkillMod(targetSkill.SkillName, targetSkill.Value));
                }
            }
        }

        private static void RemoveSkillMods(Mobile caster)
        {
            for (int i = 0; i < caster.SkillMods.Count; i++)
            {
                var casterSkillMod = caster.SkillMods[i];
                if (typeof(PossessionSkillMod) == casterSkillMod.GetType())
                {
                    caster.RemoveSkillMod(casterSkillMod);
                }
            }
        }

        public static void Possess(Mobile target, Mobile caster)
        {
            ApplyStatMods(target, caster);
            ApplySkillMods(target, caster);
            OnPossessItems(target, caster);
            SwapTitles(target, caster);
        }

        public static void Unpossess(Mobile target, Mobile caster)
        {
            SwapTitles(target, caster);
            OnUnpossessItems(target, caster);
            RemoveSkillMods(caster);
            RemoveStatMods(caster);
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
            if (e.Mobile is PlayerMobile caster && obj is Mobile target)
            {
                if (caster.IsPossessing)
                {
                    AddResponse($"You are already possessing a target: {caster.PossessedMobile.Name}!");
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

                target.Possessed = true;
                target.Frozen = true;

                PossessHelper.Possess(target, caster);

                caster.Stabled.Add(target);
                target.Internalize();

                caster.Blessed = true;

                caster.Hidden = target.Hidden;

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
                    AddResponse("You have nothing or nobody to unpossess!");
                    return;
                }

                caster.Hidden = true;
                caster.BodyMod = 0;
                caster.NameMod = null;
                caster.HueMod = -1;
                caster.SetHairMods(-1, -1);

                caster.Stabled.Remove(toRelease);
                caster.Blessed = false;

                PossessHelper.Unpossess(toRelease, caster);

                toRelease.MoveToWorld(caster.Location, caster.Map);
                toRelease.Direction = caster.Direction;
                toRelease.Frozen = false;
                toRelease.Possessed = false;

                AddResponse($"You've released control of {toRelease.Name}");
            }
        }
    }
}
