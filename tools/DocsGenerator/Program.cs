// ANcpLua.io Multi-Repo Documentation Generator
// Based on Meziantou.Analyzer patterns, scaled for cross-repo documentation
// Generates unified docs from: ANcpLua.NET.Sdk, ANcpLua.Roslyn.Utilities, ANcpLua.Analyzers

#pragma warning disable CA1812 // Avoid uninstantiated internal classes
#pragma warning disable MA0004 // Use Task.ConfigureAwait
#pragma warning disable CA1849 // Call async methods when in an async method

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;

// ═══════════════════════════════════════════════════════════════════════════════
// Configuration
// ═══════════════════════════════════════════════════════════════════════════════

var config = new DocsConfig
{
    Repos =
    [
        new RepoConfig
        {
            Name = "sdk",
            GitUrl = "https://github.com/ANcpLua/ANcpLua.NET.Sdk.git",
            DisplayName = "ANcpLua.NET.Sdk",
            Description = "MSBuild SDK with polyfills, analyzers, and opinionated defaults",
            DocsSource = DocsSourceType.Manual, // Has no docs/ folder, generate from README + code
            GenerateFromAssembly = false
        },
        new RepoConfig
        {
            Name = "utilities",
            GitUrl = "https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities.git",
            DisplayName = "ANcpLua.Roslyn.Utilities",
            Description = "Roslyn utilities for source generators and analyzers",
            // Utilities has docs copied to ANcpLua.io content already, use Manual to generate from README
            DocsSource = DocsSourceType.Manual,
            GenerateFromAssembly = false // API docs generated separately via docfx metadata
        },
        new RepoConfig
        {
            Name = "analyzers",
            GitUrl = "https://github.com/ANcpLua/ANcpLua.Analyzers.git",
            DisplayName = "ANcpLua.Analyzers",
            Description = "Custom Roslyn analyzers and code fixes",
            // Use reflection to extract analyzer rules from compiled DLLs (Meziantou pattern)
            DocsSource = DocsSourceType.Reflection,
            GenerateFromAssembly = true,
            // ANcpLua.NET.Sdk outputs to artifacts/bin/ folder
            AssemblyPath = "artifacts/bin/ANcpLua.Analyzers/release_netstandard2.0/ANcpLua.Analyzers.dll",
            CodeFixAssemblyPath =
                "artifacts/bin/ANcpLua.Analyzers.CodeFixes/release_netstandard2.0/ANcpLua.Analyzers.CodeFixes.dll"
        }
    ],
    OutputPath = "content"
};

// ═══════════════════════════════════════════════════════════════════════════════
// Main Execution
// ═══════════════════════════════════════════════════════════════════════════════

if (!TryFindGitRoot(out var gitRoot))
{
    Console.Error.WriteLine("Cannot find git root from " + Directory.GetCurrentDirectory());
    return 1;
}

var generator = new DocsGenerator(config, gitRoot);
return await generator.GenerateAsync();

// ═══════════════════════════════════════════════════════════════════════════════
// Local Functions (must be in top-level section)
// ═══════════════════════════════════════════════════════════════════════════════

static bool TryFindGitRoot(out string root)
{
    var current = Directory.GetCurrentDirectory();
    while (current != null)
    {
        if (Directory.Exists(Path.Combine(current, ".git")))
        {
            root = current;
            return true;
        }

        current = Path.GetDirectoryName(current);
    }

    root = string.Empty;
    return false;
}

// ═══════════════════════════════════════════════════════════════════════════════
// Core Generator
// ═══════════════════════════════════════════════════════════════════════════════

internal sealed class DocsGenerator(DocsConfig config, string gitRoot)
{
    private readonly DocsConfig _config = config;
    private readonly string _gitRoot = gitRoot;
    private readonly string _outputPath = Path.Combine(gitRoot, config.OutputPath);
    private readonly string _reposPath = Path.Combine(gitRoot, ".repos");
    private int _filesWritten;

