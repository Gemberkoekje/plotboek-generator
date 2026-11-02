# Plotboek Generator

A web application for LARP storytellers to plan an event's encounters together, one plotboek per
event and one page per encounter, and to export the whole plotboek as a formatted Word document.

Each LARP runs its own instance with its own configuration and database. There is no shared hosted
service.

> The user interface is in Dutch. English is planned; see `CHANGELOG.md`.

## Features

- **Plotboeken per event and year**, plus an *Evergreen* plotboek for encounters that can run at
  any time.
- **Encounter pages**: elevator pitch, plot summary, NPC roles with casting, costume and briefing
  notes, props and loot, location (with a curated list of quest locations and warnings when two
  encounters clash), start time and length, encounter types, icons, instructions for the crew that
  runs music, light and smoke, debrief questions, and attachments shared between encounters.
- **Working together**: roles (Admin, storyteller, read-only NPC, guest), a full change history
  with restore, protection against overwriting each other's changes, feedback points from the head
  storyteller, and a "volledig klaar" (completely ready) workflow.
- **Word export** into a template you can replace, with a generated table of contents and page
  references; optional upload to Google Drive.
- **Optional** elevator-pitch suggestions from Google Gemini.
- **Sign-in** through any OpenID Connect provider (Google, Microsoft Entra, Keycloak, Auth0, …).
  There are no local accounts.
- Light and dark theme.

## Quick start

You need Docker and one OpenID Connect client registered at a provider of your choice.

```bash
cp .env.example .env
```

Fill in `POSTGRES_PASSWORD` and the `OIDC_*` values in `.env`, then:

```bash
docker compose up -d
```

Open <http://localhost:8080>. Register the callback URL `http://localhost:8080/signin-oidc-<scheme>`
at your provider, where `<scheme>` is `OIDC_SCHEME` in lower case. Put your own e-mail address in
`ADMIN_EMAILS` to become Admin on your first sign-in.

The first start creates the database schema and seeds an example LARP, *De Verloren Kroon*: time
slots, events, quest locations, domains and names. Change them to your own on the Admin pages.

## Configuration

Settings use the usual ASP.NET Core sources: `appsettings.json`, environment variables (`__` for
`:`), or `dotnet user-secrets` during development.

| Key | Meaning |
|---|---|
| `ConnectionStrings:Postgres` | The PostgreSQL database (required) |
| `Authentication:Oidc:Providers:{n}:Scheme` | Name of the provider; also stored with every account that signs in with it, so do not change it later |
| `…:DisplayName`, `…:Authority`, `…:ClientId`, `…:ClientSecret` | The provider. `Authority` is the issuer URL; leave `ClientSecret` empty for a public (PKCE) client |
| `…:Scopes`, `…:CallbackPath`, `…:SignedOutCallbackPath` | Optional. Defaults: `openid profile email`, `/signin-oidc-<scheme>`, `/signout-callback-oidc-<scheme>` |
| `…:RoleClaim` | Optional: the claim that carries the user's roles at this provider |
| `Authentication:Roles:Admin`, `:User`, `:NPC` | The values of that claim that grant each role (comma separated) |
| `Authentication:Admins` | E-mail addresses that are always Admin (comma separated) |
| `Hosting:PathBase` | Serve below a path, e.g. `/plotboek` |
| `Branding:Name`, `:LogoUrl`, `:FaviconUrl`, `:ThemeColor`, `:FooterText` | How the app presents your LARP |
| `Theme:CookieName`, `Theme:SharedDomain` | Where the light/dark choice is kept; with a shared domain, sibling sites share it |
| `Terminology:*` | Your LARP's words for a few things: `Techniek`, `InstructieTechniek`, `EventBreed`, `Organisatie`, … (see `Terminologie.cs`) |
| `QuestLocaties:Kaart:Url`, `:Titel`, `:Bijschrift`, `:AltTekst` | A map of your terrain, shown next to the location field |
| `Gemini:ApiKey` | Optional: elevator-pitch suggestions |
| `Authentication:Google:ClientId`, `:ClientSecret`, `GoogleDrive:RefreshToken`, `GoogleDrive:SyncApiKey` | Optional: Google Drive upload (an OAuth client with Drive access, an Admin connects it on the Admin page) and the key that protects `POST /api/sync-google-drive` |
| `DataProtection:KeyPath`, `:CertificatePath`, `:CertificatePassword` | Keep sign-ins valid across restarts, encrypted at rest |

Users without a role see a welcome page until an Admin gives them one, or until the provider's role
claim does.

## Making it your own

Most of a LARP's own shape is configuration (above) or data an Admin edits. When you need more,
write your own host: a small ASP.NET Core project that references `PlotboekGenerator.WebApp` and
implements `IDeploymentProfile`.

```csharp
await PlotboekApp.RunAsync(args, new MyLarpProfile());
```

A profile supplies:

- the **Word template** (`IDocumentTemplateProvider`): a .docx whose content controls are tagged
  with the property names of `PlotboekTemplateData`. Start from
  `PlotboekGenerator.Host/Templates/PlotboekTemplate.docx`;
- the **reference data** a fresh database starts with (`IReferenceDataSeed`);
- optionally a different **role resolver** (`IRoleResolver`), extra sign-in handlers, Marten
  settings, or a one-off migration.

A host can also replace any page or partial by putting a file at the same path; the host's view
wins over the library's. `PlotboekGenerator.Host` is the reference host.

## Architecture

| Project | What it is |
|---|---|
| `PlotboekGenerator.core` | The domain: event-sourced aggregates (`Plotboek`, `PlotboekPagina`), events, validators |
| `PlotboekGenerator.application` | Commands, queries, handlers and the ports the domain needs |
| `PlotboekGenerator.infrastructure` | Marten (PostgreSQL) event and document store, the Word template engine, Google Drive, Gemini |
| `PlotboekGenerator.WebApp` | Razor Class Library: every page, script and style, and the shared composition `PlotboekApp` |
| `PlotboekGenerator.Host` | The reference host: neutral look, neutral template, the example LARP |
| `PlotboekGenerator.test` | NUnit specs: unit, page-model and HTTP integration specs over the reference host |

State is event-sourced with [Marten](https://martendb.io/); read models are stored documents. The
names of stored events and fields are frozen: a schema-guard spec fails when one changes, so an
existing database always keeps loading. See `CLAUDE.md` for the coding conventions.

## Development

```bash
docker compose up -d postgres
dotnet user-secrets --project PlotboekGenerator.Host set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=plotboek;Username=plotboek;Password=<yours>"
dotnet run --project PlotboekGenerator.Host
dotnet test
```

Also set one sign-in provider through user-secrets. See `CONTRIBUTING.md`.

## Upgrading

Releases follow [Semantic Versioning](https://semver.org/). A major version needs a change in your
host or a data migration. Every release's **Upgrade notes** in `CHANGELOG.md` list new settings,
changed overridable views and migrations.

## Licence

MIT, see `LICENSE`. Third-party components keep their own licences; see `THIRD-PARTY-NOTICES.md`.
