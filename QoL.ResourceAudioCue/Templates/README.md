{{#notEquals project ""}}
{{h1 "QoL - Resource Audio Cue"}}

{{> license}}
{{else}}
{{#equals release "thunderstore"}}
{{h1 "[Resource Audio Cue](https://gtfo.thunderstore.io/package/notpeelz/QoL_ResourceAudioCue)"}}
{{else}}
{{h1 "Resource Audio Cue"}}
{{/equals}}
{{/notEquals}}

Plays a sound when receiving ammo or health from a teammate.
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
