using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HeistQueueLoadLevel;

/// <summary>
/// HTTP-triggered function that receives requests and queues them for load leveling.
/// </summary>
public class QueueRequestFunction
{
    private readonly QueueLoadLevelingService _queueService;
    private readonly ILogger<QueueRequestFunction> _logger;

    public QueueRequestFunction(QueueLoadLevelingService queueService, ILogger<QueueRequestFunction> logger)
    {
        _queueService = queueService;
        _logger = logger;
    }

    [Function("SubmitLoadLevelRequest")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "submit-request")] HttpRequestData req)
    {
        _logger.LogInformation("Load leveling HTTP trigger received");
        
        var messageId = Guid.NewGuid().ToString();
        var message = "Sample message";
        
        try
        {
            var body = await req.ReadAsStringAsync();
            message = string.IsNullOrEmpty(body) ? message : body;
        }
        catch { }
        
        await _queueService.QueueMessageAsync(message, messageId);
        
        var response = req.CreateResponse(HttpStatusCode.Accepted);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteAsJsonAsync(new { messageId, status = "queued" });
        return response;
    }
}

/// <summary>
/// Service Bus queue trigger that processes messages at a steady rate.
/// </summary>
public class ProcessLoadLeveledMessageFunction
{
    private readonly QueueLoadLevelingService _queueService;
    private readonly ILogger<ProcessLoadLeveledMessageFunction> _logger;

    public ProcessLoadLeveledMessageFunction(
        QueueLoadLevelingService queueService, 
        ILogger<ProcessLoadLeveledMessageFunction> logger)
    {
        _queueService = queueService;
        _logger = logger;
    }

    [Function("ProcessLoadLeveledMessage")]
    public async Task Run(
        [ServiceBusTrigger("load-level-queue", Connection = "ServiceBusConnection")] string queueItem,
        FunctionContext context)
    {
        _logger.LogInformation("Processing message from queue: {Message}", queueItem);
        
        var messageId = Guid.NewGuid().ToString();
        int deliveryCount = 1;
        
        await _queueService.ProcessMessageAsync(queueItem, messageId, deliveryCount);
    }
}
