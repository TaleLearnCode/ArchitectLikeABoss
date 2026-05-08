# "Pull the Job" — Queue-Based Load Leveling Demo Script
## 10-Minute Live Demo

---

## OPENING NARRATION [0:00 - 0:30]

*[Stand center stage. Conversational, building tension. Heist metaphor]*

**[0:00]** "In Ocean's Eleven, the crew doesn't all rush through the same door at once. They stagger their entry—one person goes in, another waits, another follows. If everyone hit the vault at the same time, alarms would blare, security would swarm, and the heist fails."

**[0:15]** "Real-world systems have the same problem: burst traffic. You get a spike—maybe a flash sale, or a viral post—and suddenly hundreds of requests slam your backend. Your system can't process them all instantly, so they queue up, memory explodes, and everything crashes."

**[0:25]** "What you're about to see is the antidote: **Queue-Based Load Leveling**. We'll hit it with burst traffic, watch the queue absorb the load, and see steady-state processing handle it gracefully. Plus, we'll trigger a 'poison message'—a bad actor in the queue—and watch it get quarantined to the Dead Letter Queue."

---

## SETUP & PRE-FLIGHT CHECKS [0:30 - 1:30]

**[0:30] - CLICK: Open VS Code with code visible**

*Display the queue functions locally:*
- Open VS Code with the queue function code visible
- Show `SubmitLoadLevelRequest` and `ProcessLoadLeveledMessage`
- [NOTE: We're showing code locally but will execute in Azure]

**[0:38] - OBSERVE: Queue metrics**
- Show current queue length: should be 0 or very small
- Say: "Queue is empty. In moments, it will fill with messages from our burst traffic."
- [NOTE: If Portal is slow, proceed with setup while Portal loads in background]

**[0:45] - CLICK: Open Application Insights**

*Navigate to the Application Insights workspace (name from Linus). Show:*
- Live Logs view ready to stream
- Optional: Pre-configure a chart showing Message Count over time

**[0:55] - OBSERVE: Function status**
- Show `SubmitLoadLevelRequest` function (HTTP trigger) is deployed and active
- Show `ProcessLoadLeveledMessage` function (Service Bus trigger) is deployed and active
- [NOTE: If functions are not showing, check Azure Monitor and try refreshing. They should be in "Running" state.]

**[1:05] - CLICK: Prepare the Azure Function URL**

*Have the Azure Function endpoint ready for API calls:*
- Production URL: `https://func-pull-job-dev.azurewebsites.net/api/submit-request`
- We'll send bulk POST requests to this endpoint

**[1:15] - CALL OUT:**
- "HTTP trigger receives burst requests, immediately queues them, returns 202 Accepted"
- "Service Bus trigger wakes up when messages arrive, processes them one at a time"
- "If a message contains 'POISON', it fails and gets retried 3 times, then moved to Dead Letter Queue"

**[1:30] - SAY:** "Queue ready. Functions deployed. Logs streaming. Let's look at the live infrastructure."

---

## INFRASTRUCTURE SHOWCASE [1:30 - 3:00]

**[1:30] - CLICK: Open Azure Portal**

*Navigate to: https://portal.azure.com/#resource/subscriptions/{subid}/resourceGroups/rg-heist-dev-eus/overview*

**[1:35] - SAY:**
"Before we flood this queue with traffic, let me show you what's actually running in Azure. This is live infrastructure—deployed, provisioned, and ready to absorb burst traffic."

**[1:40] - OBSERVE: Resource Group Overview**

*Show `rg-heist-dev-eus` resource group with deployed resources:*
- Function App: `func-pull-job-dev`
- Service Bus Namespace: `sbheistdev`
- Application Insights: `appinsights-heist-dev`

**[1:45] - CLICK: Open Service Bus Namespace**

*Navigate to `sbheistdev` → Queues. Show the queue list.*

**[1:50] - OBSERVE: Heist Queue Properties**

*Click on `heist-queue`. Show queue properties:*
- Queue name: `heist-queue`
- Status: Active
- Max delivery count: 3
- Dead-letter queue: Enabled

**[1:55] - SAY:**
"This is the queue that will absorb our burst traffic. Notice the configuration: max delivery count is 3, meaning any message that fails 3 times gets automatically routed to the Dead Letter Queue. We'll trigger that in a moment with a poison message."

**[2:00] - OBSERVE: Current Queue Metrics**

*Point out queue metrics (should be at 0):*
- Active message count: 0
- Dead-letter message count: 0

**[2:05] - SAY:**
"Queue is empty. In moments, we'll hit it with 50 messages in rapid succession. Watch this number spike, then drain as the worker function processes them at a steady rate."

**[2:10] - CLICK: Navigate back to Resource Group → Function App**

*Open `func-pull-job-dev` Function App. Show functions list:*
- `SubmitLoadLevelRequest` (HTTP trigger)
- `ProcessLoadLeveledMessage` (Service Bus trigger)

**[2:20] - SAY:**
"Two functions: The HTTP trigger queues messages immediately and returns 202 Accepted. The Service Bus trigger wakes up when messages arrive and processes them one at a time. This decoupling is what makes load leveling work—the client gets a fast response, the backend processes at its own pace."

**[2:30] - CLICK: Open Application Insights → Live Metrics**

*Navigate to `appinsights-heist-dev` → Live Metrics Stream. Show:*
- Incoming requests
- Dependency calls (Service Bus queue operations)
- Exception tracking

**[2:40] - OBSERVE: Live Metrics Ready**

*Point out key sections:*
- **Request Rate:** Will show HTTP trigger hits
- **Server Response Time:** Should show fast <100ms for 202 responses
- **Exception Count:** Will spike when poison message is processed and retried

**[2:45] - SAY:**
"As the demo runs, you'll see the HTTP requests come in fast—all returning 202 within milliseconds. Then the Service Bus trigger will process messages steadily. And when the poison message hits, you'll see exceptions appear here as it retries 3 times before routing to the Dead Letter Queue."

**[2:55] - CALL OUT: Portal Bookmark Tip**

*[NOTE: Have Service Bus queue metrics and App Insights tabs pre-opened or bookmarked]*

**[3:00] - SAY:**
"Alright, infrastructure ready. Let's send some traffic."

**[3:00] - CONTINGENCY NOTE:**
*[If Portal is slow loading, skip to execution and say: "Portal is catching up—let's start the traffic flow and come back to watch the queue metrics."]*

---

## LIVE DEMO EXECUTION [3:00 - 7:30]

### PHASE 1: Normal Load [3:00 - 4:00]

**[3:00] - CLICK: Prepare API calls for Azure Function**

*Have a tool ready to send bulk POST requests to the Azure endpoint:*
- **Option A:** VS Code REST Client with a batch of requests
- **Option B:** Postman with Collection Runner
- **Option C:** PowerShell script with parallel `Invoke-WebRequest` calls
- **Option D:** curl in a loop

*Endpoint (production Azure Function):*
```
POST https://func-pull-job-dev.azurewebsites.net/api/submit-request
```

**[3:05] - SAY:**
"I'm about to send 10 normal messages to the queue, rapid-fire. Watch what happens: the HTTP endpoint will queue them all immediately and return 202 (Accepted) to each caller. The queue will fill. Then the Service Bus trigger will wake up and process them one at a time."

**[3:15] - CLICK: Send 10 normal messages**

*Execute bulk POST requests. Show each returning 202 Accepted almost instantly.*

**Sample curl command (if used):**
```bash
for i in {1..10}; do
  curl -X POST https://func-pull-job-dev.azurewebsites.net/api/submit-request \
    -H "Content-Type: application/json" \
    -d "Message $i"
done
```

**[3:25] - OBSERVE: Queue length spike**

*In Azure Portal or Service Bus Emulator, watch the queue length jump:*
```
Queue length: 0 → 1 → 2 → 3 → ... → 10
```

**[3:35] - NARRATE:**
"There's the queue. All 10 messages sitting in line, waiting to be processed. The HTTP endpoints responded immediately—no wait for processing. The requests are decoupled from the backend worker. **This is load leveling in action: burst traffic absorbed by the queue, backend processes at a steady pace.**"

**[3:50] - OBSERVE: Application Insights Logs**

*Watch logs stream in:*
```
[INF] Load leveling HTTP trigger received
[INF] Queuing message {MessageId}: Message 1
[INF] Message queued (MessageId), status: queued

[INF] Processing message from queue: Message 1
[INF] Processing message {MessageId} (attempt 1): Message 1
[INF] Message {MessageId} processed successfully
```

**[3:55] - OBSERVE: Queue length decrease**

*As the Service Bus trigger processes messages, queue length drops:*
```
Queue length: 10 → 9 → 8 → 7 → ... → 0
```

**[4:00] - SAY:** "Each message processes, one per second (simulated 500ms delay + overhead). The queue is draining. No errors, no crashes, just steady throughput. That's the pattern working."

**[4:00] - END PHASE 1**

---

### PHASE 2: Burst Traffic [4:00 - 5:30]

**[4:00] - SAY:**
"Now let's simulate real burst traffic. I'm sending 50 messages in rapid succession—like a flash sale going live."

**[4:05] - CLICK: Send 50 messages in rapid burst**

*Execute 50 POST requests as fast as possible. Show:*
- **HTTP responses:** All come back with 202 Accepted immediately
- **Client experience:** Fast and responsive, no timeouts

**Sample PowerShell (if used):**
```powershell
$uri = "https://func-pull-job-dev.azurewebsites.net/api/submit-request"
1..50 | ForEach-Object {
  $response = Invoke-WebRequest -Uri $uri -Method Post -Body "Burst message $_"
  Write-Host "Message $_ queued, status: $($response.StatusCode)"
}
```

**[4:20] - OBSERVE: Queue length spike**

*Queue length jumps dramatically:*
```
Queue length: 0 → 10 → 20 → 30 → ... → 50
```

**[4:30] - NARRATE:**
"50 messages queued in seconds. If we had tried to process these synchronously—like traditional REST-to-REST calls—the backend would be drowning. Response times would spike from milliseconds to seconds or timeout entirely. But with the queue, all 50 requests got a fast 202 response. The user got immediate feedback, and the backend is handling them at its own pace."

**[4:50] - OBSERVE: Steady processing**

*In Application Insights, watch the processing rate:*
```
[INF] Processing message from queue: Burst message 1
[INF] Processing message from queue: Burst message 2
[INF] Message processed successfully
[INF] Message processed successfully
...
```

*Queue length drains steadily:*
```
Queue length: 50 → 48 → 46 → 44 → ... (decreasing)
```

**[5:00] - OBSERVE: Time vs. traditional approach**

*Let the demo run visibly. Point out:*
- **Queue length dropping:** Currently at 30, still processing
- **No errors:** All successful
- **Backend not overloaded:** Processing at constant rate (~1 per second)

**[4:30] - NARRATE:**
"Here's the key difference: In a traditional synchronous system, 50 burst requests would cause:
- Long response times (every client waits for backend to process)
- Possible timeouts (some clients give up waiting)
- Memory spikes (pending requests pile up)
- Backend crashes (if overloaded)

With Queue-Based Load Leveling:
- All clients get fast responses (202 Accepted)
- Backend processes at a sustainable rate
- No crashes, no timeouts, no wasted resources
- The queue acts as a shock absorber."

**[5:25] - OBSERVE: Queue emptying**

*Queue length continues dropping:*
```
Queue length: 30 → 20 → 10 → 5 → 1 → 0
```

**[5:30] - SAY:** "And now, for the tricky part—let's introduce a bad actor."

---

### PHASE 3: Poison Messages & Dead Letter Queue [5:30 - 6:30]

**[5:30] - SAY:**
"In real systems, sometimes bad data gets into the queue. A malformed message. A message that crashes the processing logic. We can't let one bad message crash the entire queue. We need **poison message handling**: detect the bad message, retry it a few times, then quarantine it to the Dead Letter Queue (DLQ)."

**[5:40] - CLICK: Show the code**

*Display `ProcessLoadLeveledMessage` and the poison detection logic:*

```csharp
try
{
  if (message.Contains("POISON"))
  {
    throw new InvalidOperationException("Poison message detected");
  }
  await Task.Delay(500); // Process
}
catch (Exception ex)
{
  if (deliveryCount > 3)
  {
    _logger.LogError("Message {MessageId} exceeded max retries, sending to DLQ", messageId);
  }
  throw;
}
```

**[5:50] - NARRATE:**
"When a message contains 'POISON', we throw an exception. Service Bus catches it, retries the message up to 3 times (each time incrementing DeliveryCount). After 3 failed attempts, the message is automatically moved to the Dead Letter Queue. It doesn't crash the worker. It doesn't block other messages. It's quarantined."

**[6:00] - CLICK: Send normal messages + 1 poison message**

*Queue up a sequence of messages:*
```
Message: "Normal request 1"
Message: "Normal request 2"
Message: "POISON MESSAGE - will fail"  // <-- This will fail
Message: "Normal request 3"
Message: "Normal request 4"
```

**Option: Send via API or directly to Service Bus, e.g.:**
```bash
curl -X POST http://localhost:7071/api/submit-request -d "Normal request 1"
curl -X POST http://localhost:7071/api/submit-request -d "Normal request 2"
curl -X POST http://localhost:7071/api/submit-request -d "POISON MESSAGE - will fail"
curl -X POST http://localhost:7071/api/submit-request -d "Normal request 3"
curl -X POST http://localhost:7071/api/submit-request -d "Normal request 4"
```

**[6:10] - OBSERVE: Queue processing**

*In Application Insights, watch the logs:*

```
[INF] Processing message from queue: Normal request 1
[INF] Message processed successfully

[INF] Processing message from queue: Normal request 2
[INF] Message processed successfully

[INF] Processing message from queue: POISON MESSAGE - will fail
[ERR] Error processing message {MessageId}: Poison message detected
[INF] Retrying message {MessageId} (attempt 2)

[ERR] Error processing message {MessageId}: Poison message detected
[INF] Retrying message {MessageId} (attempt 3)

[ERR] Error processing message {MessageId}: Poison message detected
[INF] Retrying message {MessageId} (attempt 4)

[ERR] Message {MessageId} exceeded max retries, sending to DLQ
```

**[6:20] - NARRATE:**
"Watch what happens: The poison message fails. Service Bus retries it. It fails again. Retries again. Still fails. After 3 retries (attempt 4 total), it gives up and sends it to the Dead Letter Queue. **Critically: Notice that Normal request 3 and 4 were still processed successfully.** One bad message didn't poison the entire queue. The pipeline kept running."

**[6:25] - CLICK: Show the Dead Letter Queue**

*In Azure Portal Service Bus:*
- Navigate to the queue
- Show the "Dead Letter Queue" (usually auto-created by Service Bus)
- Display the poison message sitting there, with metadata showing retry attempts

**[6:30] - NARRATE:**
"The poison message is now in the DLQ. A human operator, or an automated system, can inspect it, fix it, and resubmit it if needed. It's not lost, just quarantined. This is **observability and resilience combined**: we see what went wrong, we understand why, and we have a path to recovery without crashing the system."

---

### PHASE 4: The Pattern Breakdown [6:30 - 7:20]

**[6:30] - CLICK: Show the HTTP trigger code**

```csharp
[Function("SubmitLoadLevelRequest")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "submit-request")] HttpRequestData req)
{
  var messageId = Guid.NewGuid().ToString();
  var message = await req.ReadAsStringAsync();
  
  await _queueService.QueueMessageAsync(message, messageId);
  
  var response = req.CreateResponse(HttpStatusCode.Accepted);
  await response.WriteAsJsonAsync(new { messageId, status = "queued" });
  return response;
}
```

**[6:40] - NARRATE:**
"HTTP trigger receives the request, generates a unique message ID, sends it to the queue, and returns **202 Accepted** immediately. Note: 202, not 200. This tells the client 'your request is accepted and queued, not yet processed, but it will be.' The client doesn't wait for processing—the queue owns that responsibility now."

**[6:55] - CLICK: Show the Service Bus trigger code**

```csharp
[Function("ProcessLoadLeveledMessage")]
public async Task Run(
    [ServiceBusTrigger("load-level-queue", Connection = "ServiceBusConnection")] string queueItem,
    FunctionContext context)
{
  _logger.LogInformation("Processing message from queue: {Message}", queueItem);
  
  var messageId = Guid.NewGuid().ToString();
  int deliveryCount = context.GetDeliveryCount();  // Retry count
  
  await _queueService.ProcessMessageAsync(queueItem, messageId, deliveryCount);
}
```

**[7:10] - NARRATE:**
"Service Bus trigger fires whenever a message arrives. The context tells us the delivery count—how many times it's been retried. We pass that to the processing logic so we can enforce the max-retries rule. If an exception is thrown, Service Bus automatically retries (up to the configured max). This retry behavior is built-in and automatic."

**[7:20] - CLICK: Show the poison message handling**

```csharp
if (deliveryCount > 3)
{
  _logger.LogError("Message {MessageId} exceeded max retries, sending to DLQ", messageId);
  // After 3 retries, Service Bus auto-moves to DLQ
}
throw;  // Always throw on error; Service Bus handles retry & DLQ routing
```

**[7:25] - NARRATE:**
"The logic is simple: if poison is detected, throw an exception. Service Bus catches it, retries automatically. We don't manually manage DLQ routing—Service Bus does that for us after max retries. All we do is throw when processing fails. The framework handles resilience."

---

## CLOSING REMARKS [7:25 - 9:00]

**[7:25] - NARRATE:**
"Queue-Based Load Leveling solves a fundamental problem: decoupling request arrival from processing capacity. Burst traffic no longer crashes your backend. Poison messages don't poison the entire queue. The system scales gracefully."

**[7:45] - REAL-WORLD CALL-OUT:**
"Think about a ride-share app during peak hours. Surge pricing, everyone opens the app, order creation floods in. If orders were processed synchronously, the system would timeout under load. But with a queue, every order gets a fast 'booking submitted' response, and the backend processes them one by one, at its own pace. Peak traffic doesn't translate to peak latency—the queue absorbs it."

**[8:00] - BRIDGE TO ARCHITECTURE:**
"This is asynchronous, event-driven architecture. The HTTP endpoint and the worker function are completely decoupled. They can scale independently. The queue is the contract between them. Add more workers? Just start more instances of the processing function. The queue distributes messages to all of them. This is serverless scaling at its core."

**[8:15] - CLOSING STATEMENT:**
"Next, we're moving to Fan-Out/Fan-In, where we coordinate not just serial queued processing, but true parallel orchestration with durable functions. This is where things get really interesting."

**[8:30] - END OF DEMO**

---

## FALLBACK & CONTINGENCIES

### If HTTP requests don't return 202:
- **[NOTE: Status code issue]** Check the function URL and authentication. May need a function key.
- If the function itself is erroring, check Function App logs in Azure Portal under "Monitor"
- Fallback: Show a screenshot of expected 202 responses and continue with narration

### If queue length doesn't spike:
- **[NOTE: Queue not visible]** Portal may be slow to refresh. Try refreshing the Service Bus namespace page.
- Alternative: Show the logs from Application Insights instead—logs are usually more real-time than the Portal UI
- Narrate the expected queue behavior while the Portal catches up

### If poison message doesn't trigger retries:
- **[NOTE: Poison detection not firing]** The message content may not match the detection logic. Try sending a message that exactly matches the condition:
  - Current logic: `message.Contains("POISON")`
  - Ensure the sent message includes the word "POISON"
- If the function itself is not reacting, check the `ProcessMessageAsync` method and verify it's updated with poison detection

### If DLQ isn't visible:
- **[NOTE: DLQ created dynamically]** Service Bus may not create the DLQ until the first poison message arrives
- Wait 30 seconds for the DLQ to be auto-created by Service Bus, then refresh the portal
- Alternatively, show the Application Insights logs showing "exceeded max retries, sending to DLQ" message—that's proof the system worked

### If Application Insights logs are delayed:
- **[NOTE: Logs not streaming]** Azure can have a 30-second delay. Keep talking about the code and pattern while waiting.
- Pre-record a screenshot of expected logs and display it if real-time logs are too slow
- Show the function output/console logs as backup

---

## KEY METRICS TO CALL OUT

From Application Insights, reference these during the demo:

1. **Queue Length Over Time:** Should show spike (0 → 50), then steady decline
2. **Message Processing Rate:** Should show ~1 message per second (or whatever the simulated delay is)
3. **Response Time for HTTP Trigger:** Should be <100ms (fast 202 return)
4. **Poison Message Retry Count:** Should show 4 attempts (initial + 3 retries) before DLQ routing
5. **No Exceptions for Normal Messages:** Only the poison message should generate exceptions
6. **DLQ Depth:** Should increase by 1 when poison message is exhausted

---

## TIMING NOTES FOR CHAD

- **Total script:** 9 minutes (130–150 words per minute of speaking)
- **Slack buffer:** 1 minute for Q&A or contingency
- **Critical timing windows:**
  - [3:20]: Queue spike should be visible (50 messages queued quickly)
  - [6:25]: Poison message should start triggering retries
  - [7:00]: DLQ should show the poison message
- **Pacing:** Slow down at [7:15], [7:30], [7:45] for code comprehension; let audience read before moving on
- **Narrative tension:** Build from normal load → burst traffic → poison message; each phase is a ramp-up in complexity
