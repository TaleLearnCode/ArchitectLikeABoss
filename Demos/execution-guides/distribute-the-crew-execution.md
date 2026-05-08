# Demo Execution Guide: Distribute the Crew (Fan-Out/Fan-In Pattern)

**Duration:** 10 minutes  
**Pattern:** Fan-Out/Fan-In Orchestration with Parallel Activity Execution  
**Date:** March 19, 2026 (Las Vegas)

---

## Pre-Demo Checklist

Before going live on stage, Chad must verify:

- [ ] **Azure Functions deployed** — HeistFanOutFanIn project deployed to `https://<function-app>.azurewebsites.net`
- [ ] **Durable Functions runtime active** — Orchestrator and activity functions are registered
- [ ] **Storage account connected** — Durable Functions task hub is configured (required for orchestration state)
- [ ] **Application Insights connected** — Telemetry shows recent orchestration logs
- [ ] **Orchestrator function running** — `StartFanOutFanIn` endpoint responds to POST
- [ ] **Activity functions registered** — `ProcessDataActivity` is available to orchestrator
- [ ] **Test trigger succeeds** — Manual trigger returns valid JSON with activity results (see **Trigger Commands** below)
- [ ] **Concurrent execution visible** — Application Insights shows activity functions running in parallel
- [ ] **Fallback strategy ready** — Pre-recorded output with orchestration timeline and parallel execution metrics
- [ ] **Task Hub accessible** — Azure Storage table for task hub shows recent orchestrations

**Pre-recorded fallback location:** `C:\Presentations\ArchitectLikeABoss\Assets\demo-fallback-fanout-fanin.mp4`

---

## Trigger Commands & Expected Outputs

### Trigger Command 1: Basic Fan-Out/Fan-In Orchestration

**PowerShell Command:**
```powershell
$url = "https://<function-app>.azurewebsites.net/api/start-fanout?code=<function-key>"
$headers = @{ "Content-Type" = "application/json" }

$response = Invoke-WebRequest -Uri $url -Method POST -Headers $headers -Body ""
Write-Host "Status: $($response.StatusCode)"
$response.Content | ConvertFrom-Json | ConvertTo-Json
```

**curl equivalent:**
```bash
curl -X POST https://<function-app>.azurewebsites.net/api/start-fanout?code=<function-key> \
  -H "Content-Type: application/json" \
  -d "{}"
```

### Expected Response (Successful Orchestration)

**Status Code:** 200 OK

**Response Body:**
```json
{
  "instanceId": "550e8400-e29b-41d4-a716-446655440000",
  "activities": [
    {
      "activityName": "ProcessData-1",
      "elapsedMs": 1000,
      "data": "Result from ProcessData 1"
    },
    {
      "activityName": "ProcessData-2",
      "elapsedMs": 1500,
      "data": "Result from ProcessData 2"
    },
    {
      "activityName": "ProcessData-3",
      "elapsedMs": 800,
      "data": "Result from ProcessData 3"
    },
    {
      "activityName": "ProcessData-4",
      "elapsedMs": 2000,
      "data": "Result from ProcessData 4"
    },
    {
      "activityName": "ProcessData-5",
      "elapsedMs": 1200,
      "data": "Result from ProcessData 5"
    }
  ],
  "totalElapsedMs": 2000
}
```

**Key observations:**

1. **Five activity results** — Five `ProcessData` activities executed
2. **Variable completion times** — Activity 1 took 1000ms, Activity 2 took 1500ms, Activity 4 took 2000ms, etc.
3. **Total time = ~2000ms** — Despite 5 activities with variable latencies, total elapsed time is only ~2000ms (the slowest activity: Activity 4 with 2000ms)
4. **Parallel execution proof** — If activities ran sequentially, total would be 1000+1500+800+2000+1200 = 6500ms, but it's only 2000ms

---

### Trigger Command 2: Repeated Orchestrations (Verify No State Conflicts)

**PowerShell Script for 3 Sequential Orchestrations:**

```powershell
$url = "https://<function-app>.azurewebsites.net/api/start-fanout?code=<function-key>"
$headers = @{ "Content-Type" = "application/json" }

Write-Host "Starting 3 sequential orchestrations..."

for ($i = 1; $i -le 3; $i++) {
    $response = Invoke-WebRequest -Uri $url -Method POST -Headers $headers -Body ""
    $result = $response.Content | ConvertFrom-Json
    
    Write-Host "Orchestration $i:"
    Write-Host "  Instance ID: $($result.instanceId)"
    Write-Host "  Total time: $($result.totalElapsedMs)ms"
    Write-Host "  Activities completed: $($result.activities.Count)"
    
    # Wait 1 second before next orchestration
    Start-Sleep -Seconds 1
}
```

