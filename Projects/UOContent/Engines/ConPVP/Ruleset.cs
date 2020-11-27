using System.Collections;
using System.Collections.Generic;

namespace Server.Engines.ConPVP
{
    public class Ruleset
    {
        public Ruleset(RulesetLayout layout)
        {
            Layout = layout;
            Options = new BitArray(layout.TotalLength);
        }

        public RulesetLayout Layout { get; }

        public BitArray Options { get; private set; }

        public string Title { get; set; }

        public Ruleset Base { get; private set; }

        public List<Ruleset> Flavors { get; } = new();

        public bool Changed { get; set; }

        public void ApplyDefault(Ruleset newDefault)
        {
            Base = newDefault;
            Changed = false;

            Options = new BitArray(newDefault.Options);

            ApplyFlavorsTo(this);
        }

        public void ApplyFlavorsTo(Ruleset ruleset)
        {
            for (var i = 0; i < Flavors.Count; ++i)
            {
                var flavor = Flavors[i];

                Options.Or(flavor.Options);
            }
        }

        public void AddFlavor(Ruleset flavor)
        {
            if (Flavors.Contains(flavor))
            {
                return;
            }

            Flavors.Add(flavor);
            Options.Or(flavor.Options);
        }

        public void RemoveFlavor(Ruleset flavor)
        {
            if (!Flavors.Contains(flavor))
            {
                return;
            }

            Flavors.Remove(flavor);
            Options.And(flavor.Options.Not());
            flavor.Options.Not();
        }

        public void SetOptionRange(string title, bool value)
        {
            var layout = Layout.FindByTitle(title);

            if (layout == null)
            {
                return;
            }

            for (var i = 0; i < layout.TotalLength; ++i)
            {
                Options[i + layout.Offset] = value;
            }

            Changed = true;
        }

        public bool GetOption(string title, string option)
        {
            var index = 0;
            var layout = Layout.FindByOption(title, option, ref index);

            if (layout == null)
            {
                return true;
            }

            return Options[layout.Offset + index];
        }

        public void SetOption(string title, string option, bool value)
        {
            var index = 0;
            var layout = Layout.FindByOption(title, option, ref index);

            if (layout == null)
            {
                return;
            }

            Options[layout.Offset + index] = value;

            Changed = true;
        }
    }
}
