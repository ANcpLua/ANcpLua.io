---
title: Configuration Files
---

# Configuration Files

The SDK includes pre-configured settings for analyzers and code style.

## EditorConfig Files

| File | Purpose |
|------|---------|
| `Analyzer.ANcpLua.Analyzers.editorconfig` | Settings for ANcpLua.Analyzers |
| `Analyzer.Meziantou.Analyzer.editorconfig` | Settings for Meziantou |
| `Analyzer.Microsoft.CodeAnalysis.Analyzers.editorconfig` | Settings for Microsoft.CodeAnalysis.Analyzers |
| `Analyzer.Microsoft.CodeAnalysis.BannedApiAnalyzers.editorconfig` | Settings for Microsoft.CodeAnalysis.BannedApiAnalyzers |
| `Analyzer.Microsoft.CodeAnalysis.NetAnalyzers.editorconfig` | Settings for Microsoft.CodeAnalysis.NetAnalyzers |
| `Analyzers.editorconfig` | Master analyzer configuration |
| `CodingStyle.editorconfig` | Code style settings (indentation, spacing, etc.) |
| `GeneratedFiles.editorconfig` | Suppresses warnings in generated code |
| `NamingConvention.editorconfig` | Naming conventions for types, members, parameters |

## Banned Symbols

| File | Description |
|------|-------------|
| `BannedSymbols.NewtonsoftJson.txt` | Bans legacy JSON library in favor of System.Text.Json |
| `BannedSymbols.txt` | Default banned APIs (use TimeProvider instead of legacy time APIs) |

## Test Run Settings

The `default.runsettings` file configures test execution settings.