### Expected Response (Multiple Orchestrations)

Each orchestration should:
- Return a **unique instanceId** (different each time)
- Run **independently** (no state conflicts)
- Complete in **~2000ms** (consistent timing)
- Return **5 activity results** (same structure)

**Console Output:**
```
Starting 3 sequential orchestrations...
Orchestration 1:
  Instance ID: 550e8400-e29b-41d4-a716-446655440000
  Total time: 2000ms
  Activities completed: 5
Orchestration 2:
  Instance ID: 550e8400-e29b-41d4-a716-446655440001
  Total time: 2000ms
  Activities completed: 5
Orchestration 3:
  Instance ID: 550e8400-e29b-41d4-a716-446655440002
  Total time: 2001ms
  Activities completed: 5
```

---

## Code Walkthrough Mapping

### File 1: `HeistFanOutFanIn/FanOutFanInFunctions.cs`

**Purpose:** Contains the HTTP trigger (orchestrator starter) and activity function definitions.

**Display lines:** 11–62

#### Part A: HTTP Trigger — StartFanOutFanIn (lines 11–38)

**Key sections to highlight:**

**Lines 24–26** — Function decorator and HTTP trigger
```csharp
[Function("StartFanOutFanIn")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "start-fanout")] HttpRequestData req)
```

**Presentation notes:**
- "This HTTP endpoint is the entry point. Chad will POST to `/api/start-fanout` to start the orchestration."
- "This is the orchestrator starter — it initiates the fan-out/fan-in pattern."

**Lines 28–31** — Generate instance ID
```csharp
_logger.LogInformation("Fan-out/fan-in orchestrator triggered");

var instanceId = Guid.NewGuid().ToString();
var result = await _fanOutFanInService.OrchestrateAsync(instanceId);
```

**Presentation notes:**
- "We generate a unique instanceId to track this orchestration."
- "OrchestrateAsync is where the magic happens — orchestrating all the parallel activities."

**Lines 33–37** — Return aggregated result
```csharp
var response = req.CreateResponse(HttpStatusCode.OK);
response.Headers.Add("Content-Type", "application/json");
await response.WriteAsJsonAsync(result);
return response;
```

**Presentation notes:**
- "We return the OrchestrationResult, which includes all activity results and total execution time."

#### Part B: Activity Function — ProcessDataActivityFunction (lines 40–62)

**Key sections to highlight:**

**Lines 56–58** — Activity trigger
```csharp
[Function("ProcessDataActivity")]
public async Task<ActivityResult> Run([ActivityTrigger] int activityId)
```

**Presentation notes:**
- "This activity function processes a single piece of data."
- "It receives an activityId (1–5) and returns an ActivityResult with execution time and data."
- "[ActivityTrigger] is how Durable Functions binds activity functions — different from [HttpTrigger]."

**Lines 59–61** — Activity execution
```csharp
_logger.LogInformation("Activity function executing for ID: {ActivityId}", activityId);
return await _fanOutFanInService.ExecuteActivityAsync("ProcessData", activityId);
```

**Presentation notes:**
- "We log the activity execution and call the service to do the actual work."
- "The service returns timing information so we can see how long each activity took."

---

### File 2: `HeistFanOutFanIn/FanOutFanInService.cs`

**Purpose:** Contains the orchestration logic and activity execution.

**Display lines:** 29–90

#### Part A: Activity Execution (lines 29–58)

```csharp
public async Task<ActivityResult> ExecuteActivityAsync(string activityName, int activityId)
{
    _logger.LogInformation("Executing activity {ActivityName}-{ActivityId}", activityName, activityId);
    
    var stopwatch = Stopwatch.StartNew();
    
    // Simulate variable execution time
    var delay = activityId switch
    {
        1 => 1000,
        2 => 1500,
        3 => 800,
        4 => 2000,
        5 => 1200,
        _ => 1000
    };

    await Task.Delay(delay);
    stopwatch.Stop();

    _logger.LogInformation("Activity {ActivityName}-{ActivityId} completed in {ElapsedMs}ms", 
        activityName, activityId, stopwatch.ElapsedMilliseconds);

    return new ActivityResult
    {
        ActivityName = $"{activityName}-{activityId}",
        ElapsedMs = stopwatch.ElapsedMilliseconds,
        Data = $"Result from {activityName} {activityId}"
    };
}
```

