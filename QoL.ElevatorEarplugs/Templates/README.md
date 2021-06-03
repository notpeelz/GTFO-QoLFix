{{#notEquals project ""}}
{{h1 "QoL - Elevator Earplugs"}}

{{> license}}
{{else}}
{{#equals release "thunderstore"}}
{{h1 "[Elevator Earplugs](https://gtfo.thunderstore.io/package/notpeelz/QoL_ElevatorEarplugs)"}}
{{else}}
{{h1 "Elevator Earplugs"}}
{{/equals}}
{{/notEquals}}

Lowers the game volume during the elevator sequence; no more alt-tabbing or screaming to your teammates!
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
