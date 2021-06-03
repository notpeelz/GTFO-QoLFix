{{#notEquals project ""}}
{{h1 "QoL - FPS Limiter"}}

{{> license}}
{{else}}
{{#equals release "thunderstore"}}
{{h1 "[FPS Limiter](https://gtfo.thunderstore.io/package/notpeelz/QoL_FPSLimiter)"}}
{{else}}
{{h1 "FPS Limiter"}}
{{/equals}}
{{/notEquals}}

Lowers your FPS when alt-tabbing to preserve system resources.

Note: FPS limiting doesn't work with v-sync.
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
