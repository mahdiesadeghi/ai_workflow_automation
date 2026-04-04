using WorkflowAutomation.Domain.ValueObjects;

namespace WorkflowAutomation.Application.Interfaces;

/// <summary>
/// Service abstraction for scraping provider offers from external sources.
/// </summary>
public interface IProviderScraperService
{
    /// <summary>
    /// Scrapes and returns available offers for the specified provider.
    /// </summary>
    /// <param name="provider">The provider name to scrape offers for.</param>
    /// <returns>A list of offers from the provider.</returns>
    Task<List<OfferInfo>> ScrapeProviderOffersAsync(string provider);
}
