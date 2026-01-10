---
title: MSBuild Testing
---

# MSBuild Testing

Integration testing infrastructure for testing with real `dotnet build` commands.

## Overview

Unlike Roslyn's in-memory testing, MSBuild testing creates actual project files, runs real builds, and validates the results. This is essential for:

- Testing MSBuild SDK behavior
- Validating analyzer packages in real builds
- Testing build-time source generators
- Verifying SARIF output and binlog contents

## Core Components

### ProjectBuilder

Fluent API for creating and building temporary .NET projects.

```csharp
using ANcpLua.Roslyn.Utilities.Testing.MSBuild;

[Fact]
public async Task Build_Succeeds()
{
    await using var project = new ProjectBuilder(Output)
        .WithTargetFramework(Tfm.Net100)
        .WithOutputType(Val.Library)
        .AddSource("Code.cs", "public class Sample { }");

    var result = await project.BuildAsync();

    result.ShouldSucceed();
}
```

### BuildResult

Contains build output with fluent assertions.

```csharp
var result = await project.BuildAsync();

// Fluent assertions
result.ShouldSucceed();
result.ShouldHaveWarning("CS0168");
result.ShouldNotHaveError("CS0246");
result.ShouldContainOutput("Build succeeded");

// Property inspection
var value = result.GetMsBuildPropertyValue("TargetFramework");

// SARIF analysis
var errors = result.GetErrors();
var warnings = result.GetWarnings();
```

### MSBuild Constants

Type-safe constants for MSBuild properties, values, and items.

```csharp
using static ANcpLua.Roslyn.Utilities.Testing.MSBuild.Tfm;
using static ANcpLua.Roslyn.Utilities.Testing.MSBuild.Prop;
using static ANcpLua.Roslyn.Utilities.Testing.MSBuild.Val;

project
    .WithProperty(TargetFramework, Net100)
    .WithProperty(OutputType, Library)
    .WithProperty(Nullable, Enable);
```

| Class | Purpose |
|-------|---------|
| `Tfm` | Target framework monikers (`Net100`, `NetStandard20`) |
| `Prop` | Property names (`TargetFramework`, `OutputType`) |
| `Val` | Property values (`Library`, `Exe`, `Enable`) |
| `Item` | Item names (`PackageReference`, `Compile`) |
| `Attr` | Attribute names (`Include`, `Version`) |

### DotNetSdkHelpers

Downloads and caches .NET SDK versions for testing.

```csharp
// Gets path to dotnet executable, downloading if needed
var dotnetPath = await DotNetSdkHelpers.Get(NetSdkVersion.Net100);
```

### RepositoryRoot

Locates repository root for file access in tests.

```csharp
var root = RepositoryRoot.Locate();
var propsFile = root["src/Directory.Build.props"];
```

## Complete Example

```csharp
public class SdkTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Analyzer_Reports_Warning_In_Real_Build()
    {
        await using var project = new ProjectBuilder(output)
            .WithTargetFramework(Tfm.Net100)
            .WithOutputType(Val.Library)
            .WithPackage("MyAnalyzer", "1.0.0")
            .AddSource("Code.cs", """
                public class Sample
                {
                    public void Method() => Console.WriteLine("test");
                }
                """);

        var result = await project.BuildAsync();

        result.ShouldSucceed();
        result.ShouldHaveWarning("MY001");
    }

    [Fact]
    public async Task SDK_Sets_Property()
    {
        await using var project = new ProjectBuilder(output)
            .WithTargetFramework(Tfm.Net100)
            .WithRootSdk("MyCompany.NET.Sdk/1.0.0")
            .AddSource("Code.cs", "class C { }");

        var result = await project.BuildAsync();

        result.ShouldSucceed();
        result.ShouldHavePropertyValue("MyCustomProperty", "true");
    }
}
```

## NuGet Configuration

Configure package sources for testing local packages:

```csharp
await using var project = new ProjectBuilder(output)
    .WithPackageSource("LocalPackages", "/path/to/packages", "MyPackage.*")
    .WithPackage("MyPackage", "1.0.0");
```

## Microsoft Testing Platform

Enable MTP mode for test projects:

```csharp
await using var project = new ProjectBuilder(output)
    .WithMtpMode()
    .WithPackage("xunit.v3", "3.2.1");
```
