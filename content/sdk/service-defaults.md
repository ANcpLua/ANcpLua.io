---
title: Service Defaults (Web SDK)
---

# Service Defaults

Opinionated service defaults for ASP.NET Core applications, inspired by .NET Aspire. The Web SDK (`ANcpLua.NET.Sdk.Web`) automatically configures these when you use `WebApplication.CreateBuilder()`.

## Features

| Feature | Description |
|---------|-------------|
| **OpenTelemetry** | Logging, metrics (ASP.NET Core, HTTP, Runtime), tracing with OTLP export |
| **Health Checks** | `/health` (readiness) and `/alive` (liveness) endpoints |
| **Service Discovery** | Microsoft.Extensions.ServiceDiscovery enabled |
| **HTTP Resilience** | Standard resilience handlers with retries and circuit breakers |
| **JSON Configuration** | CamelCase naming, enum converters, nullable annotations |
| **Security** | Forwarded headers, HTTPS redirect, HSTS, antiforgery |
| **Static Assets** | Static file serving with proper caching |
| **OpenAPI** | Optional OpenAPI/Swagger documentation |
| **DevLogs** | Frontend console log bridge for unified debugging (Development only) |

## Usage

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.UseANcpSdkConventions();

var app = builder.Build();
app.MapANcpSdkDefaultEndpoints();
app.Run();
```

> [!NOTE]
> When using `ANcpLua.NET.Sdk.Web`, the source generator automatically intercepts `WebApplication.CreateBuilder()` calls, so explicit registration is optional.

## Configuration

Customize behavior through the options callback:

```csharp
builder.UseANcpSdkConventions(options =>
{
    options.Https.Enabled = true;
    options.OpenApi.Enabled = true;
    options.AntiForgery.Enabled = false;
    options.DevLogs.Enabled = true;
    options.OpenTelemetry.ConfigureTracing = tracing => tracing.AddSource("MyApp");
});
```

## Configuration Options

### Https

HTTPS and HSTS settings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Enable/disable this feature |
| `HstsEnabled` | `bool` | `true` | Enable HTTP Strict Transport Security |

### OpenApi

OpenAPI/Swagger settings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Enable/disable this feature |
| `ConfigureOpenApi` | `Action<OpenApiOptions>?` | `null` | Custom OpenAPI options callback |

### OpenTelemetry

Telemetry configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConfigureLogging` | `Action<OpenTelemetryLoggerOptions>?` | `null` | Custom logging configuration callback |
| `ConfigureMetrics` | `Action<MeterProviderBuilder>?` | `null` | Custom metrics configuration callback |
| `ConfigureTracing` | `Action<TracerProviderBuilder>?` | `null` | Custom tracing configuration callback |

### AntiForgery

Anti-forgery token settings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Enable/disable this feature |

### StaticAssets

Static file serving

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Enable/disable this feature |

### DevLogs

Browser console bridge

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Enable/disable this feature |
| `RoutePattern` | `string` | `"/api/dev-logs"` | URL route pattern for the endpoint |
| `EnableInProduction` | `bool` | - | Allow in production (security risk) |

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

## Opt-out

To disable auto-registration and configure services manually:

```xml
<PropertyGroup>
  <AutoRegisterServiceDefaults>false</AutoRegisterServiceDefaults>
</PropertyGroup>
```
