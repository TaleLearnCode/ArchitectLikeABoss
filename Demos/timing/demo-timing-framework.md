# Demo Timing Framework — Ocean's 11 Serverless Patterns
**Architect:** Danny  
**Date:** 2026-03-19  
**Event:** Visual Studio Live! Las Vegas (75-minute presentation)

---

## Overview

Three live demos, **10 minutes each**, must fit seamlessly into the 75-minute presentation with time for transitions, Q&A, and contingency buffers. This framework allocates exact timing per demo segment, accounts for Azure service latencies, and provides fallback strategies.

**Total Demo Block:** 30 minutes
- 3 demos × 10 min each = 30 min
- Includes transitions between demos (Chad switches slides/infrastructure setup)
- No dedicated Q&A block; questions handled during demo waits or after all three complete

---

## Azure Service Latency Assumptions

Based on testing and documented behavior:

| Service | Typical Latency | Notes |
|---------|-----------------|-------|
| Function App (warm) | 50–200 ms | Consumption tier, already invoked |
| Function App (cold start) | 3–5 seconds | First invoke after idle period |
| Service Bus enqueue | 50–200 ms | Single message |
| Service Bus dequeue | 100–300 ms | Per message with processing |
| HTTP call (local Service Bus) | 200–400 ms | Network + processing |
| Durable Function orchestration | 100–200 ms per activity spawn | Parallel fanning |
| Application Insights query | 500–1000 ms | Dashboard refresh |

---

## Demo Segment Time Allocation

Each 10-minute demo follows this structure:

| Segment | Duration | Purpose | Activity |
|---------|----------|---------|----------|
| **Opening** | 1 min (0–60s) | Set context | Explain pattern, show architecture diagram, establish heist narrative |
| **Setup** | 1 min (60–120s) | Prep execution | Show code, highlight key functions, verify Azure dashboard is live |
| **Execution** | 7 min (120–540s) | Run demo | Trigger function(s), watch logs/metrics, explain what's happening |
| **Closing** | 1.5 min (540–630s) | Summarize & Q&A | Recap pattern, answer questions, bridge to next demo |
| **Buffer** | 0.5 min (630–600s)* | Contingency | Reserved for demo overruns or fast completions |

*If demo runs fast, use buffer for extended Q&A or deeper explanation.*

---

## Demo-by-Demo Timing Breakdown

### Demo 1: "Score the Diamond" (Aggregation Pattern)
**Total: 10 minutes**

| Time | Segment | Activity | Notes |
|------|---------|----------|-------|
| 0:00–1:00 | Opening | "When the team needs intel from multiple sources fast, you need aggregation. We're calling three services in parallel: target reconnaissance, security assessment, payoff calculation. All must complete in 5 seconds or we get degraded results." | Show architecture slide with 3 parallel calls |
| 1:00–2:00 | Setup | Show ScoreTheDiamond function code; highlight Task.WhenAll() and timeout logic; point to Application Insights dashboard; verify all 3 services online | Live code view; terminal showing function status |
| 2:00–2:05 | Pre-execution | "Watch the Application Insights timeline. We're sending the request to Bellagio." | Open Insights, prepare to capture trace |
| 2:05–2:10 | Execution | Hit enter; watch function execute (expect 2–3 sec execution + 500ms logs to appear) | All 3 functions should respond in parallel |
| 2:10–2:15 | Review results | "All three returned in 2.8 seconds. Notice the parallel execution here [point to timeline]. Now let me show you what happens when security takes too long." | Explain confidence score (0.95 high confidence) |
| 2:15–3:00 | Alt scenario | Re-trigger with artificial delay on security function; show degraded response with confidence score 0.65 | Demonstrates fallback resilience |
| 3:00–3:30 | Summarize | "Aggregation handles partial failures gracefully. Real-world example: If one service is slow, you don't block the user—you return the best intel available." | Relate to production patterns |
| 3:30–3:45 | Q&A buffer | "Any questions on timeouts or aggregation patterns?" | Answer briefly; guide to next demo |
| 3:45–3:50 | Transition prep | Chad advances slide; prepares Service Bus queue for demo 2 | No audience activity |
| 3:50–4:00 | Unused buffer | Reserved for overruns | *If demo ran long, this is consumed* |

