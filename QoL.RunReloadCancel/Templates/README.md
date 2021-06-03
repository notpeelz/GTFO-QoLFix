{{#notEquals project ""}}
{{h1 "QoL - Run Reload-cancel"}}

{{> license}}
{{else}}
{{#equals release "thunderstore"}}
{{h1 "[Run Reload-cancel](https://gtfo.thunderstore.io/package/notpeelz/QoL_RunReloadCancel)"}}
{{else}}
{{h1 "Run Reload-cancel"}}
{{/equals}}
{{/notEquals}}

Lets you cancel the reload animation by sprinting rather than having to swap weapons.

{{embedVideo name='runreloadcancel' height='240' url='https://i.imgur.com/8XhBKdQ.mp4'}}
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