    public async Task<int> GenerateAsync()
    {
        try
        {
            Console.WriteLine("ANcpLua.io Documentation Generator");
            Console.WriteLine($"Output: {_outputPath}");
            Console.WriteLine();

            // Step 1: Clone/update repos
            await FetchReposAsync();

            // Step 2: Build assemblies for reflection
            await BuildAssembliesAsync();

            // Step 3: Generate docs for each repo
            foreach (var repo in _config.Repos) await GenerateRepoDocsAsync(repo);

            // Step 4: Generate unified index and navigation
            GenerateUnifiedIndex();
            GenerateNavigation();

            // Step 5: Generate editorconfig files
            await GenerateEditorConfigAsync();

            Console.WriteLine();
            Console.WriteLine($"Generated {_filesWritten} file(s)");

            // Validation: fail if zero files written (indicates critical failure)
            if (_filesWritten == 0)
            {
                Console.Error.WriteLine("ERROR: Zero files generated. This indicates a critical failure.");
                Console.Error.WriteLine("       Check git connectivity, assembly builds, and file paths.");
                return 1;
            }

            if (_filesWritten > 0) await ShowGitDiffAsync();

            return 0; // Success
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("FATAL ERROR: Documentation generation failed");
            Console.Error.WriteLine($"Exception: {ex.GetType().Name}");
            Console.Error.WriteLine($"Message: {ex.Message}");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Stack Trace:");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private async Task FetchReposAsync()
    {
        Directory.CreateDirectory(_reposPath);

        foreach (var repo in _config.Repos)
        {
            var repoPath = Path.Combine(_reposPath, repo.Name);

            if (Directory.Exists(repoPath))
            {
                Console.WriteLine($"Updating {repo.Name}...");
                await RunGitAsync(repoPath, "fetch", "origin");
                await RunGitAsync(repoPath, "reset", "--hard", "origin/main");
            }
            else
            {
                Console.WriteLine($"Cloning {repo.Name}...");
                await RunGitAsync(_reposPath, "clone", "--depth=1", repo.GitUrl, repo.Name);
            }
        }
    }

    private async Task BuildAssembliesAsync()
    {
        foreach (var repo in _config.Repos.Where(r => r.GenerateFromAssembly))
        {
            var repoPath = Path.Combine(_reposPath, repo.Name);
            Console.WriteLine($"Building {repo.Name}...");
            var (exitCode, output, error) =
                await RunDotNetWithOutputAsync(repoPath, "build", "-c", "Release", "--nologo");
            if (exitCode != 0)
            {
                Console.WriteLine($"  Warning: Build failed for {repo.Name} (exit code {exitCode})");
                if (!string.IsNullOrWhiteSpace(error)) Console.WriteLine($"  Error: {error.Trim()}");
            }
        }
    }

    private async Task GenerateRepoDocsAsync(RepoConfig repo)
    {
        Console.WriteLine($"Generating docs for {repo.DisplayName}...");

        var repoPath = Path.Combine(_reposPath, repo.Name);
        var outputDir = Path.Combine(_outputPath, repo.Name);
        Directory.CreateDirectory(outputDir);

        switch (repo.DocsSource)
        {
            case DocsSourceType.Existing:
                await CopyExistingDocsAsync(repoPath, repo, outputDir);
                break;

            case DocsSourceType.Reflection:
                await GenerateAnalyzerDocsAsync(repoPath, repo, outputDir);
                break;

            case DocsSourceType.Manual:
                await GenerateManualDocsAsync(repoPath, repo, outputDir);
                break;
        }

        // Generate repo index
        GenerateRepoIndex(repo, outputDir);
    }

    private async Task CopyExistingDocsAsync(string repoPath, RepoConfig repo, string outputDir)
    {
        var sourcePath = Path.Combine(repoPath, repo.ExistingDocsPath ?? "docs");
        if (!Directory.Exists(sourcePath))
        {
            Console.WriteLine($"  Warning: No docs folder found at {sourcePath}");
            return;
        }

        foreach (var file in Directory.GetFiles(sourcePath, "*.md", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourcePath, file);
            var destPath = Path.Combine(outputDir, relativePath);
            var destDir = Path.GetDirectoryName(destPath);
            if (destDir is not null) Directory.CreateDirectory(destDir);

            var content = await File.ReadAllTextAsync(file);
            // Transform links for unified site
            content = TransformLinks(content, repo.Name);
            WriteFileIfChanged(destPath, content);
        }

        // Copy toc.yml if exists
        var tocPath = Path.Combine(sourcePath, "toc.yml");
        if (File.Exists(tocPath))
        {
            var tocContent = await File.ReadAllTextAsync(tocPath);
            WriteFileIfChanged(Path.Combine(outputDir, "toc.yml"), tocContent);
        }
    }

    private async Task GenerateAnalyzerDocsAsync(string repoPath, RepoConfig repo, string outputDir)
    {
        if (repo.AssemblyPath is null)
        {
            Console.WriteLine($"  Warning: AssemblyPath not configured for {repo.Name}");
            return;
        }

        var assemblyPath = Path.Combine(repoPath, repo.AssemblyPath);
        if (!File.Exists(assemblyPath))
        {
            // Try to find the DLL in Release folder (handles different TFMs)
            var assemblyName = Path.GetFileName(repo.AssemblyPath);
            var searchPattern = Path.Combine(repoPath, "**", "bin", "Release", "**", assemblyName);
            var foundFiles = Directory.GetFiles(repoPath, assemblyName, SearchOption.AllDirectories)
                .Where(static f => f.Contains(Path.Combine("bin", "Release")) && !f.Contains("ref"))
                .ToArray();

            if (foundFiles.Length > 0)
            {
                assemblyPath = foundFiles[0];
                Console.WriteLine($"  Found assembly at: {Path.GetRelativePath(repoPath, assemblyPath)}");
            }
            else
            {
                Console.WriteLine($"  Warning: Assembly not found at {assemblyPath}");
                Console.WriteLine($"  Searched for {assemblyName} in {repoPath}");
                return;
            }
        }

        // Load assemblies
        Assembly analyzerAssembly;
        try
        {
            analyzerAssembly = Assembly.LoadFrom(assemblyPath);
        }
        catch (Exception ex) when (ex is BadImageFormatException or FileNotFoundException or FileLoadException)
        {
            Console.WriteLine($"  Error: Failed to load analyzer assembly: {ex.Message}");
            Console.WriteLine($"         Path: {assemblyPath}");
            return;
        }

        Assembly? codeFixAssembly = null;
        if (repo.CodeFixAssemblyPath != null)
        {
            var codeFixPath = Path.Combine(repoPath, repo.CodeFixAssemblyPath);
            if (!File.Exists(codeFixPath))
            {
                // Try to find CodeFix DLL
                var codeFixName = Path.GetFileName(repo.CodeFixAssemblyPath);
                var foundCodeFix = Directory.GetFiles(repoPath, codeFixName, SearchOption.AllDirectories)
                    .Where(f => f.Contains(Path.Combine("bin", "Release")) && !f.Contains("ref"))
                    .FirstOrDefault();
                if (foundCodeFix is not null)
                {
                    codeFixPath = foundCodeFix;
                    Console.WriteLine($"  Found CodeFix at: {Path.GetRelativePath(repoPath, codeFixPath)}");
                }
            }

            if (File.Exists(codeFixPath))
            {
                try
                {
                    codeFixAssembly = Assembly.LoadFrom(codeFixPath);
                }
                catch (Exception ex) when (ex is BadImageFormatException or FileNotFoundException or FileLoadException)
                {
                    Console.WriteLine($"  Warning: Could not load CodeFix assembly: {ex.Message}");
                }
            }
        }

        // Extract analyzers via reflection (Meziantou pattern)
        var analyzers = analyzerAssembly.GetExportedTypes()
            .Where(static t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
            .Select(t =>
            {
                try { return Activator.CreateInstance(t) as DiagnosticAnalyzer; }
                catch { Console.WriteLine($"  Warning: Could not instantiate analyzer {t.Name}"); return null; }
            })
            .OfType<DiagnosticAnalyzer>()
            .ToList();

        var codeFixProviders = codeFixAssembly?.GetExportedTypes()
            .Where(static t => !t.IsAbstract && typeof(CodeFixProvider).IsAssignableFrom(t))
            .Select(t =>
            {
                try { return Activator.CreateInstance(t) as CodeFixProvider; }
                catch { Console.WriteLine($"  Warning: Could not instantiate CodeFix {t.Name}"); return null; }
            })
            .OfType<CodeFixProvider>()
            .ToList() ?? [];

        // Extract refactoring providers (AR* rules like AR0001)
        var refactoringProviders = codeFixAssembly?.GetExportedTypes()
            .Where(static t => !t.IsAbstract && typeof(CodeRefactoringProvider).IsAssignableFrom(t))
            .ToList() ?? [];

        // Generate rules table (README)
        var rulesTable = GenerateRulesTable(analyzers, codeFixProviders);
        var refactoringsTable = GenerateRefactoringsTable(refactoringProviders, repoPath);
        var readmePath = Path.Combine(outputDir, "index.md");
        var readme = $"""
                      ---
                      title: {repo.DisplayName}
                      description: {repo.Description}
                      ---

                      # {repo.DisplayName}

                      {repo.Description}

                      ## Rules

                      {rulesTable}

                      ## Refactorings

                      {refactoringsTable}

                      ## Configuration

                      See [Configuration](./configuration.md) for .editorconfig settings.
                      """;
        WriteFileIfChanged(readmePath, readme);

        // Generate individual rule pages (Meziantou pattern)
        var rulesDir = Path.Combine(outputDir, "rules");
        Directory.CreateDirectory(rulesDir);

        await GenerateIndividualRulePagesAsync(analyzers, codeFixProviders, rulesDir, repoPath);
        await GenerateRefactoringPagesAsync(refactoringProviders, rulesDir, repoPath);

        // Generate rules toc.yml (categorized)
        var rulesToc = GenerateCategorizedRulesToc(analyzers, refactoringProviders);
        WriteFileIfChanged(Path.Combine(rulesDir, "toc.yml"), rulesToc);

        // Generate configuration page
        var configContent = GenerateConfigurationPage(analyzers);
        WriteFileIfChanged(Path.Combine(outputDir, "configuration.md"), configContent);
    }

    private async Task GenerateIndividualRulePagesAsync(
        List<DiagnosticAnalyzer> analyzers,
        List<CodeFixProvider> codeFixProviders,
        string rulesDir,
        string repoPath)
    {
        var processedRules = new HashSet<string>(StringComparer.Ordinal);
        foreach (var analyzer in analyzers)
        foreach (var diagnostic in analyzer.SupportedDiagnostics)
        {
            if (!processedRules.Add(diagnostic.Id))
                continue;

            var rulePage = await GenerateSingleRulePageAsync(
                diagnostic, analyzer, codeFixProviders, repoPath);

            var ruleFilePath = Path.Combine(rulesDir, $"{diagnostic.Id}.md");
            WriteFileIfChanged(ruleFilePath, rulePage);
        }
    }

    private async Task<string> GenerateSingleRulePageAsync(
        DiagnosticDescriptor diagnostic,
        DiagnosticAnalyzer analyzer,
        List<CodeFixProvider> codeFixProviders,
        string repoPath)
    {
        var hasCodeFix = codeFixProviders.Exists(p =>
            p.FixableDiagnosticIds.Contains(diagnostic.Id, StringComparer.Ordinal));

        var rulePage = GenerateRulePage(diagnostic, analyzer, hasCodeFix, repoPath);

        // Check for existing rule page with custom content
        var existingRulePath = Path.Combine(repoPath, "docs", "rules", $"{diagnostic.Id}.md");
        if (File.Exists(existingRulePath))
        {
            var existingContent = await File.ReadAllTextAsync(existingRulePath);
            // Preserve custom content, update title and metadata
            rulePage = MergeRuleContent(rulePage, existingContent);
        }

        return rulePage;
    }

    private async Task GenerateManualDocsAsync(string repoPath, RepoConfig repo, string outputDir)
    {
        // For SDK: generate comprehensive docs from README, eng/ folder structure, etc.
        var readmePath = Path.Combine(repoPath, "README.md");
        if (File.Exists(readmePath))
        {
            var content = await File.ReadAllTextAsync(readmePath);
            WriteFileIfChanged(Path.Combine(outputDir, "index.md"), $"""
                                                                     ---
                                                                     title: {repo.DisplayName}
                                                                     description: {repo.Description}
                                                                     ---

                                                                     {content}
                                                                     """);
        }

        // Generate SDK variants documentation from src/Sdk/
        var sdkDir = Path.Combine(repoPath, "src", "Sdk");
        if (Directory.Exists(sdkDir)) await GenerateSdkVariantsDocsAsync(sdkDir, outputDir);

        // Generate polyfills documentation from eng/LegacySupport/
        var legacySupportDir = Path.Combine(repoPath, "eng", "LegacySupport");
        if (Directory.Exists(legacySupportDir)) await GeneratePolyfillsDocsAsync(legacySupportDir, outputDir);

        // Generate banned APIs documentation
        var bannedApisFile = Path.Combine(repoPath, "src", "configuration", "BannedSymbols.txt");
        if (File.Exists(bannedApisFile)) await GenerateBannedApisDocsAsync(bannedApisFile, outputDir);

        // Generate Service Defaults documentation (Web SDK)
        var serviceDefaultsDir = Path.Combine(repoPath, "eng", "ANcpSdk.AspNetCore.ServiceDefaults");
        if (Directory.Exists(serviceDefaultsDir)) await GenerateServiceDefaultsDocsAsync(serviceDefaultsDir, outputDir);

        // Generate Extensions documentation (FakeLogger, SourceGen, Comparers)
        var extensionsDir = Path.Combine(repoPath, "eng", "Extensions");
        if (Directory.Exists(extensionsDir)) await GenerateExtensionsDocsAsync(extensionsDir, outputDir);

        // Generate Shared utilities documentation (Throw, CodeTests)
        var sharedDir = Path.Combine(repoPath, "eng", "Shared");
        if (Directory.Exists(sharedDir)) await GenerateSharedDocsAsync(sharedDir, outputDir);

        // Generate Configuration files documentation
        var configDir = Path.Combine(repoPath, "src", "configuration");
        if (Directory.Exists(configDir)) await GenerateConfigurationDocsAsync(configDir, outputDir);

        // Generate Testing infrastructure documentation
        var testingSharedDir = Path.Combine(repoPath, "src", "Shared");
        if (Directory.Exists(testingSharedDir)) await GenerateTestingDocsAsync(testingSharedDir, outputDir);

        // Generate MSBuild properties reference
        await GenerateMSBuildPropertiesDocsAsync(repoPath, outputDir);

        // Generate comprehensive SDK toc.yml (only for SDK repo, not utilities)
        if (repo.Name == "sdk")
        {
            GenerateSdkToc(outputDir);
        }
        // For utilities: toc.yml is manually maintained in content/utilities/toc.yml
    }

    private Task GenerateSdkVariantsDocsAsync(string sdkDir, string outputDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: SDK Variants");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# SDK Variants");
        sb.AppendLine();
        sb.AppendLine("ANcpLua.NET.Sdk provides three SDK variants for different project types.");
        sb.AppendLine();
        sb.AppendLine("| Variant | Description |");
        sb.AppendLine("|---------|-------------|");
        sb.AppendLine("| `ANcpLua.NET.Sdk` | Base SDK for libraries, console apps, and workers |");
        sb.AppendLine("| `ANcpLua.NET.Sdk.Web` | Web SDK extending `Microsoft.NET.Sdk.Web` with service defaults |");
        sb.AppendLine("| `ANcpLua.NET.Sdk.Test` | Test SDK with xUnit v3 MTP auto-injection |");
        sb.AppendLine();
        sb.AppendLine("## Usage");
        sb.AppendLine();
        sb.AppendLine("### Base SDK (Libraries/Console)");
        sb.AppendLine();
        sb.AppendLine("```xml");
        sb.AppendLine("<Project Sdk=\"ANcpLua.NET.Sdk\">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <TargetFramework>net10.0</TargetFramework>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("</Project>");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("### Web SDK (ASP.NET Core)");
        sb.AppendLine();
        sb.AppendLine("```xml");
        sb.AppendLine("<Project Sdk=\"ANcpLua.NET.Sdk.Web\">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <TargetFramework>net10.0</TargetFramework>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("</Project>");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("### Test SDK");
        sb.AppendLine();
        sb.AppendLine("```xml");
        sb.AppendLine("<Project Sdk=\"ANcpLua.NET.Sdk.Test\">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <TargetFramework>net10.0</TargetFramework>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("</Project>");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## What Each Variant Provides");
        sb.AppendLine();
        sb.AppendLine("### ANcpLua.NET.Sdk (Base)");
        sb.AppendLine();
        sb.AppendLine("- **Analyzers**: ANcpLua.Analyzers, Meziantou.Analyzer, BannedApiAnalyzers");
        sb.AppendLine("- **Banned APIs**: See [Banned APIs](banned-apis.md) for the full list");
        sb.AppendLine("- **Guard Clauses**: `Throw.IfNull()` (opt-out: `InjectSharedThrow=false`)");
        sb.AppendLine("- **AI Support**: CLAUDE.md generation (opt-out: `GenerateClaudeMd=false`)");
        sb.AppendLine("- **Build Settings**: Nullable, ImplicitUsings, Deterministic, SourceLink");
        sb.AppendLine();
        sb.AppendLine("### ANcpLua.NET.Sdk.Web (adds)");
        sb.AppendLine();
        sb.AppendLine("- **OpenTelemetry**: Logging, metrics, tracing with OTLP export");
        sb.AppendLine("- **Health Endpoints**: `/health` and `/alive`");
        sb.AppendLine("- **HTTP Resilience**: Retries and circuit breakers");
        sb.AppendLine("- **DevLogs**: Browser console to server logs");
        sb.AppendLine("- **See**: [Service Defaults](service-defaults.md) for full configuration");
        sb.AppendLine();
        sb.AppendLine("### ANcpLua.NET.Sdk.Test (adds)");
        sb.AppendLine();
        sb.AppendLine("- **xUnit v3 MTP**: Microsoft Testing Platform runner");
        sb.AppendLine("- **AwesomeAssertions**: Fluent assertion library");
        sb.AppendLine("- **Integration Testing**: WebApplicationFactory base classes");
        sb.AppendLine("- **Analyzer Testing**: Microsoft.CodeAnalysis.Testing packages");

        WriteFileIfChanged(Path.Combine(outputDir, "variants.md"), sb.ToString());
        return Task.CompletedTask;
    }

    private async Task GeneratePolyfillsDocsAsync(string legacySupportDir, string outputDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: Polyfills");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Polyfills");
        sb.AppendLine();
        sb.AppendLine("The SDK automatically injects polyfills for older target frameworks.");
        sb.AppendLine();
        sb.AppendLine("| Feature | Min TFM | Description |");
        sb.AppendLine("|---------|---------|-------------|");

        foreach (var featureDir in Directory.GetDirectories(legacySupportDir).OrderBy(static d => d))
        {
            var name = Path.GetFileName(featureDir);
            var readmePath = Path.Combine(featureDir, "README.md");
            var description = File.Exists(readmePath)
                ? ExtractFirstParagraph(await File.ReadAllTextAsync(readmePath))
                : "No description";
            sb.AppendLine($"| {name} | netstandard2.0 | {description} |");
        }

        WriteFileIfChanged(Path.Combine(outputDir, "polyfills.md"), sb.ToString());
    }

    private async Task GenerateBannedApisDocsAsync(string bannedApisFile, string outputDir)
    {
        var content = await File.ReadAllTextAsync(bannedApisFile);
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: Banned APIs");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Banned APIs");
        sb.AppendLine();
        sb.AppendLine("The following APIs are banned by default in ANcpLua.NET.Sdk projects.");
        sb.AppendLine();
        sb.AppendLine("| API | Reason |");
        sb.AppendLine("|-----|--------|");

        foreach (var line in content.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            var parts = line.Split(';', 2);
            var api = parts[0].Trim();
            var reason = parts.Length > 1 ? parts[1].Trim() : "Banned";
            sb.AppendLine($"| `{EscapeMarkdown(api)}` | {EscapeMarkdown(reason)} |");
        }

        WriteFileIfChanged(Path.Combine(outputDir, "banned-apis.md"), sb.ToString());
    }

    private async Task GenerateServiceDefaultsDocsAsync(string serviceDefaultsDir, string outputDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: Service Defaults (Web SDK)");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Service Defaults");
        sb.AppendLine();
        sb.AppendLine("Opinionated service defaults for ASP.NET Core applications, inspired by .NET Aspire. The Web SDK (`ANcpLua.NET.Sdk.Web`) automatically configures these when you use `WebApplication.CreateBuilder()`.");
        sb.AppendLine();
        sb.AppendLine("## Features");
        sb.AppendLine();
        sb.AppendLine("| Feature | Description |");
        sb.AppendLine("|---------|-------------|");
        sb.AppendLine("| **OpenTelemetry** | Logging, metrics (ASP.NET Core, HTTP, Runtime), tracing with OTLP export |");
        sb.AppendLine("| **Health Checks** | `/health` (readiness) and `/alive` (liveness) endpoints |");
        sb.AppendLine("| **Service Discovery** | Microsoft.Extensions.ServiceDiscovery enabled |");
        sb.AppendLine("| **HTTP Resilience** | Standard resilience handlers with retries and circuit breakers |");
        sb.AppendLine("| **JSON Configuration** | CamelCase naming, enum converters, nullable annotations |");
        sb.AppendLine("| **Security** | Forwarded headers, HTTPS redirect, HSTS, antiforgery |");
        sb.AppendLine("| **Static Assets** | Static file serving with proper caching |");
        sb.AppendLine("| **OpenAPI** | Optional OpenAPI/Swagger documentation |");
        sb.AppendLine("| **DevLogs** | Frontend console log bridge for unified debugging (Development only) |");
        sb.AppendLine();
        sb.AppendLine("## Usage");
        sb.AppendLine();
        sb.AppendLine("```csharp");
        sb.AppendLine("var builder = WebApplication.CreateBuilder(args);");
        sb.AppendLine("builder.UseANcpSdkConventions();");
        sb.AppendLine();
        sb.AppendLine("var app = builder.Build();");
        sb.AppendLine("app.MapANcpSdkDefaultEndpoints();");
        sb.AppendLine("app.Run();");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("> [!NOTE]");
        sb.AppendLine("> When using `ANcpLua.NET.Sdk.Web`, the source generator automatically intercepts `WebApplication.CreateBuilder()` calls, so explicit registration is optional.");
        sb.AppendLine();
        sb.AppendLine("## Configuration");
        sb.AppendLine();
        sb.AppendLine("Customize behavior through the options callback:");
        sb.AppendLine();
        sb.AppendLine("```csharp");
        sb.AppendLine("builder.UseANcpSdkConventions(options =>");
        sb.AppendLine("{");
        sb.AppendLine("    options.Https.Enabled = true;");
        sb.AppendLine("    options.OpenApi.Enabled = true;");
        sb.AppendLine("    options.AntiForgery.Enabled = false;");
        sb.AppendLine("    options.DevLogs.Enabled = true;");
        sb.AppendLine("    options.OpenTelemetry.ConfigureTracing = tracing => tracing.AddSource(\"MyApp\");");
        sb.AppendLine("});");
        sb.AppendLine("```");
        sb.AppendLine();

        // Generate configuration options from source files
        sb.AppendLine("## Configuration Options");
        sb.AppendLine();
        await GenerateConfigOptionsFromSourceAsync(serviceDefaultsDir, sb);

        sb.AppendLine("## DevLogs - Frontend Console Bridge");
        sb.AppendLine();
        sb.AppendLine("Captures browser `console.log/warn/error` and sends to server logs. Enabled by default in Development.");
        sb.AppendLine();
        sb.AppendLine("**Add to your HTML** (only served in Development):");
        sb.AppendLine();
        sb.AppendLine("```html");
        sb.AppendLine("<script src=\"/dev-logs.js\"></script>");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("**All frontend logs appear in server output with `[BROWSER]` prefix:**");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("info: DevLogEntry[0] [BROWSER] User clicked button");
        sb.AppendLine("warn: DevLogEntry[0] [BROWSER] Deprecated API called");
        sb.AppendLine("error: DevLogEntry[0] [BROWSER] Failed to fetch data");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Opt-out");
        sb.AppendLine();
        sb.AppendLine("To disable auto-registration and configure services manually:");
        sb.AppendLine();
        sb.AppendLine("```xml");
        sb.AppendLine("<PropertyGroup>");
        sb.AppendLine("  <AutoRegisterServiceDefaults>false</AutoRegisterServiceDefaults>");
        sb.AppendLine("</PropertyGroup>");
        sb.AppendLine("```");

        WriteFileIfChanged(Path.Combine(outputDir, "service-defaults.md"), sb.ToString());
    }

    private async Task GenerateConfigOptionsFromSourceAsync(string serviceDefaultsDir, StringBuilder sb)
    {
        var configFiles = new[]
        {
            ("ANcpSdkHttpsConfiguration.cs", "Https", "HTTPS and HSTS settings"),
            ("ANcpSdkOpenApiConfiguration.cs", "OpenApi", "OpenAPI/Swagger settings"),
            ("ANcpSdkOpenTelemetryConfiguration.cs", "OpenTelemetry", "Telemetry configuration"),
            ("ANcpSdkAntiForgeryConfiguration.cs", "AntiForgery", "Anti-forgery token settings"),
            ("ANcpSdkStaticAssetsConfiguration.cs", "StaticAssets", "Static file serving"),
            ("ANcpSdkForwardedHeadersConfiguration.cs", "ForwardedHeaders", "Reverse proxy headers"),
            ("ANcpSdkDevLogsConfiguration.cs", "DevLogs", "Browser console bridge")
        };

        foreach (var (fileName, optionName, description) in configFiles)
        {
            var filePath = Path.Combine(serviceDefaultsDir, fileName);
            if (!File.Exists(filePath)) continue;

            var content = await File.ReadAllTextAsync(filePath);
            var properties = ExtractPropertiesFromSource(content);

            if (properties.Count == 0) continue;

            sb.AppendLine($"### {optionName}");
            sb.AppendLine();
            sb.AppendLine(description);
            sb.AppendLine();
            sb.AppendLine("| Property | Type | Default | Description |");
            sb.AppendLine("|----------|------|---------|-------------|");

            foreach (var prop in properties)
            {
                sb.AppendLine($"| `{prop.Name}` | `{prop.Type}` | {prop.Default} | {prop.Description} |");
            }

            sb.AppendLine();
        }
    }

    private static List<(string Name, string Type, string Default, string Description)> ExtractPropertiesFromSource(string content)
    {
        var results = new List<(string Name, string Type, string Default, string Description)>();

        // Match public properties with optional default values - line by line to avoid capturing garbage
        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            var propMatch = Regex.Match(line.Trim(),
                @"^public\s+(?<type>[\w<>?,\s]+?)\s+(?<name>\w+)\s*\{\s*get;\s*set;\s*\}(?:\s*=\s*(?<default>[^;]+))?;?\s*$");

            if (!propMatch.Success) continue;

            var name = propMatch.Groups["name"].Value.Trim();
            var type = propMatch.Groups["type"].Value.Trim();
            var defaultVal = propMatch.Groups["default"].Success ? propMatch.Groups["default"].Value.Trim() : "";

            // Skip internal properties
            if (name == "MapCalled") continue;

            // Clean up type
            type = type.Replace("  ", " ");

            // Clean up default value
            if (string.IsNullOrEmpty(defaultVal) || defaultVal == "new()")
                defaultVal = type.Contains("Action") ? "`null`" : "-";
            else if (defaultVal.StartsWith("new ") || defaultVal.Contains("ForwardedHeaders."))
                defaultVal = "(configured)";
            else if (defaultVal == "true" || defaultVal == "false")
                defaultVal = $"`{defaultVal}`";
            else if (defaultVal.StartsWith("\"") && defaultVal.EndsWith("\""))
                defaultVal = $"`{defaultVal}`";
            else
                defaultVal = $"`{defaultVal}`";

            // Generate description from property name
            var description = name switch
            {
                "Enabled" => "Enable/disable this feature",
                "HstsEnabled" => "Enable HTTP Strict Transport Security",
                "RoutePattern" => "URL route pattern for the endpoint",
                "EnableInProduction" => "Allow in production (security risk)",
                "ForwardedHeaders" => "Which headers to forward from proxy",
                "ConfigureLogging" => "Custom logging configuration callback",
                "ConfigureMetrics" => "Custom metrics configuration callback",
                "ConfigureTracing" => "Custom tracing configuration callback",
                "ConfigureOpenApi" => "Custom OpenAPI options callback",
                "ConfigureJsonOptions" => "Custom JSON serializer options callback",
                "ValidateDependencyContainersOnStartup" => "Validate DI containers at startup",
                _ => $"{name} setting"
            };

            results.Add((name, type, defaultVal, description));
        }

        return results;
    }

