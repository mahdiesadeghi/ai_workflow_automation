using Microsoft.AspNetCore.Mvc;
using WorkflowAutomation.Domain.Interfaces;
using WorkflowAutomation.Domain.ValueObjects;

namespace WorkflowAutomation.Api.Controllers;

/// <summary>
/// Provides access to available energy provider offers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OffersController : ControllerBase
{
    private readonly IOfferRepository _offerRepository;

    public OffersController(IOfferRepository offerRepository)
    {
        _offerRepository = offerRepository;
    }

    /// <summary>
    /// Retrieves all available energy offers.
    /// </summary>
    /// <returns>A list of all offers from all providers.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<OfferInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOffers()
    {
        var offers = await _offerRepository.GetAllAsync();
        return Ok(offers);
    }

    /// <summary>
    /// Searches offers by plan type and/or maximum price.
    /// </summary>
    /// <param name="planType">Filter by plan type (e.g., "electricity", "gas").</param>
    /// <param name="maxPrice">Maximum monthly price in euros.</param>
    /// <returns>A filtered list of matching offers.</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<OfferInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchOffers(
        [FromQuery] string? planType = null,
        [FromQuery] decimal? maxPrice = null)
    {
        var offers = await _offerRepository.SearchAsync(
            planType ?? string.Empty,
            maxPrice ?? decimal.MaxValue);
        return Ok(offers);
    }
}