**Contingency:**
- If execution takes >5 sec: Skip alt scenario, jump to summary
- If execution takes <1.5 sec: Show logs in detail, discuss real-world variations
- If Insights dashboard slow: Show code execution results in function logs instead

---

### Demo 2: "Pull the Job" (Queue-Based Load Leveling Pattern)
**Total: 10 minutes**

| Time | Segment | Activity | Notes |
|------|---------|----------|-------|
| 4:00–5:00 | Opening | "Imagine 50 heist execution requests arrive simultaneously. We can't process all in parallel or we'll crush the system. Queue-based load leveling smooths the spikes. Messages go to Service Bus, 4 workers process them steadily, and failures go to the dead-letter queue." | Show queue diagram; explain burst scenario |
| 5:00–6:00 | Setup | Show PullTheJobIntake and ExecuteTheJobWorker code; highlight Service Bus trigger and retry logic; open Service Bus metrics dashboard; verify queue is empty and workers are listening | Visual inspection of queue depth chart |
| 6:00–6:10 | Pre-execution | "I'm about to send 50 messages as fast as possible. Watch the queue depth jump to 50, then drop as workers process." | Point to queue depth metric |
| 6:10–6:15 | Burst load | Execute curl loop: `for i in {1..50}; do curl -X POST https://[fn].azurewebsites.net/api/PullTheJobIntake?job=$i; done` | Queue depth should spike to ~50 immediately |
| 6:15–6:50 | Observe processing | "Workers are processing at ~6 messages per second total (4 workers × ~1.5 msg/sec each). We'll see failures route to DLQ around message 5, 16, 28, 42... watch for the pattern." | Live metrics + Application Insights traces |
| 6:50–7:15 | Queue drain | "Queue's down to 10, 5, 0. All done. Let me show you the dead-letter queue." | Click to DLQ view; show ~5 failed messages |
| 7:15–7:30 | DLQ analysis | "5 messages failed. With retry logic, they attempted 3 times each. Rather than losing them, they're captured here for investigation. In production, the AnalyzeTheFailures function would log and alert." | Show DLQ messages; explain retry count |
| 7:30–7:50 | Summarize | "Load leveling decouples intake from processing. Your users get fast HTTP responses; workers handle the backlog steadily. Failures don't disappear—they're tracked and recoverable." | Relate to e-commerce checkout, payment processing |
| 7:50–8:00 | Q&A / Transition | "Questions on queue patterns? No? Perfect—let's move to the most complex one: orchestrating the entire crew in parallel." | Brief Q&A; Chad preps Durable Functions demo |

**Contingency:**
- If queue drains too fast (<20 sec): Good news! Use saved time to show DLQ processor logic and Application Insights Trace view in detail
- If queue drains slow (>45 sec): Trim DLQ analysis; skip detail on retry logic; just show final count
- If Service Bus metrics lag: Use Application Insights "Trace" view instead (more up-to-date)

---

### Demo 3: "Distribute the Crew" (Fan-Out/Fan-In Pattern)
**Total: 10 minutes**

