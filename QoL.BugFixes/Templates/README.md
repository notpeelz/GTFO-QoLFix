{{#notEquals project ""}}
{{h1 "QoL - Bug Fixes"}}

{{> license}}
{{else}}
{{#equals release "thunderstore"}}
{{h1 "[Bug Fixes](https://gtfo.thunderstore.io/package/notpeelz/QoL_BugFixes)"}}
{{else}}
{{h1 "Bug Fixes"}}
{{/equals}}
{{/notEquals}}

Fixes various game bugs:

- (**WeaponSwitchAnimationBugFix**) animation sequences (e.g. reload) would carry over when switching weapons

  {{embedVideo name='fixweaponanimations' height='240' url='https://i.imgur.com/atcrG69.mp4'}}

- (**SoundMuffleBugFix**) the scout/map muffle sound effect wouldn't get reset under certain circumstances

- (**VelocityBugFix**) the player would lose all horizontal velocity when jumping too quickly upon landing

- (**MeleeChargeBugFix**) melee charge would get cancelled if you jumped and charged on the same frame
{{#notEquals project ""}}

{{h2 "Changelog"}}

{{embedPartial "CHANGELOG.md" ..}}
{{/notEquals}}
