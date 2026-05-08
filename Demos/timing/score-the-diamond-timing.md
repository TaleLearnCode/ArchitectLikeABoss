# "Score the Diamond" Demo Timing Guide — Aggregation Pattern
**Demo 1 of 3**  
**Duration:** 10 minutes exactly  
**Pattern:** Aggregation with timeout & fallback handling  
**Presenter Lead:** Chad (reading script); Danny (monitoring logs)

---

## Executive Summary

Orchestrate 3 parallel HTTP calls (target intel, security assessment, payoff calculation) with a 5-second timeout. Demonstrates graceful degradation when one service is slow.

**Expected Execution Time:** 2–3 seconds (all functions respond in parallel)  
**Demo Goal:** Show parallel execution in Application Insights, then showcase degraded response when security function times out.

---

## Minute-by-Minute Breakdown

### :00–:60 — OPENING (Set Context & Build Anticipation)

**Chad reads (aim for 130–150 words/min = ~2–2.5 min spoken, but condense to 1 min script):**

```
"Ocean's 11 opening scene: Danny Ocean's crew needs intel on the Bellagio heist.
They can't wait for information to trickle in one service at a time.
They need three critical pieces FAST: target intel, security assessment, payoff calculation.
All three requests go out in parallel. Azure Functions orchestrates them.
But here's the catch—if security takes too long, Danny doesn't block the entire team.
He proceeds with DEGRADED confidence: 'I have 2 of 3 pieces. Let's move.'
This is the AGGREGATION PATTERN: multiple async calls, timeout handling, fallback strategy.
Watch how it works in real-time on Azure."
```

**Visual cues for Chad:**
- [ ] Show architecture diagram: ScoreTheDiamond in center; 3 functions branching out
- [ ] Emphasize timeout box (5 seconds) and fallback logic
- [ ] Point to Vegas region (West US 2) on Azure map

**Danny's role:** Monitor Application Insights in background; ensure dashboard is responsive

---

### 1:00–2:00 — SETUP (Show Code & Verify Readiness)

**Chad:**
```
"Here's the ScoreTheDiamond function. It uses Task.WhenAll() to fan out
to three services in parallel [point to code]. If any of them exceeds 5 seconds,
we use ConfigureAwait(false) and a timeout wrapper [highlight].
The three services: GetTargetIntel, CheckSecurity, EstimateCut.
Each simulates real-world latency—500ms to 2 seconds depending on network.
Application Insights will show us the execution timeline [open dashboard].
Notice the empty graph right now? That's because we haven't run anything yet."
```

**Deliverables for Chad:**
- [ ] Open VS Code (or similar) to `ScoreTheDiamondFunction.cs`
- [ ] Scroll to `Task.WhenAll()` call; highlight the timeout logic
- [ ] Open Application Insights in adjacent browser tab (Dashboard view)
- [ ] Point to "Timeline" or "Trace" view (pre-selected, ready to click)
- [ ] Verify all 3 dependent functions show in list (GetTargetIntel, CheckSecurity, EstimateCut)

**Danny's role:** 
- [ ] Verify function logs are streaming into Insights
- [ ] Note any pre-existing traces from testing (clear if necessary)
- [ ] Ensure curl command is ready to paste: `curl -X GET "https://[fn].azurewebsites.net/api/ScoreTheDiamond?target=Bellagio"`

---

### 2:00–2:05 — PRE-EXECUTION (Announce & Prepare)

**Chad:**
```
"Sending request to Bellagio now. Watch the timeline build in real-time.
Three functions spawn at T=0, execute in parallel.
Target Intel should resolve first (~500ms).
Security Assessment—that's the variable one, could be 1–2 seconds.
Payoff Calculation is quick (~300ms).
The aggregator waits for all three with a 5-second deadline."
```

**Actions:**
- [ ] Have curl command pre-typed in terminal (not executed yet)
- [ ] Point to timeline graph in Insights
- [ ] Set expectations: "We'll see three bars stacked horizontally, not vertically"

**Danny's role:**
- [ ] Ensure Insights is in "Live Metrics" or real-time trace mode
- [ ] Position cursor over timeline area (ready to zoom/inspect)
- [ ] Check that no previous executions are polluting the view

---

### 2:05–2:10 — EXECUTION (Hit Enter & Observe)

**Chad:**
```
"Here we go... [hit enter]"
```

**Expected Timeline (seconds):**
```
T=0.0s   : Request arrives at ScoreTheDiamond
T=0.05s  : Fan-out to 3 functions (GetTargetIntel, CheckSecurity, EstimateCut)
T=0.3s   : EstimateCut returns (quick)
T=0.7s   : GetTargetIntel returns
T=1.8s   : CheckSecurity returns (slowest)
T=1.85s  : Aggregation completes
T=2.1s   : Response sent to client
```

**What audience should see:**
- Terminal: HTTP 200 response + JSON payload
- Insights: Three horizontal bars in timeline, all completing by ~2 seconds

