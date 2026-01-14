# CLAUDE.md

Unified docs site for ANcpLua .NET packages. Built with DocFX, deployed to GitHub Pages.

## Quick Commands

```bash
# Preview locally
docfx docfx.json --serve

# Regenerate from source repos
dotnet run --project tools/DocsGenerator

# Full build
docfx build docfx.json
```

## Structure

```
content/           # Markdown docs
  sdk/             # ANcpLua.NET.Sdk
  utilities/       # ANcpLua.Roslyn.Utilities
  analyzers/       # ANcpLua.Analyzers + auto-generated rules
tools/DocsGenerator/  # Clones repos, generates analyzer rule pages
.repos/            # Cloned source repos (gitignored)
```

## Key Files

| File | Purpose |
|------|---------|
| `docfx.json` | DocFX config (metadata + build) |
| `content/toc.yml` | Top navigation |
| `tools/DocsGenerator/Program.cs` | Rule page generator |

## Workflow

1. Push to main → triggers CI
2. DocsGenerator clones source repos to `.repos/`
3. Builds analyzers, extracts rules via reflection
4. `docfx metadata` extracts API from XML docs
5. `docfx build` generates static site
6. Deploys to https://ancplua.github.io/ANcpLua.io/

## Source Repos

- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk)
- [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities)
- [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers)
