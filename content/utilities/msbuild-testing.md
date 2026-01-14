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
- Testing Central Package Management (CPM) scenarios
- Validating NuGet package source mapping

## Core Components

### ProjectBuilder

Fluent API for creating and building temporary .NET projects in isolated environments.

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

#### How ProjectBuilder Works

`ProjectBuilder` creates a complete isolated build environment:

1. **Temporary Directory**: Each instance creates a unique temp directory with automatic cleanup via `IAsyncDisposable`
2. **global.json**: Configures SDK version with `rollForward: latestMinor` for version stability
3. **NuGet.config**: Optional package source configuration with source mapping support
4. **Project Files**: Generates `.csproj` from fluent configuration
5. **Source Files**: Writes C# files to the project directory

```text
TempDirectory/
├── global.json              # SDK version pinning
├── NuGet.config             # Package sources (optional)
├── Directory.Build.props    # Shared properties (optional)
├── Directory.Packages.props # CPM configuration (optional)
├── TestProject.csproj       # Generated project file
├── Code.cs                  # Your source files
├── BuildOutput.sarif        # Generated after build
├── msbuild.binlog           # Binary log for analysis
└── GITHUB_STEP_SUMMARY.txt  # CI simulation file
```

#### SDK Version Management

ProjectBuilder automatically downloads and caches .NET SDK versions:

```csharp
// Use specific SDK version
project.WithDotnetSdkVersion(NetSdkVersion.Net100);

// SDK is downloaded to ~/.dotnet-sdk-cache/{version} if not present
```

Available SDK versions via `NetSdkVersion`:
- `Net100` - .NET 10.0 (default)
- `Net90` - .NET 9.0
- `Net80` - .NET 8.0

#### Build Operations

| Method | Purpose |
|--------|---------|
| `BuildAsync()` | Compiles the project |
| `RunAsync()` | Builds and executes (for console apps) |
| `TestAsync()` | Builds and runs tests |
| `PackAsync()` | Creates NuGet package |
| `RestoreAsync()` | Restores packages only |
| `ExecuteDotnetCommandAsync()` | Run any dotnet command |

Each method returns a `BuildResult` with SARIF diagnostics and binary log.

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

The SDK cache location:
- macOS/Linux: `~/.dotnet-sdk-cache/`
- Windows: `%USERPROFILE%\.dotnet-sdk-cache\`

### RepositoryRoot

Locates repository root for file access in tests.

```csharp
var root = RepositoryRoot.Locate();
var propsFile = root["src/Directory.Build.props"];
```

## ProjectBuilder Deep Dive

### NuGet Configuration

#### Simple Local Source

```csharp
await using var project = new ProjectBuilder(output)
    .WithPackageSource("LocalPackages", "/path/to/packages");
```

#### With Package Source Mapping

For security and reproducibility, use source mapping:

```csharp
await using var project = new ProjectBuilder(output)
    .WithPackageSource(
        name: "LocalPackages",
        path: "/path/to/packages",
        packagePatterns: ["MyCompany.*", "MyOrg.*"]);
```

This generates:

```xml
<configuration>
    <config>
        <add key="globalPackagesFolder" value="/path/to/packages/packages" />
    </config>
    <packageSources>
        <clear />
        <add key="LocalPackages" value="/path/to/packages" />
        <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    </packageSources>
    <packageSourceMapping>
        <packageSource key="LocalPackages">
            <package pattern="MyCompany.*" />
            <package pattern="MyOrg.*" />
        </packageSource>
        <packageSource key="nuget.org">
            <package pattern="*" />
        </packageSource>
    </packageSourceMapping>
</configuration>
```

#### Custom NuGet Config

For complete control:

```csharp
await using var project = new ProjectBuilder(output)
    .WithNuGetConfig("""
        <configuration>
            <packageSources>
                <clear />
                <add key="azure" value="https://pkgs.dev.azure.com/org/_packaging/feed/nuget/v3/index.json" />
            </packageSources>
            <packageSourceCredentials>
                <azure>
                    <add key="Username" value="user" />
                    <add key="ClearTextPassword" value="pat" />
                </azure>
            </packageSourceCredentials>
        </configuration>
        """);
```

### Directory.Build.props and Central Package Management

#### Shared Build Properties

```csharp
await using var project = new ProjectBuilder(output)
    .WithDirectoryBuildProps("""
        <Project>
            <PropertyGroup>
                <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
                <WarningsAsErrors>nullable</WarningsAsErrors>
            </PropertyGroup>
        </Project>
        """);
```

#### Central Package Management (CPM)

```csharp
await using var project = new ProjectBuilder(output)
    .WithDirectoryPackagesProps("""
        <Project>
            <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
            </PropertyGroup>
            <ItemGroup>
                <PackageVersion Include="xunit" Version="2.9.2" />
                <PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="4.12.0" />
            </ItemGroup>
        </Project>
        """);
```

### SDK Selection

#### Custom MSBuild SDK

```csharp
// Use a custom SDK package
await using var project = new ProjectBuilder(output)
    .WithRootSdk("MyCompany.NET.Sdk/1.0.0")
    .WithTargetFramework(Tfm.Net100);

// Use Microsoft.NET.Sdk.Web
await using var webProject = new ProjectBuilder(output)
    .WithRootSdk("Microsoft.NET.Sdk.Web")
    .WithTargetFramework(Tfm.Net100);
```

### Microsoft Testing Platform (MTP)

Enable MTP mode for modern test execution:

```csharp
await using var project = new ProjectBuilder(output)
    .WithMtpMode()
    .WithTargetFramework(Tfm.Net100)
    .WithOutputType(Val.Exe)  // MTP requires exe output
    .WithPackage("xunit.v3", "3.2.1")
    .WithPackage("xunit.v3.mtp.v2", "3.2.1")
    .AddSource("Tests.cs", """
        using Xunit;

        public class MyTests
        {
            [Fact]
            public void Test() => Assert.True(true);
        }
        """);

