using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowAutomation.Domain.Entities;
using WorkflowAutomation.Domain.Enums;
using WorkflowAutomation.Domain.ValueObjects;

namespace WorkflowAutomation.Infrastructure.Data.Configurations;

public class WorkflowConfiguration : IEntityTypeConfiguration<Workflow>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public void Configure(EntityTypeBuilder<Workflow> builder)
    {
        builder.ToTable("Workflows");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(w => w.CreatedAt).IsRequired();
        builder.Property(w => w.UpdatedAt).IsRequired();

        // ContractInput stored as JSON column
        builder.Property(w => w.InputData)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => DeserializeContractInput(v))
            .HasColumnType("jsonb")
            .HasColumnName("InputData")
            .IsRequired();

        // WorkflowResult stored as JSON column (nullable)
        builder.Property(w => w.Result)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                v => v == null ? null : DeserializeWorkflowResult(v))
            .HasColumnType("jsonb")
            .HasColumnName("Result");

        // Configure relationship with steps
        builder.HasMany(w => w.Steps)
            .WithOne()
            .HasForeignKey(s => s.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        // Access the backing field for Steps
        builder.Navigation(w => w.Steps)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_steps");
    }

    private static ContractInput DeserializeContractInput(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new ContractInput(
            provider: root.GetProperty("provider").GetString() ?? string.Empty,
            currentPrice: root.GetProperty("currentPrice").GetDecimal(),
            duration: root.GetProperty("duration").GetInt32(),
            planType: root.GetProperty("planType").GetString() ?? string.Empty,
            customerName: root.GetProperty("customerName").GetString() ?? string.Empty);
    }

    private static WorkflowResult DeserializeWorkflowResult(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        OfferInfo? suggestedOffer = null;
        if (root.TryGetProperty("suggestedOffer", out var offerElement) &&
            offerElement.ValueKind != JsonValueKind.Null)
        {
            var features = new List<string>();
            if (offerElement.TryGetProperty("features", out var featuresElement))
            {
                foreach (var f in featuresElement.EnumerateArray())
                {
                    features.Add(f.GetString() ?? string.Empty);
                }
            }

            suggestedOffer = new OfferInfo(
                provider: offerElement.GetProperty("provider").GetString() ?? string.Empty,
                price: offerElement.GetProperty("price").GetDecimal(),
                features: features,
                planName: offerElement.GetProperty("planName").GetString() ?? string.Empty);
        }

        return new WorkflowResult(
            recommendation: root.GetProperty("recommendation").GetString() ?? string.Empty,
            reasoning: root.GetProperty("reasoning").GetString() ?? string.Empty,
            suggestedOffer: suggestedOffer,
            estimatedSavings: root.GetProperty("estimatedSavings").GetDecimal(),
            analyzedAt: root.GetProperty("analyzedAt").GetDateTime());
    }
}
