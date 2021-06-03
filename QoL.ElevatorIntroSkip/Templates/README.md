{{#notEquals project ""}}
{{h1 "QoL - Elevator Intro Skip"}}

{{> license}}
{{else}}
{{h1 "Elevator Intro Skip"}}
{{/notEquals}}

Skips the intro that plays when dropping into a level.
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
