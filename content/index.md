---
title: ANcpLua Framework
description: Documentation for the ANcpLua .NET framework
---

# ANcpLua Framework

Welcome to the ANcpLua Framework documentation.

## Components

### [ANcpLua.NET.Sdk](./sdk/)

MSBuild SDK with polyfills, analyzers, and opinionated defaults

### [ANcpLua.Roslyn.Utilities](./utilities/)

Roslyn utilities for source generators and analyzers

### [ANcpLua.Analyzers](./analyzers/)

Custom Roslyn analyzers and code fixes

## Quick Start

```xml
<Project Sdk="ANcpLua.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```
