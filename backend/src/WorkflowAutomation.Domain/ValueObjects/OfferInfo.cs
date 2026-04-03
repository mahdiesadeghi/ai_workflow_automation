namespace WorkflowAutomation.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a provider's offer details.
/// </summary>
public sealed class OfferInfo : IEquatable<OfferInfo>
{
    /// <summary>
    /// The provider offering this plan.
    /// </summary>
    public string Provider { get; }

    /// <summary>
    /// The monthly price of the offer.
    /// </summary>
    public decimal Price { get; }

    /// <summary>
    /// List of features included in the offer.
    /// </summary>
    public List<string> Features { get; }

    /// <summary>
    /// The name of the plan.
    /// </summary>
    public string PlanName { get; }

    public OfferInfo(string provider, decimal price, List<string> features, string planName)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Price = price;
        Features = features ?? throw new ArgumentNullException(nameof(features));
        PlanName = planName ?? throw new ArgumentNullException(nameof(planName));
    }

    public bool Equals(OfferInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Provider == other.Provider
            && Price == other.Price
            && PlanName == other.PlanName
            && Features.SequenceEqual(other.Features);
    }

    public override bool Equals(object? obj) => Equals(obj as OfferInfo);

    public override int GetHashCode() =>
        HashCode.Combine(Provider, Price, PlanName, Features.Count);

    public static bool operator ==(OfferInfo? left, OfferInfo? right) =>
        Equals(left, right);

    public static bool operator !=(OfferInfo? left, OfferInfo? right) =>
        !Equals(left, right);
}
