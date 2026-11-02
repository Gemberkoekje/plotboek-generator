# Plotboek Generator — working on the code

A self-hostable web app for LARP storytellers: plotboeken (plot books) per event, an encounter per
page, a Word export. The code and the UI are in Dutch where they name the domain (`Plotboek`,
`PlotboekPagina`, `Aanpassen`, `Starttijd`, …); everything else is English. `README.md` explains
the product and its configuration; `CONTRIBUTING.md` the workflow.
## Solution Architecture & Layout

### Layered project structure
- Projects: `core`, `application`, `infrastructure`, `PlotboekGenerator.WebApp` (a Razor Class Library: every page, partial, script and style, plus the shared composition) and `PlotboekGenerator.Host` (the reference host). A deployment that needs more than configuration writes its own host on `PlotboekGenerator.WebApp`. The product knows no deployment: `ProductGenericity_specs` fails when a shared project names the deployment it was first written for.
- Layers, ProjectReferences strictly one-directional: `core` (domain) ← `application` ← `infrastructure` ← `WebApp` (RCL) ← host. Never let a lower layer reference a higher one.
- A host references ONLY `PlotboekGenerator.WebApp`, pulling the rest in transitively.
- `core` is the leaf (zero internal ProjectReferences): aggregates, value objects, ids, events, enums, validators.
- Each project gets an explicit `<RootNamespace>` and `<InternalsVisibleTo Include="PlotboekGenerator.test" />`. `PlotboekGenerator.test` references `core`+`infrastructure`+`Host` (not `application`). A deployment's own test project links the helpers from `PlotboekGenerator.test/TestHelpers` instead of copying them.
- Folder-by-responsibility per layer: application = `Commands`/`CommandHandlers`/`Queries`/`QueryHandlers`/`DTOs`/`Ports`; core = `Models`(+`Models/Ids`)/`Events`/`Enums`/`Validators`; infrastructure = `Adapters/Application`/`Services`/`MartenDb`/`Generator`.

### Ports & adapters
- Persistence/integration abstractions are `I`-prefixed interfaces in `application/Ports` (one per file), referencing no infra types; implemented as adapters under `infrastructure/Adapters/Application`.
- Prefix adapter names with the backing tech (`MartenPlotboekRepository...`, `EventStore...`); suffix is not regulated (`...Adapter`/`...ReadStore`/`...Service`/`...Writer`).
- Read DTOs are `sealed record`s in `application/DTOs`; a shared export/template data contract lives in `core` so both the port and the adapter type against it without either owning it.

### CQRS (hand-rolled)
- Empty marker interfaces `ICommand`/`ICommand<T>`/`IQuery<T>` — NO MediatR, no assembly scanning.
- Each command/query is an immutable `sealed record`; each handler is a `sealed class` with one `HandleAsync(command, CancellationToken)` returning Qowaiv `Result<T>`.
- Guard `HandleAsync` entry with `ArgumentNullException.ThrowIfNull(command)`; guard ctor deps with `?? throw new ArgumentNullException(nameof(dep))`; register every handler individually.

