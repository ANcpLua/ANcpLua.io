---
title: Testing Infrastructure
---

# Testing Infrastructure

The SDK provides base classes and utilities for testing.

## FakeLoggerExtensions

## IntegrationTestBase

Fast integration test base using in-memory TestServer.
Use for most API tests. SDK auto-injects this.

```csharp
public abstract class IntegrationTestBase<TProgram> : IClassFixture<WebApplicationFactory<TProgram>>, IAsyncLifetime
    where TProgram : class
```

## KestrelTestBase

Real Kestrel test base for HTTP/2, WebSockets, SSE, Playwright.
SDK auto-injects this. Uses UseKestrel(0) + StartServer().

```csharp
public abstract class KestrelTestBase<TProgram> : IAsyncLifetime
    where TProgram : class
```

