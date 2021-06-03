{{#notEquals project ""}}
{{h1 "QoL - Elevator Volume"}}

{{> license}}
{{else}}
{{h1 "Elevator Volume"}}
{{/notEquals}}

Lowers the game volume during the elevator sequence; no more alt-tabbing or screaming to your teammates!
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
