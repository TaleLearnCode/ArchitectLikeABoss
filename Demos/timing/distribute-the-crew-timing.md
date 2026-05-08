# "Distribute the Crew" — Fan-Out/Fan-In Demo Timing
**Architect:** Danny  
**Duration:** 10 minutes (exactly)  
**Pattern:** Fan-Out/Fan-In Orchestration with Parallel Activities  
**Presentation Date:** March 19, 2026 (Las Vegas)

---

## Executive Timing Allocation

| Segment | Time | Duration | Activity |
|---------|------|----------|----------|
| **Opening Narration** | 0:00–0:30 | 30 sec | Heist metaphor (parallelism = speed) |
| **Setup & Pre-Flight** | 0:30–1:30 | 1 min | Verify Durable Functions, show orchestrator, open Application Insights |
| **Code Walkthrough** | 1:30–2:30 | 1 min | Display orchestrator logic, Task.WhenAll pattern, activity functions |
| **Trigger Orchestration** | 2:30–3:00 | 30 sec | Run `StartFanOutFanIn` command, capture orchestration ID |
| **Live Execution (5 activities)** | 3:00–5:30 | 2.5 min | Watch 5 activities execute in parallel (~2 sec execution); observe in Application Insights; show execution timeline |
| **Scale Demonstration** | 5:30–7:00 | 1.5 min | (Optional) Trigger again with 11 activities, show same ~2 sec execution time (parallelism benefit); explain why sequential would be 15+ sec |
| **Metrics Review** | 7:00–8:30 | 1.5 min | Show Application Insights: concurrency count, activity execution times, orchestrator wait time, total duration |
| **Closing Remarks** | 8:30–10:00 | 1.5 min | Recap pattern (parallelize work, synchronize results), real-world use case (image processing), bridge to next topic |

---

## Minute-by-Minute Breakdown

### **[0:00–0:30] OPENING NARRATION**

**Timing:** 30 seconds (read naturally)

**Content:** Heist metaphor on parallelism:
- "Eleven crew members don't work sequentially—they work in parallel."
- "Fan-out/fan-in: send work to 5+ parallel processors, collect results, continue."
- "Why? 1,000 images ÷ 10 workers = done in ~10 sec. Sequentially = 100+ sec."
- "You're about to see Durable Functions orchestrating 5 parallel activities."

**Speaker Notes:**
- Make eye contact. Build energy and emphasis on "parallelism."
- Use gestures: hands spread wide for "parallel," then brought together for "collect results."
- Tie to familiar audience concept: "Like your team working on different tasks at the same time, not one at a time."

---

### **[0:30–1:30] SETUP & PRE-FLIGHT CHECKS**

**Timing:** 1 minute

**[0:30–0:38] (8 sec):** Open Azure Portal or Durable Functions dashboard
- **Action:** [CLICK] Open browser tab with Azure Portal → Function App → Durable Functions Instances
- **Expected:** Portal loads function list
- **Contingency:** If Portal slow, skip to code view (VS Code tab)

**[0:38–0:45] (7 sec):** Verify orchestrator and activity functions deployed
- **OBSERVE:** 
  - `StartFanOutFanIn` function is listed and shows "HTTP" trigger
  - `ProcessDataActivity` function is listed as activity function
  - Function App status = "Running"
- **Fallback:** If Portal shows "Not Found," proceed with local emulator or pre-recorded output

**[0:45–0:55] (10 sec):** Open Application Insights
- **Action:** [CLICK] Application Insights workspace (name: from Linus infrastructure spec, e.g., `appinsights-heist-dev-lv`)
- **Expected:** Live Logs view loads
- **Say:** "Here we'll see the orchestration in real-time: activity execution, concurrency, timing."

**[0:55–1:10] (15 sec):** Review Durable Functions monitoring dashboard
- **OBSERVE:** Durable Functions Instances view shows:
  - "Orchestrator Function" trigger (orchestration history)
  - "Activity Functions" section (parallel activity tracking)
  - Recent orchestrations (if any previous test runs)
- **Say:** "In 90 seconds, we'll trigger an orchestration and watch it fan out to 5 parallel activities. Each activity processes independently. Then the orchestrator waits for all 5 to complete."

**[1:10–1:20] (10 sec):** Open VS Code with function code
- **Action:** [CLICK] VS Code tab (or click-through to code view)
- **Expected:** Show:
  - `FanOutFanInOrchestrator.cs` (orchestrator function)
  - `ActivityFunctions.cs` (activity functions)
  - `StartFanOutFanIn.cs` (HTTP trigger)

**[1:20–1:30] (10 sec):** Buffer for Portal loading or code review
- **If slow:** Use this time to explain orchestrator pattern aloud while waiting

