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
- **Working together**: roles (Admin, storyteller, Reader, read-only NPC, guest), a full change history
  with restore, protection against overwriting each other's changes, feedback points from the head
  storyteller, and a "volledig klaar" (completely ready) workflow.
- **Word export** into a template you can replace, with a generated table of contents and page
  references; optional upload to Google Drive.
- **Optional** elevator-pitch suggestions from Google Gemini.
- **Sign-in** through any OpenID Connect provider (Google, Microsoft Entra, Keycloak, Auth0, …).
  There are no local accounts.
- **Dutch and English**, picked by the browser or a switcher in the menu; the Word export uses the
  deployment's default language. See [docs/i18n.md](docs/i18n.md) to add or improve a translation.
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
| `Authentication:Roles:Admin`, `:User`, `:Reader`, `:NPC` | The values of that claim that grant each role (comma separated) |
| `Authentication:Admins` | E-mail addresses that are always Admin (comma separated) |
| `Authentication:DefaultRole` | Optional: the role of a sign-in nothing else grants one (`Guest`, `NPC`, `Reader` or `User`). Empty: a provider without a role claim leaves the role alone, and one whose claim grants nothing makes the user a Guest |
| `Hosting:PathBase` | Serve below a path, e.g. `/plotboek` |
| `Localization:DefaultCulture` | `nl` (the default) or `en`: the language a visitor gets when their browser asks for none of the supported ones, the language of the Word export, and of the example data on a first start |
| `Localization:SupportedCultures` | The languages a visitor may read, by their browser or the switcher in the menu (comma separated; default: only the default culture) |
| `Branding:Name`, `:LogoUrl`, `:FaviconUrl`, `:ThemeColor`, `:FooterText` | How the app presents your LARP |
| `Branding:ContactUrl` | Optional: where people reach you (`mailto:` or `https:`); the privacy page links to it |
| `Theme:CookieName`, `Theme:SharedDomain` | Where the light/dark choice is kept; with a shared domain, sibling sites share it |
| `Terminology:*` | Your LARP's words for a few things: `Techniek`, `InstructieTechniek`, `EventBreed`, `Organisatie`, … (see `Terminologie.cs`) |
| `QuestLocaties:Kaart:Url`, `:Titel`, `:Bijschrift`, `:AltTekst` | A map of your terrain, shown next to the location field |
| `Gemini:ApiKey` | Optional: elevator-pitch suggestions |
| `Authentication:Google:ClientId`, `:ClientSecret`, `GoogleDrive:RefreshToken`, `GoogleDrive:SyncApiKey` | Optional: Google Drive upload (an OAuth client with Drive access, an Admin connects it on the Admin page) and the key that protects `POST /api/sync-google-drive` |
| `DataProtection:KeyPath`, `:CertificatePath`, `:CertificatePassword` | Keep sign-ins valid across restarts, encrypted at rest |
| `Demo:Enabled`, `Demo:ResetApiKey` | Run as a public demo: see below |

Users without a role see a welcome page until an Admin gives them one, or until the provider's role
claim does. The roles, from the most to the least a user may do:

| Role | May |
|---|---|
| Admin | everything, the Admin page included |
| User | write plots: a storyteller |
| Reader | read every plotboek, finished or not, with its feedback; change nothing |
| NPC | read the plotboeken marked *volledig klaar*, without their feedback |
| Guest | nothing yet |

### Running a public demo

`Demo:Enabled=true` turns a deployment into a demo: every page says so, an empty database gets the
host's example plotboek (`IDemoContent`) on the first start, and `POST /api/demo/reset` (header
`X-Api-Key: <Demo:ResetApiKey>`) puts that example back. A reset deletes every plot and every
visitor's account; admins and everyone an Admin gave a role by hand keep theirs. Call it nightly
from a cron job, and set `Authentication:DefaultRole=Reader` so visitors can look around without
changing anything. `Branding:ContactUrl` puts a "ask for write access" link in the banner.

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
  `PlotboekGenerator.Host/Templates/PlotboekTemplate.docx` (Dutch) or `PlotboekTemplate.en.docx` (English);
- the **reference data** a fresh database starts with (`IReferenceDataSeed`);
- optionally an **example** to show when it runs as a demo (`IDemoContent`);
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
