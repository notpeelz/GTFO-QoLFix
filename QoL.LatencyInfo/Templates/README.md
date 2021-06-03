{{#notEquals project ""}}
{{h1 "QoL - Latency Info"}}

{{> license}}
{{else}}
{{h1 "Latency Info"}}
{{/notEquals}}

Displays network latency on your HUD.

{{embedImage height='120' name='latencyhud_ping_hover'}} {{embedImage height='120' name='latencyhud_ping_watermark'}}
{{#notEquals project ""}}
Known bugs: due to a bug with the way GTFO estimates network latency, the ping is only updated once upon joining a game.

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
