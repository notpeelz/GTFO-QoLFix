{{#notEquals project ""}}
{{h1 "QoL - Better Lockers"}}

{{> license}}
{{else}}
{{#equals release "thunderstore"}}
{{h1 "[Better Lockers](https://gtfo.thunderstore.io/package/notpeelz/QoL_BetterLockers)"}}
{{else}}
{{h1 "Better Lockers"}}
{{/equals}}
{{/notEquals}}

Lets you put resources back in lockers.

{{embedVideo name="dropresources" height=240 url="https://i.imgur.com/SfCT6dD.mp4"}}

Also fixes resource pings showing up as "resource box".

{{embedImage name="fixlockerping" height=120}}
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