    private async Task GenerateExtensionsDocsAsync(string extensionsDir, string outputDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: Extensions");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Extensions");
        sb.AppendLine();
        sb.AppendLine("Opt-in extensions for specialized use cases.");
        sb.AppendLine();

        foreach (var extDir in Directory.GetDirectories(extensionsDir).OrderBy(static d => d))
        {
            var name = Path.GetFileName(extDir);
            var readmePath = Path.Combine(extDir, "README.md");

            sb.AppendLine($"## {name}");
            sb.AppendLine();

            if (File.Exists(readmePath))
            {
                var readme = await File.ReadAllTextAsync(readmePath);
                sb.AppendLine(readme);
            }
            else
            {
                var csFiles = Directory.GetFiles(extDir, "*.cs");
                if (csFiles.Length > 0)
                {
                    sb.AppendLine("Files:");
                    foreach (var csFile in csFiles)
                        sb.AppendLine($"- `{Path.GetFileName(csFile)}`");
                }
            }

            sb.AppendLine();
        }

        sb.AppendLine("## Enabling Extensions");
        sb.AppendLine();
        sb.AppendLine("```xml");
        sb.AppendLine("<PropertyGroup>");
        sb.AppendLine("  <!-- Roslyn source generator utilities -->");
        sb.AppendLine("  <InjectSourceGenHelpers>true</InjectSourceGenHelpers>");
        sb.AppendLine();
        sb.AppendLine("  <!-- FakeLogger for testing -->");
        sb.AppendLine("  <InjectFakeLogger>true</InjectFakeLogger>");
        sb.AppendLine("</PropertyGroup>");
        sb.AppendLine("```");

