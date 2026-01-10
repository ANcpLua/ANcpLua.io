---
title: Testing Infrastructure
---

# Testing Infrastructure

`ANcpLua.Roslyn.Utilities.Testing` provides two testing paradigms:

1. **Roslyn Testing** - In-memory compilation testing for analyzers, code fixes, and generators
2. **MSBuild Testing** - Integration testing with real `dotnet build` commands

## Installation

```xml
<PackageReference Include="ANcpLua.Roslyn.Utilities.Testing" Version="1.8.0" />
```

## Roslyn Testing

For unit testing analyzers, code fixes, and source generators without invoking MSBuild.

```csharp
using ANcpLua.Roslyn.Utilities.Testing;

[Fact]
public async Task Analyzer_ReportsWarning()
{
    var test = new AnalyzerTest<MyAnalyzer>()
        .WithSource("""
            class Sample
            {
                void Method() { [|BadApi()|]; }
            }
            """);

    await test.RunAsync();
}
```

## MSBuild Testing

For integration testing with real builds - testing SDK features, package behavior, or build-time behavior.

See [MSBuild Testing](msbuild-testing.md) for details.