**Presentation notes:**

**Lines 34–45: Variable Latency Simulation**
- "Each activity takes different amounts of time:"
  - "Activity 1: 1000ms"
  - "Activity 2: 1500ms"
  - "Activity 3: 800ms (fastest)"
  - "Activity 4: 2000ms (slowest)"
  - "Activity 5: 1200ms"
- "This simulates real-world scenarios where different operations have different latencies."

**Lines 47–49: Stopwatch & Logging**
- "We measure the elapsed time and log when the activity completes."
- "This data is crucial for the Application Insights metrics."

**Lines 51–57: Activity Result**
- "We return ActivityResult with activityName, execution time, and data."
- "The orchestrator collects all these results and aggregates them."

#### Part B: Orchestration Logic (lines 63–90)

```csharp
public async Task<OrchestrationResult> OrchestrateAsync(string instanceId)
{
    _logger.LogInformation("Starting orchestration for instance {InstanceId}", instanceId);
    
    var stopwatch = Stopwatch.StartNew();
    
    // In real implementation, these would be durable task calls
    var activities = new List<Task<ActivityResult>>();
    for (int i = 1; i <= 5; i++)
    {
        activities.Add(ExecuteActivityAsync("ProcessData", i));
    }

    var results = await Task.WhenAll(activities);
    stopwatch.Stop();

    _logger.LogInformation(
        "Orchestration {InstanceId} completed. Activities: {ActivityCount}, Total time: {ElapsedMs}ms",
        instanceId, results.Length, stopwatch.ElapsedMilliseconds
    );

    return new OrchestrationResult
    {
        InstanceId = instanceId,
        Activities = results.ToList(),
        TotalElapsedMs = stopwatch.ElapsedMilliseconds
    };
}
```

**Key concepts to highlight:**

**Lines 70–75: Fan-Out (Create Tasks)**
```csharp
var activities = new List<Task<ActivityResult>>();
for (int i = 1; i <= 5; i++)
{
    activities.Add(ExecuteActivityAsync("ProcessData", i));
}
```

**Presentation notes:**
- "This is the FAN-OUT phase."
- "We create 5 tasks: one for each activity function."
- "Notice we're creating all 5 tasks in a loop — we're not waiting for any of them yet."
- "All 5 are queued to run in parallel."

**Line 77: Fan-In (Wait for All)**
```csharp
var results = await Task.WhenAll(activities);
```

**Presentation notes:**
- "This is the FAN-IN phase."
- "Task.WhenAll waits for ALL 5 activities to complete."
- "We don't proceed until all results are ready."
- "Because they run in parallel, total time = max(1000, 1500, 800, 2000, 1200) = 2000ms."

**Lines 79–83: Logging & Aggregation**
```csharp
_logger.LogInformation(
    "Orchestration {InstanceId} completed. Activities: {ActivityCount}, Total time: {ElapsedMs}ms",
    instanceId, results.Length, stopwatch.ElapsedMilliseconds
);
```

**Presentation notes:**
- "We log the completion with activity count and total elapsed time."
- "This information appears in Application Insights for monitoring and diagnostics."

**Lines 85–90: Return Aggregated Result**
```csharp
return new OrchestrationResult
{
    InstanceId = instanceId,
    Activities = results.ToList(),
    TotalElapsedMs = stopwatch.ElapsedMilliseconds
};
```

**Presentation notes:**
- "The orchestrator returns all activity results and the total execution time."
- "The caller (HTTP client) gets the complete picture of what happened in parallel."

---

### File 3: Data Models (lines 93–105)

Display the response classes:

```csharp
public class ActivityResult
{
    public string ActivityName { get; set; } = string.Empty;
    public long ElapsedMs { get; set; }
    public string Data { get; set; } = string.Empty;
}

public class OrchestrationResult
{
    public string InstanceId { get; set; } = string.Empty;
    public List<ActivityResult> Activities { get; set; } = new();
    public long TotalElapsedMs { get; set; }
}
```

