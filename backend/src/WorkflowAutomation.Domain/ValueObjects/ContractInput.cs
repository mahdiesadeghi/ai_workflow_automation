namespace WorkflowAutomation.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing the input data for a contract analysis workflow.
/// </summary>
public sealed class ContractInput : IEquatable<ContractInput>
{
    /// <summary>
    /// The current energy provider name.
    /// </summary>
    public string Provider { get; }

    /// <summary>
    /// The current monthly price in the customer's currency.
    /// </summary>
    public decimal CurrentPrice { get; }

    /// <summary>
    /// The remaining contract duration in months.
    /// </summary>
    public int Duration { get; }

    /// <summary>
    /// The type of energy plan (e.g. "electricity", "gas").
    /// </summary>
    public string PlanType { get; }

    /// <summary>
    /// The name of the customer requesting the analysis.
    /// </summary>
    public string CustomerName { get; }

    public ContractInput(string provider, decimal currentPrice, int duration, string planType, string customerName)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        CurrentPrice = currentPrice;
        Duration = duration;
        PlanType = planType ?? throw new ArgumentNullException(nameof(planType));
        CustomerName = customerName ?? throw new ArgumentNullException(nameof(customerName));
    }

    public bool Equals(ContractInput? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Provider == other.Provider
            && CurrentPrice == other.CurrentPrice
            && Duration == other.Duration
            && PlanType == other.PlanType
            && CustomerName == other.CustomerName;
    }

    public override bool Equals(object? obj) => Equals(obj as ContractInput);

    public override int GetHashCode() =>
        HashCode.Combine(Provider, CurrentPrice, Duration, PlanType, CustomerName);

    public static bool operator ==(ContractInput? left, ContractInput? right) =>
        Equals(left, right);

    public static bool operator !=(ContractInput? left, ContractInput? right) =>
        !Equals(left, right);
}
