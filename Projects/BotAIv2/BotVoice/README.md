# Voice: what the bots say

The bots speak: when they take up work and when they finish it, when they are hit, when they die, when a war
is declared, when a hall goes up, when they want to buy something, and now and then about how they feel.
Built on 28.09.2026 for the order "socialisation: phrases when they do things, their mood, guild chat, the
world when they are looking for something, a shout when attacked."

| File | What is in it |
|---|---|
| `BotVoice.cs` | `Say(who, channel, text)`: the throttle, the delivery, the event, the transcript; the trade room; and the occasions — took, ended, struck, died, mood, a guild's news |
| `BotMoods.cs` | the one word a mood line carries, read off facts the shard keeps |
| `BotPhrases.cs` | the phrase bank, by occasion, with placeholders |
| `BotVoiceConfig.cs` | `Configuration/bot-voice.json`, which carries the whole bank so it can be edited |
| `BotVoiceModule.cs` | the module: hooks the will, runs the mood clock |

## Speech is a by-product

Nothing here is offered to the auction and nothing costs a beat. A line rides on an event that was happening
anyway, and the only decisions taken are whether to say it and where. This is the invariant every observer
on the shard keeps: the voices must never change what the population does. The one place speech was already
a mechanism — the thinking crafters' remarks, the warden's toll, the fence's threat — now passes through
`BotVoice.Aloud`, unrationed, so the page and the transcript see it.

## Six channels

| channel | audience | in the world |
|---|---|---|
| `local` | whoever stands near | said overhead |
| `guild` | the guild | guild chat, and overhead with the guild's letters |
| `trade` | whoever is in the trade room | said overhead, and posted to the chat conference `TradeConference` |
| `world` | everybody | yelled, and broadcast to every client if `Broadcast` |
| `mood` | whoever stands near | said overhead in its own hue |
| `cry` | whoever stands near | yelled in red |

Every line, whatever the channel, is an event of kind `say` on the stream and a line in `logs/bot-speech.log`,
because the person watching is more often at the page than in the world.

## Rationed per bot

One line per bot per channel every `SayEveryMs` (15 s); a cry every `CryEveryMs` (12 s); a mood every
`MoodEveryMs` (15 min), staggered from boot so fifty bots do not all speak in the same minute. A trade line about an item
(a want, a raise, an order) is also held when any bot said one about the same item within `TradeRepeatMs` (a minute). Taking or
finishing work is said with probability `WorkChance` (0.35), a failure with `FailChance` (0.6), a drop with
`DropChance` (0.15). A flight's end is always said. An order is always said in the trade channel, because it is a
question to the whole market. The throttle is by bot, not by shard: fifty bots each saying something every fifteen
seconds reads as a crowd; one bot every second is a fault.

## The phrase bank

Keys are occasions. `took:mine` is said on taking up mining and falls back to `took:*`; the same for
`finished`, `failed` and `dropped`. The rest are one occasion each: `seek`, `cry`, `cry:low`, `died`,
`mood:high`, `mood:mid`, `mood:low`, `war:declared`, `war:won`, `war:lost`, `war:drawn`, `hall`, `guild:fell`.
Placeholders: `{name}`, `{class}`, `{work}`, `{stage}`, `{guild}`, `{foe}`, `{item}`, `{amount}`, `{price}`,
`{place}`, `{reason}`, `{minutes}`, `{coin}`, `{enemy}`, `{killer}`. A placeholder nobody filled is cut. A bot
never says the same line twice running.

The whole bank is written to `bot-voice.json` the first time the shard runs. A key given there replaces the
code's list for that occasion; keys left out keep the code's. Every word a bot says can be changed without
touching the code, and the channels can be switched off one by one.

## The trade room

Built on 02.10.2026 for Patrick's "let the bots create a Trade chat and write into it, because they write into the global
one and clog half the screen". On build 389 the world channel carried 4018 lines in its first hour, 3780 an hour, and every
one of them was trade: wants, raises, offers, orders and a shop's opening. Each was a `World.Broadcast` — a system message on
every connected client, which nobody can leave.

Now they go to the `trade` channel: wants and raises (`BotMarket`), orders (`seek`), a carrier setting out (`market:bring`),
a trader's offer, the buyer's "Agreed" and the trader's "in my book" (`BotHaul`), and a shop's opening, cries and closing
(`BotShopkeep`). A trade line is said overhead in `TradeHue` (the hue the world's yell had) and posted to the engine's chat
conference named `TradeConference` ("Trade"), made at boot as a standing conference beside the engine's "Newbie Help". The
hand-over lines ("here you are", "as agreed, thank you", a keeper's "there you are") are said to one bot standing beside the
speaker and stay local unless `TradeHandOvers`. The world channel is kept, for anything said to everybody that is not trade.

**What a player does.** ClassicUO opens the engine's chat at login and the engine puts the player into the first conference
on its list, "Newbie Help"; the trade room is second (`TradeAtLogin` puts it first). To join: open the chat window (in
ClassicUO a macro, *Open → Chat*) and double-click *Trade*; to leave, the window's *Leave* button or another conference.
ClassicUO prints a conference's lines in the same corner of the screen as a broadcast, in the chat hue of the player's
profile, as `Name [ABBR]: line` — which is why the room is not joined at login and why the same item is called at most once
in `TradeRepeatMs` across the whole market. A bot is not a chat user (that needs a connection), so it posts to the room
without being in it, and nobody can ignore one by name. ClassicUO's *Leave* button sends chat action `0x43`, which the engine
did not handle; the voice registers it, where nothing else has, as "leave the conference you are in".

**What is counted.** The `Voice:` line of the five-minute summary: lines by channel since boot and an hour, the players in
the room now, trade lines posted while somebody was there, to an empty room, and the attempts held as a repeat of an item.
Every trade line is still an event of kind `say` with channel `trade` on the stream, and `[trade]` in `logs/bot-speech.log`.

| key | default | what |
|---|---|---|
| `Trade` | `true` | the channel at all |
| `TradeConference` | `Trade` | the conference's name; empty: overhead only |
| `TradeAtLogin` | `false` | the room first on the list, so a login lands in it |
| `TradeRepeatMs` | `60000` | one call for an item across the market this often; 0: no limit |
| `TradeOverhead` | `true` | also said over the speaker's head |
| `TradeHandOvers` | `false` | hand-over lines to the room too |
| `TradeHue` | `0x59` | the overhead hue |
