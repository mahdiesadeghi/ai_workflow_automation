using WorkflowAutomation.Domain.Interfaces;
using WorkflowAutomation.Domain.ValueObjects;

namespace WorkflowAutomation.Infrastructure.Repositories;

/// <summary>
/// In-memory offer repository with sample energy offers for MVP.
/// In production, this would query a database or external API.
/// </summary>
public class OfferRepository : IOfferRepository
{
    private static readonly List<OfferInfo> SampleOffers = new()
    {
        new OfferInfo(
            provider: "GreenEnergy",
            price: 85m,
            features: new List<string> { "100% renewable", "no lock-in contract", "green certificate included" },
            planName: "GreenEnergy Electricity Basic"),

        new OfferInfo(
            provider: "PowerPlus",
            price: 92m,
            features: new List<string> { "12-month contract", "fixed rate", "free smart meter" },
            planName: "PowerPlus Electricity Pro"),

        new OfferInfo(
            provider: "EcoWatt",
            price: 78m,
            features: new List<string> { "green energy", "24-month contract", "price guarantee", "carbon offset" },
            planName: "EcoWatt Green Electricity"),

        new OfferInfo(
            provider: "GasNow",
            price: 65m,
            features: new List<string> { "variable rate", "no commitment", "monthly billing" },
            planName: "GasNow Flexible Gas"),

        new OfferInfo(
            provider: "WarmHome",
            price: 72m,
            features: new List<string> { "fixed 12-month rate", "boiler maintenance included", "priority support" },
            planName: "WarmHome Gas Comfort"),

        new OfferInfo(
            provider: "FlexiPower",
            price: 88m,
            features: new List<string> { "electricity + gas bundle", "single invoice", "10% bundle discount", "flexible cancellation" },
            planName: "FlexiPower Energy Bundle")
    };

    public Task<List<OfferInfo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SampleOffers.ToList());
    }

    public Task<List<OfferInfo>> GetByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        var result = SampleOffers
            .Where(o => o.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult(result);
    }

    public Task<List<OfferInfo>> SearchAsync(string planType, decimal maxPrice, CancellationToken cancellationToken = default)
    {
        var query = SampleOffers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(planType))
        {
            query = query.Where(o =>
                o.PlanName.Contains(planType, StringComparison.OrdinalIgnoreCase) ||
                o.Features.Any(f => f.Contains(planType, StringComparison.OrdinalIgnoreCase)));
        }

        if (maxPrice > 0)
        {
            query = query.Where(o => o.Price <= maxPrice);
        }

        return Task.FromResult(query.ToList());
    }
}
