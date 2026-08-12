# Release notes

This page summarizes what shipped and what is staged in the current source. For
complete change history, see [`CHANGELOG.md`](../../CHANGELOG.md). For upgrade
steps, use the [Migration guide](migration.md).

## 10.8.0 (July 2026) — source release

> **Publication blocker:** all projects build for `net10.0`, but production
> `.csproj` files still report `9.1.21` and the public `Mvp24Hours.Core` feed has
> no 10.8.0 package. Do not request `--version 10.8.0` until package metadata is
> changed and publication is confirmed.

### Platform and compatibility

- All production and test projects target `net10.0`; there is no remaining
  `net9.0` multi-target.
- The shared build defaults to `LangVersion=latest` (C# 14 with the .NET 10
  SDK), Nullable reference types, implicit usings, and current analysis. The
  MongoDB production project currently overrides the language version to C# 12.
- Central Package Management owns dependency versions in
  `src/Directory.Packages.props`.
- Strict Release builds use warnings as errors. The only declared residual is
  the intentional `NU1510` security pin.

### Breaking changes

- Nullable annotations now match runtime behavior. Consumers with Nullable
  enabled may receive new diagnostics.
- `SetPropertyCall.Property` and `EncryptionOptions.Key` are now `required`.
- A configured `SmtpEmailOptions.ServerCertificateValidationCallback` is
  retained for source compatibility but ignored. `SmtpClient` now uses the
  operating system trust store; a warning is logged when the callback is set.

Follow the complete [9.1.x → 10.8.0 migration](migration.md?id=_91x-1000).

### Security and internal substitutions

- `System.Security.Cryptography.Xml` is pinned to `10.0.10` to address
  [GHSA-37gx-xxp4-5rgx](https://github.com/advisories/GHSA-37gx-xxp4-5rgx)
  and [GHSA-w3x6-4m5h-cxqf](https://github.com/advisories/GHSA-w3x6-4m5h-cxqf).
  The recorded transitive vulnerability audit reports zero vulnerable projects.
- SQL Server distributed locking uses `Microsoft.Data.SqlClient`.
- AWS Secrets Manager uses the AWS SDK v4 default credentials identity resolver.
- Certificate loading uses `X509CertificateLoader`.
- Field-encryption key derivation uses static `Rfc2898DeriveBytes.Pbkdf2`; tests
  verified byte-for-byte compatibility.
- `ResilientCacheProvider` uses the Polly v8-based
  `NativeResiliencePipeline` instead of the obsolete internal circuit breaker.

### Fixes

- `LockHandleBase.Dispose` and `DisposeAsync` release the lock before marking
  the handle disposed, preventing a lock from remaining held until expiration.

### Quality gates

- Local and CI Release builds use warnings as errors. A Release
  `TreatWarningsAsErrors=true` build of the solution reports zero errors and
  zero warnings, with only the intentional `NU1510` residual allowed.
- CI instruments **19** test projects in `src/Mvp24Hours.slnx` on the .NET 10 SDK
  with split **unit** (`Category!=Integration`) and **integration** (`Category=Integration`)
  jobs (integration requires Docker). Merged coverage enforces a **55%** line floor;
  product target remains **95%**.
- Versioned baseline: **59.4%** consolidated line coverage (74,605 coverable lines) —
  see [`docs/en-us/testing/coverage-baseline.md`](en-us/testing/coverage-baseline.md).
  Reproduce with `./scripts/run-ci-local.ps1 -SkipSamples`.
- Historical v10 expansion snapshot (**37.7%**, 4,492 passed / 6 skipped) remains in
  `CHANGELOG.md`; evidence files under `tasks/` are not committed.

## 9.1.210 (January 2026)

- Corrected NuGet package versions across production projects.
- The changelog says three propagation handlers and `TypedHttpClient.cs` were
  removed. That statement does not describe the current tree: those files,
  their public types, and propagation-handler tests are present. Treat them as
  supported source APIs unless a future release explicitly removes them.

## 9.1.200 (January 2026)

This .NET 9 release introduced the broad feature baseline retained by 10.8.0:

- the Mvp24Hours Mediator and CQRS stack, including behaviors, events,
  inbox/outbox, event sourcing, sagas, and scheduling;
- OpenTelemetry-based logging, tracing, and metrics;
- advanced EF Core, MongoDB, RabbitMQ, Pipeline, caching, CronJob, and Web API
  capabilities;
- native resilience, HybridCache, TimeProvider, Channels, rate limiting,
  ProblemDetails, TypedResults, Native OpenAPI, keyed services, and Aspire.

### Base infrastructure

The source also includes HTTP clients, distributed locking, file storage,
email, SMS, background-job, secret-provider, and health-check abstractions.

For the native-API transition, use the
[.NET 9+ modernization guide](modernization/migration-guide.md). For CQRS
registration and request types, use the [CQRS guide](cqrs/getting-started.md).

## Historical releases

- **8.3.261 (2024):** introduced the CronJob module.
- **8.2.102 (2024):** added Minimal API route binding and conversion helpers.
- **8.2.101 (2024):** moved the library to .NET 8.
- **4.x and earlier (2020–2023):** established asynchronous mapping, entity,
  middleware, resilience, validation, repository, messaging, and observability
  foundations.

See [`CHANGELOG.md`](../../CHANGELOG.md) for the detailed historical record.
