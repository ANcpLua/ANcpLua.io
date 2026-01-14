# Analyzer Rules

ANcpLua.Analyzers provides 24 diagnostic rules and 1 refactoring for C# code quality, organized by category.

## Design Rules

| Rule | Severity | Description |
|------|----------|-------------|
| [AL0001](AL0001.md) | Error | Prohibit reassignment of primary constructor params |
| [AL0002](AL0002.md) | Warning | Don't repeat negated patterns |
| [AL0006](AL0006.md) | Warning | Field name conflicts with primary constructor parameter |
| [AL0010](AL0010.md) | Info | Type should be partial for source generator support |

## Reliability Rules

| Rule | Severity | Description |
|------|----------|-------------|
| [AL0003](AL0003.md) | Error | Don't divide by constant zero |

## Usage Rules

| Rule | Severity | Description |
|------|----------|-------------|
| [AL0004](AL0004.md) | Warning | Use pattern matching for Span constant comparison |
| [AL0005](AL0005.md) | Warning | Use SequenceEqual for Span non-constant comparison |
| [AL0007](AL0007.md) | Warning | GetSchema should be explicitly implemented |
| [AL0008](AL0008.md) | Warning | GetSchema must return null and not be abstract |
| [AL0009](AL0009.md) | Warning | Don't call IXmlSerializable.GetSchema |
| [AL0025](AL0025.md) | Info | Anonymous function can be made static |

## Threading Rules

| Rule | Severity | Description |
|------|----------|-------------|
| [AL0011](AL0011.md) | Warning | Avoid lock keyword on non-Lock types (.NET 9+) |

## OpenTelemetry Rules

| Rule | Severity | Description |
|------|----------|-------------|
| [AL0012](AL0012.md) | Warning | Deprecated OTel semantic convention attribute |
| [AL0013](AL0013.md) | Info | Missing telemetry schema URL |

## Style Rules

| Rule | Severity | Description |
|------|----------|-------------|
| [AL0014](AL0014.md) | Info | Prefer pattern matching for null/zero comparisons |
| [AL0015](AL0015.md) | Info | Normalize null-guard style |
| [AL0016](AL0016.md) | Info | Combine declaration with subsequent null-check |

## Version Management Rules

| Rule | Severity | Description |
|------|----------|-------------|
| [AL0017](AL0017.md) | Warning | Hardcoded package version in Directory.Packages.props |

## ASP.NET Core Rules

| Rule | Severity | Description |
|------|----------|-------------|
| [AL0020](AL0020.md) | Error | IFormCollection requires explicit [FromForm] attribute |
| [AL0021](AL0021.md) | Error | Multiple structured form sources conflict |
| [AL0022](AL0022.md) | Error | Mixed IFormCollection and DTO form binding unsupported |
| [AL0023](AL0023.md) | Error | Type cannot be form-bound (abstract, interface, no constructor) |
| [AL0024](AL0024.md) | Error | [FromForm] and [FromBody] conflict on same method |

## Refactorings

| Rule | Description |
|------|-------------|
| [AR0001](AR0001.md) | Convert to primary constructor |

## Configuration

All rules can be configured via `.editorconfig`:

```ini
[*.cs]
# Disable a rule
dotnet_diagnostic.AL0001.severity = none

# Change severity
dotnet_diagnostic.AL0014.severity = warning

# Enable OpenTelemetry rules as errors in production code
[src/**/*.cs]
dotnet_diagnostic.AL0012.severity = error
dotnet_diagnostic.AL0013.severity = warning

# Relax ASP.NET Core rules in test projects
[tests/**/*.cs]
dotnet_diagnostic.AL0020.severity = none
```

## Rule Severity Levels

| Severity | Build Behavior | Use For |
|----------|----------------|---------|
| Error | Breaks build | Critical issues, runtime exceptions |
| Warning | Shows in build output | Important issues to address |
| Info | IDE only by default | Suggestions and improvements |
| None | Disabled | Rules not applicable to project |
