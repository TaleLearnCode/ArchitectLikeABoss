# Demo Execution Guide: Score the Diamond (Aggregation Pattern)

**Duration:** 10 minutes  
**Pattern:** Aggregation with Timeout Handling & Fallback  
**Date:** March 19, 2026 (Las Vegas)

---

## Pre-Demo Checklist

Before going live on stage, Chad must verify:

- [ ] **Azure Functions deployed** — HeistAggregation project deployed to `https://<function-app>.azurewebsites.net`
- [ ] **Function running** — `AggregateDownstreamServices` function is active
- [ ] **Application Insights connected** — Telemetry shows recent logs
- [ ] **HttpClient configured** — Downstream service URLs are reachable
- [ ] **Timeout handler working** — 2-second timeout is enforced per service
- [ ] **Test trigger succeeds** — Manual trigger returns valid JSON response (see **Trigger Commands** below)
- [ ] **Fallback strategy ready** — Pre-recorded output saved locally (screen recording of successful run)
- [ ] **Network connectivity** — HTTP calls to downstream services can complete within timeout window

**Pre-recorded fallback location:** `C:\Presentations\ArchitectLikeABoss\Assets\demo-fallback-aggregation.mp4`

---

## Trigger Commands & Expected Outputs

### Trigger Command 1: Basic Aggregation Request

**PowerShell Command:**
```powershell
$url = "https://<function-app>.azurewebsites.net/api/aggregate?code=<function-key>"
$headers = @{ "Content-Type" = "application/json" }

$response = Invoke-WebRequest -Uri $url -Method POST -Headers $headers -Body ""
Write-Host "Status: $($response.StatusCode)"
$response.Content | ConvertFrom-Json | ConvertTo-Json
```

**curl equivalent:**
```bash
curl -X POST https://<function-app>.azurewebsites.net/api/aggregate?code=<function-key> \
  -H "Content-Type: application/json" \
  -d "{}"
```

### Expected Response (Success Path)

**Status Code:** 200 OK

**Response Body:**
```json
{
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2026-03-19T19:30:45.123456Z",
  "services": {
    "PaymentService": {
      "serviceName": "PaymentService",
      "success": true,
      "data": "Response from PaymentService",
      "error": null,
      "elapsedMs": 500,
      "usedCache": false
    },
    "InventoryService": {
      "serviceName": "InventoryService",
      "success": true,
      "data": "Cached data for InventoryService",
      "error": null,
      "elapsedMs": 2000,
      "usedCache": true
    },
    "ShippingService": {
      "serviceName": "ShippingService",
      "success": true,
      "data": "Response from ShippingService",
      "error": null,
      "elapsedMs": 1000,
      "usedCache": false
    }
  },
  "elapsedMs": 2500
}
```

**Timing Expectations:**
- **PaymentService:** ~500ms (fast service)
- **InventoryService:** ~2000ms (timeout threshold; falls back to cache)
- **ShippingService:** ~1000ms (normal latency)
- **Total execution time:** ~2500ms (all three run in parallel; total time = max of individual times)

### Expected Response (Timeout with Fallback)

If a service times out and **no cache is available:**

```json
{
  "requestId": "550e8400-e29b-41d4-a716-446655440001",
  "timestamp": "2026-03-19T19:30:48.654321Z",
  "services": {
    "PaymentService": {
      "serviceName": "PaymentService",
      "success": true,
      "data": "Response from PaymentService",
      "error": null,
      "elapsedMs": 500,
      "usedCache": false
    },
    "InventoryService": {
      "serviceName": "InventoryService",
      "success": false,
      "data": null,
      "error": "Timeout after 2000ms and no cached data available",
      "elapsedMs": 2000,
      "usedCache": false
    },
    "ShippingService": {
      "serviceName": "ShippingService",
      "success": true,
      "data": "Response from ShippingService",
      "error": null,
      "elapsedMs": 1000,
      "usedCache": false
    }
  },
  "elapsedMs": 2500
}
```

---

## Code Walkthrough Mapping

### File 1: `HeistAggregation/AggregationFunction.cs`

**Purpose:** HTTP entry point that receives the request and delegates to the service.

**Display lines:** 23–38

