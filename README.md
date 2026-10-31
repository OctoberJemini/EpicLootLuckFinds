# Epic Loot: Luck Finds

Make every adventure—and every visit to the trader—a chance to find something special.

Luck Finds adds rare equipment upgrades, special trader offers, and triple-coin bounties to Epic Loot. Ordinary creature equipment follows your boss progression, while lucky finds give you a reason to face tougher enemies and check the bounty board.

**Version 1.5.2 — lucky bounty beta. API/build checks pass on Epic Loot 0.15.1–0.15.4; newer versions are checked at startup. Designed for single-player and the local player hosting a world. Dedicated servers and remote clients are not supported.**

## Lucky chances

| Reward | Chance |
| --- | --- |
| Creature lucky gear | 1% |
| Bounty next-region gear | 5% |
| New vendor lucky offers | 5% |
| Triple-coin bounties | 20% |

Triple-coin bounties change only coins. Tokens, equipment, and difficulty are unchanged by the coin bonus. Existing vendor offers retain their saved outcome until refresh.

## Features

- Creature equipment matches the creature's region, capped at your next undefeated boss's region. A 1% lucky roll bypasses that cap to reach the creature's own region, never above it.
- Accepted main bounty targets use their contract region, with a 5% chance to upgrade equipment to the next region.
- Each new eligible gambling offer has a 5% chance to feature equipment one region above current boss progression. The upgraded equipment is visible before purchase.
- Opening the trader with new, unseen lucky stock plays a short rising chime and displays a Lucky Find message. Reopening or relogging neither repeats the announcement nor rerolls stock.
- New bounty offers have a fixed 20% chance to become Lucky Bounties with triple coins. Tokens, equipment rewards, difficulty, and existing equipment lucky chances are unchanged.
- Rarity, enchantments, equipment quantities, creature coins, and material drops remain controlled by Epic Loot. Equipment lucky chances are configurable.

## Boss progression and vendor gambling

Boss progression follows world boss defeats in order. Skipping a boss does not skip the progression cap.

| Next undefeated boss | Normal equipment cap | Lucky vendor equipment (5%) |
| --- | --- | --- |
| Eikthyr | Meadows | Black Forest |
| The Elder | Black Forest | Swamp |
| Bonemass | Swamp | Mountains |
| Moder | Mountains | Plains |
| Yagluth | Plains | Mistlands |
| The Queen | Mistlands | Ashlands |
| Fader | Ashlands | Deep North* |
| Final configured region | Final region | No further upgrade |

The cap does not upgrade weaker creatures. Meadows creatures still yield Meadows equipment after Moder is defeated. Ordinary gambling equipment is limited by progression; its offer pool is still controlled by Epic Loot.

## Creature equipment progression

| Creature's assigned region | Normal equipment | Lucky equipment (1%) |
| --- | --- | --- |
| Meadows | Meadows | Meadows |
| Black Forest | Black Forest, capped by progression | Black Forest |
| Swamp | Swamp, capped by progression | Swamp |
| Mountains | Mountains, capped by progression | Mountains |
| Plains | Plains, capped by progression | Plains |
| Mistlands | Mistlands, capped by progression | Mistlands |
| Ashlands | Ashlands, capped by progression | Ashlands |
| Deep North* | Deep North, capped by progression | Deep North* |

For example, with Bonemass undefeated, a Plains creature normally yields Swamp equipment, with a 1% chance of Plains equipment. It cannot yield Mistlands equipment through this rule. The creature's region comes from Epic Loot's creature classification, not the player's position.

## Lucky Bounties: triple coins

Each bounty offer first shown with Luck Finds enabled has a **20% chance** to become a Lucky Bounty. Only its coin reward is tripled.

| Bounty type | Chance | Coin reward | Tokens and equipment |
| --- | --- | --- | --- |
| Normal | 80% | Standard | Unchanged |
| Lucky Bounty | 20% | 3× standard | Unchanged |

For example, a bounty normally paying **300 coins** pays **900 coins** when lucky. Gold and iron bounty tokens, target difficulty, and equipment drops remain unchanged. The existing **5% next-region bounty equipment chance** is separate and unchanged. The long-run average coin increase is 40% at these settings; individual boards may have any number of lucky offers.

Lucky offers show a gold **Lucky Bounty** label and the increased coin amount before acceptance. Outcomes and coin amounts are saved per world, player, and bounty identity. Reopening the board, relogging, or abandoning the same bounty cannot reroll its bonus or compound the multiplier. A new refresh creates new bounty identities and new chances.

Already accepted or completed bounties from before this update retain their existing rewards. Once you accept a new lucky offer, Epic Loot saves and pays its displayed coin amount through its normal reward system. Zero-coin offers stay at zero. Rewards too large to triple safely remain unchanged.

Keep **BepInEx/config/LuckyFindsBounties** to preserve offer history. If this history cannot be read or saved, new lucky bounty adjustments are disabled and a warning is logged. Previously accepted coin rewards remain in Epic Loot's own bounty save. The bounty coin chance is fixed at 20% in this release; existing equipment/vendor chance settings do not control it.

## Bounty equipment progression

Accepted main bounty targets bypass the boss cap. Accompanying creatures, unaccepted targets, and abandoned quests do not receive this exception.

