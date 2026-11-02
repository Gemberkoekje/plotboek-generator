# Contributing

Thank you for helping. Bug reports, ideas and pull requests are all welcome; for a larger change,
open an issue first so we can agree on the shape before you build it.

## Getting started

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and Docker.
2. Start a database: `cp .env.example .env`, set `POSTGRES_PASSWORD`, then `docker compose up -d postgres`.
3. Give the reference host its connection string and one sign-in provider through user-secrets:

   ```bash
   dotnet user-secrets --project PlotboekGenerator.Host set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=plotboek;Username=plotboek;Password=<yours>"
   dotnet user-secrets --project PlotboekGenerator.Host set "Authentication:Oidc:Providers:0:Scheme" "Google"
   dotnet user-secrets --project PlotboekGenerator.Host set "Authentication:Oidc:Providers:0:Authority" "https://accounts.google.com"
   dotnet user-secrets --project PlotboekGenerator.Host set "Authentication:Oidc:Providers:0:ClientId" "<your client id>"
   dotnet user-secrets --project PlotboekGenerator.Host set "Authentication:Oidc:Providers:0:ClientSecret" "<your client secret>"
   dotnet user-secrets --project PlotboekGenerator.Host set "Authentication:Admins" "<your e-mail address>"
   ```

4. Run it: `dotnet run --project PlotboekGenerator.Host`.
5. Run the specs: `dotnet test`. They need no database.

## How the code is organised

`CLAUDE.md` holds the conventions in full; the short version:

- **Layers** reference strictly downwards: `core` ← `application` ← `infrastructure` ← `WebApp` ← host.
  `core` knows no framework.
- **Event sourcing** with [Qowaiv.DomainModel](https://github.com/Qowaiv/qowaiv-domainmodel) and
  [Marten](https://martendb.io/): each mutation of an aggregate lives in its own partial
  `Model.*.Aanpassen.cs` file, validates, then applies an event.
- **Stored names are frozen.** Event class names, their property names and the names of stored
  document properties never change: a running deployment's data must keep loading. A new shape is a
  new `…V2` event. `AuditTrailEvent_schema_guard_specs` fails when an event changes.
- **Expected failures are `Result`s** (Qowaiv), not exceptions. Check `IsValid` before reading
  `Value`.
- **Pages are Razor Pages** with vanilla JavaScript; no bundler.
- **The product knows no deployment.** A LARP's own words go through `Terminologie` and
  configuration; `ProductGenericity_specs` keeps one LARP's names out of the shared projects.
- **Central package management**: versions live in `Directory.Packages.props`; lock files are
  committed, so run `dotnet restore` after changing a package.

## Adding a field to an encounter page

1. Add the property and its `When(...)` handler in a new partial: `PlotboekPagina.{Field}.Aanpassen.cs`.
2. Add the event `Events/PlotboekPagina{Field}Aangepast.cs`, deriving from `AuditTrailEvent`, and
   add it to the frozen list in `AuditTrailEvent_schema_guard_specs`.
3. Add the field to `PlotboekPagina.Data.Aanpassen.cs` and `PlotboekPagina.Data.Export.cs`.
4. Add the form control to the right partial under `Pages/Shared/Plotboek/`.
5. Bind it in `Plotboek.Pagina.cshtml.cs`.
6. Write the specs.

## Pull requests

- Keep a pull request to one change, with specs, and with an entry under **Unreleased** in
  `CHANGELOG.md`. Note anything a deployment must do under **Upgrade notes**.
- Name specs `<Unit>_specs` with snake_case sentences as method names; assert with AwesomeAssertions.
- CI builds and runs the specs on every pull request; it must be green.

By contributing you agree that your contribution is licensed under the MIT licence of this project.