**Key sections to highlight:**

1. **Lines 23–26** — HTTP trigger decorator
   - Shows endpoint route: `POST /api/aggregate`
   - Shows authorization level: `Function` (requires API key)
   
2. **Lines 28–31** — Request initialization
   - Generate unique requestId for tracing
   - Call AggregationService to execute the pattern
   
3. **Lines 33–37** — Response formatting
   - Set Content-Type to JSON
   - Serialize result back to client

**Presentation notes:**
- "This HTTP trigger is the entry point. Notice the route is `/api/aggregate` — Chad will call this endpoint."
- "The function generates a unique requestId for distributed tracing, so we can follow the request through Application Insights."
- "It delegates to AggregationService, which contains all the business logic."

---

### File 2: `HeistAggregation/AggregationService.cs`

**Purpose:** Contains the aggregation logic, parallel calls, timeout handling, and fallback.

**Display lines:** 35–74 (Main aggregation flow)

**Key sections to highlight:**

#### Section A: Parallel Service Calls (lines 48–55)
```csharp
var tasks = new[]
{
    CallDownstreamServiceAsync("PaymentService", requestId),
    CallDownstreamServiceAsync("InventoryService", requestId),
    CallDownstreamServiceAsync("ShippingService", requestId)
};

var serviceResults = await Task.WhenAll(tasks);
```

**Presentation notes:**
- "Notice the array of tasks: we're calling three services in **parallel**, not sequentially."
- "Task.WhenAll doesn't complete until all three complete — that's the aggregation pattern."
- "If we called them one-by-one, we'd wait 500ms + 2000ms + 1000ms = 3500ms. But in parallel, we wait for the slowest = ~2000ms."

#### Section B: Timeout Handling (lines 76–149)
Focus on `CallDownstreamServiceAsync` method, particularly lines 82–93 and 112–128.

**Lines 82–93: Timeout enforcement**
```csharp
using var cts = new System.Threading.CancellationTokenSource(TimeoutMs);

var delay = serviceName switch
{
    "PaymentService" => 500,
    "InventoryService" => 2500,  // Will timeout
    "ShippingService" => 1000,
    _ => 1000
};

await Task.Delay(delay, cts.Token);
```

**Presentation notes:**
- "Each service call gets a 2-second timeout using CancellationTokenSource."
- "Notice InventoryService is simulated with 2500ms delay — that's longer than the 2-second timeout."
- "When cts.Token cancels, Task.Delay throws OperationCanceledException."

**Lines 112–128: Fallback strategy**
```csharp
catch (OperationCanceledException)
{
    _logger.LogWarning("Service {ServiceName} timed out after {TimeoutMs}ms", serviceName, TimeoutMs);
    
    if (_cache.TryGetValue(serviceName, out var cached))
    {
        _logger.LogInformation("Falling back to cached data for {ServiceName}", serviceName);
        return new ServiceResult { ... UsedCache = true ... };
    }
    
    return new ServiceResult { ... Error = "Timeout after 2000ms and no cached data available" ... };
}
```

**Presentation notes:**
- "When a timeout occurs, we don't fail the whole aggregation."
- "We check if we have cached data from a previous successful call."
- "If cache exists, we return the cached data (marked `UsedCache: true`)."
- "If no cache, we return an error for that service, but the aggregation still succeeds — other services' data is still useful."

---

### File 3: Data Models (lines 152–175)

Display the response classes:

```csharp
public class AggregationResult
{
    public string RequestId { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, ServiceResult> Services { get; set; }
    public long ElapsedMs { get; set; }
}

public class ServiceResult
{
    public string ServiceName { get; set; }
    public bool Success { get; set; }
    public string? Data { get; set; }
    public string? Error { get; set; }
    public long ElapsedMs { get; set; }
    public bool UsedCache { get; set; }  // ← Key indicator of fallback
}
```

**Presentation notes:**
- "Each ServiceResult tells us: did it succeed? How long did it take? Did we use cached data?"
- "The aggregation groups these results so the client sees the full picture."

---

## Application Insights Monitoring

During the demo, Chad should watch Application Insights for:

