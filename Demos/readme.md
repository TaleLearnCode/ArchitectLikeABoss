# Serverless Design Pattern Demos

This folder contains three live demonstration projects showcasing key serverless architectural patterns from the "Crack the Code of Serverless Design" presentation.

## Projects

### 1. HeistAggregation
**Pattern:** Aggregation  
**Purpose:** Demonstrate parallel service calls with timeout handling and fallback logic

- HTTP-triggered function receives requests
- Calls 3 downstream services in parallel (Payment, Inventory, Shipping)
- Implements 2-second timeout per service
- Falls back to cached data when services timeout
- Logs all activity for observability

**Key Files:**
- `AggregationService.cs` - Core aggregation logic with timeout & fallback
- `AggregationFunction.cs` - HTTP trigger endpoint
- `Program.cs` - Dependency injection setup

**Build Status:** ✅ Compiles successfully (net8.0, Azure Functions v4)

### 2. HeistQueueLoadLevel
**Pattern:** Queue-Based Load Leveling  
**Purpose:** Demonstrate decoupling request ingestion from processing to handle burst traffic

- HTTP endpoint receives requests and queues them immediately
- Returns 202 Accepted to client (non-blocking)
- Separate Service Bus-triggered worker processes queue at steady rate
- Includes poison message detection and retry logic
- Messages with "POISON" flag are detected and routed to DLQ after 3 retries

**Key Files:**
- `QueueLoadLevelingService.cs` - Queue and message processing logic
- `QueueFunctions.cs` - HTTP trigger (submit) and Service Bus trigger (process)
- `Program.cs` - Dependency injection setup

**Build Status:** ✅ Compiles successfully (net8.0, Azure Functions v4)

### 3. HeistFanOutFanIn
**Pattern:** Fan-Out/Fan-In  
**Purpose:** Demonstrate distributed parallel processing with aggregation of results

- HTTP trigger initiates orchestration
- Orchestrator fans out to 5 parallel activity functions
- Each activity simulates work with variable latency (800-2000ms)
- Orchestrator waits for all activities to complete
- Returns aggregated results with timing metrics

**Key Files:**
- `FanOutFanInService.cs` - Orchestration and activity execution logic
- `FanOutFanInFunctions.cs` - HTTP trigger (orchestrator) and Activity function
- `Program.cs` - Dependency injection setup

**Build Status:** ✅ Compiles successfully (net8.0, Azure Functions v4)

## Building

All projects use .NET 8.0 and Azure Functions Worker (dotnet-isolated runtime).

```bash
# Build all projects
cd Demos
dotnet build

# Build individual project
cd HeistAggregation
dotnet build
```

## Local Testing

Each project includes a `local.settings.json` template. Update with your Azure credentials before running locally.

```bash
# From project directory
func start
```

## Configuration

Each project requires these environment variables:
- `AzureWebJobsStorage` - Storage connection string
- `FUNCTIONS_WORKER_RUNTIME` - Always "dotnet-isolated"
- Service Bus connection strings for queue/messaging demos

## Next Steps

1. **Danny (Architecture):** Provide heist-themed service names and confirm fallback strategies
2. **Linus (Infrastructure):** Set up Azure Service Bus, Application Insights, Storage accounts
3. **Basher (QA):** Create test scenarios and validate all error paths
4. **Rusty (Backend):** Wire up Application Insights, test live scenarios, prepare fallback strategy

See `.squad/decisions/inbox/rusty-implementation-plan.md` for detailed implementation status and blockers.

