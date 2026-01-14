---
title: ANcpLua.NET.Sdk
description: MSBuild SDK with polyfills, analyzers, and opinionated defaults
---

[![NuGet](https://img.shields.io/nuget/v/ANcpLua.NET.Sdk?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.NET.Sdk/)
[![GitHub](https://img.shields.io/badge/GitHub-Source-181717?logo=github)](https://github.com/ANcpLua/ANcpLua.NET.Sdk)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-7C3AED)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/ANcpLua/ANcpLua.NET.Sdk/blob/main/LICENSE)

# ANcpLua.NET.Sdk

Opinionated MSBuild SDK for .NET projects.

## Quick Start

Add to `global.json` (check [NuGet](https://www.nuget.org/packages/ANcpLua.NET.Sdk/) for latest version):

```json
{
  "msbuild-sdks": {
    "ANcpLua.NET.Sdk": "1.3.31"
  }
}
```

> **Tip:** Use [Dependabot](https://docs.github.com/en/code-security/dependabot)
> or [Renovate](https://docs.renovatebot.com/) to auto-update SDK versions.

```xml
<!-- Library/Console/Worker -->
<Project Sdk="ANcpLua.NET.Sdk"></Project>

<!-- Web API -->
<Project Sdk="ANcpLua.NET.Sdk.Web"></Project>
```

## What You Get

**Base SDK:**

- Banned API enforcement (`DateTime.Now` → `TimeProvider`, etc.)
- Custom analyzers via [ANcpLua.Analyzers](https://nuget.org/packages/ANcpLua.Analyzers)
- `Throw.IfNull()` guard clauses
- CLAUDE.md generation for AI assistants

**Web SDK adds:**

- OpenTelemetry (logging, metrics, tracing with OTLP)
- Health endpoints (`/health`, `/alive`)
- HTTP resilience (retries, circuit breakers)
- DevLogs (browser console → server logs)

## Opt-in Features

```xml
<PropertyGroup>
  <!-- Roslyn source generator utilities -->
  <InjectSourceGenHelpers>true</InjectSourceGenHelpers>

  <!-- FakeLogger test extensions -->
  <InjectFakeLogger>true</InjectFakeLogger>

  <!-- Legacy TFM polyfills -->
  <InjectLockPolyfill>true</InjectLockPolyfill>
  <InjectTimeProviderPolyfill>true</InjectTimeProviderPolyfill>
</PropertyGroup>
```

## Opt-out

```xml
<PropertyGroup>
  <!-- Disable service defaults for Web SDK -->
  <AutoRegisterServiceDefaults>false</AutoRegisterServiceDefaults>

  <!-- Disable specific features -->
  <GenerateClaudeMd>false</GenerateClaudeMd>
  <InjectSharedThrow>false</InjectSharedThrow>
  <IncludeDefaultBannedSymbols>false</IncludeDefaultBannedSymbols>
</PropertyGroup>
```

## DevLogs (Web SDK)

Captures browser console and sends to server logs. Add to your HTML:

```html
<script src="/dev-logs.js"></script>
```

All frontend logs appear in server output with `[BROWSER]` prefix.

## Requirements

Central Package Management enabled in `Directory.Packages.props`:

```xml
<PropertyGroup>
  <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
</PropertyGroup>
```

---

*Initial architecture inspired by [Meziantou.NET.Sdk](https://github.com/meziantou/Meziantou.NET.Sdk).*