        WriteFileIfChanged(Path.Combine(outputDir, "extensions.md"), sb.ToString());
    }

    private async Task GenerateSharedDocsAsync(string sharedDir, string outputDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: Shared Utilities");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Shared Utilities");
        sb.AppendLine();
        sb.AppendLine("Utilities automatically injected into all SDK projects.");
        sb.AppendLine();

        foreach (var utilDir in Directory.GetDirectories(sharedDir).OrderBy(static d => d))
        {
            var name = Path.GetFileName(utilDir);
            var readmePath = Path.Combine(utilDir, "README.md");

            sb.AppendLine($"## {name}");
            sb.AppendLine();

            if (File.Exists(readmePath))
            {
                var readme = await File.ReadAllTextAsync(readmePath);
                sb.AppendLine(readme);
            }
            else
            {
                var csFiles = Directory.GetFiles(utilDir, "*.cs");
                foreach (var csFile in csFiles)
                {
                    var fileName = Path.GetFileName(csFile);
                    sb.AppendLine($"### {Path.GetFileNameWithoutExtension(fileName)}");
                    sb.AppendLine();

                    var content = await File.ReadAllTextAsync(csFile);
                    var summaryMatch = Regex.Match(content, @"<summary>\s*(.*?)\s*</summary>", RegexOptions.Singleline);
                    if (summaryMatch.Success)
                        sb.AppendLine(summaryMatch.Groups[1].Value.Trim());

                    sb.AppendLine();
                }
            }
        }

        sb.AppendLine("## Opt-out");
        sb.AppendLine();
        sb.AppendLine("```xml");
        sb.AppendLine("<PropertyGroup>");
        sb.AppendLine("  <InjectSharedThrow>false</InjectSharedThrow>");
        sb.AppendLine("</PropertyGroup>");
        sb.AppendLine("```");

        WriteFileIfChanged(Path.Combine(outputDir, "shared-utilities.md"), sb.ToString());
    }

    private Task GenerateConfigurationDocsAsync(string configDir, string outputDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: Configuration Files");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Configuration Files");
        sb.AppendLine();
        sb.AppendLine("The SDK includes pre-configured settings for analyzers and code style.");
        sb.AppendLine();

        var editorConfigs = Directory.GetFiles(configDir, "*.editorconfig");
        if (editorConfigs.Length > 0)
        {
            sb.AppendLine("## EditorConfig Files");
            sb.AppendLine();
            sb.AppendLine("| File | Purpose |");
            sb.AppendLine("|------|---------|");

            foreach (var file in editorConfigs.OrderBy(static f => f))
            {
                var name = Path.GetFileName(file);
                var purpose = name switch
                {
                    "CodingStyle.editorconfig" => "Code style settings (indentation, spacing, etc.)",
                    "NamingConvention.editorconfig" => "Naming conventions for types, members, parameters",
                    "Analyzers.editorconfig" => "Master analyzer configuration",
                    "GeneratedFiles.editorconfig" => "Suppresses warnings in generated code",
                    _ when name.StartsWith("Analyzer.") => $"Settings for {name.Replace("Analyzer.", "").Replace(".editorconfig", "")}",
                    _ => "Configuration file"
                };
                sb.AppendLine($"| `{name}` | {purpose} |");
            }

            sb.AppendLine();
        }

        var bannedFiles = Directory.GetFiles(configDir, "BannedSymbols*.txt");
        if (bannedFiles.Length > 0)
        {
            sb.AppendLine("## Banned Symbols");
            sb.AppendLine();
            sb.AppendLine("| File | Description |");
            sb.AppendLine("|------|-------------|");

            foreach (var file in bannedFiles.OrderBy(static f => f))
            {
                var name = Path.GetFileName(file);
                var desc = name switch
                {
                    "BannedSymbols.txt" => "Default banned APIs (use TimeProvider instead of legacy time APIs)",
                    _ when name.Contains("Json") => "Bans legacy JSON library in favor of System.Text.Json",
                    _ => "Additional banned APIs"
                };
                sb.AppendLine($"| `{name}` | {desc} |");
            }

            sb.AppendLine();
        }

        var runsettings = Path.Combine(configDir, "default.runsettings");
        if (File.Exists(runsettings))
        {
            sb.AppendLine("## Test Run Settings");
            sb.AppendLine();
            sb.AppendLine("The `default.runsettings` file configures test execution settings.");
            sb.AppendLine();
        }

        WriteFileIfChanged(Path.Combine(outputDir, "configuration-files.md"), sb.ToString());
        return Task.CompletedTask;
    }

    private async Task GenerateTestingDocsAsync(string testingDir, string outputDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: Testing Infrastructure");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Testing Infrastructure");
        sb.AppendLine();
        sb.AppendLine("The SDK provides base classes and utilities for testing.");
        sb.AppendLine();

        var testFiles = Directory.GetFiles(testingDir, "*.cs")
            .Where(static f => !f.Contains("obj") && !f.Contains("bin"))
            .ToArray();

        foreach (var file in testFiles.OrderBy(static f => f))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var content = await File.ReadAllTextAsync(file);

            sb.AppendLine($"## {name}");
            sb.AppendLine();

            var summaryMatch = Regex.Match(content, @"/// <summary>\s*(.*?)\s*/// </summary>", RegexOptions.Singleline);
            if (summaryMatch.Success)
            {
                var summary = Regex.Replace(summaryMatch.Groups[1].Value, @"^\s*///\s*", "", RegexOptions.Multiline).Trim();
                sb.AppendLine(summary);
                sb.AppendLine();
            }

            var classMatch = Regex.Match(content, @"public\s+(abstract\s+)?class\s+(\w+)(?:<[^>]+>)?(?:\s*:\s*([^\{]+))?");
            if (classMatch.Success)
            {
                sb.AppendLine("```csharp");
                sb.AppendLine(classMatch.Value.Trim());
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        WriteFileIfChanged(Path.Combine(outputDir, "testing.md"), sb.ToString());
    }

    private Task GenerateMSBuildPropertiesDocsAsync(string repoPath, string outputDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: MSBuild Properties Reference");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# MSBuild Properties Reference");
        sb.AppendLine();
        sb.AppendLine("Complete reference of all MSBuild properties available in ANcpLua.NET.Sdk.");
        sb.AppendLine();
        sb.AppendLine("## SDK Defaults");
        sb.AppendLine();
        sb.AppendLine("These properties are set automatically by the SDK but can be overridden.");
        sb.AppendLine();
        sb.AppendLine("| Property | Default | Description |");
        sb.AppendLine("|----------|---------|-------------|");
        sb.AppendLine("| `LangVersion` | `latest` | C# language version |");
        sb.AppendLine("| `Nullable` | `enable` | Nullable reference types |");
        sb.AppendLine("| `ImplicitUsings` | `enable` | Implicit global usings |");
        sb.AppendLine("| `Deterministic` | `true` | Reproducible builds |");
        sb.AppendLine("| `EnableNETAnalyzers` | `true` | .NET code analyzers |");
        sb.AppendLine("| `AnalysisLevel` | `latest-all` | Analysis rule severity |");
        sb.AppendLine("| `GenerateDocumentationFile` | `true` | Generate XML docs |");
        sb.AppendLine("| `EnableSourceLink` | `true` | Source Link support |");
        sb.AppendLine("| `PublishRepositoryUrl` | `true` | Include repo URL in NuGet |");
        sb.AppendLine("| `EmbedUntrackedSources` | `true` | Embed sources in PDB |");
        sb.AppendLine("| `EnablePackageValidation` | `true` | NuGet package validation |");
        sb.AppendLine("| `ManagePackageVersionsCentrally` | `true` | Central Package Management |");
        sb.AppendLine("| `TreatWarningsAsErrors` | `true` (CI/Release) | Warnings as errors |");
        sb.AppendLine("| `EnforceCodeStyleInBuild` | `true` (CI/Release) | Code style enforcement |");
        sb.AppendLine();
        sb.AppendLine("## Feature Toggles");
        sb.AppendLine();
        sb.AppendLine("### Core Features");
        sb.AppendLine();
        sb.AppendLine("| Property | Default | Description |");
        sb.AppendLine("|----------|---------|-------------|");
        sb.AppendLine("| `InjectSharedThrow` | `true` | Inject `Throw.IfNull()` guard clauses |");
        sb.AppendLine("| `GenerateClaudeMd` | `true` | Generate CLAUDE.md for AI assistants |");
        sb.AppendLine("| `IncludeDefaultBannedSymbols` | `true` | Include default banned APIs |");
        sb.AppendLine("| `AutoRegisterServiceDefaults` | `true` | Auto-register service defaults (Web SDK) |");
        sb.AppendLine();
        sb.AppendLine("### Polyfills (Opt-in)");
        sb.AppendLine();
        sb.AppendLine("These inject source files for older target frameworks.");
        sb.AppendLine();
        sb.AppendLine("| Property | Default | Description |");
        sb.AppendLine("|----------|---------|-------------|");
        sb.AppendLine("| `InjectTrimAttributesOnLegacy` | `false` | Trimmer/AOT attributes (< net5.0) |");
        sb.AppendLine("| `InjectNullabilityAttributesOnLegacy` | `false` | Nullable reference type attrs (< netcoreapp3.1) |");
        sb.AppendLine("| `InjectIsExternalInitOnLegacy` | `false` | Records support (< net5.0) |");
        sb.AppendLine("| `InjectRequiredMemberOnLegacy` | `false` | `required` keyword support (< net7.0) |");
        sb.AppendLine("| `InjectCallerAttributesOnLegacy` | `false` | CallerArgumentExpression (< net6.0) |");
        sb.AppendLine("| `InjectUnreachableExceptionOnLegacy` | `false` | UnreachableException (< net7.0) |");
        sb.AppendLine("| `InjectExperimentalAttributeOnLegacy` | `false` | ExperimentalAttribute (< net8.0) |");
        sb.AppendLine("| `InjectIndexRangeOnLegacy` | `false` | Index/Range structs (< net5.0) |");
        sb.AppendLine("| `InjectStackTraceHiddenOnLegacy` | `false` | StackTraceHidden attribute |");
        sb.AppendLine("| `InjectLockPolyfill` | `false` | System.Threading.Lock polyfill |");
        sb.AppendLine("| `InjectTimeProviderPolyfill` | `false` | TimeProvider polyfill |");
        sb.AppendLine("| `InjectStringExtensionsPolyfill` | `false` | String.Contains/Replace with StringComparison |");
        sb.AppendLine();
        sb.AppendLine("### Extensions (Opt-in)");
        sb.AppendLine();
        sb.AppendLine("| Property | Default | Description |");
        sb.AppendLine("|----------|---------|-------------|");
        sb.AppendLine("| `InjectSourceGenHelpers` | `false` | EquatableArray, DiagnosticInfo, LocationInfo |");
        sb.AppendLine("| `InjectCommonComparers` | `false` | StringOrdinalComparer |");
        sb.AppendLine("| `InjectFakeLogger` | `false` | FakeLogger test extensions |");
        sb.AppendLine();
        sb.AppendLine("### Bundles (Opt-in)");
        sb.AppendLine();
        sb.AppendLine("| Property | Default | Description |");
        sb.AppendLine("|----------|---------|-------------|");
        sb.AppendLine("| `InjectAllPolyfillsOnLegacy` | `false` | All polyfills based on TFM |");
        sb.AppendLine("| `InjectAllExtensions` | `false` | All extension utilities |");
        sb.AppendLine();
        sb.AppendLine("## Test Project Properties");
        sb.AppendLine();
        sb.AppendLine("These are auto-detected or can be set explicitly.");
        sb.AppendLine();
        sb.AppendLine("| Property | Auto-Detected | Description |");
        sb.AppendLine("|----------|---------------|-------------|");
        sb.AppendLine("| `IsTestProject` | `.Tests`/`.Test` suffix or `tests` folder | Marks as test project |");
        sb.AppendLine("| `IsIntegrationTestProject` | `Integration`/`E2E` in path | Integration test project |");
        sb.AppendLine("| `IsAnalyzerTestProject` | `Analyzer` in name | Analyzer test project |");
        sb.AppendLine("| `SkipXunitInjection` | `false` | Disable xUnit auto-injection |");
        sb.AppendLine("| `InjectIntegrationTestFixtures` | `true` | Inject integration test base classes |");
        sb.AppendLine();
        sb.AppendLine("### Test Framework Defaults");
        sb.AppendLine();
        sb.AppendLine("When `IsTestProject=true`:");
        sb.AppendLine();
        sb.AppendLine("| Property | Value | Description |");
        sb.AppendLine("|----------|-------|-------------|");
        sb.AppendLine("| `OutputType` | `Exe` | Executable for MTP |");
        sb.AppendLine("| `TestingPlatformDotnetTestSupport` | `true` | MTP support |");
        sb.AppendLine("| `UseMicrosoftTestingPlatformRunner` | `true` | Use MTP runner |");
        sb.AppendLine("| `UseMicrosoftTestingPlatform` | `true` | Enable MTP |");
        sb.AppendLine();
        sb.AppendLine("## NuGet Audit Properties");
        sb.AppendLine();
        sb.AppendLine("| Property | Default | Description |");
        sb.AppendLine("|----------|---------|-------------|");
        sb.AppendLine("| `NuGetAudit` | `true` | Enable vulnerability scanning |");
        sb.AppendLine("| `NuGetAuditMode` | `all` | Audit mode (all/direct) |");
        sb.AppendLine("| `NuGetAuditLevel` | `low` | Minimum severity level |");
        sb.AppendLine();
        sb.AppendLine("## Build Optimization");
        sb.AppendLine();
        sb.AppendLine("| Property | Default | Description |");
        sb.AppendLine("|----------|---------|-------------|");
        sb.AppendLine("| `AccelerateBuildsInVisualStudio` | `true` | VS build acceleration |");
        sb.AppendLine("| `ReportAnalyzer` | `true` | Report analyzer performance |");
        sb.AppendLine("| `Features` | `strict` | Enable strict mode |");
        sb.AppendLine();
        sb.AppendLine("## Usage Examples");
        sb.AppendLine();
        sb.AppendLine("### Opt-in to Polyfills");
        sb.AppendLine();
        sb.AppendLine("```xml");
        sb.AppendLine("<PropertyGroup>");
        sb.AppendLine("  <InjectLockPolyfill>true</InjectLockPolyfill>");
        sb.AppendLine("  <InjectTimeProviderPolyfill>true</InjectTimeProviderPolyfill>");
        sb.AppendLine("</PropertyGroup>");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("### Opt-out of Features");
        sb.AppendLine();
        sb.AppendLine("```xml");
        sb.AppendLine("<PropertyGroup>");
        sb.AppendLine("  <GenerateClaudeMd>false</GenerateClaudeMd>");
        sb.AppendLine("  <InjectSharedThrow>false</InjectSharedThrow>");
        sb.AppendLine("  <IncludeDefaultBannedSymbols>false</IncludeDefaultBannedSymbols>");
        sb.AppendLine("</PropertyGroup>");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("### Web SDK Service Defaults");
        sb.AppendLine();
        sb.AppendLine("```xml");
        sb.AppendLine("<PropertyGroup>");
        sb.AppendLine("  <!-- Disable automatic service defaults registration -->");
        sb.AppendLine("  <AutoRegisterServiceDefaults>false</AutoRegisterServiceDefaults>");
        sb.AppendLine("</PropertyGroup>");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("### Test Project Configuration");
        sb.AppendLine();
        sb.AppendLine("```xml");
        sb.AppendLine("<PropertyGroup>");
        sb.AppendLine("  <!-- Use TUnit/NUnit/MSTest instead of xUnit -->");
        sb.AppendLine("  <SkipXunitInjection>true</SkipXunitInjection>");
        sb.AppendLine();
        sb.AppendLine("  <!-- Disable integration test fixture injection -->");
        sb.AppendLine("  <InjectIntegrationTestFixtures>false</InjectIntegrationTestFixtures>");
        sb.AppendLine("</PropertyGroup>");
        sb.AppendLine("```");

        WriteFileIfChanged(Path.Combine(outputDir, "msbuild-properties.md"), sb.ToString());
        return Task.CompletedTask;
    }

    private void GenerateSdkToc(string outputDir)
    {
        var toc = """
                  - name: Overview
                    href: index.md
                  - name: SDK Variants
                    href: variants.md
                  - name: MSBuild Properties
                    href: msbuild-properties.md
                  - name: Service Defaults
                    href: service-defaults.md
                  - name: Polyfills
                    href: polyfills.md
                  - name: Extensions
                    href: extensions.md
                  - name: Shared Utilities
                    href: shared-utilities.md
                  - name: Banned APIs
                    href: banned-apis.md
                  - name: Configuration Files
                    href: configuration-files.md
                  - name: Testing
                    href: testing.md
                  """;
        WriteFileIfChanged(Path.Combine(outputDir, "toc.yml"), toc);
    }

    private void GenerateRepoIndex(RepoConfig repo, string outputDir)
    {
        var tocPath = Path.Combine(outputDir, "toc.yml");
        if (File.Exists(tocPath))
            return; // Already has a toc

        var toc = """
                  - name: Overview
                    href: index.md
                  """;

        if (repo.DocsSource == DocsSourceType.Reflection)
            toc += """

                   - name: Rules
                     href: rules/
                   - name: Configuration
                     href: configuration.md
                   """;

        WriteFileIfChanged(tocPath, toc);
    }

    private void GenerateUnifiedIndex()
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: ANcpLua Framework");
        sb.AppendLine("description: Documentation for the ANcpLua .NET framework");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# ANcpLua Framework");
        sb.AppendLine();
        sb.AppendLine("Welcome to the ANcpLua Framework documentation.");
        sb.AppendLine();
        sb.AppendLine("## Components");
        sb.AppendLine();

        foreach (var repo in _config.Repos)
        {
            sb.AppendLine($"### [{repo.DisplayName}](./{repo.Name}/)");
            sb.AppendLine();
            sb.AppendLine(repo.Description);
            sb.AppendLine();
        }

        sb.AppendLine("## Quick Start");
        sb.AppendLine();
        sb.AppendLine("```xml");
        sb.AppendLine("<Project Sdk=\"ANcpLua.NET.Sdk\">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <TargetFramework>net10.0</TargetFramework>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("</Project>");
        sb.AppendLine("```");

        WriteFileIfChanged(Path.Combine(_outputPath, "index.md"), sb.ToString());
    }

    private void GenerateNavigation()
    {
        var toc = new StringBuilder();
        toc.AppendLine("- name: Home");
        toc.AppendLine("  href: index.md");

        foreach (var repo in _config.Repos)
        {
            toc.AppendLine($"- name: {repo.DisplayName}");
            toc.AppendLine($"  href: {repo.Name}/");
        }

        WriteFileIfChanged(Path.Combine(_outputPath, "toc.yml"), toc.ToString());

        // Generate docfx.json with API metadata configuration
        var docfx = new
        {
            metadata = new object[]
            {
                new
                {
                    src = new[] { new { files = new[] { "ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities/*.csproj" }, src = ".repos/utilities" } },
                    dest = "api/utilities",
                    properties = new { TargetFramework = "netstandard2.0" },
                    disableGitFeatures = false
                },
                new
                {
                    src = new[] { new { files = new[] { "src/ANcpLua.Analyzers/*.csproj", "src/ANcpLua.Analyzers.CodeFixes/*.csproj" }, src = ".repos/analyzers" } },
                    dest = "api/analyzers",
                    properties = new { TargetFramework = "netstandard2.0" },
                    disableGitFeatures = false
                }
            },
            build = new
            {
                content = new object[]
                {
                    new { files = new[] { "**/*.md", "**/toc.yml" }, src = "content" },
                    new { files = new[] { "**/*.yml" }, src = "api" }
                },
                resource = new[]
                {
                    new { files = new[] { ".nojekyll", "logo.svg", "favicon.ico" }, src = "content" }
                },
                dest = "_site",
                globalMetadata = new
                {
                    _appTitle = "ANcpLua Framework",
                    _appFooter = "ANcpLua Framework Documentation",
                    _enableSearch = true
                },
                template = new[] { "default", "modern" },
                xref = new[] { "https://learn.microsoft.com/en-us/dotnet/.xrefmap.json" }
            }
        };

        var docfxJson = JsonSerializer.Serialize(docfx, new JsonSerializerOptions { WriteIndented = true });
        WriteFileIfChanged(Path.Combine(_gitRoot, "docfx.json"), docfxJson);
    }

    private Task GenerateEditorConfigAsync()
    {
        // Generate unified .editorconfig with all analyzer rules
        foreach (var repo in _config.Repos.Where(static r =>
                     r.DocsSource == DocsSourceType.Reflection && r.AssemblyPath is not null))
        {
            if (repo.AssemblyPath is null)
                continue;

            var assemblyPath = Path.Combine(_reposPath, repo.Name, repo.AssemblyPath);
            if (!File.Exists(assemblyPath))
                continue;

            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(assemblyPath);
            }
            catch (Exception ex) when (ex is BadImageFormatException or FileNotFoundException or FileLoadException)
            {
                Console.WriteLine($"  Warning: Could not load assembly for editorconfig: {ex.Message}");
                continue;
            }

            var analyzers = assembly.GetExportedTypes()
                .Where(static t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
                .Select(t =>
                {
                    try { return Activator.CreateInstance(t) as DiagnosticAnalyzer; }
                    catch { return null; }
                })
                .OfType<DiagnosticAnalyzer>()
                .ToList();

            var configDir = Path.Combine(_outputPath, repo.Name, "configuration");
            Directory.CreateDirectory(configDir);

            // Default config
            var defaultConfig = GenerateEditorConfig(analyzers, null);
            WriteFileIfChanged(Path.Combine(configDir, "default.editorconfig"), defaultConfig);

            // None config (all disabled)
            var noneConfig = GenerateEditorConfig(analyzers, "none");
            WriteFileIfChanged(Path.Combine(configDir, "none.editorconfig"), noneConfig);
        }

        return Task.CompletedTask;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helper Methods (Meziantou-style)
    // ═══════════════════════════════════════════════════════════════════════════

    private static string GenerateRulesTable(IEnumerable<DiagnosticAnalyzer> analyzers,
        List<CodeFixProvider> codeFixProviders)
    {
        var sb = new StringBuilder();
        sb.AppendLine("|Id|Category|Description|Severity|Enabled|Code Fix|");
        sb.AppendLine("|--|--------|-----------|:------:|:-----:|:------:|");

        foreach (var diagnostic in analyzers
                     .SelectMany(static a => a.SupportedDiagnostics)
                     .DistinctBy(static d => d.Id)
                     .OrderBy(static d => d.Id, StringComparer.Ordinal))
        {
            var hasCodeFix = codeFixProviders.Exists(p =>
                p.FixableDiagnosticIds.Contains(diagnostic.Id, StringComparer.Ordinal));

            sb.Append($"|[{diagnostic.Id}](./rules/{diagnostic.Id}.md)");
            sb.Append($"|{diagnostic.Category}");
            sb.Append($"|{EscapeMarkdown(diagnostic.Title.ToString(CultureInfo.InvariantCulture))}");
            sb.Append($"|{GetSeverityEmoji(diagnostic.DefaultSeverity)}");
            sb.Append($"|{GetBoolEmoji(diagnostic.IsEnabledByDefault)}");
            sb.Append($"|{GetBoolEmoji(hasCodeFix)}|");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string GenerateRulePage(
        DiagnosticDescriptor diagnostic,
        DiagnosticAnalyzer analyzer,
        bool hasCodeFix,
        string repoPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine(
            $"title: \"{diagnostic.Id} - {EscapeYaml(diagnostic.Title.ToString(CultureInfo.InvariantCulture))}\"");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {diagnostic.Id} - {EscapeMarkdown(diagnostic.Title.ToString(CultureInfo.InvariantCulture))}");
        sb.AppendLine();

        // Source links (Meziantou pattern)
        var analyzerTypeName = analyzer.GetType().Name;
        var sourceUrl = TryFindSourceFile(repoPath, analyzerTypeName);
        if (sourceUrl != null)
        {
            sb.AppendLine($"Source: [{analyzerTypeName}.cs]({sourceUrl})");
            sb.AppendLine();
        }

        sb.AppendLine("## Description");
        sb.AppendLine();
        sb.AppendLine(diagnostic.Description.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine();
        sb.AppendLine("## Properties");
        sb.AppendLine();
        sb.AppendLine($"- **Category**: {diagnostic.Category}");
        sb.AppendLine($"- **Severity**: {diagnostic.DefaultSeverity}");
        sb.AppendLine($"- **Enabled by default**: {diagnostic.IsEnabledByDefault}");
        sb.AppendLine($"- **Code fix available**: {hasCodeFix}");
        sb.AppendLine();
        sb.AppendLine("## Configuration");
        sb.AppendLine();
        sb.AppendLine("```editorconfig");
        sb.AppendLine($"dotnet_diagnostic.{diagnostic.Id}.severity = warning");
        sb.AppendLine("```");

        return sb.ToString();
    }

    private static string MergeRuleContent(string generated, string existing)
    {
        // Keep custom content after the properties section
        var customContentMatch =
            Regex.Match(existing, @"(?<=## Examples|## See Also|## Notes).*", RegexOptions.Singleline);
        if (customContentMatch.Success) return generated + "\n" + customContentMatch.Value;

        return generated;
    }

    private static string GenerateRulesToc(IEnumerable<DiagnosticAnalyzer> analyzers)
    {
        var sb = new StringBuilder();
        sb.AppendLine("- name: Rules");
        sb.AppendLine("  items:");

        foreach (var diagnostic in analyzers
                     .SelectMany(static a => a.SupportedDiagnostics)
                     .DistinctBy(static d => d.Id)
                     .OrderBy(static d => d.Id, StringComparer.Ordinal))
        {
            sb.AppendLine($"  - name: {diagnostic.Id}");
            sb.AppendLine($"    href: {diagnostic.Id}.md");
        }

        return sb.ToString();
    }

    private static string GenerateCategorizedRulesToc(
        IEnumerable<DiagnosticAnalyzer> analyzers,
        List<Type> refactoringProviders)
    {
        var sb = new StringBuilder();
        sb.AppendLine("- name: Rules Index");
        sb.AppendLine("  href: index.md");

        // Group diagnostics by category
        var diagnosticsByCategory = analyzers
            .SelectMany(static a => a.SupportedDiagnostics)
            .DistinctBy(static d => d.Id)
            .GroupBy(static d => d.Category)
            .OrderBy(static g => GetCategoryOrder(g.Key))
            .ToList();

        foreach (var categoryGroup in diagnosticsByCategory)
        {
            sb.AppendLine($"- name: {categoryGroup.Key} Rules");
            sb.AppendLine("  items:");
            foreach (var diagnostic in categoryGroup.OrderBy(static d => d.Id, StringComparer.Ordinal))
            {
                sb.AppendLine($"  - name: {diagnostic.Id}");
                sb.AppendLine($"    href: {diagnostic.Id}.md");
            }
        }

        // Add refactorings section if any exist
        if (refactoringProviders.Count > 0)
        {
            sb.AppendLine("- name: Refactorings");
            sb.AppendLine("  items:");
            foreach (var provider in refactoringProviders.OrderBy(static t => t.Name))
            {
                var refactoringId = ExtractRefactoringId(provider);
                if (refactoringId is not null)
                {
                    sb.AppendLine($"  - name: {refactoringId}");
                    sb.AppendLine($"    href: {refactoringId}.md");
                }
            }
        }

        return sb.ToString();
    }

    private static int GetCategoryOrder(string category)
    {
        return category switch
        {
            "Design" => 0,
            "Reliability" => 1,
            "Usage" => 2,
            "Threading" => 3,
            "OpenTelemetry" => 4,
            "Style" => 5,
            "VersionManagement" => 6,
            "ASP.NET Core" => 7,
            "Performance" => 8,
            _ => 99
        };
    }

    private static string? ExtractRefactoringId(Type providerType)
    {
        // Extract ID from class name pattern:
        // Ar0001SnakeCaseToPascalCaseRefactoring -> AR0001 (note: class uses Ar not AR)
        var match = Regex.Match(providerType.Name, @"^[Aa][Rr](\d{4})", RegexOptions.IgnoreCase);
        return match.Success ? $"AR{match.Groups[1].Value}" : null;
    }

    private static string GenerateRefactoringsTable(List<Type> refactoringProviders, string repoPath)
    {
        if (refactoringProviders.Count == 0)
            return "_No refactorings available._";

        var sb = new StringBuilder();
        sb.AppendLine("|Id|Description|");
        sb.AppendLine("|--|-----------|");

        foreach (var provider in refactoringProviders.OrderBy(static t => t.Name))
        {
            var refactoringId = ExtractRefactoringId(provider);
            if (refactoringId is null) continue;

            var description = ExtractRefactoringDescription(provider);
            sb.AppendLine($"|[{refactoringId}](./rules/{refactoringId}.md)|{EscapeMarkdown(description)}|");
        }

        return sb.ToString();
    }

    private static string ExtractRefactoringDescription(Type providerType)
    {
        // Try to get description from XML doc comment or class name
        var typeName = providerType.Name;
        // Ar0001SnakeCaseToPascalCaseRefactoring -> "Snake Case To Pascal Case"
        var idMatch = Regex.Match(typeName, @"^[Aa][Rr]\d{4}(.+?)Refactoring$", RegexOptions.IgnoreCase);
        if (idMatch.Success)
        {
            var pascalName = idMatch.Groups[1].Value;
            return Regex.Replace(pascalName, "([a-z])([A-Z])", "$1 $2");
        }

        return "Code refactoring";
    }

    private async Task GenerateRefactoringPagesAsync(
        List<Type> refactoringProviders,
        string rulesDir,
        string repoPath)
    {
        foreach (var provider in refactoringProviders)
        {
            var refactoringId = ExtractRefactoringId(provider);
            if (refactoringId is null) continue;

            var description = ExtractRefactoringDescription(provider);
            var sourceUrl = TryFindSourceFile(repoPath, provider.Name);

            var page = new StringBuilder();
            page.AppendLine("---");
            page.AppendLine($"title: \"{refactoringId} - {EscapeYaml(description)}\"");
            page.AppendLine("---");
            page.AppendLine();
            page.AppendLine($"# {refactoringId} - {EscapeMarkdown(description)}");
            page.AppendLine();
            if (sourceUrl is not null)
            {
                page.AppendLine($"Source: [{provider.Name}.cs]({sourceUrl})");
                page.AppendLine();
            }

            page.AppendLine("## Description");
            page.AppendLine();
            page.AppendLine($"This is a code refactoring that {description.ToLowerInvariant()}.");
            page.AppendLine();
            page.AppendLine("## Properties");
            page.AppendLine();
            page.AppendLine("- **Type**: Refactoring (not a diagnostic)");
            page.AppendLine("- **Triggered by**: Right-click context menu or Quick Actions");
            page.AppendLine();
            page.AppendLine("> [!NOTE]");
            page.AppendLine("> Refactorings do not produce diagnostics and cannot be configured via .editorconfig.");

            var filePath = Path.Combine(rulesDir, $"{refactoringId}.md");
            WriteFileIfChanged(filePath, page.ToString());
        }

        await Task.CompletedTask;
    }

    private static string GenerateConfigurationPage(IEnumerable<DiagnosticAnalyzer> analyzers)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: Configuration");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Configuration");
        sb.AppendLine();
        sb.AppendLine("## Default Configuration");
        sb.AppendLine();
        sb.AppendLine("```editorconfig");
        sb.Append(GenerateEditorConfigContent(analyzers, null));
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Disable All Rules");
        sb.AppendLine();
        sb.AppendLine("```editorconfig");
        sb.Append(GenerateEditorConfigContent(analyzers, "none"));
        sb.AppendLine("```");

        return sb.ToString();
    }

    private static string GenerateEditorConfig(IEnumerable<DiagnosticAnalyzer> analyzers, string? overrideSeverity)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Auto-generated by ANcpLua.io DocsGenerator");
        sb.AppendLine("is_global = true");
        sb.AppendLine("global_level = -100");
        sb.AppendLine();
        sb.Append(GenerateEditorConfigContent(analyzers, overrideSeverity));
        return sb.ToString();
    }

    private static string GenerateEditorConfigContent(IEnumerable<DiagnosticAnalyzer> analyzers,
        string? overrideSeverity)
    {
        var sb = new StringBuilder();
        var first = true;

        foreach (var diagnostic in analyzers
                     .SelectMany(static a => a.SupportedDiagnostics)
                     .DistinctBy(static d => d.Id)
                     .OrderBy(static d => d.Id, StringComparer.Ordinal))
        {
            if (!first) sb.AppendLine();
            first = false;

            var severity = overrideSeverity ?? GetSeverityString(diagnostic);
            sb.AppendLine($"# {diagnostic.Id}: {diagnostic.Title}");
            sb.AppendLine($"dotnet_diagnostic.{diagnostic.Id}.severity = {severity}");
        }

        return sb.ToString();
    }

    private static string GetSeverityString(DiagnosticDescriptor diagnostic)
    {
        if (!diagnostic.IsEnabledByDefault)
            return "none";

        return diagnostic.DefaultSeverity switch
        {
            DiagnosticSeverity.Hidden => "silent",
            DiagnosticSeverity.Info => "suggestion",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Error => "error",
            _ => "warning"
        };
    }

    private static string? TryFindSourceFile(string repoPath, string typeName)
    {
        var patterns = new[]
        {
            $"{typeName}.cs",
            $"{typeName}.*.cs"
        };

        foreach (var pattern in patterns)
        {
            var files = Directory.GetFiles(repoPath, pattern, SearchOption.AllDirectories)
                .Where(f => !f.Contains("obj") && !f.Contains("bin"))
                .ToArray();

            if (files.Length == 1)
            {
                var relativePath = Path.GetRelativePath(repoPath, files[0]).Replace('\\', '/');
                return $"https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/{relativePath}";
            }
        }

        return null;
    }

    private static string TransformLinks(string content, string repoName)
    {
        // Transform relative links for unified site
        return Regex.Replace(content, @"\]\(\./(.*?)\)", $"](../{repoName}/$1)");
    }

    private static string ExtractFirstParagraph(string markdown)
    {
        var lines = markdown.Split('\n');
        var sb = new StringBuilder();

        foreach (var line in lines)
        {
            if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line))
            {
                if (sb.Length > 0) break;
                continue;
            }

            sb.Append(line.Trim()).Append(' ');
        }

        var result = sb.ToString().Trim();
        return result.Length > 200 ? result[..197] + "..." : result;
    }

    private void WriteFileIfChanged(string path, string content)
    {
        content = content.ReplaceLineEndings("\n");
        var dir = Path.GetDirectoryName(path);
        if (dir is not null) Directory.CreateDirectory(dir);

        if (File.Exists(path))
        {
            var existing = File.ReadAllText(path).ReplaceLineEndings("\n");
            if (existing.TrimEnd() == content.TrimEnd())
                return;
        }

        File.WriteAllText(path, content, new UTF8Encoding(false));
        _filesWritten++;
        Console.WriteLine($"  {(_filesWritten == 1 ? "Created" : "Updated")}: {Path.GetRelativePath(_gitRoot, path)}");
    }

    private static string GetSeverityEmoji(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Hidden => "👻",
            DiagnosticSeverity.Info => "ℹ️",
            DiagnosticSeverity.Warning => "⚠️",
            DiagnosticSeverity.Error => "❌",
            _ => "?"
        };
    }

    private static string GetBoolEmoji(bool value)
    {
        return value ? "✔️" : "❌";
    }

    private static string EscapeMarkdown(string text)
    {
        return text
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("<", "\\<")
            .Replace(">", "\\>");
    }

    private static string EscapeYaml(string text)
    {
        return text
            .Replace("\"", "\\\"");
    }

    private static async Task RunGitAsync(string workingDir, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = string.Join(' ', args),
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        var process = Process.Start(psi);
        if (process is null)
            throw new InvalidOperationException("Failed to start git process - is git installed?");

        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"Git command failed: git {string.Join(' ', args)}\n" +
                $"Working directory: {workingDir}\n" +
                $"Exit code: {process.ExitCode}\n" +
                $"Error: {error.Trim()}");
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunDotNetWithOutputAsync(string workingDir,
        params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = string.Join(' ', args),
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        var process = Process.Start(psi);
        if (process is null) return (-1, string.Empty, "Failed to start process");

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, output, error);
    }

    private async Task ShowGitDiffAsync()
    {
        Console.WriteLine();
        Console.WriteLine("Changes:");
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "--no-pager diff --stat",
            WorkingDirectory = _gitRoot,
            UseShellExecute = false
        };
        var process = Process.Start(psi);
        if (process is not null) await process.WaitForExitAsync();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Configuration Types
// ═══════════════════════════════════════════════════════════════════════════════

internal enum DocsSourceType
{
    Existing, // Copy from existing docs/ folder
    Reflection, // Generate from assembly reflection (analyzers)
    Manual // Generate from README and code structure
}

internal sealed record DocsConfig
{
    public required List<RepoConfig> Repos { get; init; }
    public required string OutputPath { get; init; }
}

internal sealed record RepoConfig
{
    public required string Name { get; init; }
    public required string GitUrl { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required DocsSourceType DocsSource { get; init; }
    public string? ExistingDocsPath { get; init; }
    public bool GenerateFromAssembly { get; init; }
    public string? AssemblyPath { get; init; }
    public string? CodeFixAssemblyPath { get; init; }
}