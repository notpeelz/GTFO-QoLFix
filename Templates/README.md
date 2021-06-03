{{h1 MOD_LONG_NAME}}

{{> license}}
{{#equals project ""}}
[![Thunderstore](https://img.shields.io/badge/Thunderstore-blue?style=for-the-badge)]({{../PKG_URL}})
{{/equals}}

A general [GTFO](https://store.steampowered.com/app/493520/GTFO) improvement mod that aims to fix various quality of life issues.

{{#equals project ""}}
{{h2 "How to install"}}

1. Download R2ModMan from [Thunderstore](https://thunderstore.io/package/ebkr/r2modman/) or [GitHub](https://github.com/ebkr/r2modmanPlus)

2. Install {{MOD_SHORT_NAME}} from [Thunderstore]({{../PKG_URL}})

{{h2 "Manual install"}}

1. Download the latest [IL2CPP x64 BepInEx build](https://builds.bepis.io/projects/bepinex_be)

2. Extract the archive to your game folder (`steamapps/common/GTFO`)

3. [Download the latest version of {{MOD_SHORT_NAME}}]({{REPO_URL}}/releases) and put the DLL file in `BepInEx/plugins`

4. Download the [Unity 2019.4.21 libraries archive](https://github.com/LavaGang/Unity-Runtime-Libraries/raw/master/2019.4.21.zip) and extract it in `BepInEx/unity-libs`. Create the folder if it doesn't exist.

5. Launch your game

{{/equals}}
{{#equals release "thunderstore"}}
[1. Features](#features)

[2. Bugfixes](#bugfixes)

[3. Changelog](#changelog)

{{/equals}}
{{h2 "Features"}}

{{#each features}}
{{#headerLevel 2}}
{{embedPartial (concat "../" (this) "/Templates/README.md") ../..}}
{{/headerLevel}}
{{/each}}
{{h2 "Credits"}}

Thanks a lot to:

- DarkCactus, fanta, Solakka, Gorilla, Phantasm, easternunit100, Dex and Project Zaero for helping me test during development

- knah, Spartan, js6pak, dak and ghorsington for answering my thousands of questions

- dak for the designing the logo
{{#equals release "thunderstore"}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/equals}}