**Presentation notes:**
- "ActivityResult holds the outcome of a single activity: name, timing, and data."
- "OrchestrationResult aggregates all activity results into one response."
- "The InstanceId allows Chad (or monitoring systems) to correlate this orchestration with its logs in Application Insights."

---

## Application Insights Monitoring

During the demo, Chad should watch Application Insights for:

### 1. Traces Tab (Orchestration & Activity Logs)

**Filter for:** Keyword `Starting orchestration` or `Activity`

**Expected logs during orchestration:**
```
Starting orchestration for instance 550e8400-e29b-41d4-a716-446655440000

Executing activity ProcessData-1
Executing activity ProcessData-2
Executing activity ProcessData-3
Executing activity ProcessData-4
Executing activity ProcessData-5

Activity ProcessData-3 completed in 800ms
Activity ProcessData-1 completed in 1000ms
Activity ProcessData-5 completed in 1200ms
Activity ProcessData-2 completed in 1500ms
Activity ProcessData-4 completed in 2000ms

Orchestration 550e8400-e29b-41d4-a716-446655440000 completed. Activities: 5, Total time: 2000ms
```

**Key observations:**
- All 5 activities are logged as starting at (nearly) the same time
- Completions appear in order of their actual durations (Activity 3 first at 800ms, Activity 4 last at 2000ms)
- **Total time is 2000ms** (proof of parallelism)

### 2. Custom Metrics in Application Insights

If custom metrics are enabled, Chad can monitor:
- **Parallel Activity Count** — Should spike to 5 concurrent activities
- **Orchestration Duration** — Should consistently show ~2000ms
- **Per-Activity Duration** — Activity 4 should consistently be ~2000ms (slowest)

### 3. Performance Insights Tab

**Operation:** `StartFanOutFanIn`
- Request duration: ~2000ms (matching orchestration time)
- Success rate: 100%
- Activity: 5 parallel activities per orchestration

---

## Parallel Execution Verification

### How to Confirm Parallelism (Not Sequential)

**Sequential execution would be:**
1000 + 1500 + 800 + 2000 + 1200 = **6500ms total**

**Actual parallel execution:**
max(1000, 1500, 800, 2000, 1200) = **2000ms total**

**In Application Insights Logs:**
- All 5 "Executing activity" logs appear within milliseconds of each other (timestamps separated by <10ms)
- Activity completion logs appear in order of completion time, not order of start
- Total elapsed time matches the slowest activity

---

## Fallback Activation (If Live Fails)

### Scenario 1: HTTP Trigger Returns Error

**Symptoms:** PowerShell command returns 500 error or Function not found.

**Fallback Steps:**
1. **Pause demo** — "Let me show you what a successful orchestration looks like."
2. **Switch display** — Show pre-recorded screen capture of HTTP request and JSON response
3. **Narrate response** — Point out the 5 activities and 2000ms total time
4. **Show metrics** — "Here's what Application Insights shows: 5 concurrent activities, 2000ms total."
5. **Continue** — Move to code walkthrough (not dependent on live execution)

### Scenario 2: Activities Don't Complete in Parallel

**Symptoms:** Total elapsed time is ~6500ms instead of ~2000ms (activities ran sequentially).

**Fallback Steps:**
1. **Show code** — Jump to lines 70–77 in FanOutFanInService.cs
2. **Narrate** — "This is the fan-out: we create 5 tasks. Task.WhenAll waits for all of them in parallel."
3. **Show Application Insights logs** — Pre-recorded screenshot showing all 5 activities starting at nearly the same time
4. **Highlight timing** — Point out "Activity 3 completed in 800ms" appearing before "Activity 4 completed in 2000ms"
5. **Explain** — "If they ran sequentially, we'd see each one complete in order. Here, they complete as they finish."

### Scenario 3: Orchestration Logs Not Visible

**Symptoms:** Application Insights shows no recent logs.

**Fallback Steps:**
1. **Explain** — "Application Insights has a slight delay. Let me show you pre-recorded logs."
2. **Show screenshot** — Display Application Insights Traces with orchestration logs
3. **Point out key logs** — Show "Starting orchestration," all 5 "Executing activity" entries, and "Orchestration completed"
4. **Narrate** — "This shows exactly what happens during orchestration: start, fan-out to 5 activities, wait for all, return results."

### Recovery & Next Steps

