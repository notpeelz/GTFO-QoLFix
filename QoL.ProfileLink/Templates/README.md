{{#notEquals project ""}}
{{h1 "QoL - Profile Link"}}

{{> license}}
{{else}}
{{h1 "Profile Link"}}
{{/notEquals}}

Lets you open the steam profile of your teammates by clicking on their name.

{{embedVideo name='profilelink' height='240' url='https://i.imgur.com/iMfZv7S.mp4'}}
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