## Dependency Injection & Composition Root
- Register ALL services in the one shared composition, `PlotboekApp` in `PlotboekGenerator.WebApp` — a documented exception to "inline in Program.cs": the composition root lives one level down so every host runs the same product. A host's `Program.cs` is `await PlotboekApp.RunAsync(args, new XyzProfile());`. Do NOT create per-layer `AddApplication()`/`AddInfrastructure()` `IServiceCollection` extension methods.
- Everything a deployment decides goes through its `IDeploymentProfile` (`ConfigureServices`: template provider, reference-data seed, optional `IRoleResolver`; `ConfigureAuthentication`: providers beyond the OIDC list; `ConfigureMarten`: e.g. a `DocumentAlias` that keeps an older table; `ConfigureApplication`: one-off migrations). Everything else is configuration (`Authentication:Oidc:Providers`, `Hosting:PathBase`, `Branding:*`, `Theme:*`, `Terminology:*`, `QuestLocaties:Kaart:*`) or reference data.
- Use only `Singleton` and `Scoped`, never `Transient`. Stateless stores/builders/generators = Singleton; repositories, read-stores, handlers, Identity stores, per-request/audit services = Scoped.
- Double-registration idiom: register the concrete type, then register its interface as a factory delegating to it — `AddScoped<IFoo>(sp => sp.GetRequiredService<Foo>())` — so both resolve to the same instance.
- Services owning an `HttpClient` register via `AddHttpClient<T>()`, then bridge the interface with a factory registration.
- No keyed services; disambiguate multiple providers by distinct interface types.
- Declare authorization policies with `AddAuthorizationBuilder().AddPolicy(...)`; reference by string in `RequireAuthorization("...")`.
- Gate optional integrations behind a config-presence check (`if (!string.IsNullOrWhiteSpace(issuer)) { ...register... }`).
- Resolve Scoped services needed at startup via `using var scope = app.Services.CreateScope();` — never from the root provider.
- `PlotboekApp` wiring order: framework options → connection string/migration → `AddMarten` → Identity + RazorPages → authentication → authorization policies → domain/infra services → `Build()` → startup seeding in a scope → middleware → minimal-API endpoints.
- Integration tests override registrations in `ConfigureTestServices` via `RemoveAll<T>()` then re-add fakes at the SAME lifetime as production.

## Configuration & Secrets
- Read config via the colon-path indexer `Configuration["Section:Key"]` and `GetValue<bool>(...)`; do NOT use the `IOptions<T>`/`.Bind()` typed-options pattern for app config.
- Name the DB connection string `"Postgres"` (not `DefaultConnection`); read with `GetConnectionString("Postgres") ?? throw`.
- Validate required secrets at the read site and fail loudly (`ArgumentNullException.ThrowIfNull` / `InvalidOperationException` naming the missing key); feature-gate optional integrations on presence. Compare API keys with `CryptographicOperations.FixedTimeEquals`.
- Two committed appsettings files only: base `appsettings.json` (non-secret defaults, every secret slot an empty string) + `appsettings.Development.json`. No `appsettings.Production.json` — production values arrive via environment variables.
- Never commit a secret. Local non-docker dev uses `dotnet user-secrets` (`<UserSecretsId>`). Commit a `.env.example` template (all keys present, values blank); gitignore the real `.env` via `*.env`.
- In `docker-compose`, read SCREAMING_SNAKE names from `.env` and re-emit them as ASP.NET keys via the double-underscore convention (`ConnectionStrings__Postgres`, `Authentication__Google__ClientId`). Encode required vs optional with `${VAR:?message}` (fail-fast) / `${VAR:-default}`.
- Dockerfile is runtime-only: `FROM aspnet`, `COPY` published output, `ENTRYPOINT` — bake no secrets/ARG/ENV.

