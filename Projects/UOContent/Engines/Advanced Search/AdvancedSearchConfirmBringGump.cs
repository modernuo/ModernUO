using Server.Network;

namespace Server.Engines.AdvancedSearch;

public class AdvancedSearchConfirmBringGump : AdvancedSearchWarningGump
{
    private readonly AdvancedSearchGump _gump;

    public AdvancedSearchConfirmBringGump(AdvancedSearchGump gump, int count) : base(
        "Bring selected objects",
        30720,
        $"Bring {count} objects?",
        0xFFC000,
        480,
        360
    ) => _gump = gump;

    protected override void OnClickResponse(NetState sender, bool okay)
    {
        var from = sender.Mobile;
        var loc = from.Location;
        var map = from.Map;

        if (okay && _gump.SearchResults != null)
        {
            for (var i = 0; i < _gump.SearchResults.Length; i++)
            {
                var entry = _gump.SearchResults[i];

                if (entry.Selected)
                {
                    entry.Entity?.MoveToWorld(loc, map);
                }
            }
        }

        _gump.Resend(from);
    }
}
