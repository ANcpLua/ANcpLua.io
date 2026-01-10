---
_layout: landing
---

# ANcpLua Framework

A modern .NET SDK with opinionated defaults, analyzers, and Roslyn utilities.

## Quick Start

```xml
<Project Sdk="ANcpLua.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

## Components

| Package | Description | NuGet |
|---------|-------------|-------|
| [ANcpLua.NET.Sdk](sdk/) | MSBuild SDK with polyfills and analyzers | [![NuGet](https://img.shields.io/nuget/v/ANcpLua.NET.Sdk)](https://nuget.org/packages/ANcpLua.NET.Sdk) |
| [ANcpLua.Roslyn.Utilities](utilities/) | Roslyn utilities for analyzers and generators | [![NuGet](https://img.shields.io/nuget/v/ANcpLua.Roslyn.Utilities)](https://nuget.org/packages/ANcpLua.Roslyn.Utilities) |
| [ANcpLua.Analyzers](analyzers/) | Custom Roslyn analyzers and code fixes | [![NuGet](https://img.shields.io/nuget/v/ANcpLua.Analyzers)](https://nuget.org/packages/ANcpLua.Analyzers) |

## API Reference

Browse the [API documentation](api/) for detailed type and method information.