| Time | Segment | Activity | Notes |
|------|---------|----------|-------|
| 8:00–9:00 | Opening | "This is the most complex pattern: orchestrating 11 crew members in parallel. Each role takes 2–9 seconds. If we assign them one-by-one, it takes 60+ seconds. Durable Functions fan-out and fan-in to run all 11 in parallel, completing in under 10 seconds. That's 84% time savings." | Show crew diagram; emphasize time optimization |
| 9:00–10:00 | Setup & code review | Show ManageTheHeist orchestrator and activity functions (AssignRole_Scout, AssignRole_Safecracker, etc.); highlight Task.WhenAll() for fan-in; explain durable state persists across function invocations; open Application Insights Durable Functions view | Code walkthrough; show empty Task Hub initially |
| 10:00–10:10 | Pre-execution | "Starting orchestration now. I'll get an instance ID back immediately, even though the crew is still working. Durable Functions handles async state tracking." | Prepare to capture instance ID |
| 10:10–10:15 | Execute | Call DistributeTheCrewOrchestrator with mission input; show HTTP response with instance ID | Should return in <500ms |
| 10:15–10:25 | Monitor | "Crew is fanning out now. Watch the timeline build as each role gets assigned... Scout, Safecracker, Driver, Hacker, FinanceGuy, Planner, Explosives, Inside, Forger, Thief, Lockpick." | Application Insights shows spawn at T=0.1s |
| 10:25–10:35 | Fan-in completion | "All 11 are executing in parallel. Notice the longest one (Hacker) finishes at 9 seconds. Everything else completes before then. Fan-in aggregates their results." | Timeline shows all bars parallel; longest is ~9s |
| 10:35–11:15 | Results & analysis | Show final mission result JSON with cost_optimization metric (84% savings); explain why parallel > serial; highlight resilience (if one activity fails, orchestrator can retry) | Relate to complex multi-step workflows: data pipelines, batch processing |
| 11:15–12:30 | Summarize & final Q&A | "Fan-out/fan-in is your go-to for orchestrating complex, multi-step workflows. Durable state ensures consistency—even if your function crashes mid-orchestration, Azure resumes from the last checkpoint." | Final audience questions; bridge to conclusion |
| 12:30–13:00 | Unused / Presentation close | "That's our serverless architecture patterns in action. Questions before we wrap?" | Chad prepares final closing slide |

**Contingency:**
- If orchestration executes <5 sec: Excellent! Show Activity Hub query to prove all 11 ran; discuss state persistence detail
- If orchestration executes 10–12 sec (normal): Trim results analysis; hit the highlights
- If orchestration takes >15 sec: Skip mission result detail; focus on timeline proof; note that variance is expected in cloud

---

## Overall Presentation Flow & Transitions

### Pre-Demo Setup (before Demo 1)
**Time allocation in presentation: ~2 minutes**
- Chad opens demo section; sets context ("Three live patterns, 30 minutes total")
- Emphasize theme: "Ocean's 11 serverless heist—every pattern is a critical crew role"
- Live check: Demo machines online, Insights dashboards ready

### Between Demos (Chad controls; audience waits ~1–2 min total)
**Demo 1 → Demo 2:** Chad advances to Service Bus slide; ensures function app is ready
**Demo 2 → Demo 3:** Chad advances to Durable Functions slide; ensures orchestrator is ready

### Post-Demo Wrap-Up
**Time allocation: ~3 minutes**
- Recap all three patterns and their real-world uses (e-commerce, data pipelines, event processing)
- Emphasize serverless benefits: auto-scaling, pay-per-execution, no infrastructure management
- Open broader Q&A (not tied to individual demos)

---

## Q&A Strategy

**Option A (Recommended): Embedded Q&A**
- Each demo ends with 30–60 sec for questions
- Keeps audience engaged; breaks up live execution monotony
- Risk: Can run over if question is complex

**Option B: End-of-ALL-DEMOS Q&A**
- Save all questions until after demo 3 completes
- Protects timing; avoids interruptions
- Risk: Audience may forget details; feels rushed

**Hybrid (My recommendation):** 
- Demo 1 & 2: Brief Q&A (30 sec max)
- Demo 3: Full 2–3 min Q&A at the end (last demo, more engagement room)

---

## Fallback Activation Checklist

