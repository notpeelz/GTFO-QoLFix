{{#notEquals project ""}}
{{h1 "QoL - Lobby Unready"}}

{{> license}}
{{else}}
{{h1 "Lobby Unready"}}
{{/notEquals}}

Lets you unready after readying up.

{{embedImage name='lobbyunready' height='60'}}
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
