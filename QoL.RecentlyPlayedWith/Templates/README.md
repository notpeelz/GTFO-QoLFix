{{#notEquals project ""}}
{{h1 "QoL - Recently Played With"}}

{{> license}}
{{else}}
{{#equals release "thunderstore"}}
{{h1 "[Recently Played With](https://gtfo.thunderstore.io/package/notpeelz/QoL_RecentlyPlayedWith)"}}
{{else}}
{{h1 "Recently Played With"}}
{{/equals}}
{{/notEquals}}

Updates the Steam recent players list.

{{embedImage name='steamrecentplayers' height='180'}}
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
