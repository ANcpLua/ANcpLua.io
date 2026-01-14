---
title: SDK Variants
---

# SDK Variants

ANcpLua.NET.Sdk provides three SDK variants for different project types.

| Variant | Description |
|---------|-------------|
| `ANcpLua.NET.Sdk` | Base SDK for libraries, console apps, and workers. Includes analyzers, banned APIs, polyfills, and CLAUDE.md generation. |
| `ANcpLua.NET.Sdk.Web` | Web SDK extending `Microsoft.NET.Sdk.Web`. Adds OpenTelemetry, health endpoints, HTTP resilience, and DevLogs. |
| `ANcpLua.NET.Sdk.Test` | Test SDK with xUnit v3 MTP auto-injection, AwesomeAssertions, and integration test fixtures. |

## Usage

### Base SDK (Libraries/Console)

```xml
<Project Sdk="ANcpLua.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

### Web SDK (ASP.NET Core)

```xml
<Project Sdk="ANcpLua.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

### Test SDK

```xml
<Project Sdk="ANcpLua.NET.Sdk.Test">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

## What Each Variant Provides

### ANcpLua.NET.Sdk (Base)

- **Analyzers**: ANcpLua.Analyzers, Meziantou.Analyzer, BannedApiAnalyzers
- **Banned APIs**: See [Banned APIs](banned-apis.md) for the full list
- **Guard Clauses**: `Throw.IfNull()` (opt-out: `InjectSharedThrow=false`)
- **AI Support**: CLAUDE.md generation (opt-out: `GenerateClaudeMd=false`)
- **Build Settings**: Nullable, ImplicitUsings, Deterministic, SourceLink

### ANcpLua.NET.Sdk.Web (adds)

- **OpenTelemetry**: Logging, metrics, tracing with OTLP export
- **Health Endpoints**: `/health` and `/alive`
- **HTTP Resilience**: Retries and circuit breakers
- **DevLogs**: Browser console to server logs

### ANcpLua.NET.Sdk.Test (adds)

- **xUnit v3 MTP**: Microsoft Testing Platform runner
- **AwesomeAssertions**: Fluent assertion library
- **Integration Testing**: WebApplicationFactory base classes
- **Analyzer Testing**: Microsoft.CodeAnalysis.Testing packages
