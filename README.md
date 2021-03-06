# QoL Fix
A general [GTFO](https://store.steampowered.com/app/493520/GTFO) improvement mod that aims to fix quality of life issues, bugs and add various tweaks.

## How to install

1. Download the latest [IL2CPP x64 BepInEx build](https://builds.bepis.io/projects/bepinex_be)
2. Extract the archive to your game folder (`steamapps/common/GTFO`)
3. [Download the latest version of QoL Fix](https://github.com/notpeelz/GTFO-QoLFix/releases) and and put the DLL file in `BepInEx/plugins`
4. Download the [Unity 2019.4.1 libraries archive](https://github.com/LavaGang/Unity-Runtime-Libraries/raw/master/2019.4.1.zip) and extract it in `BepInEx/unity-libs`. Create the folder if it doesn't exist.
5. Launch your game

## Features

### DropResources

Lets you put resources/consumables back in lockers/boxes.

<a href="https://i.imgur.com/SfCT6dD.mp4">
  <img height="240" alt="dropresources thumbnail" src="img/dropresources_thumbnail.jpg">
</a>

### LatencyHUD

Displays network latency on your HUD.

<p float="left">
  <img height="120" alt="ping hover" src="img/latencyhud_ping_hover.jpg">
  <img height="120" alt="ping watermark" src="img/latencyhud_ping_watermark.jpg">
</p>

Known bugs: due to a bug with the way GTFO estimates network latency, the ping is only updated once upon joining a game.

### SteamProfileLink

Lets you open the steam profile of your teammates by clicking on their name.

<a href="https://i.imgur.com/iMfZv7S.mp4">
  <img height="240" alt="steamprofile thumbnail" src="img/steamprofile_thumbnail.jpg">
</a>

### BetterWeaponSwap

Changes the weapon swap order dynamically based on the drama state of the game (stealth, combat, etc.)

This prevents accidentally switching back to your primary weapon after, e.g., running out of glowsticks.

<a href="https://i.imgur.com/Q4cZQff.mp4">
  <img height="240" alt="betterweaponswap thumbnail" src="img/betterweaponswap_thumbnail.jpg">
</a>

### IntroSkip

Skips the game intro on startup. Gets you on the rundown screen within seconds of launching the game!

<a href="https://i.imgur.com/4Z6XJe4.mp4">
  <img height="240" alt="introskip thumbnail" src="img/introskip_thumbnail.jpg">
</a>

### ElevatorVolume

Lowers the game volume during the elevator scene. No more alt-tabbing or screaming to your teammates during the elevator sequence!

### ElevatorIntroSkip

Skips the intro that plays when dropping into a level.

### LobbyUnready

Lets you unready after readying up.

<img height="60" alt="lobby unready" src="img/lobbyunready.jpg">

### ResourceAudioCue

Plays a sound when receiving ammo or health from a teammate.

### TerminalPingableSwaps

Relists swapped out items on terminals.

This lets you list/ping/query items after moving them.

Known bugs: when pinging a swapped item, the ping icon will not show up for swapped items unless you're the host. The ping audio will still play regardless of being host or client.

### RecentlyPlayedWidth

Updates the Steam recent players list.

<img height="180" alt="steam recent players" src="img/steamrecentplayers.jpg">

### NoiseRemoval
<sub>(default: disabled)</sub>

Disables the blue noise shader. This makes the game look clearer, although some areas might look a lot darker than normal.

| Before | After |
| ------ | ----- |
| <img height="120" alt="with noise" src="img/bluenoise_before.jpg"> | <img height="120" alt="without noise" src="img/bluenoise_after.jpg"> |

### HideCrosshair
<sub>(default: disabled)</sub>

Hides the in-game crosshair when a weapon is out. Only useful if using [an external crosshair](https://github.com/notpeelz/reshade-xhair)... or if you fancy playing without a crosshair :)

### DisableSteamRichPresence
<sub>(default: disabled)</sub>

Disables Steam Rich Presence updates; also prevents Steam friends from seeing your lobby from the rundown screen.

## Bugfixes

Fixes these bugs:
- bio tracker tags would remain on screen after multiple scans; no more floating triangles everywhere!

  <img height="240" alt="biotracker navmarker" src="img/biotracker_navmarker.jpg">

- bio tracker could be given tool refills
- resources inside of lockers/boxes weren't individually pingable

  <img height="120" alt="resource in locker ping" src="img/fixlockerping.jpg">

- c-foam globs could go through doors if aimed at the cracks

  Note: this bugfix also fixes the door double-hit exploit

- the door frames on the tech tileset weren't pingable
- the scout muffle sound effect wouldn't get reset when exiting a game too early
- the map muffle sound effect wouldn't get reset under certain circumstances
- the flashlight would turn off when dropping/swapping items

## Credits

Thanks a lot to:
- DarkCactus, fanta, Solakka, Gorilla and Phantasm for helping me test during development
- knah, Spartan, js6pak, dak and ghorsington for answering my thousands of questions

## Licensing

This project uses code from:

- (GPL-3.0) [UnityExplorer](https://github.com/sinai-dev/UnityExplorer) - uses its UI code for update notifications
- (Apache-2.0) [Roslynator](https://github.com/JosefPihrt/Roslynator) - uses its C# analyzer extension methods