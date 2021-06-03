{{#notEquals project ""}}
{{h1 "QoL - Better Movement"}}

{{> license}}
{{else}}
{{#equals release "thunderstore"}}
{{h1 "[Better Movement](https://gtfo.thunderstore.io/package/notpeelz/QoL_BetterMovement)"}}
{{else}}
{{h1 "Better Movement"}}
{{/equals}}
{{/notEquals}}

Improves the GTFO movement system. Currently only lets you charge/reload your weapons mid-air.

{{embedVideo name="bettermovement" height=240 url="https://i.imgur.com/yLqX835.mp4"}}
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
