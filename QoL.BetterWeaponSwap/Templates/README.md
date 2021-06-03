{{#notEquals project ""}}
{{h1 "QoL - Better Weapon Swap"}}

{{> license}}
{{else}}
{{h1 "Better Weapon Swap"}}
{{/notEquals}}

Changes the weapon swap order dynamically based on the drama state of the game (stealth, combat, etc.)

This prevents accidentally switching back to your primary weapon after, e.g., running out of glowsticks.

{{embedVideo name="betterweaponswap" height=240 url="https://i.imgur.com/Q4cZQff.mp4"}}
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
