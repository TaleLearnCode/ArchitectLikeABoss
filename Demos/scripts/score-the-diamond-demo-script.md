# "Score the Diamond" — Aggregation Pattern Demo Script
## 10-Minute Live Demo

---

## OPENING NARRATION [0:00 - 0:30]

*[Stand center stage. Confident, conversational tone. Heist metaphor]*

**[0:00]** "In Ocean's Eleven, Danny Ocean didn't just hit one vault—he coordinated a hit on the entire casino floor. Our services often work the same way: we need to pull data from multiple sources, fast, but some sources might be slow or fail."

**[0:15]** "What you're about to see is a real-world version: we're 'scoring the diamond'—pulling payment, inventory, and shipping info simultaneously. But here's the catch: if any service takes too long or fails, we don't wait forever. We have a backup plan: cached data."

**[0:25]** "In the next 10 minutes, you'll see timeout handling, graceful fallback, and parallel execution—all the resilience patterns you need for production systems."

---

## SETUP & PRE-FLIGHT CHECKS [0:30 - 1:30]

**[0:30] - CLICK: Open VS Code with code visible**

*Display the aggregation code locally:*
- Open VS Code with `AggregationService.cs` visible
- Show the three service calls and timeout logic
- [NOTE: We're showing code locally but will execute in Azure]

**[0:35] - OBSERVE: Function deployed**
- Show `AggregateDownstreamServices` function is active
- Point out: HTTP POST endpoint at `/api/aggregate`
- [NOTE: If Portal is slow, have Portal tab pre-loaded in background]

**[0:40] - CLICK: Open Application Insights**

*Navigate to Application Insights workspace (get name from Linus).*

**[0:45] - OBSERVE: Live Logs view ready**
- Show that Logs window is open and ready to stream
- Say: "We'll see three key streams here: the aggregation start, individual service calls, and timeout/fallback events"
- [NOTE: If logs don't appear immediately, they will stream in real-time during demo execution]

**[1:00] - CLICK: Prepare Azure Function URL**

*Have the Azure Function endpoint ready:*
- Production URL: Copy from Azure Portal Function App
- Format: `https://func-score-diamond-dev.azurewebsites.net/api/aggregate`

**[1:10] - CALL OUT:**
- "Notice the three services we'll call: PaymentService (fast—500ms), InventoryService (slow—2500ms, intentionally times out), ShippingService (normal—1000ms)"
- "2-second timeout per service. Any service not back by then gets fallback to cache"

**[1:20] - CLICK: Open Postman or REST Client**

*Have the POST call ready for the Azure Function:*
```
POST https://func-score-diamond-dev.azurewebsites.net/api/aggregate
Content-Type: application/json

{ "correlationId": "heist-2026" }
```

**[1:30] - Say:** "Infrastructure ready. Code visible. Logs streaming. Let's pull the job."

---

## INFRASTRUCTURE SHOWCASE [1:30 - 3:00]

**[1:30] - CLICK: Open Azure Portal**

*Navigate to: https://portal.azure.com/#resource/subscriptions/{subid}/resourceGroups/rg-heist-dev-eus/overview*

**[1:35] - SAY:**
"Before we execute the code, let me show you the actual Azure infrastructure behind this demo. This isn't just theoretical—these resources are live and running in Azure right now."

**[1:40] - OBSERVE: Resource Group Overview**

*Show `rg-heist-dev-eus` resource group with deployed resources:*
- Function App: `func-score-diamond-dev`
- Storage Account: `stheistvaultdev`
- Application Insights: `appinsights-heist-dev`

**[1:45] - CLICK: Open Storage Account → Containers**

*Navigate to `stheistvaultdev` storage account. Show containers list.*

**[1:50] - OBSERVE: Fallback Cache Container**

*Show the `demo-fallback-data` container:*
- Container name: `demo-fallback-data`
- Status: Active
- Notice pre-populated cached responses visible

**[1:55] - SAY:**
"Here's the storage container that holds our cached responses. Notice it's pre-populated with fallback data for when services timeout. When InventoryService takes too long—and it will—we'll pull from this vault instead of failing completely."

**[2:00] - CLICK: Navigate back to Resource Group → Function App**

*Open `func-score-diamond-dev` Function App. Show overview:*
- Status: Running
- Region: East US
- Runtime: .NET 8.0

**[2:10] - SAY:**
"This is the Function App that will aggregate the three downstream services: Payment, Inventory, and Shipping. Each call has a 2-second timeout enforced via CancellationToken. If any service doesn't respond in time, we fall back to the cache we just saw."

**[2:20] - CLICK: Open Application Insights → Live Metrics**

*Navigate to `appinsights-heist-dev` → Live Metrics Stream. Show:*
- Incoming requests (should be 0 or low)
- Server response time
- Dependency calls

**[2:30] - OBSERVE: Live Metrics Ready**

*Point out key sections on the Live Metrics dashboard:*
- **Request Rate:** Where we'll see the aggregation call hit
- **Response Time:** Should show ~2050ms (limited by timeout)
- **Dependency Duration:** Where individual service call times appear

**[2:40] - SAY:**
"This is Live Metrics Stream in Application Insights. In moments, when we trigger the aggregation, you'll see these charts update in real-time. Request rate will spike, dependency calls will appear for Payment, Inventory, and Shipping. And we'll watch InventoryService timeout right here on this screen."

**[2:50] - CALL OUT: Portal Bookmark Tip**

*[NOTE: Have this tab bookmarked or open in separate window for quick access]*

**[2:55] - SAY:**
"I've bookmarked the resource group and Application Insights for quick access. In a live demo, the last thing you want is to hunt for resources while the audience waits."

**[3:00] - CONTINGENCY NOTE:**
*[If Portal is slow loading, skip ahead to code walkthrough and say: "I'll bring the Portal back up once the execution starts—meanwhile, let's look at the code."]*

---

## LIVE DEMO EXECUTION [3:00 - 7:30]

### PHASE 1: The Call [3:00 - 3:30]

**[3:00] - CLICK: Send the aggregation POST request to Azure**

*Execute the HTTP POST call to the live Azure Function. Show the request being sent.*

```
POST https://func-score-diamond-dev.azurewebsites.net/api/aggregate
Response: 202 (request accepted, processing)
```

**[3:10] - OBSERVE: Application Insights Logs**

*Watch logs stream in real-time. You should see:*
```
[INF] Aggregation function triggered
[INF] Starting aggregation for request {RequestId}
[INF] Calling PaymentService for request {RequestId}
[INF] Calling InventoryService for request {RequestId}
[INF] Calling ShippingService for request {RequestId}
```

**[3:20] - NARRATE:**
"Three services called in parallel. PaymentService is already back—fast response. But InventoryService? It's moving slowly. Our 2-second timeout is ticking..."

**[3:30] - SAY:** "Let's see what happens next."

---

### PHASE 2: Timeout & Fallback [3:30 - 5:00]

**[3:30] - WAIT: Let the demo run for ~2 seconds**

*Services are executing. PaymentService done (500ms). ShippingService done (1000ms). InventoryService is still running.*

**[3:35] - OBSERVE: Application Insights Logs**

*Watch for timeout messages:*
```
[WRN] Service InventoryService timed out after 2000ms
[INF] Falling back to cached data for InventoryService
```

*[If this is the first run, no cache exists, so you'll see:]*
```
[WRN] Service InventoryService timed out after 2000ms
[INF] No cached data available, returning error
```

**[3:45] - NARRATE:**
"There it is. InventoryService didn't respond in time. In a real system, this could be a database timeout, API latency, network hiccup—doesn't matter. We have a choice: wait forever or move on. We moved on. If we had cached data, we'd use it. If not, we fail gracefully, return an error to the client, and let them decide what to do."

**[4:00] - OBSERVE: Response object starts appearing**

*In Application Insights or Postman, the response is being assembled:*

```json
{
  "requestId": "abc123...",
  "timestamp": "2026-03-19T14:30:00Z",
  "services": {
    "PaymentService": {
      "serviceName": "PaymentService",
      "success": true,
      "data": "Response from PaymentService",
      "elapsedMs": 500
    },
    "InventoryService": {
      "serviceName": "InventoryService",
      "success": false,
      "error": "Timeout after 2000ms and no cached data available",
      "elapsedMs": 2000,
      "usedCache": false
    },
    "ShippingService": {
      "serviceName": "ShippingService",
      "success": true,
      "data": "Response from ShippingService",
      "elapsedMs": 1000
    }
  },
  "elapsedMs": 2050
}
```

**[4:15] - CLICK: Show the full response in Postman/browser**

*Scroll through the JSON. Point out:*
- PaymentService: ✅ Success, 500ms
- InventoryService: ❌ Timeout, no cache fallback
- ShippingService: ✅ Success, 1000ms

**[4:30] - NARRATE:**
"Two of three services succeeded. One timed out, but we didn't crash. The client got a response with clear status: which services succeeded, which failed, timing for each. This is resilience. In a degraded state, we still provide value."

**[4:45] - CLICK: Open the code window again**

*Show the `CallDownstreamServiceAsync()` method:*
```csharp
using var cts = new System.Threading.CancellationTokenSource(TimeoutMs);
await Task.Delay(delay, cts.Token);
```

**[5:00] - NARRATE:**
"This is the timeout enforcement: we create a cancellation token with a 2-second deadline. If the task doesn't complete in time, we catch the `OperationCanceledException` and handle it. The timeout isn't magical—it's a CancellationToken. Azure Functions and .NET make this pattern simple to implement."

**[5:15] - SAY:** "Let's make it harder. Let me run it again, and this time, the Payment Service will also slow down."

---

### PHASE 3: Multiple Timeouts & Cache Validation [5:15 - 6:30]

**[5:15] - MODIFY CODE (if possible) or PRE-RUN scenario:**

*[OPTION A - Live code change:]*
- In `AggregationService.cs`, temporarily change PaymentService delay to 2500:
```csharp
"PaymentService" => 2500,  // Slow this time
```
- [CLICK] Save and redeploy (or for local, restart func start)
- [WAIT] for function to reload
- [NOTE: This may take 30 seconds. While waiting, narrate the setup.]

*[OPTION B - Pre-prepared scenario:]*
- Have a second script pre-deployed with different delays
- Switch to the second script endpoint

**[5:45] - SAY:** 
"Now, let's talk about caching. In a real system, you'd have cached Payment and Inventory data from previous successful calls. Let me populate the cache, then call again with slow services."

**[5:55] - CLICK: (Simulated) Populate cache**

*In the code or via a setup function, ensure the cache is seeded with data:*
```csharp
_cache["PaymentService"] = new CachedResult 
{ 
  Data = "Cached payment response",
  CachedAt = DateTime.UtcNow.AddMinutes(-5)
};
_cache["InventoryService"] = new CachedResult
{
  Data = "Cached inventory response",
  CachedAt = DateTime.UtcNow.AddMinutes(-10)
};
```

**[6:10] - CLICK: Send the aggregation request AGAIN**

*Execute another POST to `/api/aggregate` (with slow services still enabled).*

**[6:20] - OBSERVE: Application Insights Logs**

*Watch for the cache fallback messages:*
```
[WRN] Service PaymentService timed out after 2000ms
[INF] Falling back to cached data for PaymentService
[INF] Cache hit: PaymentService

[WRN] Service InventoryService timed out after 2000ms
[INF] Falling back to cached data for InventoryService
[INF] Cache hit: InventoryService
```

**[6:30] - SAY:** "That's the power of the Aggregation pattern with caching: resilience and graceful degradation. But here's the key insight..."

---

### PHASE 4: The Pattern Breakdown [6:30 - 7:30]

**[6:30] - CLICK: Show the code again**

```json
{
  "requestId": "xyz789...",
  "timestamp": "2026-03-19T14:31:00Z",
  "services": {
    "PaymentService": {
      "serviceName": "PaymentService",
      "success": true,
      "data": "Cached payment response",
      "elapsedMs": 2000,
      "usedCache": true  // <-- KEY: Cache flag set
    },
    "InventoryService": {
      "serviceName": "InventoryService",
      "success": true,
      "data": "Cached inventory response",
      "elapsedMs": 2000,
      "usedCache": true  // <-- KEY: Cache flag set
    },
    "ShippingService": {
      "serviceName": "ShippingService",
      "success": true,
      "data": "Response from ShippingService",
      "elapsedMs": 1000
    }
  },
  "elapsedMs": 2050
}
```



*Highlight these key sections:*

```csharp
var tasks = new[]
{
  CallDownstreamServiceAsync("PaymentService", requestId),
  CallDownstreamServiceAsync("InventoryService", requestId),
  CallDownstreamServiceAsync("ShippingService", requestId)
};

var serviceResults = await Task.WhenAll(tasks);
```

**[6:45] - NARRATE:**
"We call three services in parallel using `Task.WhenAll`. This is critical: we're not waiting for Payment first, then Inventory, then Shipping. We fire all three at once. The overall latency is governed by the slowest service, not the sum of all services. Without this parallelization, the call would take 500 + 2500 + 1000 = 4 seconds, even if we used timeouts. With parallelization, we get the fastest response from each service, and a timeout of 2 seconds per service, so worst case is ~2 seconds total."

**[7:00] - CLICK: Show the timeout mechanism**

```csharp
using var cts = new System.Threading.CancellationTokenSource(TimeoutMs);
await Task.Delay(delay, cts.Token);
```

**[6:45] - NARRATE:**
"This is the timeout enforcer. Every task gets a 2-second deadline via a CancellationToken. If it exceeds that, an `OperationCanceledException` is thrown, caught, and we handle it—either with cached data or a failure response."

**[7:20] - CLICK: Show the cache logic**

```csharp
if (_cache.TryGetValue(serviceName, out var cached))
{
  _logger.LogInformation("Falling back to cached data for {ServiceName}", serviceName);
  return new ServiceResult
  {
    ServiceName = serviceName,
    Success = true,
    Data = cached.Data,
    ElapsedMs = TimeoutMs,
    UsedCache = true
  };
}
```

**[7:25] - NARRATE:**
"If a service times out, we check the cache. If we have recent data, we use it. If not, we return an error to the client—transparently. The client knows which services succeeded and which fell back to cached/failed. This transparency is critical for debugging and graceful degradation."

**[7:30] - SAY:** "That's the Aggregation pattern. Three core principles: parallel execution, timeouts per service, and fallback strategies."

---

## CLOSING REMARKS [7:30 - 9:00]

**[7:35] - NARRATE:**
"The Aggregation pattern is about resilience through composition. You're not hiding failures—you're acknowledging them and providing a graceful response. In Ocean's Eleven, if one member of the crew gets caught or slowed down, the plan doesn't collapse. There's a contingency. Your distributed systems need the same."

**[7:50] - REAL-WORLD CALL-OUT:**
"Imagine you're building an e-commerce site. You need payment, inventory, and shipping info for every order. These services live in different systems, sometimes different clouds. They can fail, lag, or timeout. With the Aggregation pattern and caching, you don't wait for all three to respond perfectly. You get back what you can, and when some services are down, you serve cached results. **The user gets an experience that feels fast and reliable, even when the underlying systems aren't perfect.**"

**[8:15] - BRIDGE TO ARCHITECTURE:**
"This is serverless architecture at its best: decoupled, resilient, and observable. Each service is independent. Failures are isolated. And with Application Insights, you see everything that happens—every timeout, every cache hit, every success. You can optimize, debug, and improve in production."

**[8:35] - AUDIENCE ENGAGEMENT:**
"Who here has seen a system where one slow API broke the entire page load? [pause for hands] That's what we're solving. This is production reality for architects."

**[8:50] - CLOSING STATEMENT:**
"Next, we're moving to the queue. Instead of synchronous calls and timeouts, we're decoupling with asynchronous messaging—and it gets even more interesting when messages go bad. Let's pull the job."

**[9:00] - END OF DEMO**

---

## FALLBACK & CONTINGENCIES

### If API call fails:
- **[NOTE: Connection issue]** Pre-record the response and show a screenshot in Postman/browser
- Narrate the expected logs and outcomes
- Continue with phase 4 (code breakdown) to show the pattern even without live execution

### If Application Insights logs don't appear:
- **[NOTE: Logs delayed]** Azure logs can take 30 seconds to appear. Keep talking about the code structure.
- Check the function logs in the Azure Portal under "Monitor" tab as backup
- Have a pre-recorded screenshot of expected logs ready

### If service timeout behavior doesn't trigger:
- **[NOTE: Services too fast]** This is actually good—it means infrastructure is healthy. Narrate: "Looks like all services are responding within the timeout window. Let's simulate a slow service manually by modifying the code delay values."

### If cache is not set:
- **[NOTE: Cold start, no cache]** Explain that on first run, there's no cached data. Send two requests: first populates logs showing timeout behavior; second shows cache hits if you seed the cache in the code.

---

## KEY METRICS TO CALL OUT

From Application Insights, reference these during the demo:

1. **Request Duration:** Should be ~2050 ms (limited by the 2-second timeout or slowest service, whichever is less)
2. **Service Call Latency:** PaymentService ~500ms, ShippingService ~1000ms, InventoryService ~2000ms (timeout)
3. **Cache Hits:** On second run, should see `UsedCache: true` for timed-out services
4. **Exception Count:** `OperationCanceledException` should be logged per timeout event
5. **Success Rate:** Despite a timeout, overall request succeeds (2 of 3 services + cache = full response)

---

## TIMING NOTES FOR CHAD

- **Total script:** 9 minutes (130–150 words per minute of speaking)
- **Slack buffer:** 1 minute for Q&A or contingency
- **Critical timing:** 2:00 mark—this is when the timeout should visibly trigger (InventoryService at 2500ms delay)
- **Audience pacing:** Pause at [6:15], [6:45], [7:15] for code comprehension; let people read before moving on
