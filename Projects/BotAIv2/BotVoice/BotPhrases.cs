using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// What a bot says, by occasion. The whole bank is written to <c>Configuration/bot-voice.json</c> the first
/// time the shard runs, so the words can be changed without touching the code; what is here is the default
/// when the file says nothing about an occasion.
///
/// <para>
/// <b>Keys are occasions, and an occasion may be specific or general.</b> <c>took:mine</c> is said on taking
/// up mining; if there is no such key, <c>took:*</c> is. The same for <c>finished</c>, <c>failed</c> and
/// <c>dropped</c>. The rest are one occasion each: a cry for help, a death, a mood, a search for something to
/// buy, a war, a hall.
/// </para>
///
/// <para>
/// <b>Placeholders</b>: <c>{name}</c>, <c>{class}</c>, <c>{work}</c>, <c>{stage}</c>, <c>{guild}</c>,
/// <c>{foe}</c>, <c>{item}</c>, <c>{amount}</c>, <c>{place}</c>, <c>{reason}</c>, <c>{minutes}</c>,
/// <c>{coin}</c>, <c>{enemy}</c>, <c>{killer}</c>, <c>{price}</c>.
/// </para>
/// </summary>
public static class BotPhrases
{
    public static Dictionary<string, string[]> Defaults() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["took:*"] =
            [
                "Right. {work} it is.",
                "Off to {work}, then.",
                "Something to do at last: {work}.",
                "{work} pays best today."
            ],
            ["took:mine"] =
            [
                "Off to the hills for ore.",
                "The pick wants using. Ore.",
                "There is metal in the ground and I mean to have it.",
                "Mining. Somebody has to."
            ],
            ["took:chop"] =
            [
                "Trees. Logs. Coin.",
                "Off to the woods with the axe.",
                "The fletchers will want logs."
            ],
            ["took:hunt"] =
            [
                "There is something worth killing near here.",
                "Hunting. Wish me luck.",
                "A hide and a haunch, if it goes well."
            ],
            ["took:prowl"] =
            [
                "Going to look for trouble.",
                "Off to see what is about.",
                "A walk with a blade drawn."
            ],
            ["took:forge"] =
            [
                "To the anvil.",
                "The iron will not beat itself.",
                "Smithing. Stand clear of the sparks."
            ],
            ["took:sew"] = ["Needle and thread.", "Somebody wants leather work done.", "Sewing, for whoever pays."],
            ["took:cook"] = ["Time to cook.", "Meat on the fire.", "Supper for anybody who wants it."],
            ["took:brew"] = ["Bottles and a mortar.", "Brewing. Do not touch anything."],
            ["took:fletch"] = ["Arrows for the archers.", "Shafts and feathers. Fletching."],
            ["took:inscribe"] = ["Scrolls to write.", "Quill and reagents. Inscribing."],
            ["took:restock"] = ["I am short of {stage}.", "To the shops for what I am missing."],
            ["took:peddle"] = ["Selling to the shopkeeper.", "Off to turn goods into coin."],
            ["took:unload"] = ["My pack is full. To the counter.", "Taking this lot to sell."],
            ["took:rescue"] = ["Hold on, I am coming!", "Somebody needs a hand. Going.", "I hear you. On my way."],
            ["took:mend"] = ["Let me see to that wound.", "Bandages. Hold still.", "Patching up."],
            ["took:flee"] = ["Not today!", "Run!", "I am not dying for this."],
            ["took:delve"] = ["Into the dark, then.", "The dungeon. Stay together.", "Down we go."],
            ["took:venture"] = ["Off to the caves, alone.", "There is gold in the dark, they say.", "I will see what lives under the hill."],
            ["finished:venture"] = ["In. Now, what lives here?", "The cave mouth is behind me."],
            ["took:harrow"] = ["That ground has killed enough of us. Clearing it.", "To the killing field, all of you."],
            ["took:rally"] = ["To arms! The guild calls.", "Rallying to the hall.", "War company. Coming."],
            ["took:homeward"] = ["Nothing worth doing here. Going home.", "Homeward."],
            ["took:lodge"] = ["A bed and a roof tonight.", "To the inn; I am done for the day.", "Ale, then sleep."],
            ["took:travel"] = ["I have never seen {place}. Time I did.", "The road to {place}, then.", "New horizons: {place}."],
            ["took:tame"] = ["Easy now. Easy.", "Come here, you.", "I will not hurt you."],
            ["took:bowyer"] = ["Seven good logs make a bow.", "Bows for the archers; the counter can keep its own."],
            ["took:weave"] = ["The sheep are woolly again. Scissors!", "Wool, wheel, loom: cloth of our own."],
            ["finished:weave"] = ["A bolt of cloth, and not a coin to the tailor for it.", "Bandages enough for a week."],
            ["finished:tame"] = ["There. Mine now.", "Good beast."],
            ["finished:travel"] = ["{place}, at last.", "So this is {place}."],
            ["road:safe"] = ["The road from {from} to {place} is clear, {minutes} minutes' walk.", "Safe going from {from} to {place}."],
            ["road:quiet"] = ["Nothing much on the road from {from} to {place}; {minutes} minutes.", "The road to {place} is quiet."],
            ["road:unsafe"] = ["Careful on the road from {from} to {place}: it reads {reason}.", "The road to {place} is {reason}. Go in company."],
            ["finished:lodge"] = ["Innkeeper, a room.", "Goodnight, all."],
            ["took:herbs"] = ["Reagents to pick.", "Off to the fields for herbs."],
            ["took:forage"] = ["Something on the ground worth having.", "Foraging."],
            ["took:reclaim"] = ["My things are on my corpse. Going back for them.", "Back to where I fell."],
            ["took:escort"] = ["I will see you there safely.", "Escorting."],
            ["took:tutor"] = ["Class is in session.", "Lessons, for a fee."],
            ["took:scout"] = ["Ground nobody has walked. Going to look.", "Scouting."],
            ["took:supply"] = ["Stocking the guild's counter.", "Supplies for the hall."],
            ["took:band"] = ["Who is with me? This wants a company.", "Banding together for this."],
            ["took:enlist"] = ["Signing on with the captain.", "Enlisting."],
            ["took:order"] = ["Putting money down for {stage}.", "I will pay for {stage}."],
            ["finished:*"] =
            [
                "Done with {work}.",
                "That is {work} finished.",
                "{work}: done. {minutes} minutes.",
                "Finished. Now what."
            ],
            ["finished:mine"] = ["Ore put away.", "A good haul of ore.", "The seam paid."],
            ["finished:chop"] = ["Logs stacked.", "Enough wood for today."],
            ["finished:hunt"] = ["Down it went.", "One less of those.", "Hide and meat. Good hunting."],
            ["finished:forge"] = ["Hot work, and it came out well.", "Another piece off the anvil."],
            ["finished:sew"] = ["Sewn and ready.", "Leather work done."],
            ["finished:cook"] = ["Supper is ready.", "Cooked. Eat while it is hot."],
            ["finished:brew"] = ["Bottles filled.", "Brewed."],
            ["finished:fletch"] = ["A quiver's worth.", "Arrows done."],
            ["finished:peddle"] = ["Sold. {coin} coin richer.", "The shopkeeper paid."],
            ["finished:unload"] = ["Pack emptied.", "All on the counter."],
            ["finished:rescue"] = ["You are all right now.", "That is dealt with.", "Glad I got there."],
            ["finished:mend"] = ["There. Good as new.", "Mended."],
            ["finished:flee"] = ["Lost it.", "Clear. That was close.", "Still breathing."],
            ["finished:delve"] = ["Out of the dark, and richer.", "Back from the dungeon."],
            ["finished:reclaim"] = ["Got my things back.", "Everything accounted for."],
            ["failed:*"] =
            [
                "That did not work.",
                "Nothing came of {work}.",
                "Giving up on {work}: {reason}.",
                "Not today. {reason}."
            ],
            ["failed:hunt"] = ["Too many of them.", "That thing would not go down.", "Leaving it. Not worth dying for."],
            ["failed:rescue"] = ["I could not turn it.", "Too late, or too few."],
            ["failed:mine"] = ["The seam is dry.", "Nothing left in that rock."],
            ["failed:flee"] = ["Cornered.", "Nowhere to run."],
            ["dropped:*"] =
            [
                "Something better came up.",
                "Leaving {work} for now.",
                "This can wait."
            ],
            ["seek"] =
            [
                "Anyone selling {item}? I need {amount}.",
                "Looking for {amount} {item}. Paying {price} each.",
                "Who has {item}? Coin waiting.",
                "I will buy {item}, {amount} of them."
            ],
            // The market, hand to hand (BotMarket, 01.10.2026): a want said in the trade chat, {town} where the buyer stands; a
            // raise; a carrier setting out ({buyer}); the handing over ({seller}, {buyer}, {amount}, {item}, {price} the total).
            ["market:want"] =
            [
                "Buying {amount} {item} at {price} gold each. Bring them to me in {town}, coin on the spot.",
                "Wanted: {amount} {item}, {price} apiece. Find me and I pay by hand.",
                "Who has {item}? {amount} of them, {price} each, paid when you hand them over."
            ],
            ["market:raise"] =
            [
                "Still after {amount} {item} — {price} each now.",
                "{item}, {amount} of them: I'll pay {price} apiece.",
                "Nobody has brought me {item}. {price} gold each, then."
            ],
            ["market:bring"] =
            [
                "{buyer}, I have your {amount} {item} at {price} each. Coming to you.",
                "On my way to {buyer} with {amount} {item}.",
                "{buyer}, hold still — {amount} {item} for you."
            ],
            ["market:hand"] = ["Here you are, {buyer}: {amount} {item}.", "{amount} {item}, as asked, {buyer}.", "Count them, {buyer}. {amount} {item}."],
            ["market:paid"] = ["{price} gold, as agreed. Thank you, {seller}.", "Good. Here is your {price}, {seller}.", "Just what I needed, {seller}. {price} gold."],
            // A bot keeping shop by the bank (BotShopkeep, 29.09.2026): {goods} is its three dearest stocks, {item} and {price} the first.
            ["shop:open"] =
            [
                "Open for trade by the bank in {town}: {goods}.",
                "Goods for sale by the bank in {town} — {goods}.",
                "Come and look, by the bank in {town}: {goods}, no dearer than the shops."
            ],
            ["shop:cry"] =
            [
                "{goods}! Fair prices, and no walk across town.",
                "Who is short of {item}? {price} gold apiece, here.",
                "Selling {goods}. Step up.",
                "{item}, {amount} of it, {price} gold each."
            ],
            ["shop:sold"] = ["There you are, {buyer}: {amount} {item}.", "Pleasure doing business, {buyer}.", "Mind how you go, {buyer}."],
            ["shop:close"] = ["That is me done for now.", "Shutting up shop.", "Closing. Back later with more."],
            ["cry"] =
            [
                "Help! {foe} is on me!",
                "{foe}! Somebody!",
                "Under attack at {place}!",
                "I need help here, now!"
            ],
            ["cry:low"] =
            [
                "I am dying! {foe}!",
                "Help me, I cannot hold!",
                "Almost dead here!"
            ],
            ["died"] =
            [
                "...",
                "Tell them I tried.",
                "So that is how it ends.",
                "{killer}. Of course."
            ],
            ["mood:high"] =
            [
                "Good day for it.",
                "I could do this all week.",
                "Purse is heavy and the road is clear.",
                "Never felt better."
            ],
            ["mood:mid"] =
            [
                "Could be worse.",
                "Getting by.",
                "Another day, another few coins.",
                "Not much to say. Working."
            ],
            ["mood:low"] =
            [
                "Nothing pays around here.",
                "Bored stiff.",
                "My purse is empty and so is my patience.",
                "Something had better turn up."
            ],
            // The seven moods that ask for something (BotMoods, 01.10.2026): each line must say which, because the bot in earshot
            // answers the tag and the person at the log reads the words.
            ["mood:hurt"] = ["I am bleeding and I have nothing to bind it with.", "Every step hurts. I need a bandage.", "Hurt, and no healer in sight.", "Look at me. Half dead on my feet."],
            ["mood:tired"] = ["I am done in. My legs will not carry me much further.", "Tired to the bone. A fire and a sit would do it.", "I could sleep standing up.", "Worn out. Somebody tell me where a fire is."],
            ["mood:grieving"] = ["We lost one of ours. I keep seeing it.", "A friend is dead and I was not there.", "Grief is heavy and the road is long.", "Not today. We are burying somebody."],
            ["mood:poor"] = ["Not a coin to my name. I need to kill something that pays.", "My purse is empty. Hunting is all that is left.", "Broke. Anyone hunting? I'll take a share.", "I cannot afford so much as a bandage."],
            ["mood:lonely"] = ["Nobody to talk to for miles. I could use some company.", "Alone again. Anyone going anywhere?", "A company would be welcome. Any company.", "I miss having somebody at my back."],
            ["mood:bored"] = ["Bored stiff. Give me something to do.", "Nothing pays around here and nothing moves.", "I would fight a tree for something to do.", "Something had better turn up, and soon."],
            ["mood:proud"] = ["That went well. Very well, if I say so myself.", "Purse heavier, blade sharper. Good day.", "Did you see that? Nobody did. Shame.", "I am getting good at this."],
            ["war:declared"] =
            [
                "{guild}: we are at war with {enemy}. To arms.",
                "War on {enemy}. Every blade to the hall.",
                "{enemy} has pushed too far. It is war."
            ],
            ["war:won"] = ["{guild} wins. {enemy} yields.", "Victory over {enemy}!"],
            ["war:lost"] = ["We lost to {enemy}. Lick your wounds.", "{enemy} had the better of us."],
            ["war:drawn"] = ["The war with {enemy} is over. Nobody won.", "A truce with {enemy}."],
            ["hall"] = ["A hall for {guild}! Come and see.", "{guild} has a roof at last."],
            ["guild:fell"] = ["{name} has fallen at {place}.", "We lost {name} at {place}. Somebody go."],
            ["greet"] = ["Well met, {name}.", "{name}. Still alive, then."]
        };
}
