# Vendored: UglyToad/PdfPig

Source: https://github.com/UglyToad/PdfPig
Tag: `v0.1.15` (commit matching that GitHub release, verified against the
project's own release history at api.github.com/repos/UglyToad/PdfPig/releases)
License: Apache License 2.0 (see `LICENSE`, `NOTICES.txt`)

## Why vendored from source instead of via NuGet

The `UglyToad.PdfPig` package on nuget.org is not safe to consume as a normal
`PackageReference`. Its version listing
(`api.nuget.org/v3-flatcontainer/uglytoad.pdfpig/index.json`) shows only two
versions: an alpha prerelease, and a `1.7.0-custom-5` release published
2023-06-05 with a literal unfilled `"description": "Package Description"`
placeholder, an empty repository field, and a non-standard `-custom-N`
prerelease suffix that appears nowhere in the project's real, actively
maintained release history (clean `vX.Y.Z` tags via automated GitHub Actions
releases, currently at v0.1.15). A `1.x` version sitting above a real `0.1.x`
series, with placeholder metadata, is the signature of NuGet version-number
squatting: publishing an artificially high version under an existing package
ID so default "highest version wins" resolution picks the rogue upload
instead of the legitimate one. None of the real 0.1.x releases (up to
v0.1.15) appear in NuGet's listing for this package ID at all.

The project's actual GitHub source is legitimate, actively maintained, and
Apache-2.0 licensed - only its NuGet distribution channel is compromised. So
the source for the four library projects (plus their shared build files) is
vendored here directly from the tagged release, unmodified.

## What's included

Only the runtime library and its own project-reference dependencies - not the
test project or its (large) PDF fixture corpus:

- `src/UglyToad.PdfPig` - the public API
- `src/UglyToad.PdfPig.Core`
- `src/UglyToad.PdfPig.Fonts`
- `src/UglyToad.PdfPig.Tokenization`
- `src/UglyToad.PdfPig.Tokens`
- `src/Directory.Build.targets`, `src/.editorconfig`, `src/pdfpig.snk` - shared
  upstream build/signing config those projects expect to find as siblings

`src/Directory.Build.props` and `src/Directory.Packages.props` were added
here (not present upstream) purely to isolate this subtree from this repo's
own analyzer stack and Central Package Management - see the comments in each
file. Nothing else has been changed from the tagged release.

These five projects are deliberately **not** listed in the top-level
`PlotboekGenerator.slnx`: `PlotboekGenerator.infrastructure`'s
`ProjectReference` to `UglyToad.PdfPig` already resolves them correctly (at
its own `net9.0`) during a normal build. Adding them as flat top-level
solution entries instead makes MSBuild build every upstream-declared
`TargetFramework` (including `net462`/`net471`) for each of the five projects
on any whole-solution build or restore - a ~7x build blow-up that also pulls
in .NET Framework reference-assembly NuGet packages the shipped product never
needs. To build or debug a vendored project directly, target its `.csproj`
individually, e.g. `dotnet build ThirdParty/PdfPig/src/UglyToad.PdfPig/UglyToad.PdfPig.csproj`.

## Updating

Re-run the same selective extraction against a newer tag, replace the five
project directories and the three shared files above, keep the two added
isolation `.props` files, and rebuild. Re-verify the new tag's provenance the
same way this one was verified (real GitHub release, not just trusting
nuget.org) before updating.
