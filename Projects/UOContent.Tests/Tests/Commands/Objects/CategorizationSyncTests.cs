using System.Collections.Generic;
using System.Linq;
using Server.Commands;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Commands.Objects;

public class CategorizationSyncTests
{
    private static CAGJson Cat(string category, params System.Type[] types) =>
        new()
        {
            Category = category,
            Objects = types.Select(t => new CAGObject { Type = t }).ToArray()
        };

    [Fact]
    public void Reconcile_appends_missing_types_to_uncategorized()
    {
        var categorization = new List<CAGJson> { Cat("Items.Weapons.Swords", typeof(Katana)) };
        var discovered = new List<System.Type> { typeof(Katana), typeof(Runebook) };

        var (updated, report) = CategorizationSync.Reconcile(categorization, discovered);

        Assert.Contains("Runebook", report.Appended);
        Assert.Empty(report.Orphaned);

        var uncategorized = Assert.Single(updated, c => c.Category == "Items.Uncategorized");
        Assert.Contains(uncategorized.Objects, o => o.Type == typeof(Runebook));
    }

    [Fact]
    public void Reconcile_reports_orphans_not_in_discovered()
    {
        var categorization = new List<CAGJson> { Cat("Items.Weapons.Swords", typeof(Katana)) };
        var discovered = new List<System.Type> { typeof(Runebook) };

        var (_, report) = CategorizationSync.Reconcile(categorization, discovered);

        Assert.Contains("Katana", report.Orphaned);
        Assert.Contains("Runebook", report.Appended);
    }
}
