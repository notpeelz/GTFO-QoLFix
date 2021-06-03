{{#notEquals project ""}}
{{h1 "QoL - Resource Audio Cue"}}

{{> license}}
{{else}}
{{h1 "Resource Audio Cue"}}
{{/notEquals}}

Plays a sound when receiving ammo or health from a teammate.
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
