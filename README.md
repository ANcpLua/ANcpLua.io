# ANcpLua.io

[![Build Docs](https://github.com/ANcpLua/ANcpLua.io/actions/workflows/docs.yml/badge.svg)](https://github.com/ANcpLua/ANcpLua.io/actions/workflows/docs.yml)

Unified documentation site for the ANcpLua .NET development ecosystem.

**Live site:** [ancplua.github.io](https://ancplua.github.io)

## Architecture

This repo is the **single source of truth** for all ANcpLua documentation (like Microsoft's `dotnet/docs`).

**DocsGenerator** automatically:
- Clones all 3 source repos (SDK, Utilities, Analyzers)
- Builds assemblies for reflection
- Pulls Utilities docs from `docs/utilities/` (existing markdown)
- Generates SDK docs from README + code structure (manual)
- Generates Analyzer docs from assembly reflection (Meziantou pattern)

**Triggers:**
- Push to this repo
- Webhooks from source repos (`repository_dispatch`)
- Nightly schedule (4 AM UTC)
- Manual dispatch

**Features:**
- Auto-commits generated content
- Creates GitHub issue on failure

## Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| ANcpLua.NET.Sdk | [![NuGet](https://img.shields.io/nuget/v/ANcpLua.NET.Sdk)](https://nuget.org/packages/ANcpLua.NET.Sdk) | Zero-config MSBuild SDK |
| ANcpLua.Roslyn.Utilities | [![NuGet](https://img.shields.io/nuget/v/ANcpLua.Roslyn.Utilities)](https://nuget.org/packages/ANcpLua.Roslyn.Utilities) | Roslyn utilities |
| ANcpLua.Analyzers | [![NuGet](https://img.shields.io/nuget/v/ANcpLua.Analyzers)](https://nuget.org/packages/ANcpLua.Analyzers) | Roslyn analyzers |

## Local Development

```bash
# Install DocFX
dotnet tool install -g docfx

# Generate docs from source repos
dotnet run --project tools/DocsGenerator

# Build and serve locally
docfx docfx.json --serve
```

## Contributing

See [CLAUDE.md](CLAUDE.md) for structure and conventions.

## License

MIT
