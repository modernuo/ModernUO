using System;
using System.Collections.Generic;
using System.Linq;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Avatar;

public class AvatarShopGump : DynamicGump
{
    public const int BLANK_ITEM_ID = 0;
    public const int COST_FREE = -1;
    public const int COST_NO_BUY = 0;
    public const int GOLD_STACK_ITEM_ID = 0x0EEF;
    public const int NO_ITEM_ID = -1;

    private const int CARD_HEIGHT = 68;
    private const int CARD_WIDTH = 864 - NAVIGATION_WIDTH;
    private const int CATEGORY_WIDTH = NAVIGATION_WIDTH - 28 - 20;
    private const int COMPACT_CARD_GAP = 10;
    private const int COMPACT_CARD_HEIGHT = 40;
    private const int COMPACT_CARD_WIDTH = (CARD_WIDTH - COMPACT_CARD_GAP) / 2;
    private const int GIANT_COIN_ITEM_ID = 0x4FAD;
    private const int NAVIGATION_WIDTH = 152 + 20 + 20;

    private const int BACKGROUND_WIDTH = 904;
    private const int BACKGROUND_HEIGHT = 729;
    private const int GUMP_WIDTH = BACKGROUND_WIDTH;
    private const int GUMP_HEIGHT = BACKGROUND_HEIGHT;

    private const int TooltipCliloc = 1042971; // ~1_NOTHING~

    private const string COOL_BLUE = "#7FB8FF";
    private const string ORANGE = "#FFA54F";
    private const string RED = "#FF4040";

    private readonly PlayerContext _context;
    private readonly PlayerMobile _from;
    private readonly bool _inSanctuary;
    private readonly Action _onGumpClose;
    private readonly int _pageNumber;
    private readonly Categories _selectedCategory;
    private List<IReward> _rewards;

    public AvatarShopGump(
        PlayerMobile from, Categories selectedCategory = Categories.Information, int pageNumber = 1, Action onGumpClose = null
    ) : base(25, 25)
    {
        _context = AvatarEngine.GetOrCreateContext(from);
        _from = from;
        _onGumpClose = onGumpClose;
        _pageNumber = pageNumber;
        _selectedCategory = selectedCategory;
        _inSanctuary = AvatarSanctuary.IsIn(from);
    }

    private enum Actions
    {
        Close = 0,
        SelectCategoryBase = 10,
        PurchaseBase = 50,
        PageBase = 500
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, BACKGROUND_WIDTH, BACKGROUND_HEIGHT, 2620);
        builder.AddHtml(11, 11, GUMP_WIDTH, 20, "The Avatar's Ascent", COOL_BLUE, align: TextAlignment.Center);

        AddCategoryList(ref builder, 27, 48);

        var y = 48;

        if (_selectedCategory == Categories.Information)
        {
            AddKeyValuePairsCard(
                ref builder,
                NAVIGATION_WIDTH + 20,
                y,
                true,
                new Metric("Time Played", FormatTime(_context.GetRunGameTime(_from))),
                new Metric("Stat Cap", _from.StatCap.ToString()),
                new Metric("Skill Cap", (_from.Skills.Cap / 10).ToString("n0")),
                new Metric(
                    "Faction Bonus",
                    Math.Min(Constants.RIVAL_BONUS_MAX_POINTS, _context.RivalBonusPoints).ToString("n0"),
                    "The amount of coins you have received for killing your enemy faction."
                )
            );
            y += CARD_HEIGHT + 10;

            AddInformationCard(
                ref builder,
                NO_ITEM_ID,
                "An Avatar is Born",
                $"You have begun the Avatar's Ascent. This is a challenging journey of self-discovery and growth. You may access this gump at any time by double-clicking {Colorize("The Avatar's Ascent", ORANGE)} in your backpack.",
                y
            );
            y += CARD_HEIGHT + 10;

            AddInformationCard(
                ref builder,
                NO_ITEM_ID,
                "Everything is Temporary",
                "Unless explicitly stated, everything your character owns will be deleted when your character dies. The only exception to this is your house, which your next life will keep.",
                y
            );
            y += CARD_HEIGHT + 10;

            AddInformationCard(
                ref builder,
                NO_ITEM_ID,
                "Meta Progression",
                $"{Colorize("Ascensions", ORANGE)} are permanent and may be purchased for a fee. {Colorize("Ascensions", ORANGE)} can enable permanence between runs, unlock new features, accelerate progression, or even raise the ceiling of your character.",
                y
            );
            y += CARD_HEIGHT + 10;

            AddInformationCard(
                ref builder,
                NO_ITEM_ID,
                "Your Template",
                $"Your Avatar is weak when they are reborn. It is recommended that you select a {Colorize("Template", ORANGE)} each run. The Erudian Ascensions will allow you to recover skills using the {Colorize("Primary", ORANGE)} and {Colorize("Secondary", ORANGE)} skill pages.",
                y
            );
            y += CARD_HEIGHT + 10;

            if (_context.HasRivalFaction)
            {
                AddInformationCard(
                    ref builder,
                    NO_ITEM_ID,
                    "Enemy Faction",
                    $"Your family has been wronged by {Colorize(_context.RivalFactionName, ORANGE)}. You will receive a bonus each time you kill monsters of this faction. Once you have avenged your family, you will no longer receive the bonus.",
                    y
                );
                y += CARD_HEIGHT + 10;
            }

            AddInformationCard(
                ref builder,
                NO_ITEM_ID,
                "Opportunity Only Knocks Once...",
                $"Your vessel is frail; once you leave the {Colorize("Sanctuary", ORANGE)} you may not purchase anything from this gump.",
                y
            );

            return;
        }

