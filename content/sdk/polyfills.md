---
title: Polyfills
---

# Polyfills

The SDK automatically injects polyfills for older target frameworks.

| Feature | Min TFM | Description |
|---------|---------|-------------|
| DiagnosticAttributes | netstandard2.0 | This directory provides polyfills for various diagnostic attributes, allowing their use in projects targeting older .NET frameworks where these attributes might not be natively supported. |
| Diagnostics | netstandard2.0 | This directory contains diagnostic polyfills and helpers for older .NET target frameworks. |
| Exceptions | netstandard2.0 | This directory provides polyfills for exception types introduced in newer .NET versions. |
| Experimental | netstandard2.0 | This directory contains polyfills for experimental features or attributes. |
| IndexRange | netstandard2.0 | This directory contains polyfills for the `System.Index` and `System.Range` types, enabling their use in projects targeting older .NET frameworks that do not natively support them. |
| IsExternalInit | netstandard2.0 | Enables use of C# record types on older frameworks. |
| LanguageFeatures | netstandard2.0 | This directory provides polyfills for various C# language features, allowing their use in projects targeting older .NET frameworks where these features might not be natively supported. |
| NullabilityAttributes | netstandard2.0 | This directory contains polyfills for various C# nullability-related attributes. |
| StringExtensions | netstandard2.0 | No description |
| TimeProvider | netstandard2.0 | This directory contains a polyfill for the `System.TimeProvider` class, enabling consistent time abstraction in projects targeting older .NET frameworks that do not natively support this API. |
| TrimAttributes | netstandard2.0 | This directory contains polyfills for various .NET trimming and AOT (Ahead-of-Time) compilation related attributes ( e.g., `RequiresUnreferencedCodeAttribute`, `DynamicallyAccessedMembersAttribute`... |
