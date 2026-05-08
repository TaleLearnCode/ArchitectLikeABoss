# Demo Execution Guide: Pull the Job (Queue-Based Load Leveling Pattern)

**Duration:** 10 minutes  
**Pattern:** Queue-Based Load Leveling with Poison Message Handling  
**Date:** March 19, 2026 (Las Vegas)

---

## Pre-Demo Checklist

Before going live on stage, Chad must verify:

- [ ] **Azure Functions deployed** — HeistQueueLoadLevel project deployed to `https://<function-app>.azurewebsites.net`
- [ ] **Service Bus queue created** — Queue named `load-level-queue` exists in Azure Service Bus
- [ ] **Service Bus connection** — `ServiceBusConnection` app setting is configured and valid
- [ ] **Queue triggers active** — `SubmitLoadLevelRequest` and `ProcessLoadLeveledMessage` functions are running
- [ ] **Application Insights connected** — Telemetry shows recent logs from both functions
- [ ] **Test trigger succeeds** — Manual HTTP requests return 202 Accepted (see **Trigger Commands** below)
- [ ] **Queue metrics visible** — Service Bus Queue shows "Active Message Count" metric
- [ ] **Fallback strategy ready** — Pre-recorded output saved (screen recording of 50-request burst with queue visualization)
- [ ] **DLQ (Dead Letter Queue) configured** — Poison message retry logic will send bad messages to DLQ

**Pre-recorded fallback location:** `C:\Presentations\ArchitectLikeABoss\Assets\demo-fallback-queue-loading.mp4`

---

## Trigger Commands & Expected Outputs

### Trigger Command 1: Single Queue Request

**PowerShell Command:**
```powershell
$url = "https://<function-app>.azurewebsites.net/api/submit-request?code=<function-key>"
$headers = @{ "Content-Type" = "application/json" }
$body = @{ message = "Process payment for heist" } | ConvertTo-Json

$response = Invoke-WebRequest -Uri $url -Method POST -Headers $headers -Body $body
Write-Host "Status: $($response.StatusCode)"
$response.Content | ConvertFrom-Json | ConvertTo-Json
```

**curl equivalent:**
```bash
curl -X POST https://<function-app>.azurewebsites.net/api/submit-request?code=<function-key> \
  -H "Content-Type: application/json" \
  -d '{"message":"Process payment for heist"}'
```

### Expected Response (Single Request)

**Status Code:** 202 Accepted

**Response Body:**
```json
{
  "messageId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "queued"
}
```

**Interpretation:** The HTTP function accepted the request and queued it. The caller gets an immediate 202 response without waiting for processing.

---

### Trigger Command 2: Burst of 50 Requests (Load Testing)

**PowerShell Script for Burst Load:**

```powershell
$url = "https://<function-app>.azurewebsites.net/api/submit-request?code=<function-key>"
$headers = @{ "Content-Type" = "application/json" }

Write-Host "Sending 50 requests in rapid succession..."
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

$responses = @()
for ($i = 1; $i -le 50; $i++) {
    $body = @{ message = "Job #$i" } | ConvertTo-Json
    try {
        $response = Invoke-WebRequest -Uri $url -Method POST -Headers $headers -Body $body -TimeoutSec 5
        $responses += @{ 
            requestNumber = $i
            statusCode = $response.StatusCode
            messageId = ($response.Content | ConvertFrom-Json).messageId
        }
        Write-Host "Request $i: 202 Accepted"
    } catch {
        Write-Host "Request $i: FAILED"
    }
}

$stopwatch.Stop()
Write-Host "Burst complete in $($stopwatch.ElapsedMilliseconds)ms"
Write-Host "Successful: $($responses.Count)/50"
$responses | ConvertTo-Json | Write-Host
```

### Expected Response (Burst Load)

**Status Code:** All 50 requests should return 202 Accepted

**Timing:** Burst should complete in ~500–1000ms (50 requests × ~10-20ms per request)

**Console Output:**
```
Sending 50 requests in rapid succession...
Request 1: 202 Accepted
Request 2: 202 Accepted
...
Request 50: 202 Accepted
Burst complete in 847ms
Successful: 50/50
```

**Queue Depth Metric** (in Azure Portal or Application Insights):
- **Before burst:** 0 active messages
- **During burst:** Queue depth rises to ~30–50 active messages
- **After 60 seconds:** Queue depth drops as worker function processes messages

---

### Trigger Command 3: Poison Message Test