---

### **[1:30–2:30] CODE WALKTHROUGH**

**Timing:** 1 minute

**[1:30–1:50] (20 sec):** Explain the orchestrator logic
- **Action:** [DISPLAY] Show `FanOutFanInOrchestrator.cs`
- **Call-outs:**
  ```csharp
  // Line XX: Create 5 tasks in parallel
  var tasks = new List<Task<string>>();
  for (int i = 0; i < 5; i++) {
      tasks.Add(context.CallActivityAsync<string>(
          "ProcessDataActivity", 
          $"Data_{i+1}"));
  }
  
  // Line YY: Wait for all to complete (synchronization)
  var results = await Task.WhenAll(tasks);
  ```
- **Say:** "The orchestrator creates 5 tasks—one for each activity. They all start at the same time. Then Task.WhenAll waits for all 5 to complete before returning results."
- **Timing:** 20 sec to show code, highlight the WhenAll pattern

**[1:50–2:10] (20 sec):** Explain activity function logic
- **Action:** [DISPLAY] Show `ActivityFunctions.cs`
- **Call-outs:**
  ```csharp
  // Line AA: Activity function receives data, processes it
  [FunctionName("ProcessDataActivity")]
  public static async Task<string> ProcessDataActivity(
      [ActivityTrigger] string data, 
      ILogger log)
  {
      // Simulate work: call downstream service or compute
      await Task.Delay(400); // 400ms per activity
      return $"Processed: {data}";
  }
  ```
- **Say:** "Each activity function is independent. It receives data, processes it, returns a result. In this demo, each activity simulates 400 milliseconds of work (calling a service, transforming data, etc.)."

**[2:10–2:30] (20 sec):** Preview orchestration flow
- **Say:** "When we trigger the orchestrator:
  1. It receives an HTTP request.
  2. Immediately fans out to 5 activities.
  3. All 5 run at the same time (parallel).
  4. Since each takes ~400ms, the total is ~400ms, not 2 seconds.
  5. Orchestrator collects results and returns them.
  6. In Application Insights, we'll see the 5 activities on the timeline—overlapping, not sequential."

---

### **[2:30–3:00] TRIGGER ORCHESTRATION**

**Timing:** 30 seconds

**[2:30–2:40] (10 sec):** Issue trigger command
- **Action:** [CLICK] PowerShell console (or terminal tab)
- **Command:**
  ```powershell
  $url = "https://func-heist-dev-lv.azurewebsites.net/api/start-fanout?code=<KEY>"
  Invoke-WebRequest -Uri $url -Method POST -Headers @{"Content-Type"="application/json"} -Body ""
  ```
- **Expected Response:**
  ```json
  {
    "id": "abc123def456",
    "statusQueryGetUri": "https://func-heist-dev-lv.azurewebsites.net/runtime/webhooks/durabletask/instances/abc123def456",
    "sendEventPostUri": "...",
    "terminatePostUri": "..."
  }
  ```
- **Note the ID:** `abc123def456` (for monitoring)

**[2:40–3:00] (20 sec):** Confirm orchestration started
- **Action:** [CLICK] Application Insights Live Logs
- **OBSERVE:**
  - Logs show `[Orchestrator] Starting orchestration: abc123def456`
  - Logs show `[Activity] ProcessDataActivity started: Data_1`
  - Logs show `[Activity] ProcessDataActivity started: Data_2`
  - (etc., all 5 starting ~simultaneously)
- **Say:** "Watch the logs—all 5 activities are starting almost at the same time. They're not waiting for each other. This is parallelism in action."

---

### **[3:00–5:30] LIVE EXECUTION (5 Activities)**

**Timing:** 2 minutes 30 seconds

**[3:00–3:10] (10 sec):** Activities running
- **OBSERVE in Application Insights:**
  - Timeline shows 5 activity bars on the chart (should be overlapping horizontally, not sequential)
  - Each activity shows execution time: ~400ms
  - Concurrency metric shows: 5
- **Say:** "Here's the key: all 5 are running right now. Notice the timeline—they're all active at the same time. No waiting. No queue."

**[3:10–4:00] (50 sec):** Wait for activities to complete
- **Observation window:** Let orchestration complete naturally (~1–2 sec total)
- **Action:** [WAIT] Allow silence (5–10 sec) while logs populate
- **Use this time:** "While the activities execute, let me explain the benefit: 5 activities × 400ms each = 2,000ms sequentially. Running in parallel = just 400ms. You've saved 80% of the time."
- **Continue explaining:** "In real world: 1,000 images, 10 workers, each image 100ms = 10 seconds parallel vs. 1,700 seconds sequential. That's why we fan out."

