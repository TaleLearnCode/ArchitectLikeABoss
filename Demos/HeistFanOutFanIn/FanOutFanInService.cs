using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HeistFanOutFanIn;

/// <summary>
/// FanOutFanInService demonstrates the fan-out/fan-in pattern using Durable Functions:
/// - Durable function orchestrator receives request
/// - Orchestrator fans out to 3-5 activity functions in parallel
/// - Orchestrator waits for all activities to complete
/// - Shows concurrency and timing in logs
/// </summary>
public class FanOutFanInService
{
    private readonly ILogger<FanOutFanInService> _logger;

    public FanOutFanInService(ILogger<FanOutFanInService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Simulate a parallel activity function.
    /// </summary>
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

    /// <summary>
    /// Orchestrate parallel activity execution.
    /// </summary>
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
}

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
