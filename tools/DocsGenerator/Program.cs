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
            DocsSource = DocsSourceType.Existing, // Has docs/ folder
            ExistingDocsPath = "docs/utilities",
            GenerateFromAssembly = true,
            AssemblyPath =
                "ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities/bin/Release/netstandard2.0/ANcpLua.Roslyn.Utilities.dll"
        },
        new RepoConfig
        {
            Name = "analyzers",
            GitUrl = "https://github.com/ANcpLua/ANcpLua.Analyzers.git",
            DisplayName = "ANcpLua.Analyzers",
            Description = "Custom Roslyn analyzers and code fixes",
            DocsSource = DocsSourceType.Reflection, // Generate from assembly reflection
            GenerateFromAssembly = true,
            AssemblyPath = "src/ANcpLua.Analyzers/bin/Release/netstandard2.0/ANcpLua.Analyzers.dll",
            CodeFixAssemblyPath =
                "src/ANcpLua.Analyzers.CodeFixes/bin/Release/netstandard2.0/ANcpLua.Analyzers.CodeFixes.dll"
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

sealed class DocsGenerator(DocsConfig config, string gitRoot)
{
    private readonly string _reposPath = Path.Combine(gitRoot, ".repos");
    private readonly string _outputPath = Path.Combine(gitRoot, config.OutputPath);
    private int _filesWritten;
    private readonly DocsConfig _config = config;
    private readonly string _gitRoot = gitRoot;

    public async Task<int> GenerateAsync()
    {
        Console.WriteLine($"ANcpLua.io Documentation Generator");
        Console.WriteLine($"Output: {_outputPath}");
        Console.WriteLine();

        // Step 1: Clone/update repos
        await FetchReposAsync();

        // Step 2: Build assemblies for reflection
        await BuildAssembliesAsync();

        // Step 3: Generate docs for each repo
        foreach (var repo in _config.Repos)
        {
            await GenerateRepoDocsAsync(repo);
        }

        // Step 4: Generate unified index and navigation
        GenerateUnifiedIndex();
        GenerateNavigation();

        // Step 5: Generate editorconfig files
        await GenerateEditorConfigAsync();

        Console.WriteLine();
        Console.WriteLine($"Generated {_filesWritten} file(s)");

        if (_filesWritten > 0)
        {
            await ShowGitDiffAsync();
        }

        return _filesWritten;
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
            await RunDotNetAsync(repoPath, "build", "-c", "Release", "-v", "q", "--nologo");
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
            if (destDir is not null)
            {
                Directory.CreateDirectory(destDir);
            }

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
            Console.WriteLine($"  Warning: Assembly not found at {assemblyPath}");
            return;
        }

        // Load assemblies
        var analyzerAssembly = Assembly.LoadFrom(assemblyPath);
        Assembly? codeFixAssembly = null;
        if (repo.CodeFixAssemblyPath != null)
        {
            var codeFixPath = Path.Combine(repoPath, repo.CodeFixAssemblyPath);
            if (File.Exists(codeFixPath))
            {
                codeFixAssembly = Assembly.LoadFrom(codeFixPath);
            }
        }

        // Extract analyzers via reflection (Meziantou pattern)
        var analyzers = analyzerAssembly.GetExportedTypes()
            .Where(static t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
            .Select(static t => Activator.CreateInstance(t))
            .OfType<DiagnosticAnalyzer>()
            .ToList();

        var codeFixProviders = codeFixAssembly?.GetExportedTypes()
            .Where(static t => !t.IsAbstract && typeof(CodeFixProvider).IsAssignableFrom(t))
            .Select(static t => Activator.CreateInstance(t))
            .OfType<CodeFixProvider>()
            .ToList() ?? [];

        // Generate rules table (README)
        var rulesTable = GenerateRulesTable(analyzers, codeFixProviders);
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

                      ## Configuration

                      See [Configuration](./configuration.md) for .editorconfig settings.
                      """;
        WriteFileIfChanged(readmePath, readme);

        // Generate individual rule pages (Meziantou pattern)
        var rulesDir = Path.Combine(outputDir, "rules");
        Directory.CreateDirectory(rulesDir);

        await GenerateIndividualRulePagesAsync(analyzers, codeFixProviders, rulesDir, repoPath);

        // Generate rules toc.yml
        var rulesToc = GenerateRulesToc(analyzers);
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
        {
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
        // For SDK: generate docs from README, eng/ folder structure, etc.
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
        if (Directory.Exists(sdkDir))
        {
            await GenerateSdkVariantsDocsAsync(sdkDir, outputDir);
        }

        // Generate polyfills documentation from eng/LegacySupport/
        var legacySupportDir = Path.Combine(repoPath, "eng", "LegacySupport");
        if (Directory.Exists(legacySupportDir))
        {
            await GeneratePolyfillsDocsAsync(legacySupportDir, outputDir);
        }

        // Generate banned APIs documentation
        var bannedApisFile = Path.Combine(repoPath, "src", "configuration", "BannedSymbols.txt");
        if (File.Exists(bannedApisFile))
        {
            await GenerateBannedApisDocsAsync(bannedApisFile, outputDir);
        }
    }

    private async Task GenerateSdkVariantsDocsAsync(string sdkDir, string outputDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: SDK Variants");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# SDK Variants");
        sb.AppendLine();
        sb.AppendLine("| Variant | Description |");
        sb.AppendLine("|---------|-------------|");

        foreach (var variant in Directory.GetDirectories(sdkDir))
        {
            var name = Path.GetFileName(variant);
            var readmePath = Path.Combine(variant, "Readme.md");
            var description = File.Exists(readmePath)
                ? ExtractFirstParagraph(await File.ReadAllTextAsync(readmePath))
                : "No description";
            sb.AppendLine($"| `{name}` | {description} |");
        }

        WriteFileIfChanged(Path.Combine(outputDir, "variants.md"), sb.ToString());
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

    private void GenerateRepoIndex(RepoConfig repo, string outputDir)
    {
        var tocPath = Path.Combine(outputDir, "toc.yml");
        if (File.Exists(tocPath))
            return; // Already has a toc

        var toc = $"""
                   - name: Overview
                     href: index.md
                   """;

        if (repo.DocsSource == DocsSourceType.Reflection)
        {
            toc += """

                   - name: Rules
                     href: rules/
                   - name: Configuration
                     href: configuration.md
                   """;
        }

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

        // Generate docfx.json
        var docfx = new
        {
            metadata = Array.Empty<object>(),
            build = new
            {
                content = new[]
                {
                    new { files = new[] { "**/*.md", "**/toc.yml" }, src = "content" }
                },
                dest = "_site",
                globalMetadata = new
                {
                    _appTitle = "ANcpLua Framework",
                    _appFooter = "ANcpLua Framework Documentation"
                },
                template = new[] { "default", "modern" }
            }
        };

        var docfxJson = JsonSerializer.Serialize(docfx, new JsonSerializerOptions { WriteIndented = true });
        WriteFileIfChanged(Path.Combine(_gitRoot, "docfx.json"), docfxJson);
    }

    private Task GenerateEditorConfigAsync()
    {
        // Generate unified .editorconfig with all analyzer rules
        foreach (var repo in _config.Repos.Where(static r => r.DocsSource == DocsSourceType.Reflection && r.AssemblyPath is not null))
        {
            if (repo.AssemblyPath is null)
                continue;

            var assemblyPath = Path.Combine(_reposPath, repo.Name, repo.AssemblyPath);
            if (!File.Exists(assemblyPath))
                continue;

            var assembly = Assembly.LoadFrom(assemblyPath);
            var analyzers = assembly.GetExportedTypes()
                .Where(static t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
                .Select(static t => Activator.CreateInstance(t))
                .OfType<DiagnosticAnalyzer>()
                .ToList();

            var configDir = Path.Combine(_outputPath, repo.Name, "configuration");
            Directory.CreateDirectory(configDir);

            // Default config
            var defaultConfig = GenerateEditorConfig(analyzers, overrideSeverity: null);
            WriteFileIfChanged(Path.Combine(configDir, "default.editorconfig"), defaultConfig);

            // None config (all disabled)
            var noneConfig = GenerateEditorConfig(analyzers, overrideSeverity: "none");
            WriteFileIfChanged(Path.Combine(configDir, "none.editorconfig"), noneConfig);
        }

        return Task.CompletedTask;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helper Methods (Meziantou-style)
    // ═══════════════════════════════════════════════════════════════════════════

    private static string GenerateRulesTable(IEnumerable<DiagnosticAnalyzer> analyzers, List<CodeFixProvider> codeFixProviders)
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
        if (customContentMatch.Success)
        {
            return generated + "\n" + customContentMatch.Value;
        }

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

    private static string GenerateEditorConfigContent(IEnumerable<DiagnosticAnalyzer> analyzers, string? overrideSeverity)
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
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
        }

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

    private static string GetSeverityEmoji(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Hidden => "👻",
        DiagnosticSeverity.Info => "ℹ️",
        DiagnosticSeverity.Warning => "⚠️",
        DiagnosticSeverity.Error => "❌",
        _ => "?"
    };

    private static string GetBoolEmoji(bool value) => value ? "✔️" : "❌";

    private static string EscapeMarkdown(string text) => text
        .Replace("[", "\\[")
        .Replace("]", "\\]")
        .Replace("<", "\\<")
        .Replace(">", "\\>");

    private static string EscapeYaml(string text) => text
        .Replace("\"", "\\\"");

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
        if (process is not null)
        {
            await process.WaitForExitAsync();
        }
    }

    private static async Task RunDotNetAsync(string workingDir, params string[] args)
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
        if (process is not null)
        {
            await process.WaitForExitAsync();
        }
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
        if (process is not null)
        {
            await process.WaitForExitAsync();
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Configuration Types
// ═══════════════════════════════════════════════════════════════════════════════

enum DocsSourceType
{
    Existing, // Copy from existing docs/ folder
    Reflection, // Generate from assembly reflection (analyzers)
    Manual // Generate from README and code structure
}

sealed record DocsConfig
{
    public required List<RepoConfig> Repos { get; init; }
    public required string OutputPath { get; init; }
}

sealed record RepoConfig
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