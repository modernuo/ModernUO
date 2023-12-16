using System;
using System.Diagnostics.CodeAnalysis;

namespace Server.Gumps
{
    public readonly ref struct GumpResponseData
    {
        private readonly TextRelay[] textEntries;

        public readonly int ButtonId;
        public readonly ReadOnlySpan<int> Switches;

        internal GumpResponseData(int buttonId, ReadOnlySpan<int> switches, TextRelay[] textEntries)
        {
            ButtonId = buttonId;
            Switches = switches;
            this.textEntries = textEntries;
        }

        public bool IsSwitched(int switchId)
        {
            for (int i = 0; i < Switches.Length; i++)
            {
                if (Switches[i] == switchId)
                {
                    return true;
                }
            }

            return false;
        }

        public string GetText(int entryId)
        {
            if (TryGetText(entryId, out string? text))
            {
                return text;
            }

            return "";
        }

        public bool TryGetText(int entryId, [NotNullWhen(true)] out string? text)
        {
            for (int i = 0; i < textEntries.Length; i++)
            {
                ref TextRelay entry = ref textEntries[i];

                if (entry.EntryID == entryId)
                {
                    text = entry.Text;
                    return true;
                }
            }

            text = null;
            return false;
        }
    }
}
