using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HeistFanOutFanIn;

/// <summary>
/// HTTP-triggered function that initiates fan-out/fan-in orchestration.
/// </summary>
public class FanOutFanInOrchestratorFunction
{
    private readonly FanOutFanInService _fanOutFanInService;
    private readonly ILogger<FanOutFanInOrchestratorFunction> _logger;

    public FanOutFanInOrchestratorFunction(
        FanOutFanInService fanOutFanInService, 
        ILogger<FanOutFanInOrchestratorFunction> logger)
    {
        _fanOutFanInService = fanOutFanInService;
        _logger = logger;
    }

    [Function("StartFanOutFanIn")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "start-fanout")] HttpRequestData req)
    {
        _logger.LogInformation("Fan-out/fan-in orchestrator triggered");
        
        var instanceId = Guid.NewGuid().ToString();
        var result = await _fanOutFanInService.OrchestrateAsync(instanceId);
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteAsJsonAsync(result);
        return response;
    }
}

/// <summary>
/// Activity function for processing data (parallel execution).
/// </summary>
public class ProcessDataActivityFunction
{
    private readonly FanOutFanInService _fanOutFanInService;
    private readonly ILogger<ProcessDataActivityFunction> _logger;

    public ProcessDataActivityFunction(
        FanOutFanInService fanOutFanInService,
        ILogger<ProcessDataActivityFunction> logger)
    {
        _fanOutFanInService = fanOutFanInService;
        _logger = logger;
    }

    [Function("ProcessDataActivity")]
    public async Task<ActivityResult> Run([ActivityTrigger] int activityId)
    {
        _logger.LogInformation("Activity function executing for ID: {ActivityId}", activityId);
        return await _fanOutFanInService.ExecuteActivityAsync("ProcessData", activityId);
    }
}