After using a fallback:
- **Do NOT attempt** another live trigger
- **Focus on code walkthrough** — This illustrates the pattern regardless of live execution
- **Show final metrics slide** — Summarize the fan-out/fan-in pattern benefits

---

## Demo Script Timeline

| Time | Action | Expected Output |
|------|--------|-----------------|
| 0:00 | Intro: "Distribute the Crew - Fan-Out/Fan-In Pattern" | Visual slide |
| 0:30 | Show HTTP trigger (StartFanOutFanIn, lines 24–37) | HTTP orchestrator visible |
| 1:30 | Highlight: "Create unique instanceId for tracking" | instanceId highlighted |
| 2:00 | Show activity function (ProcessDataActivityFunction, lines 56–61) | Activity function visible |
| 2:30 | Show orchestration logic (OrchestrateAsync, lines 70–77) | Task.WhenAll highlighted |
| 3:00 | **Pause and explain** — "Here's where the parallel magic happens" | Code narration |
| 3:30 | Show activity delays (lines 36–44) | Variable latencies shown (1000, 1500, 800, 2000, 1200) |
| 4:00 | Point out: "Activity 4 is slowest (2000ms)" | Line 41 highlighted |
| 4:30 | **LIVE TRIGGER** — Submit orchestration | 202 Accepted or immediate response |
| 5:00 | Show response JSON: 5 activities, ~2000ms total | Response visible on screen |
| 5:30 | Highlight proof of parallelism: "Total 2000ms, not 6500ms" | Math shown: max(1000,1500,800,2000,1200) = 2000 |
| 6:00 | Show Application Insights: All 5 activities logged | Traces tab visible |
| 6:30 | Point out completion order: "3, 1, 5, 2, 4 (not sequential)" | Log timestamps shown |
| 7:00 | Run 3 sequential orchestrations | Show 3 unique instanceIds, all ~2000ms |
| 7:30 | Explain: "Each orchestration runs independently, no state conflicts" | Architectural benefit explained |
| 8:00 | Summary: "Fan-out: create parallel tasks. Fan-in: wait for all. Time = max, not sum." | Summary slide |
| 8:30 | Show use cases: "Image processing, batch data transformation, distributed calculations" | Use case examples |
| 9:00 | Transition to closing remarks | Visual transition |

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| HTTP trigger returns 404 | Verify endpoint URL and function key; check function deployment status |
| Activities don't run in parallel (total time ~6500ms) | Verify code uses Task.WhenAll (line 77); check that no sequential awaits exist |
| Application Insights shows no activity logs | Wait 2–3 minutes for telemetry; verify Application Insights connection string in function app settings |
| Total elapsed time varies wildly | Expected if activities are truly parallel; variation is normal. Average should be ~2000ms. |
| Multiple orchestrations show same instanceId | Verify instanceId is generated with Guid.NewGuid() (line 30); each orchestration should have unique ID |
| Some activities never appear in logs | Check that all 5 activities are created in the loop (lines 71–74); verify logging is enabled |

---

## Key Takeaway for Audience

> **"The fan-out/fan-in pattern lets you decompose work into independent parallel tasks. Instead of doing 5 tasks sequentially in 6.5 seconds, you send them all to run in parallel and wait 2 seconds for the slowest one. This is essential for orchestrating complex workflows where tasks can run independently—image processing, data transformations, distributed calculations. Durable Functions make this pattern easy and reliable."**

---

## Advanced Concepts (Optional Deep Dive)

### Why Durable Functions Matter for This Pattern

In the code, we're using `Task.WhenAll` for parallel execution. Real Durable Functions extend this with:

1. **Persistent State** — Orchestration state is saved to Storage, allowing recovery from failures
2. **Automatic Retries** — If an activity fails, Durable Functions can automatically retry it
3. **Versioning** — Multiple versions of orchestrations can run simultaneously
4. **Checkpointing** — Long-running orchestrations are saved at each activity completion

**Note for Chad:** This demo is a simplified version. Production use would leverage these Durable Functions features for enterprise reliability.

### Monitoring Real Durable Functions Orchestrations

In Azure Portal, Durable Functions Dashboard shows:
- **Instance History** — All orchestration runs with status and timing
- **Execution Timeline** — Visual timeline of when each activity ran
- **Replay Logs** — Detailed logs of orchestration replay (how Durable Functions recovers state)

These are beyond the scope of this demo but are important for production deployments.
