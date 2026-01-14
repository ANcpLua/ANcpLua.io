---
title: MSBuild Properties Reference
---

# MSBuild Properties Reference

Complete reference of all MSBuild properties available in ANcpLua.NET.Sdk.

## SDK Defaults

These properties are set automatically by the SDK but can be overridden.

| Property | Default | Description |
|----------|---------|-------------|
| `LangVersion` | `latest` | C# language version |
| `Nullable` | `enable` | Nullable reference types |
| `ImplicitUsings` | `enable` | Implicit global usings |
| `Deterministic` | `true` | Reproducible builds |
| `EnableNETAnalyzers` | `true` | .NET code analyzers |
| `AnalysisLevel` | `latest-all` | Analysis rule severity |
| `GenerateDocumentationFile` | `true` | Generate XML docs |
| `EnableSourceLink` | `true` | Source Link support |
| `PublishRepositoryUrl` | `true` | Include repo URL in NuGet |
| `EmbedUntrackedSources` | `true` | Embed sources in PDB |
| `EnablePackageValidation` | `true` | NuGet package validation |
| `ManagePackageVersionsCentrally` | `true` | Central Package Management |
| `TreatWarningsAsErrors` | `true` (CI/Release) | Warnings as errors |
| `EnforceCodeStyleInBuild` | `true` (CI/Release) | Code style enforcement |

## Feature Toggles

### Core Features

| Property | Default | Description |
|----------|---------|-------------|
| `InjectSharedThrow` | `true` | Inject `Throw.IfNull()` guard clauses |
| `GenerateClaudeMd` | `true` | Generate CLAUDE.md for AI assistants |
| `IncludeDefaultBannedSymbols` | `true` | Include default banned APIs |
| `AutoRegisterServiceDefaults` | `true` | Auto-register service defaults (Web SDK) |

### Polyfills (Opt-in)

These inject source files for older target frameworks.

| Property | Default | Description |
|----------|---------|-------------|
| `InjectTrimAttributesOnLegacy` | `false` | Trimmer/AOT attributes (< net5.0) |
| `InjectNullabilityAttributesOnLegacy` | `false` | Nullable reference type attrs (< netcoreapp3.1) |
| `InjectIsExternalInitOnLegacy` | `false` | Records support (< net5.0) |
| `InjectRequiredMemberOnLegacy` | `false` | `required` keyword support (< net7.0) |
| `InjectCallerAttributesOnLegacy` | `false` | CallerArgumentExpression (< net6.0) |
| `InjectUnreachableExceptionOnLegacy` | `false` | UnreachableException (< net7.0) |
| `InjectExperimentalAttributeOnLegacy` | `false` | ExperimentalAttribute (< net8.0) |
| `InjectIndexRangeOnLegacy` | `false` | Index/Range structs (< net5.0) |
| `InjectStackTraceHiddenOnLegacy` | `false` | StackTraceHidden attribute |
| `InjectLockPolyfill` | `false` | System.Threading.Lock polyfill |
| `InjectTimeProviderPolyfill` | `false` | TimeProvider polyfill |
| `InjectStringExtensionsPolyfill` | `false` | String.Contains/Replace with StringComparison |

### Extensions (Opt-in)

| Property | Default | Description |
|----------|---------|-------------|
| `InjectSourceGenHelpers` | `false` | EquatableArray, DiagnosticInfo, LocationInfo |
| `InjectCommonComparers` | `false` | StringOrdinalComparer |
| `InjectFakeLogger` | `false` | FakeLogger test extensions |

### Bundles (Opt-in)

| Property | Default | Description |
|----------|---------|-------------|
| `InjectAllPolyfillsOnLegacy` | `false` | All polyfills based on TFM |
| `InjectAllExtensions` | `false` | All extension utilities |

## Test Project Properties

These are auto-detected or can be set explicitly.

| Property | Auto-Detected | Description |
|----------|---------------|-------------|
| `IsTestProject` | `.Tests`/`.Test` suffix or `tests` folder | Marks as test project |
| `IsIntegrationTestProject` | `Integration`/`E2E` in path | Integration test project |
| `IsAnalyzerTestProject` | `Analyzer` in name | Analyzer test project |
| `SkipXunitInjection` | `false` | Disable xUnit auto-injection |
| `InjectIntegrationTestFixtures` | `true` | Inject integration test base classes |

### Test Framework Defaults

When `IsTestProject=true`:

| Property | Value | Description |
|----------|-------|-------------|
| `OutputType` | `Exe` | Executable for MTP |
| `TestingPlatformDotnetTestSupport` | `true` | MTP support |
| `UseMicrosoftTestingPlatformRunner` | `true` | Use MTP runner |
| `UseMicrosoftTestingPlatform` | `true` | Enable MTP |

## NuGet Audit Properties

| Property | Default | Description |
|----------|---------|-------------|
| `NuGetAudit` | `true` | Enable vulnerability scanning |
| `NuGetAuditMode` | `all` | Audit mode (all/direct) |
| `NuGetAuditLevel` | `low` | Minimum severity level |

## Build Optimization

| Property | Default | Description |
|----------|---------|-------------|
| `AccelerateBuildsInVisualStudio` | `true` | VS build acceleration |
| `ReportAnalyzer` | `true` | Report analyzer performance |
| `Features` | `strict` | Enable strict mode |

## Usage Examples

### Opt-in to Polyfills

```xml
<PropertyGroup>
  <InjectLockPolyfill>true</InjectLockPolyfill>
  <InjectTimeProviderPolyfill>true</InjectTimeProviderPolyfill>
</PropertyGroup>
```

### Opt-out of Features

```xml
<PropertyGroup>
  <GenerateClaudeMd>false</GenerateClaudeMd>
  <InjectSharedThrow>false</InjectSharedThrow>
  <IncludeDefaultBannedSymbols>false</IncludeDefaultBannedSymbols>
</PropertyGroup>
```

### Web SDK Service Defaults

```xml
<PropertyGroup>
  <!-- Disable automatic service defaults registration -->
  <AutoRegisterServiceDefaults>false</AutoRegisterServiceDefaults>
</PropertyGroup>
```

### Test Project Configuration

```xml
<PropertyGroup>
  <!-- Use TUnit/NUnit/MSTest instead of xUnit -->
  <SkipXunitInjection>true</SkipXunitInjection>

  <!-- Disable integration test fixture injection -->
  <InjectIntegrationTestFixtures>false</InjectIntegrationTestFixtures>
</PropertyGroup>
```
