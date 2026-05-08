# "Pull the Job" Demo Timing Guide — Queue-Based Load Leveling Pattern
**Demo 2 of 3**  
**Duration:** 10 minutes exactly  
**Pattern:** Queue-based load leveling with Service Bus, worker functions, & dead-letter queue (DLQ)  
**Presenter Lead:** Chad (reading script); Danny (monitoring logs & metrics)

---

## Executive Summary

Absorb a burst of 50 requests via HTTP intake, enqueue to Service Bus, and process with 4 concurrent workers. Demonstrate how queuing smooths spiky load, and how the dead-letter queue captures ~5 failures (~10% rate) without losing data.

**Expected Execution Time:** 
- Intake: 10–15 seconds (50 curl requests in rapid succession)
- Processing: 30–45 seconds (4 workers @ ~1.5 msg/sec each = ~6 msg/sec total)
- DLQ processing: 5–10 seconds (monitor DLQ messages)
- Total: ~50–60 seconds of active observation

**Demo Goal:** Show queue depth spike to 50, drain as workers process, and failures landing gracefully in DLQ.

---

## Minute-by-Minute Breakdown

### 4:00–5:00 — OPENING (Set Context & Build Narrative)

**Chad reads (aim for ~130–150 words in 1 minute):**

```
"Back to the Ocean's 11 crew. 
Now they're executing heists across Vegas. 
Simultaneously, 50+ robbery execution requests flood in.
The execution team can't handle 50 in parallel or they'd overload.
Solution: Queue them up. Put all 50 in Service Bus.
Four workers pull from the queue steadily, processing ~1.5 requests each per second.
But here's reality—10% fail on first attempt. Service Bus retries.
After 3 attempts, still failing? They go to the Dead-Letter Queue (DLQ).
The team investigates the DLQ, sees what broke, fixes it.
This is QUEUE-BASED LOAD LEVELING: Intake is decoupled from processing.
Users get fast HTTP responses ('Your job is queued!').
Workers handle the backlog at their own pace.
Let's see it in action."
```

**Visual cues for Chad:**
- [ ] Show architecture diagram: HTTP Intake → Service Bus Queue → 4 Workers
- [ ] Highlight DLQ box (failures go here)
- [ ] Emphasize the decoupling: "Intake is fast; processing is steady"
- [ ] Show real-world use case slide: E-commerce checkouts, payment processing, order fulfillment

**Danny's role:** Monitor Service Bus metrics in background; ensure queue is empty to start

---

### 5:00–6:00 — SETUP (Show Code & Verify Infrastructure)

**Chad:**
```
"Here's the PullTheJobIntake function [show code].
It receives a heist execution request: crew, target, date.
Validates it. Then enqueues to Service Bus in ONE LINE: 
  await queueClient.SendMessageAsync(new ServiceBusMessage(json)).

That's it. Fast. Returns to the user immediately.

Now, the ExecuteTheJobWorker [switch to code].
It's a Service Bus Trigger function. Listens on the queue.
When a message arrives, it processes it: simulated 5–15 seconds of 'work'.
Then randomly, 10% chance, throws an exception.

The magic happens in Service Bus configuration [show Azure Portal].
Max Delivery Count = 3. So it retries automatically.
After 3 failures, message → Dead-Letter Queue.

Let me show you the current state of the queue."
```

**Deliverables for Chad:**
- [ ] Open VS Code: `PullTheJobIntake.cs` (highlight SendMessageAsync)
- [ ] Open VS Code: `ExecuteTheJobWorker.cs` (highlight Service Bus trigger and try/catch)
- [ ] Open Azure Portal → Service Bus → heist-queue → Properties
  - Confirm: MaxDeliveryCount = 3
  - Confirm: Dead-Lettering on message expiration = enabled
- [ ] Open Service Bus Metrics view: Queue Depth chart (should show 0 messages initially)

**Danny's role:**
- [ ] Verify 4 worker functions are deployed and listening
- [ ] Check Service Bus logs for any existing messages (clear if necessary)
- [ ] Have curl command ready: `curl -X POST "https://[fn].azurewebsites.net/api/PullTheJobIntake?job={i}"`
- [ ] Have bash/PowerShell loop ready for bulk submission:
  ```powershell
  for ($i=1; $i -le 50; $i++) {
    curl -X POST "https://[fn].azurewebsites.net/api/PullTheJobIntake?job=$i"
  }
  ```

---

### 6:00–6:10 — PRE-EXECUTION (Announce Load Test)

**Chad:**
```
"Right now, the queue is empty. Zero messages. Four workers are sitting idle, ready to go.
In 10 seconds, I'm going to send 50 heist execution requests all at once.
Watch three things:
1. Queue Depth metric spikes to 50
2. Workers spin up and start processing
3. Failures land in the dead-letter queue

Let's go."
```

