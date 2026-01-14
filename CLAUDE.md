# CLAUDE.md

> **ANcpLua.io** — Unified documentation site for the ANcpLua .NET development ecosystem.

---

## 1. What This Repository Is

This is the **unified documentation repository** for three interconnected .NET packages:

| Package                      | Description                       | NuGet                                                                                                                    |
|------------------------------|-----------------------------------|--------------------------------------------------------------------------------------------------------------------------|
| **ANcpLua.NET.Sdk**          | Zero-config MSBuild SDK           | [![NuGet](https://img.shields.io/nuget/v/ANcpLua.NET.Sdk)](https://nuget.org/packages/ANcpLua.NET.Sdk)                   |
| **ANcpLua.Roslyn.Utilities** | Roslyn source generator utilities | [![NuGet](https://img.shields.io/nuget/v/ANcpLua.Roslyn.Utilities)](https://nuget.org/packages/ANcpLua.Roslyn.Utilities) |
| **ANcpLua.Analyzers**        | Custom Roslyn analyzer rules      | [![NuGet](https://img.shields.io/nuget/v/ANcpLua.Analyzers)](https://nuget.org/packages/ANcpLua.Analyzers)               |

**Pattern:** Like Microsoft's `dotnet/docs`, React's `react.dev`, or Kubernetes's `kubernetes/website`.

---

## 2. Repository Structure

```
ANcpLua.io/
├── .claude-plugin/           # Local plugin config
├── .github/workflows/
│   ├── docs.yml              # Build + deploy to GitHub Pages
│   └── trigger-docs.yml      # Template for source repo triggers
├── tools/DocsGenerator/      # Meziantou-style analyzer rule generator
│   ├── DocsGenerator.csproj
│   └── Program.cs
├── content/                  # DocFX content root
│   ├── index.md              # Landing page
│   ├── toc.yml               # Top navigation
│   ├── sdk/                  # SDK documentation
│   ├── utilities/            # Utilities documentation
│   ├── analyzers/            # Analyzers documentation
│   │   └── rules/            # Auto-generated rule pages
│   └── api/                  # Auto-generated API reference
├── docfx.json                # DocFX configuration
└── .repos/                   # Cloned source repos (gitignored)
```

---

## 3. Source Repositories

DocsGenerator clones these repos to `.repos/` at build time:

| Repo          | GitHub URL                         | Docs Strategy                       |
|---------------|------------------------------------|-------------------------------------|
| **SDK**       | `ANcpLua/ANcpLua.NET.Sdk`          | Manual (README + code structure)    |
| **Utilities** | `ANcpLua/ANcpLua.Roslyn.Utilities` | Existing (`docs/utilities/`)        |
| **Analyzers** | `ANcpLua/ANcpLua.Analyzers`        | Reflection (assembly introspection) |

**Where docs live in source repos:**

| Repo      | Docs Location     | Status                        |
|-----------|-------------------|-------------------------------|
| SDK       | None              | Generated from README         |
| Utilities | `docs/utilities/` | Copied by DocsGenerator       |
| Analyzers | `docs/rules/`     | Merged with generated content |

---

## 4. Content Pipeline

DocsGenerator (`tools/DocsGenerator/Program.cs`) handles everything automatically:

### Generation Strategy by Repo

| Repo          | Strategy     | What DocsGenerator Does                                                                            |
|---------------|--------------|----------------------------------------------------------------------------------------------------|
| **Utilities** | `Existing`   | Copies `docs/utilities/*.md`, transforms links                                                     |
| **Analyzers** | `Reflection` | Loads DLLs, extracts `DiagnosticAnalyzer` + `CodeFixProvider` via reflection, generates rule pages |
| **SDK**       | `Manual`     | Generates from README, `src/Sdk/` variants, `BannedSymbols.txt`                                    |

### Running DocsGenerator

```bash
dotnet run --project tools/DocsGenerator
```

This will:

1. Clone/update all repos to `.repos/`
2. Build assemblies (`dotnet build -c Release`)
3. Generate `content/{sdk,utilities,analyzers}/*.md`
4. Generate `content/analyzers/rules/AL{XXXX}.md` (Meziantou pattern)
5. Generate `.editorconfig` files for analyzer configuration

### DocFX Build

```bash
docfx build docfx.json
# Outputs: _site/
```

---

## 5. DocFX Markdown (DFM)

### Alerts

```markdown
> [!NOTE]
> Informational content

> [!WARNING]
> Important warning

> [!TIP]
> Helpful suggestion
```

### Code Tabs

```markdown
# [C#](#tab/csharp)

```csharp
// C# example
```

# [CLI](#tab/cli)

```bash
dotnet build
```

---

```

### Cross-References
```markdown
<xref:ANcpLua.Roslyn.Utilities.DiagnosticFlow`1>
```

---

## 6. Navigation (toc.yml)

Every content directory needs a `toc.yml`:

```yaml
# content/toc.yml (top-level)
- name: Home
  href: index.md
- name: SDK
  href: sdk/
- name: Utilities
  href: utilities/
- name: Analyzers
  href: analyzers/
- name: API Reference
  href: api/
```

```yaml
# content/sdk/toc.yml
- name: Overview
  href: index.md
- name: Getting Started
  href: getting-started.md
- name: Variants
  href: variants.md
```

---

## 7. Local Development

### Preview site locally

```bash
# Install docfx if needed
dotnet tool install -g docfx

# Build and serve
docfx docfx.json --serve
# Opens at http://localhost:8080
```

### Regenerate analyzer rules

```bash
dotnet run --project tools/DocsGenerator
```

### Full build

```bash
docfx metadata docfx.json  # Generate API docs from DLLs
docfx build docfx.json     # Build final site
```

---

## 8. CI/CD Pipeline

**Trigger:** Push to main OR repository_dispatch from source repos

```yaml
# Source repo calls this to trigger docs rebuild:
gh workflow run docs.yml --repo ANcpLua/ANcpLua.io
```

**Pipeline steps:**

1. Clone source repos to `.repos/`
2. Build source repos (`dotnet build -c Release`)
3. Run DocsGenerator (analyzer rules)
4. Run `docfx metadata` (API from DLLs)
5. Run `docfx build` (final site)
6. Deploy to GitHub Pages

---

## 9. File Conventions

### YAML Front Matter

Every `.md` file should have:

```yaml
---
title: Page Title
---
```

### Code Blocks

Always specify language:

```markdown
```csharp
public void Example() { }
```

```

### Internal Links
Use relative paths:
```markdown
See [Getting Started](../sdk/getting-started.md)
```

---

## 10. Version Verification

Before documenting versions, verify against NuGet:

```bash
# Check latest versions
curl -s https://api.nuget.org/v3-flatcontainer/ancplua.net.sdk/index.json | jq '.versions[-1]'
curl -s https://api.nuget.org/v3-flatcontainer/ancplua.analyzers/index.json | jq '.versions[-1]'
curl -s https://api.nuget.org/v3-flatcontainer/ancplua.roslyn.utilities/index.json | jq '.versions[-1]'
```

---

## 11. Workflow Summary

| Task                | Command                                              |
|---------------------|------------------------------------------------------|
| Preview locally     | `docfx docfx.json --serve`                           |
| Regenerate rules    | `dotnet run --project tools/DocsGenerator`           |
| Full build          | `docfx metadata && docfx build`                      |
| Deploy              | Push to main (auto via GitHub Actions)               |
| Trigger from source | `gh workflow run docs.yml --repo ANcpLua/ANcpLua.io` |

---

## 12. Related Files

| File                             | Purpose                                |
|----------------------------------|----------------------------------------|
| `docfx.json`                     | DocFX configuration (metadata + build) |
| `tools/DocsGenerator/Program.cs` | Analyzer rule generator                |
| `.github/workflows/docs.yml`     | CI/CD pipeline                         |
| `content/toc.yml`                | Top-level navigation                   |