---
title: ANcpLua.NET.Sdk
description: MSBuild SDK with polyfills, analyzers, and opinionated defaults
---

[![NuGet](https://img.shields.io/nuget/v/ANcpLua.NET.Sdk?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.NET.Sdk/)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-7C3AED)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

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
- Auto-instrumentation for GenAI (OpenAI, Anthropic, Ollama) and Database calls
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

## [OTel] Attribute (Web SDK)

Mark properties with OpenTelemetry semantic convention names for automatic tag generation:

```csharp
using ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation;

public record ChatRequest(
    [OTel("gen_ai.request.model")] string Model,
    [OTel("gen_ai.request.max_tokens")] int? MaxTokens);

// Generated extension method:
activity.SetTagsFromChatRequest(request);
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

## Documentation

Full reference: https://ancplua.mintlify.app/sdk/overview

## Related

- [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers) - Custom Roslyn analyzers (auto-injected)
- [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) - Roslyn utilities for source
  generators

---

*Initial architecture inspired by [Meziantou.NET.Sdk](https://github.com/meziantou/Meziantou.NET.Sdk).*