**[4:00–4:30] (30 sec):** Activities complete; orchestrator collects results
- **OBSERVE:**
  - Application Insights shows all 5 activities marked as "Completed"
  - Orchestrator status changes to "Completed"
  - Final result returned: JSON with all 5 processed values
- **Say:** "All done. The orchestrator has collected results from all 5 activities and returned the final result."

**[4:30–5:30] (1 min):** Review orchestration timeline
- **Action:** [CLICK] Durable Functions Instance detail (if available)
- **OBSERVE:**
  - Execution timeline shows:
    - Orchestrator start: 0ms
    - Activity 1, 2, 3, 4, 5 start: 0–10ms (nearly simultaneous)
    - All activities complete by: ~400–450ms
    - Orchestrator ends: ~450ms
  - Total duration: ~450ms (not 2,000ms if sequential)
- **Highlight:** "Look at the timeline: all 5 bars are side-by-side, not stacked. Parallelism."

---

### **[5:30–7:00] SCALE DEMONSTRATION (Optional)**

**Timing:** 1 minute 30 seconds

**Context:** Show the benefit of parallelism by scaling to 11 activities.

**[5:30–5:45] (15 sec):** Issue second trigger with 11 activities
- **Action:** [CLICK] PowerShell console
- **Modified command:**
  ```powershell
  # Trigger with 11 activities instead of 5
  $url = "https://func-heist-dev-lv.azurewebsites.net/api/start-fanout?code=<KEY>&count=11"
  Invoke-WebRequest -Uri $url -Method POST -Headers @{"Content-Type"="application/json"} -Body ""
  ```
- **Expected:** New orchestration ID returned
- **Say:** "Now let's scale to 11 activities—the full Ocean's Eleven crew. If we were sequential, this would take 4.4 seconds. Parallel? Still around 400ms."

**[5:45–6:45] (1 min):** Monitor 11-activity execution
- **OBSERVE in Application Insights:**
  - Timeline shows 11 activity bars (all overlapping)
  - Concurrency metric shows: 11
  - Total duration: still ~400–450ms (NOT 4.4 seconds!)
- **Pause and comment:** "Same execution time with 11 activities as with 5. That's the power of parallelism. You don't pay a sequential penalty. You scale horizontally."

**[6:45–7:00] (15 sec):** Compare timing
- **Action:** [DISPLAY] Side-by-side comparison of two orchestration timelines
  - 5 activities: ~450ms
  - 11 activities: ~450ms
- **Say:** "This is why fan-out/fan-in is essential for serverless. You're not limited to one worker. You spawn many, they all work together, and the total time stays fast."

---

### **[7:00–8:30] METRICS REVIEW**

**Timing:** 1 minute 30 seconds

**[7:00–7:20] (20 sec):** Show concurrency metrics
- **OBSERVE in Application Insights:**
  - Chart: "Activity Concurrency Over Time"
  - Shows peak concurrency = 11 (in second orchestration)
- **Say:** "This chart shows: at any given moment, 11 activities were running. That means 11 parallel processors working simultaneously. No bottleneck. No queue buildup."

**[7:20–7:40] (20 sec):** Show execution timeline
- **OBSERVE:**
  - Detailed timeline of orchestration (start → fan-out → activities → fan-in → complete)
  - Total orchestration time: ~450ms
- **Metric to highlight:**
  - Orchestrator overhead: ~50ms (spawn + wait + return)
  - Activity execution: ~400ms (actual work)
  - Total: ~450ms (vs. 4.4 seconds sequential)
  - **Efficiency:** 90% of the time is doing work, not overhead

**[7:40–8:00] (20 sec):** Show activity-level breakdown
- **OBSERVE in Application Insights:**
  - Per-activity metrics:
    - Activity_1 duration: 400ms
    - Activity_2 duration: 405ms
    - Activity_3 duration: 398ms
    - (etc.)
  - Variation: ±5ms (normal variance, no bottleneck)
- **Say:** "Each activity took ~400ms. Notice they all finished around the same time—no outliers. If one activity was much slower, it would delay the entire orchestration. We'd see it clearly here."

**[8:00–8:30] (30 sec):** Cost implications
- **Say:** "Cost perspective: 11 activities × 2 sec execution time = 22 execution-seconds. But you're running in parallel on the Consumption plan, so you're only charged for concurrent executions (up to your plan limit), not sequential time. In real-world: image processing pipeline with 1,000 images, 10 workers: 100 execution-seconds total cost instead of 100,000 if sequential."

---

### **[8:30–10:00] CLOSING REMARKS**

