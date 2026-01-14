---
title: Extensions
---

# Extensions

Opt-in extensions for specialized use cases.

## Comparers

Files:
- `StringOrdinalComparer.cs`

## FakeLogger

Files:
- `FakeLoggerExtensions.cs`

## SourceGen

Files:
- `SyntaxValueProvider.cs`
- `DiagnosticsExtensions.cs`

## Enabling Extensions

```xml
<PropertyGroup>
  <!-- Roslyn source generator utilities -->
  <InjectSourceGenHelpers>true</InjectSourceGenHelpers>

  <!-- FakeLogger for testing -->
  <InjectFakeLogger>true</InjectFakeLogger>
</PropertyGroup>
```
