# Fencemark

A distributed web application built with [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview), featuring a Blazor Server frontend and an ASP.NET Core API backend.

## Overview

Fencemark is a modern cloud-native application that demonstrates the power of .NET Aspire for building distributed systems. It consists of:

- **Web Frontend** - A Blazor Server application with interactive components
- **API Service** - An ASP.NET Core minimal API with OpenAPI documentation
- **Service Defaults** - Shared service configuration for observability and resilience
- **App Host** - The orchestration layer that manages the distributed application

```
┌─────────────────────────────────────────────────────────┐
│                      App Host                           │
│  ┌─────────────────────┐    ┌─────────────────────┐    │
│  │    Web Frontend     │───▶│     API Service     │    │
│  │   (Blazor Server)   │    │   (Minimal API)     │    │
│  └─────────────────────┘    └─────────────────────┘    │
│              │                        │                 │
│              └────────┬───────────────┘                 │
│                       ▼                                 │
│              ┌─────────────────┐                        │
│              │ Service Defaults│                        │
│              └─────────────────┘                        │
└─────────────────────────────────────────────────────────┘
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [.NET Aspire workload](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling)

To install the Aspire workload:

```bash
dotnet workload install aspire
```

## Getting Started

### Clone the repository

```bash
git clone https://github.com/your-username/fencemark.git
cd fencemark
```

### Run the application

Start the application using the App Host:

```bash
dotnet run --project fencemark.AppHost
```

The Aspire dashboard will open in your browser, providing:
- Real-time logs from all services
- Distributed tracing
- Health check status
- Service endpoints

> [!TIP]
> The web frontend is available at the URL shown in the Aspire dashboard under the `webfrontend` resource.

## Project Structure

| Project | Description |
|---------|-------------|
| `fencemark.AppHost` | Aspire orchestration host that coordinates all services |
| `fencemark.Web` | Blazor Server frontend with interactive components |
| `fencemark.ApiService` | ASP.NET Core minimal API backend |
| `fencemark.ServiceDefaults` | Shared configuration for OpenTelemetry, health checks, and resilience |
| `fencemark.Tests` | Integration tests using Aspire's distributed testing framework |

## Running Tests

Run the integration tests:

```bash
dotnet test
```

The tests use Aspire's `DistributedApplicationTestingBuilder` to spin up the full application for integration testing.

## Features

- **Service Discovery** - Automatic service discovery between components using Aspire
- **Health Checks** - Built-in health endpoints at `/health` for each service
- **OpenAPI** - API documentation available in development mode
- **Output Caching** - Response caching in the web frontend
- **Resilience** - HTTP client resilience patterns for service-to-service communication

## Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor)
- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