**Setup:**
- [ ] Point cursor to queue depth metric on screen
- [ ] Have curl loop command ready to paste (or pre-typed in terminal)
- [ ] Position Application Insights (Trace view) on second screen (optional, for worker details)
- [ ] Set expectations: "This will take ~50 seconds total"

**Danny's role:**
- [ ] Position cursor over "Execute" button in PowerShell
- [ ] Verify that you can see the queue metrics update in real-time
- [ ] Set a phone timer for 60 seconds (backup reminder that processing should be done by then)

---

### 6:10–6:15 — BURST LOAD (Execute & Observe Initial Spike)

**Chad:** (No speech; quiet execution)

**Actions:**
- [ ] Paste/execute curl loop in terminal
- [ ] Loop completes in ~10 seconds (50 requests, ~5 req/sec)
- [ ] Terminal shows 50 HTTP 202 (Accepted) responses

**Expected Queue Depth Behavior:**
```
T=0s:     Queue Depth = 0
T=0–10s:  Queue Depth climbs 0 → 50 (one request per 200ms)
T=10s:    Queue Depth peaks at ~50 (intake complete, workers start pulling)
```

**Visual feedback:**
- [ ] Service Bus Metrics: Queue Depth line shoots up to 50 on the chart
- [ ] Terminal: 50 x "202 Accepted" messages
- [ ] Workers: Application Insights should show 4 function invocations starting

**Chad's narration during load (~20 seconds):**
```
"Fifty requests landed in the queue in 10 seconds.
Notice the queue depth spike on the chart—went from 0 to 50.
No errors. No timeouts. HTTP responses were fast (202 Accepted).
Users think their jobs are submitted. They are.
Now the workers are pulling messages and processing them.
Each worker processes at about 1.5 messages per second.
That's 6 messages per second total across 4 workers.
At that rate, we'll drain the queue in about 8–10 seconds.
Let's watch."
```

**Danny's role:**
- [ ] Watch queue depth metric in real-time
- [ ] Note when it peaks (should be ~50)
- [ ] Be ready to explain any unexpected behavior

---

### 6:15–6:50 — OBSERVE PROCESSING (Queue Drains; Workers Process; DLQ Accumulates)

**Chad:**
```
"Here's where it gets interesting.
Queue depth is dropping: 50 → 40 → 30 → 20...
Workers are processing in parallel.
Some messages succeed. Some fail.
Failed messages are retried by Service Bus automatically.
Watch for the DLQ to start accumulating."
```

**Timeline (expected):**
```
T=0s:     Queue Depth = 50, Workers = 4 active
T=5s:     Queue Depth = 25 (messages draining at ~5 msg/sec)
T=10s:    Queue Depth = 10 (nearly done)
T=13s:    Queue Depth = 0 (all intake messages processed or moved to DLQ)
```

**Failures & Retries (real-time explanation):**
- Message #5 fails on worker 1. Service Bus retries.
- Message #16 fails on worker 2. Service Bus retries.
- Message #28 fails on worker 3. Service Bus retries.
- Message #42 fails on worker 4. Service Bus retries.
- After 3 attempts each, these ~5 messages → DLQ.

**Chad's narration during observation (~20 seconds):**
```
"The queue is draining steadily.
Workers are pulling messages, processing, and committing.
Some messages fail—that's intentional, we coded a 10% failure rate.
Service Bus sees the exception, retries. 
After 3 attempts, if still failing, message moves to DLQ.
You can see the pattern—every 10 messages or so, one fails.
In production, failures might be network blips, timeouts, invalid data.
DLQ ensures they're not lost; they're captured for investigation."
```

**Visual cues:**
- [ ] Service Bus Metrics: Queue Depth line drops steadily
- [ ] Service Bus Metrics: DLQ Depth line climbs (slowly, ~5 total)
- [ ] Application Insights (optional): Show worker function invocations, trace successful vs. failed executions

**Danny's role:**
- [ ] Watch the queue depth live-update
- [ ] Note when DLQ depth starts increasing (should be around T=5-8s)
- [ ] Prepare to show DLQ messages in next segment

---

### 6:50–7:15 — QUEUE EMPTY & DLQ INSPECTION (Zoom Into DLQ)

**Chad:**
```
"Queue depth is now zero. All 50 messages have been processed or sent to DLQ.
Let me show you what's in the dead-letter queue."
```

**Actions:**
- [ ] Navigate to Service Bus → heist-queue → Dead Letter Queue (in Azure Portal)
- [ ] Show message count: ~5 messages
- [ ] Click on one message; show details:
  ```json
  {
    "job_id": "heist-vegas-042",
    "crew_lead": "Danny Ocean",
    "target": "Bellagio Vault",
    "error_count": 3,
    "reason": "Execution simulated failure"
  }
  ```

