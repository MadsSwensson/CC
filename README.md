# Hiring Case: Azure Functions & Service Bus

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Getting Started

### 1. Start the Local Infrastructure

```bash
docker compose up -d
```

This starts:
- **Azure Service Bus Emulator** with pre-configured topics and subscriptions
- **SQL Server** (required by the Service Bus emulator)
- **Azurite** storage emulator (required by Azure Functions runtime)

Wait about 30 seconds for the Service Bus emulator to fully initialize.

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Running

```bash
cd CustomerCase.Functions
func start
```

### 4. Testing

Use the included `requests.http` file (supported by VS Code REST Client, Rider, and Visual Studio).

### 5. Running Tests

```bash
dotnet test
```
