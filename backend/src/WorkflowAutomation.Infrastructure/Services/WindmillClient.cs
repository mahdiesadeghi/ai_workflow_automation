using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WorkflowAutomation.Infrastructure.Services;

/// <summary>
/// Mock Windmill.dev client that simulates calling the Windmill API.
/// This is the integration point for connecting to a real Windmill instance.
///
/// In production, this would make HTTP calls to the Windmill API:
///   POST {windmillUrl}/api/w/{workspace}/jobs/run/f/{scriptPath}
///   GET  {windmillUrl}/api/w/{workspace}/jobs_u/get/{jobId}
/// </summary>
public class WindmillClient
{
    private readonly ILogger<WindmillClient> _logger;
    private readonly string _windmillUrl;
    private readonly string _workspace;

    public WindmillClient(IConfiguration configuration, ILogger<WindmillClient> logger)
    {
        _logger = logger;
        _windmillUrl = configuration["Windmill:Url"] ?? "http://localhost:8000";
        _workspace = configuration["Windmill:Workspace"] ?? "default";
    }

    /// <summary>
    /// Triggers a workflow execution on Windmill and returns a job ID.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="input">The input data for the workflow as a dictionary.</param>
    /// <returns>A mock job ID representing the Windmill job.</returns>
    public Task<string> TriggerWorkflow(Guid workflowId, Dictionary<string, object> input)
    {
        var jobId = $"windmill-job-{Guid.NewGuid():N}";

        _logger.LogInformation(
            "Mock Windmill: Triggering workflow {WorkflowId} on {WindmillUrl}/api/w/{Workspace}. " +
            "Assigned job ID: {JobId}",
            workflowId, _windmillUrl, _workspace, jobId);

        _logger.LogDebug("Mock Windmill: Input parameters: {InputKeys}",
            string.Join(", ", input.Keys));

        // In production, this would be:
        // var response = await _httpClient.PostAsJsonAsync(
        //     $"{_windmillUrl}/api/w/{_workspace}/jobs/run/f/workflow_{workflowId}",
        //     input);
        // var result = await response.Content.ReadFromJsonAsync<WindmillJobResponse>();
        // return result.JobId;

        return Task.FromResult(jobId);
    }

    /// <summary>
    /// Gets the status of a running Windmill job.
    /// </summary>
    /// <param name="jobId">The Windmill job ID to check.</param>
    /// <returns>A mock status object with the job state.</returns>
    public Task<WindmillJobStatus> GetJobStatus(string jobId)
    {
        _logger.LogInformation(
            "Mock Windmill: Checking status of job {JobId} on {WindmillUrl}",
            jobId, _windmillUrl);

        // In production, this would be:
        // var response = await _httpClient.GetFromJsonAsync<WindmillJobStatus>(
        //     $"{_windmillUrl}/api/w/{_workspace}/jobs_u/get/{jobId}");

        var status = new WindmillJobStatus
        {
            JobId = jobId,
            State = "completed",
            Success = true,
            StartedAt = DateTime.UtcNow.AddSeconds(-5),
            CompletedAt = DateTime.UtcNow
        };

        return Task.FromResult(status);
    }
}

/// <summary>
/// Represents the status of a Windmill job.
/// </summary>
public class WindmillJobStatus
{
    public string JobId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public bool Success { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
}