**Chad's narration (~15 seconds):**
```
"Here's the dead-letter queue. Five messages.
Each attempted processing three times, then gave up.
The details are preserved: job ID, target, error count.
In production, an 'AnalyzeTheFailures' function would read these messages,
log them to storage, and send a Slack alert or create a support ticket.
The team investigates: 'Why did message 42 fail?'
Maybe it's bad data, maybe a downstream service was overloaded.
They fix the root cause, replay the message, and resume processing.
Resilience: No data is lost. Everything is recoverable."
```

**Visual inspection:**
- [ ] DLQ Message Count: ~5
- [ ] Sample message details: job_id, crew_lead, error_count
- [ ] (Optional) Show "Properties" of a message: Delivery Count = 3, DeadLetterReason, DeadLetterErrorDescription

**Danny's role:**
- [ ] Click through DLQ messages; note error patterns
- [ ] Be ready to explain to audience: "These aren't corruption; they're logged failures with full context."

---

### 7:15–7:30 — DLQ ANALYSIS & EXPLANATION (Tie to Pattern)

**Chad:**
```
"Look at these details. 
Delivery Count = 3. That message tried three times.
DeadLetterReason tells us the error.
In the real ExecuteTheJobWorker, we would have richer context:
  - Which crew member failed to deploy?
  - Which target location couldn't be reached?
  - What was the timeout that triggered the failure?

All of that is in the message, ready for investigation.

Now imagine this in production. Instead of 50 heist requests,
you have 5,000 e-commerce checkout requests.
If queue-based leveling didn't exist, you'd try to process all 5,000 in parallel.
Your system would crash. Timeouts. Failed orders.
With queue-based load leveling:
  - Checkouts enqueue instantly (user sees 'Processing...').
  - Workers handle 10–20 per second, steady.
  - Failed checkouts retry automatically.
  - After 3 retries, they go to DLQ for manual recovery.
Users don't see cascading failures. System stays stable.
That's the power of this pattern."
```

**Time duration:** ~15 seconds for Chad's explanation

**Key insights to highlight:**
- Decoupling: Intake speed ≠ Processing speed
- Automatic retry: Service Bus handles 3 attempts without app code
- Graceful degradation: DLQ prevents data loss
- Real-world relevance: E-commerce, payment processing, order fulfillment

**Danny's role:**
- [ ] Be ready to dive into a specific DLQ message if asked
- [ ] Prepare to show the ExecuteTheJobWorker code again (exception handling)

---

### 7:30–7:50 — SUMMARY & KEY INSIGHT

**Chad:**
```
"Queue-Based Load Leveling Pattern: The principle.
When requests spike, don't process them all in parallel.
Enqueue them. Decouple intake from processing.
Your intake layer responds instantly to users.
Your worker layer processes at sustainable speed.
Failures don't cascade; they retry automatically and are captured.

This pattern is critical for production systems.
It's how large companies handle flash sales, payment surges, and event spikes.
Without it, your system crashes under load.
With it, you scale smoothly, failures are recoverable, and users stay happy."
```

**Key metrics to recap:**
- 50 requests processed in ~15 seconds (intake + processing)
- ~6 messages/sec throughput (4 workers × 1.5 msg/sec)
- ~10% failure rate (realistic; some requests fail)
- All failures logged and recoverable (no data loss)

**Time duration:** ~20 seconds

**Danny's role:**
- [ ] Stand by for Q&A
- [ ] Have backup facts ready: "In production, we'd tune worker concurrency, retry counts, DLQ monitoring, etc."

---

### 7:50–8:00 — Q&A & TRANSITION

**Chad:** "Any quick questions on queue-based load leveling or the DLQ?"

**Likely questions & answers:**
1. **Q: What if a message is truly bad (not a transient failure)?**  
   A: Good question. DLQ is for investigation. If it's bad data, manual intervention (delete or fix) before replay.

2. **Q: Can you inspect DLQ messages while processing is happening?**  
   A: Yes! DLQ is a separate queue. You can read it in parallel.

3. **Q: What's a realistic failure rate in production?**  
   A: Depends on your downstream services. 1–5% is common for transient failures (timeouts, network blips).

**Time management:**
- Keep Q&A to 10 seconds max
- Defer deep questions: "Let's grab coffee after; this is a rich topic"

**Transition (Chad & Danny behind scenes, ~5 seconds):**
- [ ] Chad: Advance to Demo 3 slide (Durable Functions / Fan-Out/Fan-In)
- [ ] Chad: Close Azure Portal; minimize Service Bus metrics
- [ ] Danny: Verify Durable Functions orchestrator is warm (invoke once if needed)
- [ ] Danny: Check Task Hub storage is ready
- [ ] Chad & Danny: Quick huddle: "Ready for the final demo?"

