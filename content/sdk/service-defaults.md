---
title: Service Defaults (Web SDK)
---

# Service Defaults

The Web SDK (`ANcpLua.NET.Sdk.Web`) automatically configures common ASP.NET Core services.

## Features

- **OpenTelemetry**: OpenTelemetry (logging, metrics, tracing with OTLP export)
- **DevLogs**: DevLogs (browser console to server logs)
- **Https**: HTTPS redirection and HSTS
- **ForwardedHeaders**: Forwarded headers for reverse proxies
- **AntiForgery**: Anti-forgery token configuration
- **StaticAssets**: Static file serving with proper caching
- **OpenApi**: OpenAPI/Swagger documentation

## Usage

Service defaults are automatically registered when using `ANcpLua.NET.Sdk.Web`.
The source generator intercepts `WebApplication.CreateBuilder()` calls.

```csharp
// This call is automatically enhanced by the SDK
var builder = WebApplication.CreateBuilder(args);
```

## Opt-out

```xml
<PropertyGroup>
  <AutoRegisterServiceDefaults>false</AutoRegisterServiceDefaults>
</PropertyGroup>
```

## Details

# ANcpSdk.AspNetCore.ServiceDefaults

Opinionated service defaults for ASP.NET Core applications, inspired by .NET Aspire.

## Features

- **OpenTelemetry**: Logging, metrics (ASP.NET Core, HTTP, Runtime), tracing with OTLP export
- **Health Checks**: `/health` (readiness) and `/alive` (liveness) endpoints
- **Service Discovery**: Microsoft.Extensions.ServiceDiscovery enabled
- **HTTP Resilience**: Standard resilience handlers with retries and circuit breakers
- **JSON Configuration**: CamelCase naming, enum converters, nullable annotations
- **Security**: Forwarded headers, HTTPS redirect, HSTS, antiforgery
- **OpenAPI**: Optional OpenAPI document generation
- **DevLogs**: Frontend console log bridge for unified debugging (Development only)

## Usage

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.UseANcpSdkConventions();

var app = builder.Build();
app.MapANcpSdkDefaultEndpoints();
app.Run();
```

## Configuration

```csharp
builder.UseANcpSdkConventions(options =>
{
    options.Https.Enabled = true;
    options.OpenApi.Enabled = true;
    options.AntiForgery.Enabled = false;
    options.DevLogs.Enabled = true; // Default: true in Development
    options.OpenTelemetry.ConfigureTracing = tracing => tracing.AddSource("MyApp");
});
```

## DevLogs - Frontend Console Bridge

Captures browser `console.log/warn/error` and sends to server logs. Enabled by default in Development.

**Add to your HTML** (only served in Development):

```html
<script src="/dev-logs.js"></script>
```

**All frontend logs appear in server output with `[BROWSER]` prefix:**

```
info: DevLogEntry[0] [BROWSER] User clicked button
warn: DevLogEntry[0] [BROWSER] Deprecated API called
error: DevLogEntry[0] [BROWSER] Failed to fetch data
```

**Configuration:**

```csharp
options.DevLogs.Enabled = true;           // Default: true
options.DevLogs.RoutePattern = "/api/dev-logs"; // Default
options.DevLogs.EnableInProduction = false;     // Default: false
```

## Auto-Registration

When used with `ANcpLua.NET.Sdk.Web`, service defaults are auto-registered via source generation.
Opt-out: `<AutoRegisterServiceDefaults>false</AutoRegisterServiceDefaults>`

## License

MIT
