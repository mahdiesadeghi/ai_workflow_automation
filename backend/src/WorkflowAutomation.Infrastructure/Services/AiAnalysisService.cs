using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using WorkflowAutomation.Application.Interfaces;
using WorkflowAutomation.Domain.ValueObjects;

namespace WorkflowAutomation.Infrastructure.Services;

/// <summary>
/// AI-powered contract analysis service. Uses Semantic Kernel when an OpenAI key
/// is configured; otherwise falls back to a deterministic mock analysis.
/// </summary>
public class AiAnalysisService : IAiAnalysisService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiAnalysisService> _logger;

    private const string AnalysisPrompt =
        "Analyze this energy contract: Provider={0}, Price={1}/month, Duration={2} months, Type={3}. " +
        "Available alternatives: {4}. " +
        "Recommend whether to keep or switch. Provide reasoning and estimated savings.";

    public AiAnalysisService(IConfiguration configuration, ILogger<AiAnalysisService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WorkflowResult> AnalyzeContractAsync(ContractInput input, List<OfferInfo> offers)
    {
        var openAiKey = _configuration["OpenAI:ApiKey"];

        // Fall back to OPENAI_API_KEY environment variable if config value is missing
        if (string.IsNullOrWhiteSpace(openAiKey) || openAiKey == "your-openai-api-key-here")
        {
            openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        }

        if (!string.IsNullOrWhiteSpace(openAiKey) && openAiKey != "your-openai-api-key-here")
        {
            return await AnalyzeWithSemanticKernelAsync(input, offers, openAiKey);
        }

        _logger.LogInformation("No OpenAI API key configured. Using deterministic mock analysis.");
        return PerformDeterministicAnalysis(input, offers);
    }

    private async Task<WorkflowResult> AnalyzeWithSemanticKernelAsync(
        ContractInput input,
        List<OfferInfo> offers,
        string apiKey)
    {
        try
        {
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion("gpt-4o-mini", apiKey);
            var kernel = builder.Build();

            var offersDescription = string.Join("; ", offers.Select(o =>
                $"{o.Provider}: {o.PlanName} at {o.Price:C}/month [{string.Join(", ", o.Features)}]"));

            var prompt = string.Format(
                AnalysisPrompt,
                input.Provider,
                input.CurrentPrice,
                input.Duration,
                input.PlanType,
                offersDescription);

            _logger.LogInformation("Sending contract analysis to Semantic Kernel for workflow");

            var result = await kernel.InvokePromptAsync(prompt);
            var response = result.GetValue<string>() ?? string.Empty;

            _logger.LogDebug("SK response: {Response}", response);

            // Parse the AI response into a structured result
            return ParseAiResponse(response, input, offers);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Semantic Kernel analysis failed, falling back to deterministic analysis");
            return PerformDeterministicAnalysis(input, offers);
        }
    }

    private WorkflowResult ParseAiResponse(string response, ContractInput input, List<OfferInfo> offers)
    {
        var lowerResponse = response.ToLowerInvariant();
        var shouldSwitch = lowerResponse.Contains("switch") || lowerResponse.Contains("recommend changing");

        OfferInfo? suggestedOffer = null;
        decimal estimatedSavings = 0m;

        if (shouldSwitch)
        {
            suggestedOffer = offers
                .Where(o => o.Price < input.CurrentPrice)
                .OrderBy(o => o.Price)
                .FirstOrDefault();

            if (suggestedOffer is not null)
            {
                estimatedSavings = input.CurrentPrice - suggestedOffer.Price;
            }
        }

        return new WorkflowResult(
            recommendation: shouldSwitch ? "switch" : "keep",
            reasoning: response,
            suggestedOffer: suggestedOffer,
            estimatedSavings: estimatedSavings,
            analyzedAt: DateTime.UtcNow);
    }

    /// <summary>
    /// Deterministic mock analysis: recommends switching if a cheaper offer exists
    /// with greater than 10% savings compared to the current price.
    /// </summary>
    private WorkflowResult PerformDeterministicAnalysis(ContractInput input, List<OfferInfo> offers)
    {
        _logger.LogInformation(
            "Running deterministic analysis for {Provider} at {Price}/month",
            input.Provider, input.CurrentPrice);

        // Filter offers by plan type relevance
        var relevantOffers = offers
            .Where(o =>
                o.PlanName.Contains(input.PlanType, StringComparison.OrdinalIgnoreCase) ||
                o.Features.Any(f => f.Contains(input.PlanType, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // If no type-matched offers, consider all offers
        if (relevantOffers.Count == 0)
        {
            relevantOffers = offers;
        }

        // Exclude current provider's offers
        var alternatives = relevantOffers
            .Where(o => !o.Provider.Equals(input.Provider, StringComparison.OrdinalIgnoreCase))
            .OrderBy(o => o.Price)
            .ToList();

        var cheapestOffer = alternatives.FirstOrDefault();

        if (cheapestOffer is null)
        {
            return new WorkflowResult(
                recommendation: "keep",
                reasoning: "No alternative offers available for comparison. " +
                           "Recommend keeping the current contract.",
                suggestedOffer: null,
                estimatedSavings: 0m,
                analyzedAt: DateTime.UtcNow);
        }

        var savings = input.CurrentPrice - cheapestOffer.Price;
        var savingsPercentage = input.CurrentPrice > 0
            ? (savings / input.CurrentPrice) * 100
            : 0;

        if (savingsPercentage > 10)
        {
            return new WorkflowResult(
                recommendation: "switch",
                reasoning: $"Found a significantly cheaper alternative: {cheapestOffer.PlanName} " +
                           $"from {cheapestOffer.Provider} at {cheapestOffer.Price:C}/month " +
                           $"(current: {input.CurrentPrice:C}/month). " +
                           $"Estimated savings of {savings:C}/month ({savingsPercentage:F1}%). " +
                           $"Features: {string.Join(", ", cheapestOffer.Features)}. " +
                           $"This exceeds the 10% savings threshold, making it a strong recommendation to switch.",
                suggestedOffer: cheapestOffer,
                estimatedSavings: savings,
                analyzedAt: DateTime.UtcNow);
        }

        return new WorkflowResult(
            recommendation: "keep",
            reasoning: $"The cheapest alternative is {cheapestOffer.PlanName} from {cheapestOffer.Provider} " +
                       $"at {cheapestOffer.Price:C}/month, " +
                       $"which would save {savings:C}/month ({savingsPercentage:F1}%). " +
                       $"This is below the 10% savings threshold. " +
                       $"Recommend keeping the current contract with {input.Provider}.",
            suggestedOffer: null,
            estimatedSavings: savings > 0 ? savings : 0m,
            analyzedAt: DateTime.UtcNow);
    }
}