---

## Contingency Plans

### If Curl Loop Doesn't Execute (Network Error)

**Scenario:** Curl commands fail; no messages enqueued

**Action (Chad + Danny, <30 sec):**
1. Check network connectivity: `ping azure.microsoft.com`
2. Verify function app is responding: `curl -I https://[fn].azurewebsites.net/api/health`
3. If function is down: **Activate Fallback**
4. If network is down: Check WiFi/ethernet; try again

**Fallback:** Show pre-queued messages (pre-load 20 test messages earlier) or skip to DLQ explanation with pre-recorded metrics.

### If Queue Drains Too Fast (<10 seconds)

**Scenario:** All 50 messages processed in <10 sec (fewer failures, faster execution than expected)

**Action:**
- Good sign! Explain: "In low-load periods, workers are very fast. When the system is under stress, latency increases."
- Show DLQ message details in more depth
- Discuss: "In production, you'd use Application Insights to track queue depth over time and adjust worker count accordingly."
- Use saved time for deeper code walkthrough

### If Queue Drains Too Slowly (>45 seconds)

**Scenario:** Messages still processing at 7:45 mark; won't be done by 8:00

**Action:**
- Don't wait for 100% drain
- At 7:45, explain: "Queue is at 5 messages remaining. Let's look at the DLQ while workers finish in the background."
- Show DLQ (which should be ready)
- Use remaining time for summary; move to demo 3

### If Service Bus Metrics Dashboard Doesn't Load

**Fallback:** Use Application Insights "Trace" view instead
- Show worker function invocations
- Count successful vs. failed executions
- Discuss the pattern verbally

**Chad's explanation:** "We can't see the queue depth chart, but Application Insights shows the worker executions. Here's the parallel processing in action."

### If DLQ Doesn't Show Messages (Unexpectedly)

**Scenario:** All 50 processed successfully; no failures

**Action:**
- Surprising but not catastrophic
- Explain: "Low failure rate this time. In production, we might run multiple bursts and eventually capture failures. Or, we increase the simulated failure rate in the code."
- Show the ExecuteTheJobWorker code: `random(1–10) == 1 ? throw exception`
- Acknowledge: "Randomness means sometimes zero failures in a run."

---

## Success Criteria Checklist

- [ ] Demo completes in 10 minutes ±30 seconds
- [ ] 50 messages enqueued successfully (HTTP 202 responses)
- [ ] Queue depth spikes to ~50, then drains to 0
- [ ] ~5 messages land in DLQ (10% failure rate)
- [ ] DLQ message details visible (error info preserved)
- [ ] Audience understands load leveling + decoupling concept
- [ ] Q&A answered briefly; smooth transition to Demo 3

---

## Speaking Notes (1 minute is ~130–150 words)

### Opening (~130 words for 1 min)
"Fifty robbery execution requests flood in simultaneously. Team can't handle 50 in parallel or they'd overload. Solution: Queue them up. Put all 50 in Service Bus. Four workers pull steadily, processing ~1.5 requests each per second. 10% fail; Service Bus retries. After 3 attempts, failures go to Dead-Letter Queue (DLQ). Team investigates, fixes, replays. QUEUE-BASED LOAD LEVELING: Intake decoupled from processing. Users get fast HTTP responses. Workers handle backlog at own pace."

### Setup (~100 words for 45 sec)
"PullTheJobIntake validates heist requests and enqueues to Service Bus in one line. ExecuteTheJobWorker listens on Service Bus queue. Processes simulated 5–15 seconds of work. Randomly, 10% throws exception. Service Bus retries automatically. MaxDeliveryCount = 3. After 3 failures, message goes to Dead-Letter Queue. That's the infrastructure. Now let's load test."

### Execution Narration (~80 words for 30 sec)
"Fifty requests landed in queue in 10 seconds. Queue depth spike from 0 to 50. Workers pulling and processing. Each worker ~1.5 msg/sec. That's 6 msg/sec total. We'll drain queue in 8–10 seconds. Failures every 10 messages or so—10% rate. After 3 retries, they route to DLQ."

### Summary (~120 words for 1 min)
"QUEUE-BASED LOAD LEVELING: When requests spike, don't process all in parallel. Enqueue them. Decouple intake from processing. Intake responds instantly to users. Workers process sustainably. Failures retry automatically, captured, recoverable. Critical for production. How large companies handle flash sales, payment surges. Without it, system crashes. With it, scales smoothly, failures recoverable, users happy."

---

## Next Demo: "Distribute the Crew" (Fan-Out/Fan-In with Durable Functions)

At 8:00 (after transition), Chad will introduce orchestrating 11 crew members in parallel using Durable Functions. Danny will show the orchestration timeline in Application Insights and the final mission result with 84% time savings vs. serial execution.
