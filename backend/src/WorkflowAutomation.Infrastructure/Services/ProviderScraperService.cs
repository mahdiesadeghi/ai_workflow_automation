using Microsoft.Extensions.Logging;
using WorkflowAutomation.Application.Interfaces;
using WorkflowAutomation.Domain.ValueObjects;

namespace WorkflowAutomation.Infrastructure.Services;

/// <summary>
/// Mock implementation of the provider scraper service.
/// Returns sample offers simulating what a real web scraper would retrieve.
///
/// In production, this would use Microsoft.Playwright to:
///   1. Launch a headless browser via Playwright.CreateAsync()
///   2. Navigate to provider comparison websites
///   3. Fill in search forms with plan type and location
///   4. Extract offer data from result tables using page.QuerySelectorAllAsync()
///   5. Parse prices, features, and contract terms from the DOM
///   6. Return structured OfferInfo objects
///
/// Example Playwright usage:
///   using var playwright = await Playwright.CreateAsync();
///   await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
///   var page = await browser.NewPageAsync();
///   await page.GotoAsync("https://energy-comparison-site.example.com");
///   await page.FillAsync("#search-input", provider);
///   await page.ClickAsync("#search-button");
///   var offers = await page.QuerySelectorAllAsync(".offer-card");
/// </summary>
public class ProviderScraperService : IProviderScraperService
{
    private readonly ILogger<ProviderScraperService> _logger;

    public ProviderScraperService(ILogger<ProviderScraperService> logger)
    {
        _logger = logger;
    }

    public async Task<List<OfferInfo>> ScrapeProviderOffersAsync(string provider)
    {
        _logger.LogInformation("Scraping offers for provider: {Provider} (mock)", provider);

        // Simulate network/scraping delay
        await Task.Delay(300);

        // Return mock scraped data as if it came from a comparison website
        var scrapedOffers = new List<OfferInfo>
        {
            new OfferInfo(
                provider: "SolarDirect",
                price: 75m,
                features: new List<string> { "solar-powered", "dynamic pricing", "app control" },
                planName: "SolarDirect Smart Electricity"),

            new OfferInfo(
                provider: "NordGas",
                price: 68m,
                features: new List<string> { "Nordic sourced", "12-month fixed", "carbon neutral" },
                planName: "NordGas Premium"),

            new OfferInfo(
                provider: "CityPower",
                price: 82m,
                features: new List<string> { "urban network", "no exit fees", "weekly billing" },
                planName: "CityPower Urban Plan")
        };

        _logger.LogInformation("Scraped {Count} offers from comparison sites", scrapedOffers.Count);

        return scrapedOffers;
    }
}
