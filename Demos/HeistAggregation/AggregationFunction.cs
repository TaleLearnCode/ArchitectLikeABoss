using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HeistAggregation;

/// <summary>
/// HTTP-triggered Azure Function that demonstrates the aggregation pattern.
/// Receives requests and aggregates responses from multiple downstream services.
/// </summary>
public class AggregationFunction
{
    private readonly AggregationService _aggregationService;
    private readonly ILogger<AggregationFunction> _logger;

    public AggregationFunction(AggregationService aggregationService, ILogger<AggregationFunction> logger)
    {
        _aggregationService = aggregationService;
        _logger = logger;
    }

    [Function("AggregateDownstreamServices")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "aggregate")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Aggregation function triggered");
        
        var requestId = Guid.NewGuid().ToString();
        var aggregationResult = await _aggregationService.AggregateAsync(requestId);
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        
        await response.WriteAsJsonAsync(aggregationResult);
        return response;
    }
}
