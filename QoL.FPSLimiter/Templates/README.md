{{#notEquals project ""}}
{{h1 "QoL - FPS Limiter"}}

{{> license}}
{{else}}
{{h1 "FPS Limiter"}}
{{/notEquals}}

Lowers your FPS when alt-tabbing to preserve system resources.

Note: FPS limiting doesn't work with v-sync.
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