        if (_pageNumber == 1)
        {
            y = AddCategoryHeader(ref builder, y);
        }

        var rewards = RewardFactory.CreateRewards(_from, _selectedCategory, _context, _inSanctuary);
        if (rewards == null || rewards.Count == 0)
        {
            return;
        }

        _context.RewardCache ??= new Dictionary<Categories, List<int>>();

        if (!_context.RewardCache.TryGetValue(_selectedCategory, out var randomRewardIndexes))
        {
            randomRewardIndexes = BuildRewardIndexes(rewards);
        }

        if (randomRewardIndexes == null)
        {
            return;
        }

        var queryable = randomRewardIndexes
            .Select(index => index < rewards.Count ? rewards[index] : null)
            .Where(reward => reward != null);

        if (_selectedCategory != Categories.Ascensions && _selectedCategory != Categories.FullSkillArchive)
        {
            queryable = queryable.OrderBy(reward => reward.Name);
        }

        var randomRewards = queryable.ToList();
        if (randomRewards.Count < 1)
        {
            return;
        }

        var useCompactCard = _selectedCategory == Categories.FullSkillArchive;
        var itemsPerPage = useCompactCard ? 24 : 8; // Compact: 12 rows x 2 columns
        var infoCardSlots = useCompactCard ? 2 : 1;

        var toTake = itemsPerPage;
        var skip = (_pageNumber - 1) * toTake;

        // All pages except the Information page have a description card
        if (_pageNumber == 1)
        {
            toTake -= infoCardSlots;
            y += CARD_HEIGHT + 10;
        }
        else
        {
            skip -= infoCardSlots;
        }

        _rewards = [];
        var itemIndex = 0;
        const int START_X = NAVIGATION_WIDTH + 20;
        var x = START_X;
        foreach (var reward in randomRewards.Skip(skip).Take(toTake))
        {
            _rewards.Add(reward);

            var tooltip = (reward as ActionReward)?.PrequisiteTooltip;
            var canPurchase = (_inSanctuary || reward.CanSelectAnywhere) && reward.CanSelect &&
                              string.IsNullOrWhiteSpace(tooltip);

            if (useCompactCard)
            {
                AddCompactCard(ref builder, x, y, reward.Name, reward.Description, canPurchase, itemIndex, tooltip);

                if (itemIndex % 2 == 0)
                {
                    x += COMPACT_CARD_WIDTH + COMPACT_CARD_GAP;
                }
                else
                {
                    x = START_X;
                    y += COMPACT_CARD_HEIGHT + COMPACT_CARD_GAP;
                }
            }
            else
            {
                var cost = reward is ActionReward { IsComplete: true } ? COST_NO_BUY : reward.Cost;

                AddCard(
                    ref builder,
                    _context.PointsSaved,
                    reward.Graphic,
                    reward.Name,
                    reward.Description,
                    canPurchase,
                    cost,
                    itemIndex,
                    y,
                    tooltip
                );

                y += CARD_HEIGHT + 10;
            }

            ++itemIndex;
        }

        y = GUMP_HEIGHT - 33;

