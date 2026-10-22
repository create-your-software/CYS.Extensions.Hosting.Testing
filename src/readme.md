# CYS.Extensions.Hosting.Testing

## Introduction

CYS.Extensions.Hosting.Testing provides integration-testing support for applications built on the .NET Generic Host.

Its primary type is `HostApplicationFactory<TEntryPoint>`.

The library is designed around a simple principle:

- The application owns application behavior.
- The test owns the host lifecycle.

`HostApplicationFactory<TEntryPoint>` executes the application's real entry point, captures the constructed host, and allows integration tests to control host startup, shutdown, and inspection without requiring test-only branches in `Program.cs`.

## Key Features

- Executes the application's real entry point.
- Captures the constructed host.
- Supports service overrides during host construction.
- Supports configuration overrides during host construction.
- Controls host startup and shutdown during testing.
- Provides access to application services.
- Requires no test-specific branches in `Program.cs`.

## It Just Works

When an application follows standard .NET Generic Host patterns, `HostApplicationFactory<TEntryPoint>` requires no changes to `Program.cs`.

No testing branch is required.

No alternate startup path is required.

No custom startup implementation is required.

No application code needs to be aware that it is running inside an integration test.

The Program class **MUST NOT** be static.

## Why Was This Library Written?

Many hosted applications ultimately perform finite work:

1. Read input.
2. Process the input.
3. Produce output.
4. Exit.

Traditional integration tests often duplicate startup logic, introduce test-specific entry points, or add code paths that exist solely for testing.

`HostApplicationFactory<TEntryPoint>` takes a different approach.

The factory executes the application's real entry point, captures the constructed host, and transfers lifecycle ownership to the test.

Production startup remains in `Program.cs` while tests gain deterministic control over startup, shutdown, and inspection.

## When Should I Use This?

Use `HostApplicationFactory<TEntryPoint>` when:

- the application uses the .NET Generic Host,
- the application performs finite work,
- tests need access to the application's real host,
- tests need to override services or configuration, and
- startup code should remain in `Program.cs`.

## Installation

```bash
dotnet add package CYS.Extensions.Hosting.Testing
```

## Basic Usage

```csharp
await using var application =
    new HostApplicationFactory<Program>();

await application.RunAsync();
```

## Example: Replacing a Service with a Mock

```csharp
var mock = new Mock<IMyDependency>();

await using var application =
    new HostApplicationFactory<Program>(
        builder =>
        {
            builder.Services.RemoveAll<IMyDependency>();
            builder.Services.AddSingleton(mock.Object);
        });

// arrange

mock
    .Setup(x => x.ProcessAsync())
    .Returns(Task.CompletedTask);

// act

await application.RunAsync();

// assert

mock.Verify(
    x => x.ProcessAsync(),
    Times.Once);
```

## Example: Using WireMock.Net for Input

```csharp
await using var server = WireMockServer.Start();

// arrange

server
    .Given(Request.Create().WithPath("/customers").UsingGet())
    .RespondWith(Response.Create().WithStatusCode(200));

await using var application =
    new HostApplicationFactory<Program>(
        builder =>
        {
            builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["CustomerApi:BaseUrl"] = server.Url,
                });
        });

// act

await application.RunAsync();

// assert

// Verify generated output.
```

## Example: Verifying HTTP Output with WireMock.Net

```csharp
await using var server = WireMockServer.Start();

await using var application =
    new HostApplicationFactory<Program>(
        builder =>
        {
            builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["CustomerApi:BaseUrl"] = server.Url,
                });
        });

// arrange

// act

await application.RunAsync();

// assert

server.ShouldHaveReceived(
    Request.Create()
        .WithPath("/customers")
        .UsingPost());
```

## Host Interception

The factory executes the application's actual entry point and subscribes to hosting diagnostic events.

- `HostBuilding` exposes the `IHostApplicationBuilder`.
- `HostBuilt` exposes the completed `IHost`.

```text
Host.CreateApplicationBuilder(args)
    |
    v
Production registrations
    |
    v
Build()
    |
    v
HostBuilding
    |
    v
Test overrides applied
    |
    v
HostBuilt
    |
    v
Host captured
```

## Completion Model

After startup, the factory observes hosted workloads and waits for finite work to complete.

```text
Program.cs configures services
    |
    v
Factory captures host
    |
    v
Factory calls StartAsync()
    |
    v
Application work completes
    |
    v
Factory calls StopAsync()
    |
    v
Factory disposes host
```

## Design Philosophy

Application code should perform application work.

Integration tests should control application execution.

Hosted services should not be responsible for starting, stopping, or disposing the host.

During production execution, host ownership typically belongs to `Program.cs`.

During integration testing, host ownership belongs to `HostApplicationFactory<TEntryPoint>`.

## Design Goals

- Execute the real application entry point.
- Preserve production configuration.
- Preserve production dependency registration.
- Apply test overrides before service-provider creation.
- Support deterministic integration tests.
- Avoid test-only branches in `Program.cs`.
- Avoid duplicating the application's composition root.
