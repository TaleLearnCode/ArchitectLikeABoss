using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HeistQueueLoadLevel;

/// <summary>
/// QueueLoadLevelingService demonstrates the queue-based load leveling pattern:
/// - HTTP trigger receives burst of requests
/// - Requests are queued to Azure Service Bus
/// - Separate worker function processes queue at steady rate
/// - Includes poison message handling (bad messages → DLQ)
/// </summary>
public class QueueLoadLevelingService
{
    private readonly ILogger<QueueLoadLevelingService> _logger;

    public QueueLoadLevelingService(ILogger<QueueLoadLevelingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Queue a message to the load leveling queue.
    /// </summary>
    public async Task QueueMessageAsync(string message, string messageId)
    {
        _logger.LogInformation("Queuing message {MessageId}: {Message}", messageId, message);
        // TODO: Implement Service Bus queue send
        await Task.Delay(100); // Simulate queueing
    }

    /// <summary>
    /// Process a message from the queue.
    /// Includes retry logic and poison message detection.
    /// </summary>
    public async Task ProcessMessageAsync(string message, string messageId, int deliveryCount)
    {
        _logger.LogInformation("Processing message {MessageId} (attempt {DeliveryCount}): {Message}", 
            messageId, deliveryCount, message);

        try
        {
            // Simulate processing
            if (message.Contains("POISON"))
            {
                throw new InvalidOperationException("Poison message detected");
            }

            await Task.Delay(500); // Simulate work
            _logger.LogInformation("Message {MessageId} processed successfully", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId}", messageId);
            
            if (deliveryCount > 3)
            {
                _logger.LogError("Message {MessageId} exceeded max retries, sending to DLQ", messageId);
                // TODO: Send to DLQ
            }
            throw;
        }
    }
}
