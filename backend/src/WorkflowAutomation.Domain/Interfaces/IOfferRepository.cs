using WorkflowAutomation.Domain.ValueObjects;

namespace WorkflowAutomation.Domain.Interfaces;

/// <summary>
/// Repository abstraction for querying cached provider offers.
/// </summary>
public interface IOfferRepository
{
    /// <summary>
    /// Retrieves all available offers.
    /// </summary>
    Task<List<OfferInfo>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves offers from a specific provider.
    /// </summary>
    Task<List<OfferInfo>> GetByProviderAsync(string provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for offers matching the given plan type with a price at or below <paramref name="maxPrice"/>.
    /// </summary>
    Task<List<OfferInfo>> SearchAsync(string planType, decimal maxPrice, CancellationToken cancellationToken = default);
}
