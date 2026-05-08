using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HeistAggregation;

/// <summary>
/// AggregationService demonstrates the aggregation pattern:
/// - Calls multiple downstream services in parallel
/// - Handles timeouts gracefully
/// - Falls back to cached data when services timeout
/// - Logs all activities for observability
/// </summary>
public class AggregationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AggregationService> _logger;
    private static readonly Dictionary<string, CachedResult> _cache = new();
    private const int TimeoutMs = 2000; // 2 second timeout per service

    public AggregationService(HttpClient httpClient, ILogger<AggregationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Aggregates responses from three parallel downstream services.
    /// Implements timeout handling and fallback to cached results.
    /// </summary>
    public async Task<AggregationResult> AggregateAsync(string requestId)
    {
        _logger.LogInformation("Starting aggregation for request {RequestId}", requestId);
        var stopwatch = Stopwatch.StartNew();
        
        var result = new AggregationResult 
        { 
            RequestId = requestId,
            Timestamp = DateTime.UtcNow,
            Services = new Dictionary<string, ServiceResult>()
        };

        // Simulate parallel calls to three services
        var tasks = new[]
        {
            CallDownstreamServiceAsync("PaymentService", requestId),
            CallDownstreamServiceAsync("InventoryService", requestId),
            CallDownstreamServiceAsync("ShippingService", requestId)
        };

        var serviceResults = await Task.WhenAll(tasks);
        
        foreach (var serviceResult in serviceResults)
        {
            result.Services[serviceResult.ServiceName] = serviceResult;
        }

        stopwatch.Stop();
        result.ElapsedMs = stopwatch.ElapsedMilliseconds;
        
        _logger.LogInformation(
            "Aggregation completed for request {RequestId} in {ElapsedMs}ms. Services: {ServiceCount}, Failures: {FailureCount}",
            requestId,
            stopwatch.ElapsedMilliseconds,
            result.Services.Count,
            result.Services.Values.Count(s => !s.Success)
        );

        return result;
    }

    private async Task<ServiceResult> CallDownstreamServiceAsync(string serviceName, string requestId)
    {
        try
        {
            _logger.LogInformation("Calling {ServiceName} for request {RequestId}", serviceName, requestId);
            
            using var cts = new System.Threading.CancellationTokenSource(TimeoutMs);
            
            // Simulate variable latency and occasional failures
            var delay = serviceName switch
            {
                "PaymentService" => 500,     // Fast service
                "InventoryService" => 2500,  // Will timeout
                "ShippingService" => 1000,   // Normal
                _ => 1000
            };

            await Task.Delay(delay, cts.Token);
            
            var responseData = new { 
                serviceName, 
                status = "success",
                data = $"Response from {serviceName}",
                timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Service {ServiceName} succeeded in {Delay}ms", serviceName, delay);
            
            return new ServiceResult
            {
                ServiceName = serviceName,
                Success = true,
                Data = responseData.ToString(),
                ElapsedMs = delay
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Service {ServiceName} timed out after {TimeoutMs}ms", serviceName, TimeoutMs);
            
            // Fallback to cached data
            if (_cache.TryGetValue(serviceName, out var cached))
            {
                _logger.LogInformation("Falling back to cached data for {ServiceName}", serviceName);
                return new ServiceResult
                {
                    ServiceName = serviceName,
                    Success = true,
                    Data = cached.Data,
                    ElapsedMs = TimeoutMs,
                    UsedCache = true
                };
            }

            return new ServiceResult
            {
                ServiceName = serviceName,
                Success = false,
                Error = $"Timeout after {TimeoutMs}ms and no cached data available",
                ElapsedMs = TimeoutMs
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling {ServiceName}", serviceName);
            return new ServiceResult
            {
                ServiceName = serviceName,
                Success = false,
                Error = ex.Message,
                ElapsedMs = 0
            };
        }
    }
}

public class AggregationResult
{
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, ServiceResult> Services { get; set; } = new();
    public long ElapsedMs { get; set; }
}

public class ServiceResult
{
    public string ServiceName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Data { get; set; }
    public string? Error { get; set; }
    public long ElapsedMs { get; set; }
    public bool UsedCache { get; set; }
}

public class CachedResult
{
    public string Data { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
}
