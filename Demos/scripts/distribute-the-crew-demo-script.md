# "Distribute the Crew" — Fan-Out/Fan-In Pattern Demo Script
## 10-Minute Live Demo

---

## OPENING NARRATION [0:00 - 0:30]

*[Stand center stage. Dramatic, building energy. Heist metaphor with emphasis on coordination]*

**[0:00]** "In Ocean's Eleven, eleven crew members don't work sequentially—they work in parallel. The con artist distracts the guard. The hacker breaches the system. The safe cracker opens the vault. All at the same time. If they worked one-by-one, the heist would take forever and security would catch them. Parallelism is survival."

**[0:15]** "In serverless systems, we have the same challenge: you need to fan out work to multiple parallel processors, collect all results, and then move forward. Think image resizing—you have 1,000 images, 10 parallel workers, each processes 100. In 10 seconds, you're done. Sequentially? That's 100+ seconds."

**[0:25]** "What you're about to see is **Durable Functions orchestrating 5 parallel activities**, collecting results, and returning a summary. This is the Fan-Out/Fan-In pattern in pure action."

---

## SETUP & PRE-FLIGHT CHECKS [0:30 - 1:30]

**[0:30] - CLICK: Open VS Code with code visible**

*Display the Durable Functions code locally:*
- Open VS Code with the orchestrator and activity functions visible
- Show `StartFanOutFanIn`, `ProcessDataActivity`, `FanOutFanInService`
- [NOTE: We're showing code locally but will execute in Azure]

**[0:38] - OBSERVE: Function App deployed**
- Show `StartFanOutFanIn` (HTTP trigger, orchestrator function) is active
- Show `ProcessDataActivity` (activity function) is registered
- [NOTE: If Portal is slow, proceed with code review while Portal loads]

**[0:45] - CLICK: Open Application Insights**

*Navigate to the workspace (name from Linus). Show:*
- Live Logs view ready
- Optional: Pre-configure a chart showing Activity Execution Duration over time

**[0:55] - OBSERVE: Durable Functions monitoring**
- Show the "Durable Functions Instances" view is ready to stream orchestration status
- Say: "In moments, we'll trigger an orchestration. We'll see it fan out to 5 parallel activities, each with its own execution time, all running concurrently. Then the orchestrator waits for all to complete before returning the final result."

**[1:10] - CLICK: Prepare the Azure Function URL**

*Have the orchestrator endpoint ready:*
- Production URL: `https://func-distribute-crew-dev.azurewebsites.net/api/start-fanout`
- We'll trigger the orchestration from this endpoint

**[1:20] - CALL OUT:**
- "The orchestrator fans out to 5 parallel activities using `Task.WhenAll`"
- "Each activity has a different execution time (800ms to 2000ms)"
- "The orchestrator waits for all activities to complete"
- "Total time is governed by the slowest activity, not the sum of all (because they run in parallel)"

**[1:30] - SAY:** "Functions ready. Monitoring ready. Let's see the live infrastructure."

---

## INFRASTRUCTURE SHOWCASE [1:30 - 3:00]

**[1:30] - CLICK: Open Azure Portal**

*Navigate to: https://portal.azure.com/#resource/subscriptions/{subid}/resourceGroups/rg-heist-dev-eus/overview*

**[1:35] - SAY:**
"Before we orchestrate 11 parallel activities, let me show you what makes this possible. This is live Azure infrastructure—Durable Functions running with all the plumbing behind the scenes."

**[1:40] - OBSERVE: Resource Group Overview**

*Show `rg-heist-dev-eus` resource group with deployed resources:*
- Function App: `func-distribute-crew-dev`
- Storage Account: `stheistvaultdev`
- Application Insights: `appinsights-heist-dev`

**[1:45] - CLICK: Open Function App**

*Navigate to `func-distribute-crew-dev`. Show functions list:*
- `StartFanOutFanIn` (HTTP trigger, orchestrator)
- `ProcessDataActivity` (Activity function)

**[1:50] - SAY:**
"Two functions: The orchestrator coordinates the parallel work. The activity function is the worker—each parallel task is an instance of this activity running concurrently. When we fan out to 5 activities, this function runs 5 times in parallel. Scale to 11? It runs 11 times."

**[2:00] - CLICK: Navigate to Storage Account**

*Open `stheistvaultdev` → Show containers or tables (whichever is easier to navigate):*
- Point out: Task Hub storage (used by Durable Functions to track orchestration state)

**[2:05] - SAY:**
"Behind the scenes, Durable Functions uses blob storage as a Task Hub to coordinate state across parallel activities. When an activity completes, its result is persisted here. The orchestrator polls this storage to know when all activities are done. It's invisible to us, but critical for resilience—if the Function App crashes mid-orchestration, it can resume from this state."

**[2:15] - CLICK: Navigate back to Resource Group → Application Insights**

*Open `appinsights-heist-dev` → Live Metrics Stream. Show:*
- Incoming requests
- Server response time
- Concurrent requests (will spike with parallel activities)

**[2:25] - OBSERVE: Live Metrics Ready**

*Point out key sections:*
- **Request Rate:** Where orchestrator and activity executions appear
- **Server Response Time:** Should show orchestration completing in ~2050ms
- **Server Metrics → Request Execution Time:** Will show concurrency when activities run in parallel

**[2:35] - SAY:**
"This Live Metrics view will show us the parallelism in action. When we trigger the orchestration, you'll see 5 activity requests hit nearly simultaneously. The concurrency metric will spike—that's our proof that these activities are running in parallel, not sequentially."

**[2:45] - CLICK: (Optional) Show Durable Functions Instances Tab**

*If available in Azure Portal:*
- Navigate to Function App → Durable Functions
- Show the Instances view (should be empty or show past runs)

**[2:50] - SAY:**
"This is the Durable Functions dashboard. When we trigger the orchestration in a moment, a new instance will appear here with a unique ID. We can drill into it and see each activity, when it started, when it completed, and the results. Full observability into orchestrated workflows."

**[2:55] - CALL OUT: Portal Bookmark Tip**

*[NOTE: Have App Insights Live Metrics and Durable Functions Instances tabs pre-opened or bookmarked]*

**[3:00] - SAY:**
"Alright, infrastructure ready. Let's trigger the orchestration and watch the crew distribute."

**[3:00] - CONTINGENCY NOTE:**
*[If Portal is slow or Durable Functions dashboard not available, skip to execution and say: "Portal is catching up—let's trigger the orchestration and watch the logs instead."]*

---

## LIVE DEMO EXECUTION [3:00 - 7:30]

### PHASE 1: Trigger the Orchestration [3:00 - 3:45]

**[3:00] - CLICK: Prepare the HTTP trigger call for Azure**

*Have an API call ready:*
```
POST https://func-distribute-crew-dev.azurewebsites.net/api/start-fanout
Content-Type: application/json

{ "activityCount": 5 }
```

**[1:35] - SAY:**
"I'm about to trigger the orchestrator. This HTTP call will kick off an orchestration instance that will fan out to 5 parallel activities. Watch the Durable Functions dashboard and Application Insights—you'll see the orchestration instantly, and then each activity light up as it executes."

**[3:15] - CLICK: Send the HTTP POST request to Azure**

*Execute the request to the live Azure Function. Expect a response like:*

```json
{
  "instanceId": "abc123def456...",
  "activities": [],
  "totalElapsedMs": 0
}
```

(The response might be empty or show partial data if the orchestration hasn't completed yet—this is normal for async orchestrations.)

**[3:20] - OBSERVE: Durable Functions Instances dashboard**

*In Azure Portal (Durable Functions Instances tab) or local dashboard, watch:*

```
Orchestration Instance Created
Instance ID: abc123def456...
Status: Running
Started: 2026-03-19T14:30:00Z
```

**[3:30] - OBSERVE: Application Insights Logs**

*Watch logs stream in. You should see:*

```
[INF] Fan-out/fan-in orchestrator triggered
[INF] Starting orchestration for instance {InstanceId}
[INF] Executing activity ProcessData-1
[INF] Executing activity ProcessData-2
[INF] Executing activity ProcessData-3
[INF] Executing activity ProcessData-4
[INF] Executing activity ProcessData-5
```

**[3:40] - NARRATE:**
"There it is. Orchestration started. Five activities kicked off in parallel. Notice the timestamps—they're all nearly identical. That's parallel execution. In a sequential model, activity 2 wouldn't start until activity 1 finished. Here, all five start at nearly the same time."

**[3:45] - SAY:** "Now we wait. Let's see each activity complete."

---

### PHASE 2: Parallel Activity Execution [3:45 - 4:45]

**[3:45] - OBSERVE: Activity execution logs**

*As activities execute, watch logs stream in with actual execution times:*

```
[INF] Activity {ActivityName}-1 executing
[WAIT] ... 1000ms passes ...
[INF] Activity ProcessData-1 completed in 1000ms
[INF] Result: Result from ProcessData 1

[INF] Activity {ActivityName}-3 executing
[WAIT] ... 800ms passes ...
[INF] Activity ProcessData-3 completed in 800ms
[INF] Result: Result from ProcessData 3

[INF] Activity {ActivityName}-5 executing
[WAIT] ... 1200ms passes ...
[INF] Activity ProcessData-5 completed in 1200ms
[INF] Result: Result from ProcessData 5

[INF] Activity {ActivityName}-2 executing
[WAIT] ... 1500ms passes ...
[INF] Activity ProcessData-2 completed in 1500ms
[INF] Result: Result from ProcessData 2

[INF] Activity {ActivityName}-4 executing
[WAIT] ... 2000ms passes ...
[INF] Activity ProcessData-4 completed in 2000ms
[INF] Result: Result from ProcessData 4
```

**[4:00] - NARRATE:**
"Watch the execution times:
- Activity 3: 800ms (fastest)
- Activity 1: 1000ms
- Activity 5: 1200ms
- Activity 2: 1500ms
- Activity 4: 2000ms (slowest)

These aren't waiting for each other. They're running concurrently. In a sequential model, total time would be 800 + 1000 + 1200 + 1500 + 2000 = 6,500ms. Here, all five are running in parallel, so total time is limited by the slowest one: 2000ms."

**[4:15] - OBSERVE: Durable Functions dashboard**

*The orchestration instance status should now show:*

```
Instance ID: abc123def456...
Status: Completed
Completed: 2026-03-19T14:30:02.000Z
Duration: ~2000ms
```

**[4:25] - CLICK: Show the orchestration history**

*In the Durable Functions dashboard, expand the instance to show:*
- 5 activity tasks, each with its execution time
- All started at approximately the same time
- All completed by T+2000ms

**[3:40] - NARRATE:**
"This is orchestration visibility. We can see exactly when each activity started, when it completed, how long it took, and what order they executed in. This is critical for debugging and understanding system behavior. If one activity suddenly starts taking 10 seconds instead of 2, we see it immediately."

**[4:45] - OBSERVE: Final response**

*The orchestrator waits for all activities to complete, then returns the aggregated result. You should see (in Postman, logs, or dashboard):*

```json
{
  "instanceId": "abc123def456...",
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
  "totalElapsedMs": 2050
}
```

**[4:55] - NARRATE:**
"Total elapsed time: ~2050ms. That includes the overhead of orchestrator startup, activity scheduling, and result aggregation. But the key point: we processed 5 parallel activities in the time it takes to process the slowest one. This is the power of Fan-Out/Fan-In."

**[5:00] - SAY:** "Now let's trigger the orchestration again, but this time, I'll scale it up. Let's fan out to 11 activities—like coordinating Ocean's Eleven."

---

### PHASE 3: Scaling to 11 Activities [5:00 - 6:00]

**[5:00] - CLICK: Modify the code (or use a pre-prepared variant)**

*If live coding:*
- Open `FanOutFanInService.cs`
- Change the loop from `for (int i = 1; i <= 5; i++)` to `for (int i = 1; i <= 11; i++)`
- Save and redeploy (or restart local function)

**[5:10] - [WAIT] for redeployment**

*Narrate while waiting:*
"This is the beauty of Fan-Out/Fan-In: it's trivial to scale from 5 to 11 to 100 parallel activities. We just change a number and redeploy. Conceptually, the pattern is identical. Orchestrate, fan out, fan in, return results. The framework handles all the complexity of parallel execution, synchronization, and result collection."

**[5:20] - SAY:** "Alright, redeployed. Now let's trigger the 11-activity orchestration."

**[5:25] - CLICK: Send the HTTP POST request again**

*Execute the same endpoint. This time, it will trigger 11 parallel activities.*

**[5:30] - OBSERVE: Durable Functions dashboard**

*Watch the orchestration instance show:*

```
Instance ID: xyz789abc...
Status: Running
Activities: 11
Started activities...
```

**[5:40] - OBSERVE: Application Insights Logs**

*Watch 11 activity execution logs stream in:*

```
[INF] Executing activity ProcessData-1
[INF] Executing activity ProcessData-2
[INF] Executing activity ProcessData-3
[INF] Executing activity ProcessData-4
[INF] Executing activity ProcessData-5
[INF] Executing activity ProcessData-6
[INF] Executing activity ProcessData-7
[INF] Executing activity ProcessData-8
[INF] Executing activity ProcessData-9
[INF] Executing activity ProcessData-10
[INF] Executing activity ProcessData-11
```

(All logs appear nearly simultaneously, showing parallel execution.)

**[5:55] - NARRATE:**
"11 activities fanned out in parallel. Each will execute on its own timeline. The orchestrator is waiting for all 11 to complete. This is massive scalability. Add 100? Same code, same pattern. Add 1,000? Still works. The Durable Functions framework handles the complexity of scheduling, managing state, and synchronizing results."

**[6:00] - OBSERVE: Orchestration completion**

*After ~2 seconds (slowest activity duration), watch the orchestration complete:*

```
Instance ID: xyz789abc...
Status: Completed
Completed: 2026-03-19T14:30:04.000Z
Duration: ~2050ms
Activities: 11 completed
```

**[6:10] - OBSERVE: Final response with 11 results**

*The response will show all 11 activity results aggregated:*

```json
{
  "instanceId": "xyz789abc...",
  "activities": [ ... 11 activity results ... ],
  "totalElapsedMs": 2050
}
```

**[6:20] - NARRATE:**
"Total time to process 11 parallel activities: still ~2050ms. If we had done this sequentially, activity 1 then 2 then 3... we'd be waiting for minutes. The parallelism is the entire value. This is how you process 50,000 images in a reasonable timeframe."

**[6:30] - SAY:** "Now let's talk about what makes this possible."

---

### PHASE 4: The Pattern Breakdown [6:30 - 7:20]

**[6:30] - CLICK: Show the orchestrator code**

```csharp
[Function("StartFanOutFanIn")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "start-fanout")] HttpRequestData req)
{
  _logger.LogInformation("Fan-out/fan-in orchestrator triggered");
  
  var instanceId = Guid.NewGuid().ToString();
  var result = await _fanOutFanInService.OrchestrateAsync(instanceId);
  
  var response = req.CreateResponse(HttpStatusCode.OK);
  await response.WriteAsJsonAsync(result);
  return response;
}
```

**[6:40] - NARRATE:**
"HTTP trigger receives the request, generates a unique instance ID, and calls the orchestration service. The orchestrator does all the heavy lifting—fanning out, waiting, collecting results. All we do is return the result to the caller. This decouples the HTTP request lifecycle from the actual work being done."

**[6:55] - CLICK: Show the orchestration logic**

```csharp
public async Task<OrchestrationResult> OrchestrateAsync(string instanceId)
{
  _logger.LogInformation("Starting orchestration for instance {InstanceId}", instanceId);
  
  var stopwatch = Stopwatch.StartNew();
  
  var activities = new List<Task<ActivityResult>>();
  for (int i = 1; i <= 5; i++)  // or 11, or 100, or 1000
  {
    activities.Add(ExecuteActivityAsync("ProcessData", i));
  }

  var results = await Task.WhenAll(activities);
  stopwatch.Stop();
  
  return new OrchestrationResult
  {
    InstanceId = instanceId,
    Activities = results.ToList(),
    TotalElapsedMs = stopwatch.ElapsedMilliseconds
  };
}
```

**[7:05] - NARRATE:**
"The orchestrator creates a list of activity tasks and fans them all out using `Task.WhenAll(activities)`. This is the magic line: `Task.WhenAll` waits for all tasks to complete. Until all activities are done, the orchestrator blocks. Once all complete, it collects the results and returns them. The number of activities is a parameter—change the loop to 1000, and it scales automatically."

**[7:15] - CLICK: Show the activity function**

```csharp
[Function("ProcessDataActivity")]
public async Task<ActivityResult> Run([ActivityTrigger] int activityId)
{
  _logger.LogInformation("Activity function executing for ID: {ActivityId}", activityId);
  return await _fanOutFanInService.ExecuteActivityAsync("ProcessData", activityId);
}
```

**[7:20] - NARRATE:**
"An activity function is decorated with `[ActivityTrigger]`. It receives a parameter (activityId), executes its work, and returns a result. Multiple instances of this function can run in parallel—Azure Durable Functions schedules them across available compute. If you scale your Function App, more instances mean more parallel activities can run simultaneously."

---

## CLOSING REMARKS [7:20 - 9:00]

**[7:20] - NARRATE:**
"Fan-Out/Fan-In is the orchestration pattern for massive parallelism. You decompose a large job into independent parallel tasks, coordinate them, and wait for results. It's like Ocean's Eleven: 11 crew members do their jobs in parallel, then reconvene to share what they learned."

**[7:40] - REAL-WORLD CALL-OUT:**
"Imagine you're processing 50,000 images for a real estate platform. Each image needs resizing, thumbnail generation, watermarking, metadata extraction. With 10 parallel activity functions, you can process 50,000 images in minutes instead of hours. The Fan-Out/Fan-In pattern, combined with Durable Functions, makes this trivial to implement and monitor."

**[7:55] - BRIDGE TO ARCHITECTURE:**
"The key insight: Durable Functions provides orchestration, state management, and synchronization out of the box. You don't build custom coordination logic. You declare the pattern (fan out, wait, collect), and the framework handles retries, timeouts, distributed tracing, and result persistence. This is serverless orchestration at scale."

**[8:10] - CLOSING STATEMENT:**
"You've now seen three core patterns: **Aggregation** for parallel composition with timeout/fallback, **Queue-Based Load Leveling** for asynchronous decoupling and resilience, and **Fan-Out/Fan-In** for massive parallelism and coordination. Together, these patterns solve the vast majority of distributed systems challenges. Combine them, and you can build systems that scale, fail gracefully, and stay observable."

**[8:30] - END OF DEMO**

---

## FALLBACK & CONTINGENCIES

### If the HTTP request doesn't trigger orchestration:
- **[NOTE: Orchestration not starting]** Check the Function App logs for any startup errors.
- Verify the Durable Functions runtime is configured (check `local.settings.json` for `FUNCTIONS_WORKER_RUNTIME`)
- Fallback: Show pre-recorded screenshots of an orchestration instance and its results

### If Durable Functions dashboard isn't available:
- **[NOTE: Dashboard not accessible]** Not all regions have full Durable Functions UI. Fall back to Application Insights logs.
- Logs will show orchestration status and activity execution, which is sufficient to narrate the pattern
- Have a screenshot of the dashboard pre-captured for reference

### If activities don't run in parallel:
- **[NOTE: Sequential execution observed]** Check logs to confirm if activities are starting at different times.
- This could indicate a configuration issue. Verify `Task.WhenAll` is being called (not sequential awaits)
- Fallback: Narrate the expected parallel behavior while showing the code; audience can see from the code that parallelism is intentional

### If Application Insights logs are delayed:
- **[NOTE: Logs not streaming]** Azure can have a 30-second delay. Keep talking while waiting.
- Show logs from the local console output as backup (if running locally)
- Pre-record a screenshot of expected logs

### If the 11-activity redeploy takes too long:
- **[NOTE: Deployment delay]** Azure deployments can take 1-2 minutes. Have a pre-prepared version with 11 activities already deployed.
- Alternatively, show code slides explaining the scaling from 5 to 11 and narrate the expected behavior without live execution
- The 5-activity demo is sufficient to prove the pattern; 11 is a bonus for dramatic effect

---

## KEY METRICS TO CALL OUT

From Application Insights and Durable Functions monitoring, reference these:

1. **Activity Execution Time:** Each activity should show actual execution time (800ms–2000ms range)
2. **Orchestration Total Time:** Should be ~2000ms–2050ms (slowest activity + overhead), NOT 5000+ms (which would indicate sequential execution)
3. **Activity Parallelism:** Start timestamps should be within 10ms of each other (showing true parallelism)
4. **Orchestration State:** Should show "Completed" after all activities are done
5. **Result Count:** Should match the number of fanned-out activities (5 or 11)

---

## TIMING NOTES FOR CHAD

- **Total script:** 9 minutes (130–150 words per minute of speaking)
- **Slack buffer:** 1 minute for Q&A or contingency
- **Critical timing windows:**
  - [2:30]: First orchestration should start
  - [4:15]: First orchestration (5 activities) should complete
  - [5:15]: Second orchestration (11 activities) should start
  - [6:15]: Second orchestration should complete
- **Pacing:** Slow down at [6:30], [7:00], [7:20], [7:40] for code comprehension
- **Narrative arc:** Start with 5 activities → show parallelism → scale to 11 → explain the code
- **Visual contrast:** Emphasize time difference between sequential (6.5 seconds) vs. parallel (2 seconds)

---

## DURABLE FUNCTIONS SPECIFIC NOTES

- **Instance ID:** Each orchestration gets a unique instance ID. This is persisted and queryable, enabling you to check status even hours later.
- **Activity Trigger:** Activities are invoked by the orchestrator via `[ActivityTrigger]` and can be called multiple times (retried) within the same orchestration without creating new instances.
- **Task.WhenAll:** This is the synchronization point. All activities are scheduled in parallel, but orchestrator waits here until all complete.
- **State Persistence:** Durable Functions persists orchestration state to table storage or custom backends, enabling recovery from infrastructure failures (e.g., a VM crash in the middle of orchestration).
- **Scaling:** The number of activities can be scaled up to hundreds or thousands without code changes—the pattern remains identical. Infrastructure (Function App instances) scales horizontally to handle the load.
