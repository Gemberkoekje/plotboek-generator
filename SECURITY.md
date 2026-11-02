# Security

## Reporting a vulnerability

Please do not open a public issue for a security problem. Report it privately through GitHub:
**Security** tab → **Report a vulnerability**.

Include what an attacker can do, the steps to reproduce it, and the version you tested.

## Supported versions

Fixes go into the latest minor release. Deployments are expected to follow it; the upgrade notes in
`CHANGELOG.md` say what a release needs.

## What a deployment should do

- Serve it over HTTPS only, behind a reverse proxy that sets `X-Forwarded-Proto`.
- Set `DataProtection:KeyPath` to a volume, and `DataProtection:CertificatePath` to encrypt the
  keys there: whoever can read unencrypted keys can forge a sign-in cookie for any user.
- Keep `.env` and any client secrets out of version control.
- Leave `GoogleDrive:SyncApiKey` empty unless you call the sync endpoint; without it the endpoint
  refuses every request.
