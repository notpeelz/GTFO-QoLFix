{{#notEquals project ""}}
{{h1 "QoL - Better Lockers"}}

{{> license}}
{{else}}
{{h1 "Better Lockers"}}
{{/notEquals}}

Lets you put resources back in lockers.

{{embedVideo name="dropresources" height=240 url="https://i.imgur.com/SfCT6dD.mp4"}}

Also fixes resource pings showing up as "resource box".

{{embedImage name="fixlockerping" height=120}}
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
