namespace Server.Engines.Avatar;

public static class Constants
{
    public const string AVATAR_INTRO_TEXT = @"You may choose the path of the Avatar, where each life is but one step upon a greater Ascent. Your story begins at a sanctuary where fate has gathered you before you are cast into the world. Here you are weak and nearly bereft of skill, yet you carry a blessed tome called The Avatar's Ascent.<br><br>Before you leave this sanctuary, choose a Template to guide your rebirth, and consider your Ascensions carefully. Ascensions are permanent gifts to your lineage that may preserve knowledge between lives, unlock greater potential, or soften the harsh truth that awaits you outside. Unless an Ascension says otherwise, everything you own is lost when you die, and you cannot be resurrected as others are.<br><br>Death will end your current life and return you here to begin anew, while the coins you farmed are banked for your next preparations. Your house alone may endure. Once you step beyond the sanctuary, you may read your tome but you may purchase nothing more from it until fate brings you back.<br><br>This is a challenging journey of self-discovery. Each run is temporary; each death is a lesson. Those who endure may raise their stat and skill ceilings, master Primary and Secondary skills from past lives, and someday claim vengeance against the faction that wronged their family. Will you begin the Avatar's Ascent?";

    public const int DRAFT_LEVELS_PER_PICK = 4;
    public const int DRAFT_MAX_LEVEL = 100;
    public const int DRAFT_PICKS_PER_ROUND = 3;
    public const int DRAFT_START_PICK_AMOUNT = 4;

    public const int RIVAL_BONUS_MAX_POINTS = 50 * 1000;
    public const int RIVAL_BONUS_PERCENT = 50;
    public const int SKILL_CAP_BASE = 3000;

    public const int BASE_STAT_CAP = 100;

    public const int KILL_COIN_BREATH_LARGE = 135;

    public const int IMPROVED_TEMPLATE_MAX_COUNT = 5;
    public const int POINT_GAIN_RATE_MAX_LEVEL = 100;
    public const int POINT_GAIN_RATE_PER_LEVEL = 1;
    public const int RECORDED_SKILL_CAP_INTERVAL = 5;
    public const int RECORDED_SKILL_CAP_MAX_AMOUNT = 125;

    public const int RECORDED_SKILL_CAP_MAX_LEVEL =
        (RECORDED_SKILL_CAP_MAX_AMOUNT - RECORDED_SKILL_CAP_MIN_AMOUNT) / RECORDED_SKILL_CAP_INTERVAL;

    public const int RECORDED_SKILL_CAP_MIN_AMOUNT = 30;
    public const int SAFETY_DEPOSIT_BOX_MAX_LEVEL = 10;
    public const int SKILL_CAP_MAX_LEVEL = 70;
    public const int SKILL_CAP_PER_LEVEL = 10;
    public const int SKILL_GAIN_RATE_MAX_LEVEL = 10;
    public const int SKILL_GAIN_RATE_PER_LEVEL = 5;
    public const int STAT_CAP_MAX_LEVEL = 150;
    public const int STAT_CAP_PER_LEVEL = 1;
}