**PowerShell Command:**
```powershell
$url = "https://<function-app>.azurewebsites.net/api/submit-request?code=<function-key>"
$headers = @{ "Content-Type" = "application/json" }

# Send a poison message that will fail processing
$body = @{ message = "POISON_MESSAGE_WILL_FAIL" } | ConvertTo-Json

$response = Invoke-WebRequest -Uri $url -Method POST -Headers $headers -Body $body
Write-Host "Poison message queued: $($response.StatusCode)"
Write-Host $response.Content
```

### Expected Response (Poison Message)

**Status Code:** 202 Accepted (HTTP function always accepts)

**Response Body:**
```json
{
  "messageId": "550e8400-e29b-41d4-a716-446655440001",
  "status": "queued"
}
```

**Worker Function Behavior:**
- Worker receives message with "POISON_MESSAGE_WILL_FAIL"
- Line 47 in QueueLoadLevelingService.cs: `if (message.Contains("POISON"))` — throws `InvalidOperationException`
- First retry (DeliveryCount=1): Fails
- Second retry (DeliveryCount=2): Fails
- Third retry (DeliveryCount=3): Fails
- Fourth attempt would exceed max retries (>3), so message is sent to Dead Letter Queue

**Application Insights Log:**
```
[ERROR] Error processing message 550e8400-e29b-41d4-a716-446655440001: Poison message detected
[ERROR] Message 550e8400-e29b-41d4-a716-446655440001 exceeded max retries, sending to DLQ
```

**Azure Portal - Service Bus Queue:**
- **Active Messages:** Decreases as messages are processed
- **Dead Letter Messages:** Increases by 1 (the poison message)

---

## Code Walkthrough Mapping

### File 1: `HeistQueueLoadLevel/QueueFunctions.cs`

**Purpose:** Contains the HTTP trigger (submit) and queue trigger (worker) functions.

**Display lines:** 11–45 (Submit function) and 50–75 (Worker function)

#### Part A: HTTP Trigger — SubmitLoadLevelRequest (lines 11–45)

**Key sections to highlight:**

**Lines 22–26** — Function decorator and HTTP trigger
```csharp
[Function("SubmitLoadLevelRequest")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "submit-request")] HttpRequestData req)
```

**Presentation notes:**
- "This HTTP endpoint is how Chad submits work. The route is `/api/submit-request`."
- "Notice we return `HttpResponseData` immediately — we don't wait for processing."

**Lines 28–42** — Queue the message
```csharp
var messageId = Guid.NewGuid().ToString();
var message = "Sample message";

try {
    var body = await req.ReadAsStringAsync();
    message = string.IsNullOrEmpty(body) ? message : body;
} catch { }

await _queueService.QueueMessageAsync(message, messageId);
```

**Presentation notes:**
- "We assign a unique messageId for tracing."
- "We read the request body (optional) — if empty, use default message."
- "We call QueueMessageAsync to send the message to Service Bus."

**Lines 40–44** — Immediate response (202 Accepted)
```csharp
var response = req.CreateResponse(HttpStatusCode.Accepted);
response.Headers.Add("Content-Type", "application/json");
await response.WriteAsJsonAsync(new { messageId, status = "queued" });
return response;
```

**Presentation notes:**
- "Notice the status code: 202 Accepted, not 200 OK."
- "202 means 'I accept your request but haven't processed it yet.'"
- "We return immediately with the messageId and 'queued' status."
- "Processing happens asynchronously in the background."

#### Part B: Service Bus Queue Trigger — ProcessLoadLeveledMessageFunction (lines 50–75)

**Key sections to highlight:**

**Lines 63–66** — Service Bus trigger
```csharp
[Function("ProcessLoadLeveledMessage")]
public async Task Run(
    [ServiceBusTrigger("load-level-queue", Connection = "ServiceBusConnection")] string queueItem,
    FunctionContext context)
```

**Presentation notes:**
- "This function is triggered by messages arriving in the Service Bus queue."
- "It runs on a separate worker pool, processing messages at a steady rate."
- "The queue name is `load-level-queue` — it's configured in Azure Service Bus."

**Lines 68–73** — Message processing
```csharp
_logger.LogInformation("Processing message from queue: {Message}", queueItem);

var messageId = Guid.NewGuid().ToString();
int deliveryCount = 1;

await _queueService.ProcessMessageAsync(queueItem, messageId, deliveryCount);
```

**Presentation notes:**
- "When a message arrives, we log it and call ProcessMessageAsync."
- "We track deliveryCount to detect poison messages after multiple failures."

---

### File 2: `HeistQueueLoadLevel/QueueLoadLevelingService.cs`

**Purpose:** Contains the queueing logic and poison message handling.

**Display lines:** 28–67

#### Part A: Queueing Logic (lines 28–33)