**Timing:** 1 minute 30 seconds

**[8:30–8:50] (20 sec):** Recap the pattern
- **Say:** "Fan-Out/Fan-In is the pattern for parallelizing work. Orchestrator sends tasks to many workers. Workers execute independently. Orchestrator waits for all to complete. You've just seen it: 5 → 11 activities, ~400ms execution time regardless of parallelism count. That's serverless scalability."

**[8:50–9:10] (20 sec):** Real-world use case
- **Say:** "Real-world examples:
  - **Image processing:** Resize 10,000 images in parallel. Metadata enrichment. Watermarking. All at once.
  - **Data aggregation:** Query 50 databases in parallel. Combine results.
  - **Order fulfillment:** Payment, inventory, shipping—all verified simultaneously, not sequentially.
  - In Vegas terms: 'Distribute the crew'—each heist member has a task. They all work at the same time. Speed and efficiency."

**[9:10–9:30] (20 sec):** Serverless design principle
- **Say:** "This is a core serverless principle: leverage parallelism and stateless execution. Don't think 'one request → one process.' Think 'many requests → many workers in parallel.' Azure Durable Functions handles the coordination. You just define the work and fan out."

**[9:30–10:00] (30 sec):** Bridge to next topic / Q&A prompt
- **Say:** "We've covered three patterns: aggregation with resilience, queue-based load leveling for burst handling, and fan-out/fan-in for parallelism. You now have three powerful tools in your serverless toolkit. Any quick questions before we move to the final topic?"
- **[PAUSE for 5–10 sec questions if time allows; otherwise move forward]**
- **Closing line:** "Let's talk about [next slide topic]. You're building a serverless system that's resilient, scalable, and efficient."

---

## Contingency Procedures

### **Scenario: Orchestration Hangs (>5 sec elapsed)**

**Action:**
1. [CLICK] Application Insights → check if activities are actually running (logs confirm)
2. If hung:
   - Say: "Looks like we're experiencing a slight delay. This sometimes happens with cold starts on the cloud."
   - Activate fallback: [CLICK] switch to pre-recorded demo output (MP4 video or screenshot slideshow)
   - Show pre-recorded timeline and results
   - Continue with metrics review (still valid)

**Time adjustment:** Fallback adds 30–45 sec; cut "Scale Demonstration" to 1 min total, or skip "Metrics Review" details.

### **Scenario: Application Insights Logs Not Showing**

**Action:**
1. [CLICK] Refresh Application Insights tab
2. If still blank:
   - Proceed with code walkthrough (already shown at [1:30–2:30])
   - Trigger orchestration (still send the command)
   - Observe via PowerShell response (orchestration ID) instead of live logs
   - Say: "Logs are catching up. Let's proceed with the execution and we'll see the final results."

### **Scenario: PowerShell Command Fails (401 Unauthorized, Invalid Key)**

**Action:**
1. [PAUSE] Say: "We're experiencing an authentication issue. Let me switch to the local emulator instead."
2. [CLICK] Switch to local Function App running in VS Code terminal
3. Issue curl command to localhost endpoint:
   ```bash
   curl -X POST http://localhost:7071/api/start-fanout -H "Content-Type: application/json" -d "{}"
   ```
4. Continue execution locally (logs visible in VS Code terminal, not Application Insights)
5. Show results (takes same ~400ms, but in terminal output)

---

## Speaker Tips

1. **Energy & Pacing:** This demo has a lot of waiting (activities execute in ~400ms). Use wait time to explain serverless benefits, not silence. Speak confidently during the execution phase.

2. **Heist Theme Consistency:** Refer to "crew members," "parallel operations," "Ocean's Eleven style coordination." This ties presentation to location (Las Vegas) and team (Ocean's 11 casting).

3. **Audience Engagement:** Ask rhetorical questions during execution:
   - "What would this look like if we did it sequentially?" (Answer: 4.4 seconds)
   - "Why is parallelism important in serverless?" (Answer: Cost + speed)

4. **Metrics Matter:** Always tie logs/metrics back to the pattern. "Notice the concurrency spike—that's 11 activities running at once. That's the fan-out."

5. **Fallback Confidence:** If you activate fallback, do it smoothly. "Cloud systems sometimes have latency. Let me show you the results we're expecting to see." No apologies. Confidence sells.

---

## Post-Demo Summary

**Pattern learned:** Fan-Out/Fan-In orchestration with Durable Functions  
**Key metric:** 11 activities in ~450ms (vs. ~4,400ms sequential)  
**Audience takeaway:** Parallelize work for speed and cost efficiency  
**Next step:** Q&A or transition to "Pattern Reality Check" section