### If Demo Fails *Early* (Before 2 minutes in)
1. **Trigger:** "Let me restart this" (appears clean, not a crash)
2. **Time impact:** 30–45 seconds (clear logs, re-run)
3. **Action:** Restart the function/orchestration
4. **Audience communication:** "Testing failure scenarios is key to serverless design. Let me show you what resilience looks like."

### If Demo Fails *Mid-Execution* (2–7 minutes in)
1. **Trigger:** Function returns error; queue doesn't drain; orchestration hangs
2. **Time impact:** 60–90 seconds (troubleshoot or bail to fallback)
3. **Action:** 
   - **Try #1 (30 sec):** Quickly check Application Insights for root cause; re-trigger if obvious fix
   - **Fallback (60 sec):** Switch to pre-recorded video or screenshot + verbal explanation
4. **Audience communication:** "Cloud behavior varies—this is why testing in production-like conditions is critical. Here's what we saw when this ran yesterday."

### If Demo Succeeds *Faster Than Planned*
1. **Trigger:** Execution finishes in <50% of allocated time
2. **Action:** Fill time with:
   - Detailed log analysis (show every step)
   - Discussion of real-world variations (network, cold starts, concurrency)
   - Bonus question: "What would happen if we doubled the crew size?"
3. **Time cost:** 0 (use allocated buffer)

### Recovery Time Budget
- **Early restart:** 45 sec (minimal time loss; can recover in demo closing)
- **Mid-demo fallback:** 60 sec (eat into closing; compress Q&A)
- **Late demo fallback (7+ min in):** Skip full execution; show pre-recorded results; proceed to next demo

---

## Demo Environment Pre-Flight Checklist

### 30 Minutes Before Presentation
- [ ] All Function Apps warm (invoke once to trigger cold start)
- [ ] Service Bus queue empty; DLQ empty; 4 workers listening
- [ ] Durable Functions Task Hub ready; sample orchestration tested
- [ ] Application Insights dashboards open in browser tabs (one per demo)
- [ ] Curl/Postman commands ready to copy-paste
- [ ] Pre-recorded fallback videos loaded on backup device
- [ ] Slack notification set to mute (no interruptions mid-demo)
- [ ] Network connectivity tested (hardwired internet strongly recommended)

### 5 Minutes Before Presentation
- [ ] Live demo: One test run of each demo
- [ ] Application Insights: Verify logs are streaming
- [ ] Terminal: Clear screen; paste curl commands ready
- [ ] Confidence check: Ask Rusty/Basher "All green?"

---

## Key Metrics & Success Criteria

| Demo | Target Time | Acceptable Range | Fallback Trigger |
|------|-------------|------------------|------------------|
| Score the Diamond | 2–3 sec execution | <5 sec | >7 sec total time |
| Pull the Job | 30–40 sec queue drain | 20–50 sec | Queue doesn't start draining by 2 min |
| Distribute the Crew | 9–10 sec orchestration | 8–12 sec | >15 sec or orchestration doesn't complete |

---

## Summary for Chad (Presentation Delivery)

1. **Each demo is 10 minutes.** Opening (1 min) → Setup (1 min) → Execution (7 min) → Closing (1.5 min) → Buffer (0.5 min).

2. **Azure latencies are real.** Functions take 2–5 sec, Service Bus queues take 30–60 sec to drain, orchestrations take 9–10 sec. Plan for this; don't panic.

3. **If a demo goes sideways**, restart quickly (if <2 min) or fallback to pre-recorded (if mid-demo).

4. **Fill execution waits with explanation.** Don't just stand there—talk through the logs, explain the pattern, relate to their world.

5. **Q&A is embedded.** 30 sec per demo + 2 min after demo 3. Don't let it run over.

6. **Transitions are Chad's job.** Advance slides, ensure next demo's infrastructure is ready. Danny watches the logs.

---

**Next:** See demo-specific timing guides for minute-by-minute detail.