        // Prev page
        if (skip > 0)
        {
            builder.AddButton(NAVIGATION_WIDTH + 20, y, 4014, 4015, (int)Actions.PageBase + (_pageNumber - 1));
        }

        // Next page
        if (randomRewards.Count - skip - toTake > 0)
        {
            builder.AddButton(GUMP_WIDTH - 47, y, 4005, 4007, (int)Actions.PageBase + _pageNumber + 1);
        }
    }

    private int AddCategoryHeader(ref DynamicGumpBuilder builder, int y)
    {
        switch (_selectedCategory)
        {
            case Categories.Ascensions:
                {
                    AddInformationCard(
                        ref builder,
                        BLANK_ITEM_ID,
                        "Ascensions - Unlock Permanent Enhancements",
                        "Ascensions are a way to apply permanent changes to your lineage. Some of these changes may allow you to persist knowledge between runs while others may simply make you stronger.",
                        y,
                        addBackground: false
                    );
                    break;
                }

            case Categories.Templates:
                {
                    AddInformationCard(
                        ref builder,
                        BLANK_ITEM_ID,
                        "Templates - Select Your Beginning",
                        "A template can provide a fixed combination of skills, stats, and/or items to aid with your run. The templates available to you will change each run.",
                        y,
                        addBackground: false
                    );
                    break;
                }

            case Categories.PrimaryBoosts:
            case Categories.SecondaryBoosts:
                {
                    var isPrimary = _selectedCategory == Categories.PrimaryBoosts;
                    AddInformationCard(
                        ref builder,
                        BLANK_ITEM_ID,
                        isPrimary ? "Available Primary Skills" : "Available Secondary Skills",
                        $"Only half the {(isPrimary ? "Primary" : "Secondary")} skills that you've ever become proficient in ({Constants.RECORDED_SKILL_CAP_MIN_AMOUNT} skill) are available for selection. As long as you have capacity, selecting a skill will immediately raise it to the displayed value.",
                        y,
                        addBackground: false
                    );
                    break;
                }

            case Categories.FullSkillArchive:
                {
                    AddInformationCard(
                        ref builder,
                        BLANK_ITEM_ID,
                        "Skill Archive",
                        $"Review the highest you've ever reached in each skill. Once you become proficient ({Constants.RECORDED_SKILL_CAP_MIN_AMOUNT} skill), you may be able to restore the skill to that value for free.",
                        y,
                        addBackground: false
                    );
                    break;
                }

            case Categories.Items:
                {
                    AddInformationCard(
                        ref builder,
                        BLANK_ITEM_ID,
                        "Items - Purchase Temporary Conveniences",
                        "Items can be purchased to assist you with your next run. Be wary of how much you invest, as these items are lost upon death.",
                        y,
                        addBackground: false
                    );
                    break;
                }

            case Categories.Statistics:
                {
                    AddInformationCard(
                        ref builder,
                        BLANK_ITEM_ID,
                        "My Stats",
                        "Review your current stats and your lifetime achievements.",
                        y,
                        addBackground: false
                    );

                    y += CARD_HEIGHT + 10;
                    AddKeyValuePairsCard(
                        ref builder,
                        NAVIGATION_WIDTH + 20,
                        y,
                        true,
                        new Metric("Time Played", FormatTime(_context.GetRunGameTime(_from))),
                        new Metric("Stat Cap", _from.StatCap.ToString()),
                        new Metric("Skill Cap", (_from.Skills.Cap / 10).ToString("n0")),
                        new Metric(
                            "Faction Bonus",
                            Math.Min(Constants.RIVAL_BONUS_MAX_POINTS, _context.RivalBonusPoints).ToString("n0"),
                            "The amount of coins you have received for killing your enemy faction."
                        )
                    );

                    y += CARD_HEIGHT + 10;
                    AddKeyValuePairsCard(
                        ref builder,
                        NAVIGATION_WIDTH + 20,
                        y,
                        true,
                        new Metric("Lifetime", ""),
                        new Metric("Time Played", FormatTime(_context.LifetimeGameTime + _context.GetRunGameTime(_from))),
                        new Metric("Coins Gained", _context.GrandTotalPoints.ToString("n0")),
                        new Metric("Deaths", _context.LifetimeDeaths.ToString("n0"))
                    );

                    y += CARD_HEIGHT + 10;
                    AddKeyValuePairsCard(
                        ref builder,
                        NAVIGATION_WIDTH + 20,
                        y,
                        true,
                        new Metric("Lifetime", ""),
                        new Metric("Faction Kills", _context.LifetimeEnemyFactionKills.ToString("n0")),
                        new Metric(
                            "Other Kills",
                            (_context.LifetimeCreatureKills - _context.LifetimeEnemyFactionKills).ToString("n0")
                        )
                    );
                    break;
                }

            case Categories.Draft:
                {
                    AddInformationCard(
                        ref builder,
                        BLANK_ITEM_ID,
                        "Draft - Pick Skills to Train",
                        "Draft mode allows you to pick skills to train each run. While in the Sanctuary, selecting a skill will automatically increase it.",
                        y,
                        addBackground: false
                    );

                    y += CARD_HEIGHT + 10;
                    AddKeyValuePairsCard(
                        ref builder,
                        NAVIGATION_WIDTH + 20,
                        y,
                        true,
                        new Metric("Level", $"{_context.DraftLevel}/{Constants.DRAFT_MAX_LEVEL}"),
                        new Metric("Experience", _context.DraftTotalExperienceGained.ToString("n0")),
                        new Metric("To Next Pick", _context.DraftExperienceToNextPick.ToString("n0")),
                        new Metric(
                            "Picks Earned",
                            $"{_context.DraftPicksAvailable - Constants.DRAFT_START_PICK_AMOUNT}/{Constants.DRAFT_MAX_LEVEL / Constants.DRAFT_LEVELS_PER_PICK}"
                        ),
                        new Metric("Picks Spent", $"{_context.DraftPicksSpent}/{_context.DraftPicksAvailable}")
                    );
                    break;
                }
        }

        return y;
    }

    private List<int> BuildRewardIndexes(List<IReward> rewards)
    {
        var randomRewardIndexes = new List<int>();

        switch (_selectedCategory)
        {
            case Categories.Ascensions:
            case Categories.FullSkillArchive:
                {
                    for (var i = 0; i < rewards.Count; i++)
                    {
                        randomRewardIndexes.Add(i);
                    }

                    break;
                }

            case Categories.Templates:
            case Categories.PrimaryBoosts:
            case Categories.SecondaryBoosts:
            case Categories.Items:
                {
                    AddStaticIndexes(rewards, randomRewardIndexes);

                    var nonStatic = GetNonStaticIndexes(rewards);
                    if (nonStatic.Count > 0)
                    {
                        var addEntireList = _context.UnlockFullSkillArchive &&
                                            _selectedCategory is Categories.PrimaryBoosts or Categories.SecondaryBoosts;

                        if (addEntireList)
                        {
                            randomRewardIndexes.AddRange(nonStatic);
                        }
                        else
                        {
                            nonStatic.Shuffle();
                            randomRewardIndexes.AddRange(nonStatic.Take(nonStatic.Count / 2));
                        }

                        _context.RewardCache[_selectedCategory] = randomRewardIndexes;
                    }

                    break;
                }

            case Categories.Draft:
                {
                    AddStaticIndexes(rewards, randomRewardIndexes);

                    var nonStatic = GetNonStaticIndexes(rewards);
                    if (nonStatic.Count > 0)
                    {
                        nonStatic.Shuffle();
                        randomRewardIndexes.AddRange(nonStatic.Take(Constants.DRAFT_PICKS_PER_ROUND));

                        // Only cache if random options were returned
                        _context.RewardCache[Categories.Draft] = randomRewardIndexes;
                    }

                    break;
                }
        }

        return randomRewardIndexes;
    }

    private static void AddStaticIndexes(List<IReward> rewards, List<int> indexes)
    {
        for (var i = 0; i < rewards.Count; i++)
        {
            if (rewards[i].Static)
            {
                indexes.Add(i);
            }
        }
    }

    private static List<int> GetNonStaticIndexes(List<IReward> rewards)
    {
        var indexes = new List<int>();
        for (var i = 0; i < rewards.Count; i++)
        {
            if (!rewards[i].Static)
            {
                indexes.Add(i);
            }
        }

        return indexes;
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var buttonID = info.ButtonID;
        if (buttonID == 0)
        {
            _onGumpClose?.Invoke();
            return;
        }

        if (sender.Mobile is not PlayerMobile player)
        {
            return;
        }

        if (buttonID >= (int)Actions.PageBase)
        {
            var page = buttonID - (int)Actions.PageBase;
            player.SendGump(new AvatarShopGump(_from, _selectedCategory, page, _onGumpClose));
            return;
        }

        if (buttonID >= (int)Actions.PurchaseBase)
        {
            var index = buttonID - (int)Actions.PurchaseBase;
            if (_rewards == null || index < 0 || index >= _rewards.Count)
            {
                return;
            }

            var reward = _rewards[index];
            var cost = Math.Max(0, reward.Cost);

            // Re-validate: the sanctuary and lock state may have changed since the gump was sent
            var tooltip = (reward as ActionReward)?.PrequisiteTooltip;
            var canPurchase = (AvatarSanctuary.IsIn(player) || reward.CanSelectAnywhere) && reward.CanSelect &&
                              string.IsNullOrWhiteSpace(tooltip);

            if (!canPurchase)
            {
                player.SendMessage("You cannot purchase that right now.");
            }
            else if (_context.PointsSaved < cost)
            {
                player.SendMessage("You do not have enough coins to purchase this.");
            }
            else if (reward is ItemReward itemReward)
            {
                var item = itemReward.OnSelect();
                if (item != null)
                {
                    SendPurchased(player, reward.Name, cost);

                    _context.PointsSaved -= cost;
                    player.AddToBackpack(item);
                }
            }
            else if (reward is ActionReward actionReward)
            {
                if (_selectedCategory == Categories.Templates)
                {
                    player.SendMessage("You have selected a template.");
                    Timer.DelayCall(TimeSpan.FromSeconds(0.25), actionReward.OnSelect);
                }
                else if (_selectedCategory == Categories.FullSkillArchive)
                {
                    Timer.DelayCall(TimeSpan.FromSeconds(0.25), actionReward.OnSelect);
                }
                else
                {
                    SendPurchased(player, reward.Name, cost);

                    _context.PointsSaved -= cost;
                    actionReward.OnSelect();
                    AvatarEngine.ApplyContext(player, _context);
                }
            }

            player.SendGump(new AvatarShopGump(_from, _selectedCategory, _pageNumber, _onGumpClose));
            return;
        }

        if (buttonID >= (int)Actions.SelectCategoryBase)
        {
            var selectedCategory = (Categories)(buttonID - (int)Actions.SelectCategoryBase);
            player.SendGump(new AvatarShopGump(_from, selectedCategory, 1, _onGumpClose));
        }
    }

    private static void SendPurchased(PlayerMobile player, string name, int cost)
    {
        if (cost > 0)
        {
            player.SendMessage($"You have purchased '{name}' for '{cost:n0}' coins.");
        }
        else
        {
            player.SendMessage($"You have purchased '{name}'.");
        }
    }

    private void AddCard(
        ref DynamicGumpBuilder builder,
        int points,
        int itemId,
        string name,
        string description,
        bool canPurchase,
        int purchaseCost,
        int index,
        int y,
        string tooltip = null,
        bool scrollable = false,
        bool addBackground = true
    )
    {
        const int START_X = NAVIGATION_WIDTH + 20;
        const int GRAPHIC_SLOT_WIDTH = 73;
        const int GRAPHIC_SLOT_HEIGHT = 68;
        const int PURCHASE_WIDTH = 130;

        var x = START_X;
        var cost = purchaseCost switch
        {
            COST_NO_BUY => int.MinValue,
            COST_FREE   => 0,
            _           => purchaseCost
        };

        if (addBackground)
        {
            builder.AddBackground(x, y, CARD_WIDTH, CARD_HEIGHT + 5, 2620);
        }

        // Item image
        if (itemId > BLANK_ITEM_ID)
        {
            builder.AddBackground(x, y, GRAPHIC_SLOT_WIDTH, CARD_HEIGHT + 5, 2620);
            AddCenteredItem(ref builder, itemId, x, y, GRAPHIC_SLOT_WIDTH, GRAPHIC_SLOT_HEIGHT);
            x += GRAPHIC_SLOT_WIDTH;
        }

        // Item text
        x += 10;
        y += 5; // Top padding

        var descriptionWidth = CARD_WIDTH - (x - START_X);
        if (cost >= 0)
        {
            descriptionWidth -= PURCHASE_WIDTH;
        }

        const int LAZY_AMOUNT = 30; // Arbitrary value to account for left padding
        builder.AddHtml(x, y, descriptionWidth - LAZY_AMOUNT, 20, name, ORANGE);
        builder.AddHtml(x + 10, y + 20, descriptionWidth - LAZY_AMOUNT, 40, description, COOL_BLUE, scrollbar: scrollable);

        if (cost < 0)
        {
            return;
        }

        x = START_X + CARD_WIDTH - PURCHASE_WIDTH;
        y += 6;

        // Purchase section
        const int GRAPHIC_WIDTH = 55;
        var canAfford = cost <= points;
        if (cost > 0)
        {
            builder.AddHtml(x + GRAPHIC_WIDTH, y + 2, 80, 20, cost.ToString("n0"), canAfford ? ORANGE : RED);
        }

        if (!canAfford)
        {
            return;
        }

        y += 30;

        if (canPurchase)
        {
            builder.AddButton(x + 13, y - 1, 4023, 4023, (int)Actions.PurchaseBase + index);
        }
        else
        {
            builder.AddImage(x + 21, y + 3, 2092); // Lock icon
            if (!string.IsNullOrWhiteSpace(tooltip))
            {
                builder.AddTooltip(TooltipCliloc, tooltip);
            }
        }

        var purchaseText = _selectedCategory switch
        {
            Categories.Templates                                     => "Select",
            Categories.PrimaryBoosts or Categories.SecondaryBoosts  => "Teach Me",
            Categories.Ascensions or Categories.Draft                => "Unlock",
            Categories.FullSkillArchive                              => "Drop Skill",
            _                                                        => "Purchase"
        };

        builder.AddHtml(x + GRAPHIC_WIDTH, y + 2, 60, 20, purchaseText, ORANGE);
    }

    private void AddCategoryList(ref DynamicGumpBuilder builder, int x, int y)
    {
        // Show current coins
        var firstRowY = y + 20 - 3;
        var secondRowY = firstRowY + 20;

        builder.AddBackground(x, y, CATEGORY_WIDTH, CARD_HEIGHT + 5, 2620);
        AddCenteredItem(ref builder, GIANT_COIN_ITEM_ID, x + 10, y, 40, CARD_HEIGHT + 5);
        builder.AddTooltip(TooltipCliloc, "Coins are earned by killing monsters.");
        builder.AddHtml(
            x + 60,
            firstRowY,
            CATEGORY_WIDTH - 20,
            40,
            (_context.PointsSaved + _context.PointsFarmed).ToString("n0"),
            ORANGE
        );
        builder.AddHtml(x + 60, secondRowY, CATEGORY_WIDTH - 20, 40, "Coins", COOL_BLUE);
        y += CARD_HEIGHT + 10;

        ReadOnlySpan<Categories> categoriesToShow = _context.DraftModeEnabled
            ?
            [
                Categories.Information,
                Categories.Ascensions,
                Categories.Templates,
                Categories.Draft,
                Categories.FullSkillArchive,
                Categories.Statistics
            ]
            :
            [
                Categories.Information,
                Categories.Ascensions,
                Categories.Templates,
                Categories.PrimaryBoosts,
                Categories.SecondaryBoosts,
                Categories.FullSkillArchive,
                Categories.Statistics
            ];

        const int CATEGORY_CARD_HEIGHT = 34;
        const int TOP_PADDING = 6;
        const int HEIGHT_PER_ITEM = CATEGORY_CARD_HEIGHT + TOP_PADDING; // Extra top padding
        const int LEFT_PADDING = 36;
        const int HIDDEN_BUTTON_ID = 1150;
        const int HIDDEN_BUTTON_WIDTH = 28;

        for (var i = 0; i < categoriesToShow.Length; i++)
        {
            var category = categoriesToShow[i];
            var isSelected = _selectedCategory == category;
            var rowY = y + i * HEIGHT_PER_ITEM;

            if (!isSelected)
            {
                var buttonId = (int)Actions.SelectCategoryBase + (int)category;
                builder.AddButton(x, rowY + TOP_PADDING, HIDDEN_BUTTON_ID, HIDDEN_BUTTON_ID, buttonId);
                builder.AddButton(x + HIDDEN_BUTTON_WIDTH, rowY + TOP_PADDING, HIDDEN_BUTTON_ID, HIDDEN_BUTTON_ID, buttonId);
                builder.AddButton(
                    x + HIDDEN_BUTTON_WIDTH * 2,
                    rowY + TOP_PADDING,
                    HIDDEN_BUTTON_ID,
                    HIDDEN_BUTTON_ID,
                    buttonId
                );
            }

            builder.AddBackground(x, rowY, CATEGORY_WIDTH, HEIGHT_PER_ITEM - 6, 2620);
            builder.AddImage(x + 17, rowY + 10, isSelected ? 1210 : 1209, isSelected ? 1152 : 0);

            var categoryName = category switch
            {
                Categories.PrimaryBoosts    => "Primary Skills",
                Categories.SecondaryBoosts  => "Secondary Skills",
                Categories.FullSkillArchive => "Skill Archive",
                _                           => category.ToString()
            };

            builder.AddHtml(
                x + LEFT_PADDING,
                rowY + 7,
                CATEGORY_WIDTH - LEFT_PADDING,
                16,
                categoryName,
                isSelected ? ORANGE : COOL_BLUE
            );
        }
    }

    private static void AddCompactCard(
        ref DynamicGumpBuilder builder, int x, int y, string name, string value, bool canAct, int index,
        string tooltip = null
    )
    {
        const int PADDING = 10;
        const int COMPACT_CARD_HALF_WIDTH = COMPACT_CARD_WIDTH / 2;
        const int CHECKED_BOX = 4017;
        const int LOCK_ICON = 2092;

        var leftColumnX = x + PADDING;
        var leftColumnWidth = COMPACT_CARD_HALF_WIDTH - PADDING;
        var rightColumnX = leftColumnX + leftColumnWidth + PADDING;
        var rightColumnWidth = leftColumnWidth - PADDING;

        builder.AddBackground(x, y, COMPACT_CARD_WIDTH, COMPACT_CARD_HEIGHT, 2620);

        y += PADDING;

        builder.AddHtml(leftColumnX + 35, y, leftColumnWidth, 20, name, COOL_BLUE);

        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AddHtml(rightColumnX, y, rightColumnWidth, 20, value, COOL_BLUE, align: TextAlignment.Right);
        }

        if (canAct)
        {
            builder.AddButton(leftColumnX, y - 3, CHECKED_BOX, CHECKED_BOX, (int)Actions.PurchaseBase + index);
        }
        else
        {
            builder.AddImage(leftColumnX + 8, y + 3, LOCK_ICON);
            if (!string.IsNullOrWhiteSpace(tooltip))
            {
                builder.AddTooltip(TooltipCliloc, tooltip);
            }
        }
    }

    private void AddInformationCard(
        ref DynamicGumpBuilder builder, int itemId, string name, string description, int y, bool addBackground = true
    ) =>
        AddCard(ref builder, 0, itemId, name, description, false, 0, 0, y, addBackground: addBackground);

    private static void AddKeyValuePairsCard(
        ref DynamicGumpBuilder builder, int x, int y, bool addBackground, params ReadOnlySpan<Metric> metrics
    )
    {
        if (addBackground)
        {
            builder.AddBackground(x, y, CARD_WIDTH, CARD_HEIGHT + 5, 2620);
        }

        var count = metrics.Length;
        if (count < 1)
        {
            return;
        }

        var cardWidth = CARD_WIDTH / count;
        x += (CARD_WIDTH - cardWidth * count) / 2;

        var firstRowY = y + 20;
        var secondRowY = firstRowY + 20;

        foreach (var metric in metrics)
        {
            builder.AddHtml(x, firstRowY, cardWidth, 40, metric.Value, ORANGE, align: TextAlignment.Center);
            builder.AddHtml(x, secondRowY, cardWidth, 40, metric.Label, COOL_BLUE, align: TextAlignment.Center);
            if (!string.IsNullOrWhiteSpace(metric.Tooltip))
            {
                builder.AddTooltip(TooltipCliloc, metric.Tooltip);
            }

            x += cardWidth;
        }
    }

    private static void AddCenteredItem(ref DynamicGumpBuilder builder, int itemId, int x, int y, int width, int height)
    {
        var bounds = ItemBounds.Bounds;
        if (bounds == null || itemId < 0 || itemId >= bounds.Length)
        {
            builder.AddItem(x, y, itemId);
            return;
        }

        var b = bounds[itemId];
        builder.AddItem(x + (width - b.Width) / 2 - b.X, y + (height - b.Height) / 2 - b.Y, itemId);
    }

    private static string Colorize(string text, string color) => $"<BASEFONT COLOR={color}>{text}</BASEFONT>";

    private static string FormatTime(TimeSpan time) => $"{(int)time.TotalDays}d {time.Hours:00}h {time.Minutes:00}m";

    private readonly record struct Metric(string Label, string Value, string Tooltip = null);
}
