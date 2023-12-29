using Server.Network;

namespace Server.Engines.AdvancedSearch;

public class AdvancedSearchConfirmDeleteGump : AdvancedSearchWarningGump
{
    private readonly AdvancedSearchGump _gump;

    public AdvancedSearchConfirmDeleteGump(AdvancedSearchGump gump, int count) : base(
        "Delete selected objects",
        30720,
        $"Delete {count} objects?",
        0xFFC000,
        480,
        360
    ) => _gump = gump;

    protected override void OnClickResponse(NetState sender, bool okay)
    {
        if (okay && _gump.SearchResults != null)
        {
            for (var i = 0; i < _gump.SearchResults.Length; i++)
            {
                var entry = _gump.SearchResults[i];

                if (entry.Selected)
                {
                    entry.Entity?.Delete();
                }
            }
        }

        _gump.Resend(sender.Mobile);
    }
}
