{{#notEquals project ""}}
{{h1 "QoL - Latency Info"}}

{{> license}}
{{else}}
{{#equals release "thunderstore"}}
{{h1 "[Latency Info](https://gtfo.thunderstore.io/package/notpeelz/QoL_LatencyInfo)"}}
{{else}}
{{h1 "Latency Info"}}
{{/equals}}
{{/notEquals}}

Displays network latency on your HUD.

{{embedImage height='120' name='latencyinfo_ping_hover'}} {{embedImage height='120' name='latencyinfo_ping_watermark'}}
{{#notEquals project ""}}
Known bugs: due to a bug with the way GTFO estimates network latency, the ping is only updated once upon joining a game.

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
