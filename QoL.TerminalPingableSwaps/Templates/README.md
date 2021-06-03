{{#notEquals project ""}}
{{h1 "QoL - Terminal-pingable Swaps"}}

{{> license}}
{{else}}
{{#equals release "thunderstore"}}
{{h1 "[Terminal-pingable Swaps](https://gtfo.thunderstore.io/package/notpeelz/QoL_TerminalPingableSwaps)"}}
{{else}}
{{h1 "Terminal-pingable Swaps"}}
{{/equals}}
{{/notEquals}}

Relists swapped out items on terminals.

This lets you list/ping/query items after moving them.
{{#notEquals project ""}}
Known bugs: when pinging a swapped item, the ping icon will not show up for swapped items unless you're the host.
The ping audio will still play regardless of being host or client.

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