**Danny's role:**
- [ ] Watch Insights timeline build in real-time
- [ ] Note completion times for each function
- [ ] Verify no timeouts or errors (confidence should be 0.95)
- [ ] Capture screenshot or note the exact times for callback discussion

**Chad's narration during wait (fill 5 seconds):**
```
"Notice the parallel execution. All three functions are working simultaneously.
If each ran sequentially, this would take 3+ seconds.
Parallel means aggregation completes in ~2 seconds—dominated by the slowest function.
That's 33% faster than serial. In production, with higher variance, the gains are even bigger."
```

---

### 2:10–2:15 — REVIEW RESULTS (Analyze First Run)

**Chad:**
```
"All three returned. Here's the JSON response [point to terminal]:
- Mission: Score the Diamond
- Target: Bellagio
- Security Level: High (24 guards, shift change weakness at 2am)
- Payoff: 50 million
- Confidence: 0.95 (high confidence, all three sources confirmed)"
```

**Insights analysis:**
- [ ] Click on "Trace" view; show call hierarchy
- [ ] Highlight the three parallel lines
- [ ] Point to timing: "CheckSecurity took 1.8 seconds—the longest."
- [ ] Note: "Task.WhenAll() waited for this slowest one, then returned."

**Danny's role:**
- [ ] Click into individual function traces if needed
- [ ] Note latencies for real-time discussion
- [ ] Prepare for alternate scenario (force timeout next)

**Chad's transition:**
```
"That's the happy path. All three services cooperate.
But in production, networks fail. Services get overwhelmed.
Let me show you what happens if security assessment takes too long."
```

---

### 2:15–3:00 — ALTERNATE SCENARIO (Timeout & Degradation)

**Strategy:** Re-run with artificial delay on `CheckSecurity` function to trigger timeout.

**Options:**
1. **Pre-recorded scenario:** If live re-trigger might be risky, show pre-recorded screenshot
2. **Live re-trigger with environment variable:** Set `FORCE_SECURITY_DELAY=true` and re-run
3. **Manual test:** Modify query string: `?target=Bellagio&forceSecurityDelay=true`

**Recommended (safest):** Pre-recorded screenshot + verbal explanation.

**Chad:**
```
"In the real world, security sometimes takes longer.
Let's imagine that assessment got stuck behind other requests.
Here's what the response looks like when security times out at 5 seconds
[show pre-recorded or re-run with forced delay]:

{
  'mission': 'Score the Diamond',
  'status': 'degraded',
  'available_intel': {
    'target': 'retrieved',
    'security': 'timeout',
    'cut': 'retrieved'
  },
  'confidence': 0.65
}

Notice the confidence dropped from 0.95 to 0.65.
Danny can still plan the heist, but he's working with incomplete intel.
In production, this might trigger a retry, escalation, or fallback plan.
The point: the service doesn't CRASH. It DEGRADES GRACEFULLY."
```

**Visual aids:**
- [ ] Show both JSON responses side-by-side (live + degraded)
- [ ] Highlight confidence score change: 0.95 → 0.65
- [ ] Point to Insights trace showing security timeout

**Danny's role:**
- [ ] Be ready to trigger timeout if Chad chooses live re-run
- [ ] Or, have degraded response screenshot pre-loaded
- [ ] Note any real-world discussion points for Q&A

---

### 3:00–3:30 — SUMMARY & KEY INSIGHT

**Chad:**
```
"Aggregation Pattern: The key principle.
When you depend on multiple services, don't block forever.
Set timeouts. Declare partial success.
Your system keeps operating—maybe at reduced capacity, but operating.
In real life: microservices APIs, third-party integrations, database replicas.
If one fails, you don't fail. You degrade and keep going.
Azure Functions + Task.WhenAll() + timeout logic = resilience built-in."
```

**Key metrics to highlight:**
- Parallel execution: 3 services, 2 seconds (vs. 3+ seconds serial)
- Timeout handling: Graceful degradation at 5 seconds
- Confidence scoring: Quantifies reliability of response

**Danny's role:**
- [ ] Stand by for questions
- [ ] Be ready to dive into Application Insights if asked "how did you measure that?"

---

### 3:30–3:45 — Q&A BUFFER (Audience Questions)

**Chad:** "Any quick questions on timeouts or parallel aggregation?"

**Likely questions & answers:**
1. **Q: What if all three time out?**  
   A: Function returns error + fallback response (empty intel, confidence 0.0). Danny might cancel the mission or use pre-cached intel.

2. **Q: How do you choose 5 seconds for the timeout?**  
   A: Tuning. Test in production-like environment. For this demo, 5 sec is generous; real APIs might use 1–2 sec.

3. **Q: Can you retry a failed service?**  
   A: Yes! This code could be enhanced with exponential backoff. Aggregation + retry = even more resilient.

**Time management:**
- Keep Q&A to 30–45 seconds max
- If question is complex, defer: "Great question—let's grab coffee after the presentation"

---

### 3:45–3:50 — TRANSITION PREP

