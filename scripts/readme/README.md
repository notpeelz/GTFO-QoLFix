# QoL Fix

[![GitHub](https://img.shields.io/github/license/{{REPO_PATH}}?color=green&style=for-the-badge)]({{REPO_URL}})

A general [GTFO](https://store.steampowered.com/app/493520/GTFO) improvement mod that aims to fix various quality of life issues.

{{#ifEquals release "standalone"}}
## How to install

1. Download the latest [IL2CPP x64 BepInEx build](https://builds.bepis.io/projects/bepinex_be)

2. Extract the archive to your game folder (`steamapps/common/GTFO`)

3. [Download the latest version of QoL Fix]({{../REPO_URL}}/releases) and put the DLL file in `BepInEx/plugins`

4. Download the [Unity 2019.4.1 libraries archive](https://github.com/LavaGang/Unity-Runtime-Libraries/raw/master/2019.4.1.zip) and extract it in `BepInEx/unity-libs`. Create the folder if it doesn't exist.

5. Launch your game

{{/ifEquals}}
{{#ifEquals release "thunderstore"}}
[1. Features](#features)

[2. Bugfixes](#bugfixes)

[3. Changelog](#changelog)

{{/ifEquals}}
## Features

### DropResources

Lets you put resources/consumables back in lockers/boxes.

{{embedVideo name='dropresources' height='240' url='https://i.imgur.com/SfCT6dD.mp4'}}

### LatencyHUD

Displays network latency on your HUD.

{{embedImage height='120' name='latencyhud_ping_hover'}} {{embedImage height='120' name='latencyhud_ping_watermark'}}

Known bugs: due to a bug with the way GTFO estimates network latency, the ping is only updated once upon joining a game.

### SteamProfileLink

Lets you open the steam profile of your teammates by clicking on their name.

{{embedVideo name='steamprofile' height='240' url='https://i.imgur.com/iMfZv7S.mp4'}}

### BetterWeaponSwap

Changes the weapon swap order dynamically based on the drama state of the game (stealth, combat, etc.)

This prevents accidentally switching back to your primary weapon after, e.g., running out of glowsticks.

{{embedVideo name='betterweaponswap' height='240' url='https://i.imgur.com/Q4cZQff.mp4'}}

### IntroSkip

Skips the game intro on startup. Gets you on the rundown screen within seconds of launching the game!

{{embedVideo name='introskip' height='240' url='https://i.imgur.com/4Z6XJe4.mp4'}}

### ElevatorVolume

Lowers the game volume during the elevator scene. No more alt-tabbing or screaming to your teammates during the elevator sequence!

### ElevatorIntroSkip

Skips the intro that plays when dropping into a level.

### BetterInteractions

Fixes several quirks of the interaction system, namely:

- Hacking tool minigame would get cancelled if you swapped weapons or moved too early

- The mine deployer would prioritize interacting with doors over placing mines

- Mines placed high up would be hard to pick up while jumping

- Placing sentries/mines would get cancelled while the player is walking/running

- Overlapping items (e.g. cells) would cause the interaction to cancel because the game couldn't decide which one you were trying to interact with

- Reloading would disable interactions

- Reviving teammates would cancel for seemingly no reason; with this patch, you can **look around freely** while reviving.

  NOTE: for balance reasons, this also disables shooting/ADSing while reviving.

{{embedVideo name='betterinteractions' height='240' url='https://i.imgur.com/Mi7jy8l.mp4'}}

### RunReloadCancel

Lets you cancel your reload by sprinting rather than having to swap weapons.

{{embedVideo name='runreloadcancel' height='240' url='https://i.imgur.com/8XhBKdQ.mp4'}}

### BetterMovement

Improves the GTFO movement system. Currently only lets you charge/reload your weapons mid-air.

{{embedVideo name='bettermovement' height='240' url='https://i.imgur.com/yLqX835.mp4'}}

### LobbyUnready

Lets you unready after readying up.

{{embedImage name='lobbyunready' height='60'}}

### ResourceAudioCue

Plays a sound when receiving ammo or health from a teammate.

### TerminalPingableSwaps

Relists swapped out items on terminals.

This lets you list/ping/query items after moving them.

Known bugs: when pinging a swapped item, the ping icon will not show up for swapped items unless you're the host. The ping audio will still play regardless of being host or client.

### RecentlyPlayedWidth

Updates the Steam recent players list.

{{embedImage name='steamrecentplayers' height='180'}}

### FramerateLimiter

Lowers your FPS when alt-tabbing to preserve system resources.

### ScreenLiquidRemoval

{{sub text='(default: disabled)'}}

Prevents screen liquid effects from playing. This can be configured to remove blood, spitter or infection effects.

### NoiseRemoval

{{sub text='(default: disabled)'}}

Disables the blue noise shader. This makes the game look clearer, although some areas might look a lot darker than normal.

| Before | After |
| ------ | ----- |
| {{embedImage name='bluenoise_before' height='120'}} | {{embedImage name='bluenoise_after' height='120'}} |

### PlayerAmbientLight

{{sub text='(default: disabled)'}}

This lets you alter the player ambient light range. The ambient light is often the only source of light in dark areas. Careful not to set it too low!
NOTE: for balance reasons, you can't increase it past the default vanilla value (10)

### HideCrosshair

{{sub text='(default: disabled)'}}

Hides the in-game crosshair when a weapon is out. Only useful if using [an external crosshair](https://github.com/notpeelz/reshade-xhair)... or if you fancy playing without a crosshair :)

### DisableSteamRichPresence

{{sub text='(default: disabled)'}}

Disables Steam Rich Presence updates; also prevents Steam friends from seeing your lobby from the rundown screen.

## Bugfixes

Fixes these bugs:

- (**FixBioScannerNavMarker**) bio tracker tags would remain on screen after multiple scans; no more floating triangles everywhere!

  {{embedImage name='biotracker_navmarker' height='240'}}

- (**FixToolRefillBioScanner**) bio tracker could be given tool refills

- (**FixLockerPing**) resources inside of lockers/boxes weren't individually pingable

  {{embedImage name='fixlockerping' height='120'}}

- (**FixDoorCollisionPatch**) c-foam globs could go through doors if aimed at the cracks

  Note: this bugfix also fixes the door double-hit exploit

- (**FixDoorFramePing**) the door frames on the tech tileset weren't pingable

- (**FixSoundMuffle**) the scout/map muffle sound effect wouldn't get reset under certain circumstances

- (**FixFlashlightState**) the flashlight would turn off when dropping/swapping items

- (**FixWeaponAnimations**) animation sequences (e.g. reload) would carry over when switching weapons

  {{embedVideo name='fixweaponanimations' height='240' url='https://i.imgur.com/atcrG69.mp4'}}

## Credits

Thanks a lot to:
- DarkCactus, fanta, Solakka, Gorilla, Phantasm, easternunit100, Dex and Project Zaero for helping me test during development
- knah, Spartan, js6pak, dak and ghorsington for answering my thousands of questions

## Licensing

This project uses code from:

- (GPL-3.0) [UnityExplorer](https://github.com/sinai-dev/UnityExplorer) - uses its UI code for update notifications
- (Apache-2.0) [Roslynator](https://github.com/JosefPihrt/Roslynator) - uses its C# analyzer extension methods
{{#ifEquals release "thunderstore"}}

## Changelog

{{>changelog}}
{{/ifEquals}}
