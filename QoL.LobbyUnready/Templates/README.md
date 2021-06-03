{{#notEquals project ""}}
{{h1 "QoL - Lobby Unready"}}

{{> license}}
{{else}}
{{#equals release "thunderstore"}}
{{h1 "[Lobby Unready](https://gtfo.thunderstore.io/package/notpeelz/QoL_LobbyUnready)"}}
{{else}}
{{h1 "Lobby Unready"}}
{{/equals}}
{{/notEquals}}

Lets you unready after readying up.

{{embedImage name='lobbyunready' height='60'}}
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
