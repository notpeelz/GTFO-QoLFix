{{#notEquals project ""}}
{{h1 "QoL - Intro Skip"}}

{{> license}}
{{else}}
{{h1 "Intro Skip"}}
{{/notEquals}}

Skips the game intro on startup. Gets you on the rundown screen within seconds of launching the game!

{{embedVideo name='introskip' height='240' url='https://i.imgur.com/4Z6XJe4.mp4'}}
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
