# Changelog

All notable changes to Plotboek Generator are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html): a major version needs a code change
or a data migration in a deployment.

Every release lists, under **Upgrade notes**, the new configuration keys, the overridable views
that changed and any migration a deployment has to run.

## [Unreleased]

## [1.0.0] - 2026-11-02

The first public release. Plotboek Generator grew up as the plot book tool of one Dutch LARP; this
release makes it a product any LARP can run on its own server.

### Added
- Plotboeken per event and year, an Evergreen plotboek, and encounter pages with NPC roles,
  props and loot, quest locations with clash warnings, time slots, icons, debrief questions,
  shared attachments, feedback points, a change history with restore and a "volledig klaar"
  workflow.
- Word export into a replaceable template, with a generated table of contents and page references;
  optional Google Drive upload and Gemini elevator-pitch suggestions.
- Sign-in through any number of OpenID Connect providers (`Authentication:Oidc:Providers`), roles
  from a provider's claim (`RoleClaim`, `Authentication:Roles:*`) and admins by e-mail address
  (`Authentication:Admins`).
- A reference host (`PlotboekGenerator.Host`) with a neutral look and template and an example LARP,
  and the extension points to write your own: `IDeploymentProfile`, `IDocumentTemplateProvider`,
  `IReferenceDataSeed`, `IRoleResolver`, and view overrides.
- Configuration for branding (`Branding:*`), terminology (`Terminology:*`), the theme cookie
  (`Theme:*`), a terrain map (`QuestLocaties:Kaart:*`) and the path base (`Hosting:PathBase`).
- A container image, `ghcr.io/gemberkoekje/plotboek-generator`, and a `docker-compose.yml` to run it
  with PostgreSQL.

### Upgrade notes
- A fresh database needs nothing: the schema is created and the example LARP seeded on the first
  start.