## Persistence (Marten / Postgres)
- Use Marten as BOTH event store (write model) and document store (read/reference model).
- Wrap Marten behind the app's own Marten-free ports so consumers never `using Marten`: `IDocumentStore`/`IDocumentSession` for documents, generic `IEventStore<TId>` for events. Only `Marten*` wrapper classes touch `Marten.*`.
- Instantiate the event store only for Qowaiv `Uuid`; cast domain ids to `Uuid` at the repository boundary.
- Event-source aggregates via Qowaiv.DomainModel: Save appends `Buffer.Uncommitted` at `Buffer.CommittedVersion` then `MarkAllAsCommitted`; Load replays via `EventBuffer.FromStorage` + `Aggregate.FromStorage<T,TId>`. Guard empty/not-found ids by returning an error `Result`, never throwing.
- Enforce optimistic concurrency with `ExpectedVersionOnServer`; catch `EventStreamUnexpectedMaxEventIdException` and convert to a domain `Result` with a user-facing message.
- Do NOT use Marten projections/async daemon. Hand-roll read models: after appending events, `Store` the aggregate's read-model document in a separate session.
- Register document identity fluently in the single `AddMarten(...)` block via `Schema.For<T>().Identity(x => x.Id)` — no `[Identity]` attributes.
- Set `AutoCreateSchemaObjects = JasperFx.AutoCreate.CreateOrUpdate` unconditionally.
- Session convention in raw-Marten code: `LightweightSession` for writes, `QuerySession` for read-only queries.
- Migrate renamed event types with raw Npgsql SQL at startup (a profile's `ConfigureApplication`), config-flag guarded and swallowing 42P01 for fresh DBs.

## Logging
- Inject `ILogger<T>` typed to the concrete containing class — never non-generic `ILogger` or a base type.
- Always use structured templates with named PascalCase placeholders (`"Plotboek {PlotboekId} created by {User}"`). Never interpolate/concatenate into a log call.
- Pass the exception as the first arg (`LogError(ex, "...")`); never interpolate `ex.Message` into the template.
- Levels: `Information` = successful state changes / audit actions; `Warning` = expected/recoverable failures (not-found, concurrency conflict, validation failure); `Error` = unexpected or external-system failures; `Debug` = trace.
- When logging a `Result`'s failures, join: `string.Join(", ", result.Messages.Select(m => m.Message))`.
- Model expected/business failures as `Result` flowing domain → handler `Task<Result<T>>` → ports; reserve exceptions for programmer errors/misconfiguration. The `core` domain does no logging — logging starts at the application layer.
- At the web/UI boundary translate an invalid `Result` into `LogWarning` + a user-facing state string + `return Page()`; wrap only genuinely-throwing collaborators (HTTP, doc export) in try/catch with `LogError`.
- Configure levels via appsettings `Logging:LogLevel` (Default `Information`, `Microsoft.AspNetCore` `Warning`); Development bumps auth namespaces to `Debug`.

## Web UI (Razor Pages)
- Front end is ASP.NET Core Razor Pages — NOT Blazor and NOT MVC. Every screen is `@page` + a `PageModel` code-behind. Reserve minimal-API `MapPost`/`MapGet` for a few AJAX/webhook targets only.
- The pages live in the `PlotboekGenerator.WebApp` RCL; their static files are served from `~/_content/PlotboekGenerator.WebApp/…`. A host overrides a view by putting a file at the same path (the app's view wins over the RCL's). Keep the list of overridden views short: every upstream change to one of them is a manual merge for that deployment.
- Labels a LARP names its own way come from `Terminologie` (core; `Terminologie.Huidig`, injected into views as `Terminologie`), never from literals. Stored names that predate this rule (`InstructiePentagon`, the JSON names `PLIN` and `KeycloakRole`) are frozen; change only what people read.
- PageModels: file-scoped namespace, `sealed`, dependencies via primary constructor (no fields).
- Split a large PageModel into `partial class` files by concern (`.Properties.cs`, `.Handlers.cs`, `.Loading.cs`, `.Authorization.cs`) rather than one monolith.
- Compose pages from feature-grouped shared partials under `Pages/Shared/<Feature>/_X.cshtml`; keep the page a thin orchestrator rendering `<partial ... model="Model" />`.
- Authentication UI is Identity's scaffolded default-UI pages under `Areas/Identity`; override only customized pages.
- UI chrome is Bootstrap 5 + Font Awesome; vendored libs under `wwwroot/lib`, page CSS under `wwwroot/css`. Standard `_Layout`/`_ViewImports`/`_ViewStart` with tag helpers enabled.
- The look is stock Bootstrap 5.3 (vendored in the RCL) with a few `--pb-*` colour tokens in `site.css`; a host with its own look points those tokens at its colours. Light/dark is `data-bs-theme` on `<html>`, from the theme cookie (`Theme:CookieName`). Write colours as `var(--bs-…)` / `var(--pb-…)` so one rule holds in both themes; never add a dark-only override. Use `text-bg-*` on coloured badges and headers, not `bg-*` with `text-dark`/`text-white`.

### Client JS
- Pure vanilla JS (`querySelectorAll`, `addEventListener`, `fetch`, `dataset`, `FormData`) — no jQuery in app code (it exists only as the validation-unobtrusive dependency).
- No bundler, no ES modules: small single-responsibility classic scripts in `wwwroot/js` sharing global scope, loaded in a fixed order in `@section Scripts` (the orchestrator/form-state script loads last).
- Reusable, server-value-independent behavior goes in external `wwwroot/js` files fed by `data-*` attributes; init needing Razor-injected values goes in inline `<script>` IIFEs in `@section Scripts`.
- Version static assets with `asp-append-version="true"`.
- Default client↔server channel is a classic form POST; reserve `fetch()` for the minimal-API endpoints. Even drag-drop reorder submits a hidden form.
- Handle unsaved changes client-side by snapshotting the form to a sorted `key=value` string at load and diffing on `beforeunload` (guarded by a global submit flag); handle server conflicts via a hidden version field + optimistic concurrency.
- Rich text uses Quill (from CDN); persist by syncing sanitized semantic HTML into a hidden input on submit — never read the mounted container's `innerHTML`.

## Build, Analyzers & Language
- Central Package Management: declare every version once in `Directory.Packages.props` (`<PackageVersion>`); reference packages in `.csproj` with NO `Version` attribute.
- Set `LangVersion` and shared props once in `Directory.Build.props`; repeat `<TargetFramework>` per csproj (single-target, no multi-targeting).
- Nullable is layer-specific: OFF (SDK default) in `core`/`application`/`infrastructure`; ON only in WebApp and test projects.
- In test projects add framework usings via MSBuild `<Using Include="..." />` (e.g. `NUnit.Framework`), not `GlobalUsings.cs`.
- Do NOT treat warnings as errors in this repo — rely on `EnableNETAnalyzers` + `EnforceCodeStyleInBuild` + `GenerateDocumentationFile`.
- Register the analyzer stack once in `Directory.Build.props` as `PackageReference` items with `PrivateAssets="all"`; add project-specific analyzers per csproj.
- Tune analyzer severities centrally in a single `is_global` `.globalconfig` (not per-project `.editorconfig`), annotating overrides with a `#` title + `[Justification: ...]`. Disable StyleCop `SA1xxx` layout/ordering/doc rules wholesale; keep SonarAnalyzer `Sxxxx` as the quality gate; silence any `CA` rule duplicating a Sonar/IDisposableAnalyzers rule.
- Enforce no `this.` prefix (`SA1101` = none, `SX1101` = warning).
- Single trimmed NuGet source (`<clear/>` then nuget.org); `RestorePackagesWithLockFile` always, `RestoreLockedMode` + `ContinuousIntegrationBuild` only on CI. Enable `NuGetAudit` with `NuGetAuditMode=All`.
- Default projects to non-packable/non-publishable library output; opt hosts into `exe`/publish explicitly.

## Testing (project specifics)
- Use NUnit with `[Test]`/`[TestCase]`/`[SetUp]`/`[OneTimeSetUp]`; add `NUnit.Framework` as a project-wide `<Using>`, not per-file.
- Assert with AwesomeAssertions fluent style (`.Should().Be(...)`, `.BeEquivalentTo(new {...})`) as a global using — not FluentAssertions or raw asserts; add because-reason strings.
- Name test classes `<Unit>_specs` (`public sealed class`, filename = class name); name methods as snake_case English sentences (no Should/Given/When scheme). Mirror production-layer folders.
- Split tests three ways: pure unit specs (no doubles), page-model integration specs (real repos + in-memory stores + NSubstitute at the edges), HTTP integration specs over `HttpClient`.
- The HTTP specs run over the reference host; `TestTemplate`/`TestReferenceData` point at its template and seed. `Terminologie.Huidig` is process-wide: assert labels through it, not with literals.
- Prefer hand-written in-memory fakes of ports (`TestDocumentStore`, `InMemoryEventStore`) over mocking; reserve NSubstitute for edge services/loggers. No shared base test class — share via a `TestHelpers/` folder. Name the object under test `_sut`.
- For HTTP integration subclass `WebApplicationFactory<Program>`, `RemoveAll<T>()` production ports and re-add fakes at the same lifetime, register a `TestAuthHandler` authenticating a seeded admin, stub anti-forgery, expose `Reset()`.
- Drive variations with inline `[TestCase(...)]` (not `TestCaseSource`/`Values`/`Theory`).
- For event-sourced code, add a reflection-based schema-guard spec that fingerprints every concrete event type against a frozen list, so events can only be added (as vN), never silently modified/removed.
