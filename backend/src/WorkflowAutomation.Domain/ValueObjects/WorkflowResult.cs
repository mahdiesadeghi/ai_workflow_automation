namespace WorkflowAutomation.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing the outcome of a workflow analysis.
/// </summary>
public sealed class WorkflowResult : IEquatable<WorkflowResult>
{
    /// <summary>
    /// The recommendation: "keep" the current contract or "switch" to a new offer.
    /// </summary>
    public string Recommendation { get; }

    /// <summary>
    /// Human-readable explanation of the recommendation.
    /// </summary>
    public string Reasoning { get; }

    /// <summary>
    /// The suggested alternative offer, if a switch is recommended.
    /// </summary>
    public OfferInfo? SuggestedOffer { get; }

    /// <summary>
    /// Estimated monthly savings in the customer's currency.
    /// </summary>
    public decimal EstimatedSavings { get; }

    /// <summary>
    /// The timestamp when the analysis was performed.
    /// </summary>
    public DateTime AnalyzedAt { get; }

    public WorkflowResult(
        string recommendation,
        string reasoning,
        OfferInfo? suggestedOffer,
        decimal estimatedSavings,
        DateTime analyzedAt)
    {
        Recommendation = recommendation ?? throw new ArgumentNullException(nameof(recommendation));
        Reasoning = reasoning ?? throw new ArgumentNullException(nameof(reasoning));
        SuggestedOffer = suggestedOffer;
        EstimatedSavings = estimatedSavings;
        AnalyzedAt = analyzedAt;
    }

    public bool Equals(WorkflowResult? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Recommendation == other.Recommendation
            && Reasoning == other.Reasoning
            && Equals(SuggestedOffer, other.SuggestedOffer)
            && EstimatedSavings == other.EstimatedSavings
            && AnalyzedAt == other.AnalyzedAt;
    }

    public override bool Equals(object? obj) => Equals(obj as WorkflowResult);

    public override int GetHashCode() =>
        HashCode.Combine(Recommendation, Reasoning, SuggestedOffer, EstimatedSavings, AnalyzedAt);

    public static bool operator ==(WorkflowResult? left, WorkflowResult? right) =>
        Equals(left, right);

    public static bool operator !=(WorkflowResult? left, WorkflowResult? right) =>
        !Equals(left, right);
}