var result = await project.TestAsync();
result.ShouldSucceed();
```

`WithMtpMode()` updates global.json to:

```json
{
  "sdk": {
    "rollForward": "latestMinor",
    "version": "10.0.100"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

### GitHub Actions Simulation

Test CI-specific behavior:

```csharp
await using var project = new ProjectBuilder(output);

var result = await project
    .WithTargetFramework(Tfm.Net100)
    .AddSource("Code.cs", code)
    .BuildAsync(
        environmentVariables: project.GitHubEnvironmentVariables.ToArray());

// Check what was written to step summary
var summary = project.GetGitHubStepSummaryContent();
Assert.Contains("## Build Results", summary);
```

### Running Applications

For console application testing:

```csharp
await using var project = new ProjectBuilder(output)
    .WithTargetFramework(Tfm.Net100)
    .WithOutputType(Val.Exe)
    .AddSource("Program.cs", """
        Console.WriteLine($"Hello, {args[0]}!");
        """);

var result = await project.RunAsync(["World"]);

result.ShouldSucceed();
result.ShouldContainOutput("Hello, World!");
```

### Executing Arbitrary Commands

```csharp
// Run dotnet format
var formatResult = await project.ExecuteDotnetCommandAsync(
    "format",
    ["--verify-no-changes"]);

// Run dotnet new
var newResult = await project.ExecuteDotnetCommandAsync(
    "new",
    ["console", "-n", "MyApp"]);
```

### File Access

Add arbitrary files to the project:

```csharp
await using var project = new ProjectBuilder(output);

// Add configuration files
project.AddFile("appsettings.json", """
    {
        "ConnectionStrings": {
            "Default": "Server=localhost"
        }
    }
    """);

// Add resources
project.AddFile("Resources/data.txt", "sample data");

// Access the project directory
var projectDir = project.RootFolder;
```

## Complete Examples

### Testing an Analyzer Package

```csharp
public class AnalyzerPackageTests(ITestOutputHelper output)
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
}
```

### Testing a Custom SDK

```csharp
public class CustomSdkTests(ITestOutputHelper output)
{
    [Fact]
    public async Task SDK_Sets_Default_Properties()
    {
        await using var project = new ProjectBuilder(output)
            .WithTargetFramework(Tfm.Net100)
            .WithRootSdk("MyCompany.NET.Sdk/1.0.0")
            .WithPackageSource("local", PackageOutputPath, "MyCompany.*")
            .AddSource("Code.cs", "class C { }");

        var result = await project.BuildAsync();

        result.ShouldSucceed();
        result.ShouldHavePropertyValue("TreatWarningsAsErrors", "true");
        result.ShouldHavePropertyValue("Nullable", "enable");
    }
}
```

### Testing Source Generators

```csharp
public class GeneratorTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Generator_Produces_Expected_Output()
    {
        await using var project = new ProjectBuilder(output)
            .WithTargetFramework(Tfm.Net100)
            .WithOutputType(Val.Exe)
            .WithPackage("MyGenerator", "1.0.0")
            .AddSource("Program.cs", """
                [AutoGenerate]
                partial class MyClass { }

                Console.WriteLine(new MyClass().GeneratedMethod());
                """);

        var result = await project.RunAsync();

        result.ShouldSucceed();
        result.ShouldContainOutput("Generated!");
    }
}
```

### Testing with Banned APIs

```csharp
public class BannedApiTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Banned_API_Reports_Error()
    {
        await using var project = new ProjectBuilder(output)
            .WithTargetFramework(Tfm.Net100)
            .WithPackage("Microsoft.CodeAnalysis.BannedApiAnalyzers", "3.3.4")
            .AddSource("Code.cs", """
                using System;
                public class C
                {
                    void M(TimeProvider time) => time.GetUtcNow().ToString();
                }
                """);

        project.AddFile("BannedSymbols.txt", """
            M:System.TimeProvider.GetLocalNow;Use GetUtcNow instead
            """);

        var result = await project.BuildAsync();

        result.ShouldSucceed();
    }
}
```

## Extending ProjectBuilder

Create a derived builder for project-specific defaults:

```csharp
public class MyProjectBuilder : ProjectBuilder
{
    public MyProjectBuilder(ITestOutputHelper? output = null) : base(output)
    {
        // Apply project defaults
        WithTargetFramework(Tfm.Net100);
        WithProperty(Prop.Nullable, Val.Enable);
        WithProperty(Prop.ImplicitUsings, Val.Enable);
    }

    protected override void GenerateCsprojFile()
    {
        // Customize project generation if needed
        base.GenerateCsprojFile();
    }
}
```

## Troubleshooting

### Viewing Build Output

Pass `ITestOutputHelper` to see all generated files and build commands:

```csharp
await using var project = new ProjectBuilder(output);  // Enables verbose logging
```

### Inspecting Binary Logs

`BuildResult` includes the binary log for detailed analysis:

```csharp
var result = await project.BuildAsync();
var binlog = result.BinaryLog;  // byte[] of msbuild.binlog
File.WriteAllBytes("debug.binlog", binlog);
// Open with https://msbuildlog.com/
```

### Environment Isolation

`ProjectBuilder` automatically removes interfering environment variables:
- `CI`
- `GITHUB_*`
- `MSBuild*`
- `RUNNER_*`
- `DOTNET_ENVIRONMENT`

This ensures consistent builds regardless of the host environment.
