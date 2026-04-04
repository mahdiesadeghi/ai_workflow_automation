using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WorkflowAutomation.Infrastructure.Services;

/// <summary>
/// Client for the Windmill.dev API.
/// Connects to a self-hosted Windmill instance to trigger and monitor workflow jobs.
/// </summary>
public class WindmillClient
{
    private readonly ILogger<WindmillClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _windmillUrl;
    private readonly string _workspace;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public WindmillClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<WindmillClient> logger)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _windmillUrl = (configuration["Windmill:Url"] ?? "http://localhost:8000").TrimEnd('/');
        _workspace = configuration["Windmill:Workspace"] ?? "default";

        var token = configuration["Windmill:Token"];
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    /// <summary>
    /// Triggers a script or flow on Windmill and returns the job ID.
    /// </summary>
    /// <param name="scriptPath">The Windmill script/flow path (e.g. "f/workflows/energy_analysis").</param>
    /// <param name="input">Input arguments for the script.</param>
    /// <returns>The Windmill job ID.</returns>
    public async Task<string> TriggerScript(string scriptPath, Dictionary<string, object> input)
    {
        var url = $"{_windmillUrl}/api/w/{_workspace}/jobs/run/f/{scriptPath}";

        _logger.LogInformation(
            "Windmill: Triggering script {ScriptPath} at {Url}",
            scriptPath, url);

        var response = await _httpClient.PostAsJsonAsync(url, input, JsonOptions);
        response.EnsureSuccessStatusCode();

        var jobId = await response.Content.ReadAsStringAsync();
        // Windmill returns the job ID as a plain quoted string
        jobId = jobId.Trim().Trim('"');

        _logger.LogInformation("Windmill: Job started with ID {JobId}", jobId);
        return jobId;
    }

    /// <summary>
    /// Triggers a workflow execution on Windmill and returns a job ID.
    /// This is a convenience wrapper that builds a script path from the workflow ID.
    /// </summary>
    public async Task<string> TriggerWorkflow(Guid workflowId, Dictionary<string, object> input)
    {
        var scriptPath = $"f/workflows/workflow_{workflowId:N}";
        return await TriggerScript(scriptPath, input);
    }

    /// <summary>
    /// Gets the status of a Windmill job.
    /// </summary>
    /// <param name="jobId">The Windmill job ID to check.</param>
    /// <returns>The job status.</returns>
    public async Task<WindmillJobStatus> GetJobStatus(string jobId)
    {
        var url = $"{_windmillUrl}/api/w/{_workspace}/jobs_u/get/{jobId}";

        _logger.LogInformation("Windmill: Checking status of job {JobId}", jobId);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        var status = new WindmillJobStatus
        {
            JobId = jobId,
            State = json.TryGetProperty("type", out var typeProp) ? typeProp.GetString() ?? "" : "",
            Success = json.TryGetProperty("success", out var successProp) && successProp.GetBoolean(),
            StartedAt = json.TryGetProperty("started_at", out var startProp) && startProp.ValueKind != JsonValueKind.Null
                ? startProp.GetDateTime() : null,
            CompletedAt = json.TryGetProperty("completed_at", out var completeProp) && completeProp.ValueKind != JsonValueKind.Null
                ? completeProp.GetDateTime() : null,
            Error = json.TryGetProperty("result", out var resultProp) &&
                    resultProp.ValueKind == JsonValueKind.Object &&
                    resultProp.TryGetProperty("error", out var errorProp)
                ? errorProp.GetString() : null
        };

        _logger.LogInformation(
            "Windmill: Job {JobId} state={State} success={Success}",
            jobId, status.State, status.Success);

        return status;
    }

    /// <summary>
    /// Lists available scripts/flows in the workspace.
    /// Useful for verifying connectivity and discovering available workflows.
    /// </summary>
    public async Task<List<WindmillScript>> ListScripts()
    {
        var url = $"{_windmillUrl}/api/w/{_workspace}/scripts/list";

        _logger.LogInformation("Windmill: Listing scripts in workspace {Workspace}", _workspace);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var scripts = await response.Content.ReadFromJsonAsync<List<WindmillScript>>(JsonOptions)
            ?? new List<WindmillScript>();

        _logger.LogInformation("Windmill: Found {Count} scripts", scripts.Count);
        return scripts;
    }

    /// <summary>
    /// Health check: verifies the Windmill instance is reachable.
    /// </summary>
    public async Task<bool> IsHealthy()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_windmillUrl}/api/version");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Windmill health check failed for {Url}", _windmillUrl);
            return false;
        }
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

/// <summary>
/// Represents a Windmill script entry from the list endpoint.
/// </summary>
public class WindmillScript
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;
}
