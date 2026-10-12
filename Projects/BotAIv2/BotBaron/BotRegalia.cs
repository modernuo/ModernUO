using ModernUO.Serialization;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// What the Baron wears, and the one promise all seven pieces make: they do not wear out.
///
/// <para>
/// <b>Regalia is not equipment, and the difference is the whole reason these types exist.</b> Every other
/// bot on this shard is dressed by the market — it wants a piece, posts the want, pays a crafter, collects
/// it and eventually orders another when the first is scrap. That chain is the point of the economy and
/// nothing here is meant to replace it. The Baron is outside it on purpose: he takes no wage, holds no
/// purse worth spending and would never appear on the board as a customer, so gear that decayed would leave
/// him naked inside a week with no mechanism able to notice.
/// </para>
///
/// <para>
/// <b>Two different ways of saying "never wears", because the engine has two.</b> Armour and clothing both
/// gate their wear on <c>MaxHitPoints &gt; 0</c> — see <c>BaseArmor.OnHit</c> and
/// <c>BaseClothing.OnHit</c> — so a zero there is the engine's own word for a piece with no durability, and
/// <c>BotUpkeep.Life</c> already reads it that way and declines to order a replacement. A weapon does not
/// work like that: at nought hits and nought maximum, <c>BaseWeapon.OnHit</c> falls through both branches to
/// <c>Delete()</c>, so a halberd written the same way would quietly cease to exist on about the hundredth
/// blow. It keeps real durability and has it restored instead.
/// </para>
///
/// <para>
/// <b>The colour is the engine's own gold and not a hue picked to look like it.</b> Setting
/// <see cref="BaseArmor.Resource"/> to <c>CraftResource.Gold</c> is what a smith who worked gold would
/// produce: the hue, the resistances and the durability scaling all come from the same table the craft
/// system uses. A literal <c>0x8A5</c> in this file would have been a second place to be wrong the first
/// time anybody edits <c>ResourceInfo</c>.
/// </para>
/// </summary>
public static class BotRegalia
{
    /// <summary>The red of the cloak. The dye tub's own deep red, so it reads as livery rather than as a stain.</summary>
    public const int RoyalRed = 0x26;
}

/// <summary>
/// The Baron's halberd: a polearm swung with Swordsmanship, and the one weapon on the shard that never
/// blunts.
///
/// <para>
/// Restored rather than made indestructible, and the note above says why: a weapon with no durability at all
/// is a weapon the engine deletes. So the blow lands, the engine takes its point, and the point is put back
/// — which also means <c>BotUpkeep</c> reads it at full life every time and never asks the population for a
/// replacement nobody could make.
/// </para>
/// </summary>
[SerializationGenerator(0, false)]
public partial class BotBaronHalberd : Halberd
{
    [Constructible]
    public BotBaronHalberd()
    {
        Name = "the Baron's halberd";
        Hue = BotRegalia.RoyalRed;
    }

    public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1.0)
    {
        var hits = HitPoints;
        var max = MaxHitPoints;

        base.OnHit(attacker, defender, damageBonus);

        if (Deleted)
        {
            return;
        }

        // Maximum first: HitPoints refuses to move while the maximum is nought, so the other order would
        // silently do nothing on the one blow that mattered.
        MaxHitPoints = max;
        HitPoints = hits;
    }
}

/// <summary>The Baron's cuirass. Gold plate, and it does not wear — see <see cref="BotRegalia"/>.</summary>
[SerializationGenerator(0, false)]
public partial class BotBaronChest : PlateChest
{
    [Constructible]
    public BotBaronChest()
    {
        Name = "the Baron's cuirass";

        // Resource first and the durability afterwards: setting the resource rescales the maximum, so a
        // nought written before it would be scaled straight back up to a real number.
        Resource = CraftResource.Gold;
        MaxHitPoints = 0;
    }
}

/// <summary>The Baron's greaves.</summary>
[SerializationGenerator(0, false)]
public partial class BotBaronLegs : PlateLegs
{
    [Constructible]
    public BotBaronLegs()
    {
        Name = "the Baron's greaves";
        Resource = CraftResource.Gold;
        MaxHitPoints = 0;
    }
}

/// <summary>The Baron's vambraces.</summary>
[SerializationGenerator(0, false)]
public partial class BotBaronArms : PlateArms
{
    [Constructible]
    public BotBaronArms()
    {
        Name = "the Baron's vambraces";
        Resource = CraftResource.Gold;
        MaxHitPoints = 0;
    }
}

/// <summary>The Baron's gauntlets.</summary>
[SerializationGenerator(0, false)]
public partial class BotBaronGloves : PlateGloves
{
    [Constructible]
    public BotBaronGloves()
    {
        Name = "the Baron's gauntlets";
        Resource = CraftResource.Gold;
        MaxHitPoints = 0;
    }
}

/// <summary>The Baron's gorget.</summary>
[SerializationGenerator(0, false)]
public partial class BotBaronGorget : PlateGorget
{
    [Constructible]
    public BotBaronGorget()
    {
        Name = "the Baron's gorget";
        Resource = CraftResource.Gold;
        MaxHitPoints = 0;
    }
}

/// <summary>The Baron's helm.</summary>
[SerializationGenerator(0, false)]
public partial class BotBaronHelm : PlateHelm
{
    [Constructible]
    public BotBaronHelm()
    {
        Name = "the Baron's helm";
        Resource = CraftResource.Gold;
        MaxHitPoints = 0;
    }
}

/// <summary>
/// The red royal cloak. Clothing rather than armour, so it stops nothing and is worn for one reason: it is
/// how a bot standing in a field is recognisable as the Baron from further away than a name will carry.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BotBaronCloak : Cloak
{
    [Constructible]
    public BotBaronCloak()
    {
        Name = "a red royal cloak";
        Hue = BotRegalia.RoyalRed;
        MaxHitPoints = 0;
    }
}
