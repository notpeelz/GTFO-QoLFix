{{#notEquals project ""}}
{{h1 "QoL - Elevator Intro Skip"}}

{{> license}}
{{else}}
{{#equals release "thunderstore"}}
{{h1 "[Elevator Intro Skip](https://gtfo.thunderstore.io/package/notpeelz/QoL_ElevatorIntroSkip)"}}
{{else}}
{{h1 "Elevator Intro Skip"}}
{{/equals}}
{{/notEquals}}

Skips the intro that plays when dropping into a level.
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
