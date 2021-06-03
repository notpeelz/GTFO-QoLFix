{{#notEquals project ""}}
{{h1 "QoL - Recently Played With"}}

{{> license}}
{{else}}
{{h1 "Recently Played With"}}
{{/notEquals}}

Updates the Steam recent players list.

{{embedImage name='steamrecentplayers' height='180'}}
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
