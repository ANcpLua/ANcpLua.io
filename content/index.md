---
title: ANcpLua Framework
description: Documentation for the ANcpLua .NET framework
---

# ANcpLua Framework

Welcome to the ANcpLua Framework documentation.

## Components

| Package | Description | Links |
|---------|-------------|-------|
| [ANcpLua.NET.Sdk](sdk/index.md) | MSBuild SDK with polyfills, analyzers, and opinionated defaults | [![NuGet](https://img.shields.io/nuget/v/ANcpLua.NET.Sdk?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.NET.Sdk/) [![GitHub](https://img.shields.io/badge/GitHub-Source-181717?logo=github)](https://github.com/ANcpLua/ANcpLua.NET.Sdk) |
| [ANcpLua.Roslyn.Utilities](utilities/index.md) | Roslyn utilities for source generators and analyzers | [![NuGet](https://img.shields.io/nuget/v/ANcpLua.Roslyn.Utilities?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities/) [![GitHub](https://img.shields.io/badge/GitHub-Source-181717?logo=github)](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) |
| [ANcpLua.Analyzers](analyzers/index.md) | Custom Roslyn analyzers and code fixes | [![NuGet](https://img.shields.io/nuget/v/ANcpLua.Analyzers?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Analyzers/) [![GitHub](https://img.shields.io/badge/GitHub-Source-181717?logo=github)](https://github.com/ANcpLua/ANcpLua.Analyzers) |

## Quick Start

```xml
<Project Sdk="ANcpLua.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

## Source Repositories

- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) - The MSBuild SDK
- [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) - Roslyn utilities
- [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers) - Custom analyzers
- [ANcpLua.io](https://github.com/ANcpLua/ANcpLua.io) - This documentation site
