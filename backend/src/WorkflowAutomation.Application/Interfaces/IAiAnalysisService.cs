using WorkflowAutomation.Domain.ValueObjects;

namespace WorkflowAutomation.Application.Interfaces;

/// <summary>
/// Service abstraction for AI-powered contract analysis.
/// </summary>
public interface IAiAnalysisService
{
    /// <summary>
    /// Analyzes a customer's current contract against available offers
    /// and produces a recommendation.
    /// </summary>
    /// <param name="input">The customer's current contract details.</param>
    /// <param name="offers">Available alternative offers to compare against.</param>
    /// <returns>An analysis result with a recommendation.</returns>
    Task<WorkflowResult> AnalyzeContractAsync(ContractInput input, List<OfferInfo> offers);
}
