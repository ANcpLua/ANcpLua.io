---
title: ANcpLua.Analyzers
description: Custom Roslyn analyzers and code fixes
---

# ANcpLua.Analyzers

Custom Roslyn analyzers and code fixes

## Rules

|Id|Category|Description|Severity|Enabled|Code Fix|
|--|--------|-----------|:------:|:-----:|:------:|
|[AL0001](./rules/AL0001.md)|Design|Prohibit reassignment of primary constructor parameters|❌|✔️|❌|
|[AL0002](./rules/AL0002.md)|Design|Don't repeat negated patterns|⚠️|✔️|✔️|
|[AL0003](./rules/AL0003.md)|Reliability|Don't divide by constant zero|❌|✔️|❌|
|[AL0004](./rules/AL0004.md)|Usage|Use pattern matching when comparing Span with constants|⚠️|✔️|✔️|
|[AL0005](./rules/AL0005.md)|Usage|Use SequenceEqual when comparing Span with non-constants|⚠️|✔️|✔️|
|[AL0006](./rules/AL0006.md)|Design|Field name conflicts with primary constructor parameter|⚠️|✔️|❌|
|[AL0007](./rules/AL0007.md)|Usage|GetSchema should be explicitly implemented|❌|✔️|❌|
|[AL0008](./rules/AL0008.md)|Usage|GetSchema must return null and not be abstract|❌|✔️|✔️|
|[AL0009](./rules/AL0009.md)|Usage|Don't call IXmlSerializable.GetSchema|❌|✔️|❌|
|[AL0010](./rules/AL0010.md)|Design|Type should be partial|ℹ️|❌|✔️|
|[AL0011](./rules/AL0011.md)|Threading|Avoid lock keyword on non-Lock types|⚠️|✔️|❌|
|[AL0012](./rules/AL0012.md)|OpenTelemetry|Deprecated semantic convention attribute|⚠️|✔️|✔️|
|[AL0013](./rules/AL0013.md)|OpenTelemetry|Missing telemetry schema URL|ℹ️|✔️|❌|
|[AL0014](./rules/AL0014.md)|Style|Prefer pattern matching for null and zero comparisons|ℹ️|✔️|✔️|
|[AL0015](./rules/AL0015.md)|Style|Normalize null-guard style|ℹ️|✔️|✔️|
|[AL0016](./rules/AL0016.md)|Style|Combine declaration with subsequent null-check|ℹ️|✔️|✔️|
|[AL0017](./rules/AL0017.md)|VersionManagement|Hardcoded package version detected|⚠️|✔️|❌|
|[AL0020](./rules/AL0020.md)|ASP.NET Core|IFormCollection requires explicit attribute|❌|✔️|❌|
|[AL0021](./rules/AL0021.md)|ASP.NET Core|Multiple structured form sources|❌|✔️|❌|
|[AL0022](./rules/AL0022.md)|ASP.NET Core|Mixed form collection and DTO|❌|✔️|❌|
|[AL0023](./rules/AL0023.md)|ASP.NET Core|Unsupported form type|❌|✔️|❌|
|[AL0024](./rules/AL0024.md)|ASP.NET Core|Form and body conflict|❌|✔️|❌|
|[AL0025](./rules/AL0025.md)|Usage|Anonymous function can be made static|ℹ️|✔️|✔️|


## Refactorings

|Id|Description|
|--|-----------|
|[AR0001](./rules/AR0001.md)|Snake Case To Pascal Case|
|[AR0002](./rules/AR0002.md)|Make Static Lambda|


## Configuration

See [Configuration](./configuration.md) for .editorconfig settings.