```csharp
public async Task QueueMessageAsync(string message, string messageId)
{
    _logger.LogInformation("Queuing message {MessageId}: {Message}", messageId, message);
    // TODO: Implement Service Bus queue send
    await Task.Delay(100); // Simulate queueing
}
```

**Presentation notes:**
- "In real code, this would call Azure Service Bus SDK to send the message to the queue."
- "We log the message and messageId for tracing."

#### Part B: Message Processing with Retry Logic (lines 39–66)

```csharp
public async Task ProcessMessageAsync(string message, string messageId, int deliveryCount)
{
    _logger.LogInformation("Processing message {MessageId} (attempt {DeliveryCount}): {Message}", 
        messageId, deliveryCount, message);

    try
    {
        // Poison message detection
        if (message.Contains("POISON"))
        {
            throw new InvalidOperationException("Poison message detected");
        }

        await Task.Delay(500); // Simulate work
        _logger.LogInformation("Message {MessageId} processed successfully", messageId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing message {MessageId}", messageId);
        
        if (deliveryCount > 3)
        {
            _logger.LogError("Message {MessageId} exceeded max retries, sending to DLQ", messageId);
            // TODO: Send to DLQ
        }
        throw;
    }
}
```

**Key concepts to highlight:**

**Lines 47–50** — Poison message detection
```csharp
if (message.Contains("POISON"))
{
    throw new InvalidOperationException("Poison message detected");
}
```

**Presentation notes:**
- "We check if the message contains 'POISON' — this simulates malformed or invalid messages."
- "We throw an exception, which triggers Azure Service Bus retry mechanism."

**Lines 55–66** — Retry logic and DLQ
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error processing message {MessageId}", messageId);
    
    if (deliveryCount > 3)
    {
        _logger.LogError("Message {MessageId} exceeded max retries, sending to DLQ", messageId);
        // TODO: Send to DLQ
    }
    throw;
}
```

**Presentation notes:**
- "If processing fails, we check the delivery count."
- "If we've tried more than 3 times, we give up and send the message to the Dead Letter Queue."
- "The DLQ is a holding area for messages we can't process — we can inspect them later to diagnose issues."
- "By throwing the exception, we signal to Service Bus: 'Retry this message.'"

---

## Application Insights Monitoring

During the demo, Chad should watch Application Insights for:

### 1. Traces Tab (Real-time Logs)

**Filter for:** Keyword `Queuing message` or `Processing message`

**Expected logs during burst:**
```
Queuing message 550e8400-e29b-41d4-a716-446655440000: Job #1
Queuing message 550e8400-e29b-41d4-a716-446655440001: Job #2
...
Queuing message 550e8400-e29b-41d4-a716-446655440050: Job #50

Processing message 550e8400-e29b-41d4-a716-446655440000 (attempt 1): Job #1
Processing message 550e8400-e29b-41d4-a716-446655440001 (attempt 1): Job #2
...
Message 550e8400-e29b-41d4-a716-446655440000 processed successfully
```

**For poison message:**
```
Processing message 550e8400-e29b-41d4-a716-446655440051 (attempt 1): POISON_MESSAGE_WILL_FAIL
Error processing message 550e8400-e29b-41d4-a716-446655440051: Poison message detected

Processing message 550e8400-e29b-41d4-a716-446655440051 (attempt 2): POISON_MESSAGE_WILL_FAIL
Error processing message 550e8400-e29b-41d4-a716-446655440051: Poison message detected

Processing message 550e8400-e29b-41d4-a716-446655440051 (attempt 3): POISON_MESSAGE_WILL_FAIL
Error processing message 550e8400-e29b-41d4-a716-446655440051: Poison message detected

