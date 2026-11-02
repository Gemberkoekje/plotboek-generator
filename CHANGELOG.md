# Changelog

All notable changes to Plotboek Generator are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html): a major version needs a code change
or a data migration in a deployment.

Every release lists, under **Upgrade notes**, the new configuration keys, the overridable views
that changed and any migration a deployment has to run.

## [Unreleased]

## [1.1.0] - 2026-11-02

### Added
- **An English UI.** Every page, message, script text, change-history entry and export text comes from
  resource files, in Dutch and English. `Localization:DefaultCulture` and
  `Localization:SupportedCultures` decide which languages a deployment speaks; a visitor gets theirs
  from the switcher in the menu (a cookie) or their browser. Dates and numbers keep their format.
- The Word export uses the deployment's default language, whoever exports it. The reference host
  ships an English template and an English example LARP and demo plot book, used when the default
  language is English.
- A **Reader** role: reads every plotboek, finished or not, with its feedback, and changes nothing.
  It ranks between User and NPC (`Authentication:Roles:Reader`).
- `Authentication:DefaultRole`: the role a sign-in gets when nothing else grants one, for example
  Reader for everyone who signs in with Google. Admin is refused.
- **Demo mode** (`Demo:Enabled`): a banner on every page, the host's example plotboek in an empty
  database (`IDemoContent`, which the reference host implements), and `POST /api/demo/reset`
  (`Demo:ResetApiKey`) that puts the example back and removes visitors' accounts.
- A privacy page that says what the app keeps, naming only the integrations a deployment switched
  on, and a link to it in the footer. `Branding:ContactUrl` says where to ask about it.

### Changed
- Readers and NPCs get the Plotboek link in the menu instead of Home.
- The login, logout, profile and error pages, which were in English, follow the language too. The
  error page no longer shows the framework's developer instructions.
- `Terminologie`: a word a deployment does not configure is the neutral word in the reader's language.

### Upgrade notes
- New configuration keys, all optional: `Localization:DefaultCulture`, `Localization:SupportedCultures`,
  `Authentication:DefaultRole`, `Authentication:Roles:Reader`, `Branding:ContactUrl`, `Demo:Enabled`,
  `Demo:ResetApiKey`. Leaving them out keeps 1.0.0's behaviour: Dutch only. `docker-compose.yml` now
  passes `DEFAULT_CULTURE` (default `nl`) and `SUPPORTED_CULTURES` (default `nl,en`).
- Overridable views that changed: every view now reads its text from the resources. Of the usual
  overrides, `Pages/Shared/_Layout.cshtml` gained `lang` from the request, the language switcher, the
  demo banner, the menu change and the footer link, and `Areas/Identity/Pages/Account/Login.cshtml`
  its texts. A host that overrides the layout keeps its own language and gets none of these unless it
  adds them. The pages' scripts read their texts from `Pages/Shared/_ScriptTekst.cshtml`, which the
  plotboek page renders itself, so an overridden layout needs nothing for that.
- A host with its own `IDeploymentProfile` that wants English example data or an English template
  picks them itself; `DefaultProfile` shows how.
- No data migration: roles are stored as strings, so Reader needs nothing in the database.

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
