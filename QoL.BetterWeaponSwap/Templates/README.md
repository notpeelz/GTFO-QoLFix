{{#notEquals project ""}}
{{h1 "QoL - Better Weapon Swap"}}

{{> license}}
{{else}}
{{#equals release "thunderstore"}}
{{h1 "[Better Weapon Swap](https://gtfo.thunderstore.io/package/notpeelz/QoL_BetterWeaponSwap)"}}
{{else}}
{{h1 "Better Weapon Swap"}}
{{/equals}}
{{/notEquals}}

Changes the weapon swap order dynamically based on the drama state of the game (stealth, combat, etc.)

This prevents accidentally switching back to your primary weapon after, e.g., running out of glowsticks.

{{embedVideo name="betterweaponswap" height=240 url="https://i.imgur.com/Q4cZQff.mp4"}}
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