1. **Traces tab** — Filter for logs with keyword `Aggregation`
   - Should see: "Starting aggregation for request {RequestId}"
   - Should see: "Calling {ServiceName}" for each service
   - Should see: "Service {ServiceName} succeeded in Xms" or "Service {ServiceName} timed out"
   - Should see: "Falling back to cached data for {ServiceName}" (if timeout occurs)

2. **Performance tab** — Watch Request Details
   - Operation name: `POST /api/aggregate`
   - Request duration: should show ~2500ms for full aggregation
   - Success rate: should be 100% (even if individual services timeout, the aggregation succeeds)

3. **Failures tab** — Should show no failed requests (timeouts are handled gracefully)

---

## Fallback Activation (If Live Fails)

### Scenario 1: HTTP Request Fails to Reach Function

**Symptoms:** PowerShell command times out or returns 500 error.

**Fallback Steps:**
1. **Pause the demo** — "Let me show you what this would look like when run successfully."
2. **Switch display** — Alt+Tab to screen recording of successful previous run
3. **Play video** — Show the expected PowerShell output and Application Insights logs
4. **Continue narration** — Explain the output while video plays
5. **Recover** — After video ends: "That's the aggregation pattern in action. Let me show you how the code handles timeouts..."

### Scenario 2: Timeout Threshold Not Being Hit

**Symptoms:** InventoryService completes in 2000ms instead of exceeding it.

**Fallback Steps:**
1. **Pause** — "In this demo, InventoryService always times out due to external latency."
2. **Show code** — Jump to lines 85–89 of AggregationService.cs
3. **Narrate** — "Notice the delay is 2500ms, which exceeds our 2-second timeout."
4. **Advance** — Skip the live trigger and jump to showing the expected output (previously recorded)

### Scenario 3: Application Insights Not Loading

**Fallback Steps:**
1. **Continue without live metrics** — Show the Application Insights screen recording instead
2. **Narrate** — "We'd see these logs in Application Insights. Here's what they look like..."
3. **Focus on code** — Shift emphasis to code walkthrough (which is not dependent on live execution)

### Recovery & Next Steps

After using a fallback:
- **Do NOT attempt** another live trigger during the demo
- **Continue with code walkthrough** — This is independent of live execution
- **Transition to next pattern** — Move to "Pull the Job" (queue pattern) demo

---

## Demo Script Timeline

| Time | Action | Expected Output |
|------|--------|-----------------|
| 0:00 | Intro: "Score the Diamond - Aggregation Pattern" | Visual slide |
| 0:30 | Show AggregationFunction.cs (lines 23–38) | HTTP trigger visible on screen |
| 1:30 | Show AggregationService.cs (lines 48–55) | Parallel tasks highlighted |
| 2:30 | Show timeout handling (lines 82–93) | CancellationTokenSource visible |
| 3:30 | Show fallback logic (lines 112–128) | Cache lookup visible |
| 4:30 | **LIVE TRIGGER** — Run PowerShell command | JSON response with 3 services |
| 5:30 | Show Application Insights logs | "Starting aggregation..." log entry |
| 6:30 | Highlight timeout: "InventoryService timed out" | "Falling back to cached data" log |
| 7:30 | Show response: `"usedCache": true` | Response JSON highlighted |
| 8:30 | Explain: "Aggregation succeeded despite timeout" | Visual explanation |
| 9:00 | Summary: "Resilience through fallback" | Slide summary |
| 9:30 | Transition to next pattern | Visual transition |

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Function returns 404 | Verify endpoint URL and function key; check deployment status in Azure Portal |
| Timeout too short/long | Modify `TimeoutMs` constant in AggregationService.cs (line 23) and redeploy |
| Application Insights empty | Wait 1–2 minutes for telemetry to appear; refresh browser |
| All services timing out | Reduce `TimeoutMs` to a lower value (e.g., 1000ms) or reduce service delays |
| Cache not being used | Pre-populate cache by running demo once before going live; see code line 22 |

---

## Key Takeaway for Audience

> **"The aggregation pattern shows how to reliably combine data from multiple services. Even if one service times out, we don't fail the entire request. We use cached data as a fallback, ensuring graceful degradation. This is how resilient systems handle partial failures."**