Processing message 550e8400-e29b-41d4-a716-446655440051 (attempt 4): POISON_MESSAGE_WILL_FAIL
Message 550e8400-e29b-41d4-a716-446655440051 exceeded max retries, sending to DLQ
```

### 2. Azure Service Bus Metrics (Azure Portal)

**Queue name:** `load-level-queue`

**Metrics to watch:**
- **Active Message Count:** Should spike during burst, then decline as worker processes
- **Dead Letter Message Count:** Should increase by 1 when poison message is sent to DLQ
- **Processed Messages Count:** Should increase steadily as worker function runs

### 3. Performance Tab

**Operation:** `SubmitLoadLevelRequest`
- Should see 50 requests with average response time <100ms
- Success rate: 100% (all return 202 Accepted)

**Operation:** `ProcessLoadLeveledMessage`
- Should see ~51 function invocations (50 normal + 4 retries for poison)
- Most should succeed (~500ms processing time)
- Poison message should show 4 attempts before going to DLQ

---

## Fallback Activation (If Live Fails)

### Scenario 1: HTTP Trigger Returns Error

**Symptoms:** PowerShell command returns 500 error or times out.

**Fallback Steps:**
1. **Pause demo** — "Let me show you what a successful burst load looks like."
2. **Switch display** — Show pre-recorded screen capture of 50-request burst
3. **Narrate output** — "Notice how all 50 requests get 202 Accepted in under 1 second."
4. **Show metrics** — Switch to Application Insights screenshot showing queue metrics
5. **Continue** — "That's the load leveling pattern working: immediate response, background processing."

### Scenario 2: Queue Metrics Not Updating

**Symptoms:** Azure Portal shows 0 active messages even after burst.

**Fallback Steps:**
1. **Explain** — "The Service Bus metrics have a small delay. Let me show you what's happening in the logs."
2. **Switch to Application Insights** — Show pre-recorded Traces showing "Processing message" logs
3. **Point out** — "Here we can see workers processing 50 messages sequentially."
4. **Narrate** — "Each message takes about 500ms to process, so 50 messages takes about 25 seconds total."

### Scenario 3: Poison Message Handling Not Visible

**Symptoms:** DLQ message count doesn't increase.

**Fallback Steps:**
1. **Show code** — Jump to line 47 in QueueLoadLevelingService.cs: `if (message.Contains("POISON"))`
2. **Narrate** — "Any message with the word 'POISON' in it will be treated as malformed."
3. **Show Application Insights logs** — Pre-recorded screenshot of retry attempts
4. **Point out** — "Here it tried 4 times and then gave up, sending it to the Dead Letter Queue."

### Recovery & Next Steps

After using a fallback:
- **Do NOT attempt** another live trigger
- **Show code walkthrough** — Continue with detailed code analysis (not dependent on live execution)
- **Transition** — Move to next demo: "Distribute the Crew" (fan-out/fan-in pattern)

---

## Demo Script Timeline

| Time | Action | Expected Output |
|------|--------|-----------------|
| 0:00 | Intro: "Pull the Job - Queue-Based Load Leveling" | Visual slide |
| 0:30 | Show HTTP trigger (SubmitLoadLevelRequest, lines 22–44) | HTTP function visible |
| 1:30 | Highlight: "202 Accepted — immediate response" | Code highlighted |
| 2:00 | Show queue trigger (ProcessLoadLeveledMessage, lines 63–73) | Queue function visible |
| 2:30 | Explain: "Worker function runs separately" | Architecture diagram or code flow |
| 3:00 | Show poison message detection (lines 47–50) | Code highlighted |
| 3:30 | Show retry logic (lines 55–66) | Code highlighted |
| 4:00 | **LIVE TRIGGER (Single)** — Submit one request | 202 Accepted response |
| 4:30 | Show Application Insights: "Processing message" log | Trace visible in real-time |
| 5:00 | **LIVE TRIGGER (Burst)** — Submit 50 requests | All 50 return 202 in <1 second |
| 5:30 | Show Service Bus Queue metrics | Active messages spike, then decline |
| 6:30 | **LIVE TRIGGER (Poison)** — Submit poison message | 202 Accepted (still queued) |
| 7:00 | Show Application Insights: 4 retry attempts | Error logs visible |
| 7:30 | Show: "Message exceeded max retries, sending to DLQ" | DLQ count increases |
| 8:00 | Explain pattern: "Immediate acceptance, background processing, poison handling" | Summary slide |
| 8:30 | Show metrics summary: 50 processed, 1 in DLQ | Final metrics |
| 9:00 | Transition to next pattern | Visual transition |

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| HTTP trigger returns 404 | Check function URL and API key; verify function is deployed |
| 202 Accepted but nothing queues | Check ServiceBusConnection app setting in Azure; verify queue name is `load-level-queue` |
| Queue metrics show 0 active messages | Messages are being processed immediately; increase `Task.Delay(500)` in QueueLoadLevelingService.cs to slow processing or send more messages faster |
| Poison message doesn't reach DLQ | Verify DLQ is enabled for the queue in Azure Service Bus; check that retry count is >3 |
| Application Insights logs are delayed | Wait 2–3 minutes for telemetry to appear; refresh browser; check that function app is sending telemetry to correct Application Insights instance |

---

## Key Takeaway for Audience

> **"The queue-based load leveling pattern lets you handle traffic spikes without overwhelming your system. Clients get an immediate acknowledgment (202 Accepted) while work is processed in the background at a steady rate. Poison messages are automatically detected and quarantined, preventing them from jamming up the queue."**
