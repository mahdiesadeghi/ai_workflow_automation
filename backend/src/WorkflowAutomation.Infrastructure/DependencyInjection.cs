using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowAutomation.Application.Interfaces;
using WorkflowAutomation.Domain.Interfaces;
using WorkflowAutomation.Infrastructure.Data;
using WorkflowAutomation.Infrastructure.Repositories;
using WorkflowAutomation.Infrastructure.Services;

namespace WorkflowAutomation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database context
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(3);
                }));
        }
        else
        {
            // Fallback to in-memory database for development
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("WorkflowAutomationDb"));
        }

        // Repositories
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();

        // Orchestrators — keyed by execution mode
        services.AddKeyedScoped<IWorkflowOrchestrator, WorkflowOrchestrator>("dotnet");
        services.AddKeyedScoped<IWorkflowOrchestrator, WindmillOrchestrator>("windmill");
        // Default (non-keyed) registration for backward compatibility
        services.AddScoped<IWorkflowOrchestrator, WorkflowOrchestrator>();

        services.AddScoped<IAiAnalysisService, AiAnalysisService>();
        services.AddScoped<IProviderScraperService, ProviderScraperService>();

        // HTTP client for external integrations
        services.AddHttpClient();

        // Windmill client (scoped — uses IHttpClientFactory)
        services.AddScoped<WindmillClient>();

        return services;
    }
}
