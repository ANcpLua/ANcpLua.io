---
title: ANcpLua.Roslyn.Utilities
---

# ANcpLua.Roslyn.Utilities

Roslyn utilities for source generators, analyzers, and comprehensive testing.

## Packages

| Package | Description |
|---------|-------------|
| `ANcpLua.Roslyn.Utilities` | Core Roslyn helpers for generators and analyzers |
| `ANcpLua.Roslyn.Utilities.Sources` | Source-only package (no runtime dependency) |
| `ANcpLua.Roslyn.Utilities.Testing` | Test infrastructure for Roslyn and MSBuild |

## Installation

```xml
<PackageReference Include="ANcpLua.Roslyn.Utilities" Version="1.8.0" />
```

For testing:

```xml
<PackageReference Include="ANcpLua.Roslyn.Utilities.Testing" Version="1.8.0" />
```

## Features

### Core Utilities
- Symbol analysis helpers
- Syntax generation utilities
- Diagnostic helpers

### Testing Infrastructure
- **Roslyn Testing**: Fluent API for testing analyzers, code fixes, and generators
- **MSBuild Testing**: Integration testing with real `dotnet build` commands
