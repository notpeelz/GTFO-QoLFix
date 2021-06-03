{{#notEquals project ""}}
{{h1 "QoL - Common"}}

{{> license}}
{{else}}
{{error "can't render QoL.Common README outside of project"}}
{{/notEquals}}

This project is a library that regroups code shared by the QoL plugins. It doesn't do anything on its own.
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
