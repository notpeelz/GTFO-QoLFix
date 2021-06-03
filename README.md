[//]: # (THIS FILE WAS GENERATED FROM Templates/README.md)
[//]: # (release: standalone)

# GTFO-QoL

[![GitHub](https://img.shields.io/github/license/notpeelz/GTFO-QoLFix?color=green&style=for-the-badge)](https://github.com/notpeelz/GTFO-QoLFix)
[![Thunderstore](https://img.shields.io/badge/Thunderstore-blue?style=for-the-badge)](https://gtfo.thunderstore.io/package/notpeelz/QoLFix)

A general [GTFO](https://store.steampowered.com/app/493520/GTFO) improvement mod that aims to fix various quality of life issues.

## How to install

1. Download R2ModMan from [Thunderstore](https://thunderstore.io/package/ebkr/r2modman/) or [GitHub](https://github.com/ebkr/r2modmanPlus)

2. Install QoL from [Thunderstore](https://gtfo.thunderstore.io/package/notpeelz/QoLFix)

## Manual install

1. Download the latest [IL2CPP x64 BepInEx build](https://builds.bepis.io/projects/bepinex_be)

2. Extract the archive to your game folder (`steamapps/common/GTFO`)

3. [Download the latest version of QoL](https://github.com/notpeelz/GTFO-QoLFix/releases) and put the DLL file in `BepInEx/plugins`

4. Download the [Unity 2019.4.21 libraries archive](https://github.com/LavaGang/Unity-Runtime-Libraries/raw/master/2019.4.21.zip) and extract it in `BepInEx/unity-libs`. Create the folder if it doesn't exist.

5. Launch your game

## Features

## Better Lockers

Lets you put resources back in lockers.

<a href="https://i.imgur.com/SfCT6dD.mp4"><img height="240" src="img/dropresources_thumbnail.jpg"></a>

Also fixes resource pings showing up as "resource box".

<img height="120" src="img/fixlockerping.jpg">

## Elevator Earplugs

Lowers the game volume during the elevator sequence; no more alt-tabbing or screaming to your teammates!

## Intro Skip

Skips the game intro on startup. Gets you on the rundown screen within seconds of launching the game!

<a href="https://i.imgur.com/4Z6XJe4.mp4"><img height="240" src="img/introskip_thumbnail.jpg"></a>

## Elevator Intro Skip

Skips the intro that plays when dropping into a level.

## Latency Info

Displays network latency on your HUD.

<img height="120" src="img/latencyhud_ping_hover.jpg"> <img height="120" src="img/latencyhud_ping_watermark.jpg">

## Resource Audio Cue

Plays a sound when receiving ammo or health from a teammate.

## Run Reload-cancel

Lets you cancel the reload animation by sprinting rather than having to swap weapons.

<a href="https://i.imgur.com/8XhBKdQ.mp4"><img height="240" src="img/runreloadcancel_thumbnail.jpg"></a>

## Better Weapon Swap

Changes the weapon swap order dynamically based on the drama state of the game (stealth, combat, etc.)

This prevents accidentally switching back to your primary weapon after, e.g., running out of glowsticks.

<a href="https://i.imgur.com/Q4cZQff.mp4"><img height="240" src="img/betterweaponswap_thumbnail.jpg"></a>

## Better Movement

Improves the GTFO movement system. Currently only lets you charge/reload your weapons mid-air.

<a href="https://i.imgur.com/yLqX835.mp4"><img height="240" src="img/bettermovement_thumbnail.jpg"></a>

## Terminal-pingable Swaps

Relists swapped out items on terminals.

This lets you list/ping/query items after moving them.

## Recently Played With

Updates the Steam recent players list.

<img height="180" src="img/steamrecentplayers.jpg">

## Profile Link

Lets you open the steam profile of your teammates by clicking on their name.

<a href="https://i.imgur.com/iMfZv7S.mp4"><img height="240" src="img/steamprofile_thumbnail.jpg"></a>

## Lobby Unready

Lets you unready after readying up.

<img height="60" src="img/lobbyunready.jpg">

## FPS Limiter

Lowers your FPS when alt-tabbing to preserve system resources.

Note: FPS limiting doesn't work with v-sync.

## Bug Fixes

Fixes various game bugs:

- (**WeaponSwitchAnimationBugFix**) animation sequences (e.g. reload) would carry over when switching weapons

  <a href="https://i.imgur.com/atcrG69.mp4"><img height="240" src="img/fixweaponanimations_thumbnail.jpg"></a>

- (**SoundMuffleBugFix**) the scout/map muffle sound effect wouldn't get reset under certain circumstances

- (**VelocityBugFix**) the player would lose all horizontal velocity when jumping too quickly upon landing

- (**MeleeChargeBugFix**) melee charge would get cancelled if you jumped and charged on the same frame

## Credits

Thanks a lot to:

- DarkCactus, fanta, Solakka, Gorilla, Phantasm, easternunit100, Dex and Project Zaero for helping me test during development

- knah, Spartan, js6pak, dak and ghorsington for answering my thousands of questions

- dak for the designing the logo