**No audience activity. Chad & Danny behind the scenes:**

- [ ] Chad: Advance to Demo 2 slide (Service Bus / Queue Leveling)
- [ ] Chad: Close VS Code; minimize Insights
- [ ] Danny: Verify Service Bus queue is empty and workers are ready
- [ ] Danny: Check that PullTheJobIntake function is warm (invoke once if needed)
- [ ] Chad & Danny: Quick huddle: "All green for demo 2?"

---

### 3:50–4:00 — UNUSED BUFFER

**Reserved for:**
- Demo running slow? Use this 10 seconds to catch up
- Early completion? Use for extended Q&A or deeper code dive
- Technical hiccup? Recover here without derailing main timeline

---

## Contingency Plans

### If Function Doesn't Respond (No HTTP 200)

**Scenario:** Curl returns timeout or error

**Action (Chad + Danny, <30 sec):**
1. Check terminal: "Let me verify the function is online"
2. Invoke a simple health check: `curl -I https://[fn].azurewebsites.net/api/health`
3. If health check fails: **Activate Fallback**
4. If health check succeeds: Re-run ScoreTheDiamond (function might have been in cold start)

**Fallback:** Show pre-recorded response + explain: "Network hiccups happen. Here's what we saw yesterday when this ran smoothly."

### If Execution Takes >5 Seconds

**Scenario:** Real-world cold start or network delay

**Action:**
- Don't panic. Explain: "Azure Functions is sometimes slower on first invoke. This is normal."
- Skip alternate scenario (timeout demo)
- Jump to summary
- Use buffer time to discuss cold start mitigation (function keep-alive timers)

### If Execution Takes <1 Second (Unexpectedly Fast)

**Scenario:** All functions respond immediately; no latency

**Action (Good sign!):**
- Show Insights traces in detail
- Discuss: "In low-load environments, Azure Functions respond very quickly. In production, with concurrent requests, latency increases."
- Walk through the logs: "Here's GetTargetIntel. Here's CheckSecurity. Here's EstimateCut. All parallel."

### If Application Insights Dashboard Doesn't Load

**Fallback view:** Function logs in terminal instead
```
[3/19/2026 10:05:30 AM] GetTargetIntel started
[3/19/2026 10:05:30.492 AM] GetTargetIntel completed in 492ms
[3/19/2026 10:05:30 AM] CheckSecurity started
[3/19/2026 10:05:31.783 AM] CheckSecurity completed in 1783ms
[3/19/2026 10:05:30 AM] EstimateCut started
[3/19/2026 10:05:30.301 AM] EstimateCut completed in 301ms
[3/19/2026 10:05:31.800 AM] Aggregation complete. Confidence: 0.95
```

**Chad's explanation:** "The logs show the same timing. Three functions fanned out at T=0, EstimateCut and GetTargetIntel resolved fast, CheckSecurity took longest."

---

## Success Criteria Checklist

- [ ] Demo completes in 10 minutes ±30 seconds
- [ ] All 3 functions respond and appear in logs/Insights
- [ ] Aggregation completes in 2–3 seconds
- [ ] Confidence score displayed (0.95 or 0.65 if degraded)
- [ ] Alternate scenario shown (live or pre-recorded)
- [ ] Audience understands timeout + degradation concept
- [ ] Q&A answered briefly; transition smooth to Demo 2

---

## Speaking Notes (1 minute is ~130–150 words)

### Opening (~130 words for 1 min)
"Ocean's 11 opening: Danny Ocean's crew needs intel on the Bellagio heist. Three critical pieces FAST: target intel, security assessment, payoff calculation. All go out in parallel. Azure Functions orchestrates them. If security takes too long, Danny doesn't block. He proceeds with DEGRADED confidence. This is AGGREGATION: multiple async calls, timeout handling, fallback strategy."

### Setup (~100 words for 45 sec)
"ScoreTheDiamond uses Task.WhenAll() to fan out to three services in parallel. Each simulates real-world latency: 500ms to 2 seconds. If any exceeds 5 seconds, timeout triggers. Application Insights shows the execution timeline. Three services, simultaneous execution. Watch the timeline build."

### Execution (~80 words for 30 sec during wait)
"Notice parallel execution. All three functions work simultaneously. Sequential would take 3+ seconds. Parallel: ~2 seconds, dominated by the slowest function. 33% faster. In production, gains are bigger with higher variance."

### Summary (~100 words for 1 min)
"Aggregation Pattern: When you depend on multiple services, don't block forever. Set timeouts. Declare partial success. Your system degrades and keeps operating. In real life: microservices APIs, third-party integrations, database replicas. One failure doesn't crash you. Degrade and continue. Azure Functions + Task.WhenAll() + timeout logic = resilience."

---

## Next Demo: "Pull the Job" (Queue-Based Load Leveling)

At 4:00 (after transition), Chad will introduce queue-based load leveling with Service Bus and worker functions. Danny will show how 50 messages are queued and processed by 4 workers in parallel, with ~5 failing to the dead-letter queue.