| Bounty region | Normal equipment | Lucky equipment (5%) |
| --- | --- | --- |
| Meadows | Meadows | Black Forest |
| Black Forest | Black Forest | Swamp |
| Swamp | Swamp | Mountains |
| Mountains | Mountains | Plains |
| Plains | Plains | Mistlands |
| Mistlands | Mistlands | Ashlands |
| Ashlands | Ashlands | Deep North* |
| Deep North* | Deep North | Deep North (final tier) |

These rules apply to eligible equipment rolls. They do not guarantee that every kill drops equipment. Creature lucky gear uses 1% (about 1 in 100 eligible rolls); bounty and vendor lucky gear use 5% (about 1 in 20). These are independent chances, not guaranteed rewards after a fixed number of attempts.

*Deep North requires eligible equipment from your installed content. Progression follows Epic Loot's configured biome order. Vendor upgrades require an eligible item in the same equipment category; otherwise the original offer remains. Creature and bounty selection preserves category where possible, then falls back to the same equipment slot/type, then another eligible item in the target region. Empty region pools retain Epic Loot's boss-gated result with a warning.*

## Vendor details

Regular and token equipment gambles can qualify; ordinary shop sales do not. Lucky offers retain their original price and guaranteed-rarity options. Rarity and enchantments still roll when purchased. There is no second lucky upgrade roll at purchase.

Both successful and unsuccessful stock rolls are saved per world, player, stock refresh, and original offer. Existing offers keep their outcomes even if progression or settings change during that refresh interval. New stock can produce new lucky finds and a new announcement.

## Installation and settings

Install with a Thunderstore-compatible mod manager and the listed dependencies. For manual installation, copy the included plugins folder into BepInEx, with Epic Loot and its dependencies already installed. Restart Valheim.

If you previously installed a private build, keep only one copy of EpicLootLuckyFinds.dll. The internal plugin identifier remains local.epicloot.luckyfinds so existing settings and saved stock remain compatible.

Settings are generated in BepInEx/config/local.epicloot.luckyfinds.cfg:

| Setting | Default | Purpose |
| --- | --- | --- |
| General / Enabled | true | Enable Luck Finds |
| Creatures / Lucky Gear Chance Percent | 1 | Creature lucky equipment chance |
| Bounties / Lucky Gear Chance Percent | 5 | Bounty next-region equipment chance |
| General / Log Lucky Finds | true | Log equipment selection decisions |
| Vendor / Lucky Gear Chance Percent | 5 | Lucky upgrade chance for new gambling offers |

Keep BepInEx/config/LuckyFindsStock to preserve saved vendor outcomes and announcements. Corrupt history is preserved and reported; persistence failures disable new lucky stock rather than allowing repeated rerolls.

## Compatibility and beta status

While active, Luck Finds uses boss-based Epic Loot equipment gating instead of recipe-knowledge gating. Non-creature loot retains that base boss-gated behavior; existing equipment is unchanged. Unknown creature classifications also retain base gating.

This build checks the Epic Loot APIs it uses instead of requiring one exact version. It verifies 84 type/member/enum/patch-signature requirements before loading its engine. Newer versions can load when these checks and Harmony patch installation succeed. If a required API is missing or changed, Luck Finds stays inactive and explains why in the log. If patch installation fails, its own patches are removed. API checks cannot guarantee unchanged gameplay behavior in future releases. Other mods that change Epic Loot gating, creature drops, or gambling may conflict. Install it on a separate test profile before adopting it in an established world.

Automated progression, vendor-history, and bounty-reward tests passed. API and build checks passed against Epic Loot 0.15.1–0.15.4. These checks do not replace in-game testing: startup, lucky bounty display and payout, and vendor purchase/audio still need live verification for this beta.

This is an independent add-on for Epic Loot, not an official Epic Loot release.

### Epic Loot compatibility

| Epic Loot release | Status |
| --- | --- |
| 0.15.1, 0.15.2, 0.15.3, 0.15.4 | API, compile, and normal-loot patch-pattern checks passed |
| Newer releases | Allowed when startup API and patch checks pass; gameplay compatibility is not guaranteed |
| 0.15.0 | Not verified |
| 0.13.3 and tested 0.14.x releases | Unsupported: required creature classification and/or loot APIs differ |

The Thunderstore dependency minimum is 0.15.1. Do not downgrade Epic Loot in an existing world solely to test this add-on. Existing Luck Finds configuration and vendor stock history retain their identifiers and format.



### Updating from 1.5.0 or earlier

Version 1.5.2 introduces separate Lucky Gear Chance Percent settings for Creatures, Bounties, and Vendor, defaulting to 1%, 5%, and 5%. Older shared Chance Percent and vendor Lucky Chance Percent keys are no longer used. Existing saved vendor offers keep their outcomes until the next stock refresh. Triple-coin bounties remain at 20%.


## Source and support

[Source code](https://github.com/OctoberJemini/EpicLootLuckFinds) · [Report an issue](https://github.com/OctoberJemini/EpicLootLuckFinds/issues) · [Thunderstore](https://thunderstore.io/c/valheim/p/OctoberJemini/Epic_Loot_Luck_Finds/)

For build instructions see [BUILDING.md](https://github.com/OctoberJemini/EpicLootLuckFinds/blob/main/BUILDING.md). The icon was created with AI assistance. No Epic Loot or Valheim binaries are included in this repository.

