// Windmill Script: AI Analysis
// Path: f/workflows/ai_analysis
// Language: TypeScript (Deno)
//
// Performs deterministic contract analysis comparing current contract
// against available alternatives. Recommends "keep" or "switch" based
// on a 10% savings threshold.

interface OfferInfo {
  provider: string;
  price: number;
  features: string[];
  planName: string;
}

export async function main(
  workflow_id: string,
  step: string,
  provider: string,
  current_price: number,
  duration: number,
  plan_type: string,
  customer_name: string,
): Promise<string> {
  // Simulated offers (same as provider_scraping step)
  const offers: OfferInfo[] = [
    {
      provider: "SolarDirect",
      price: 75,
      features: ["solar-powered", "dynamic pricing", "app control"],
      planName: "SolarDirect Smart Electricity",
    },
    {
      provider: "NordGas",
      price: 68,
      features: ["Nordic sourced", "12-month fixed", "carbon neutral"],
      planName: "NordGas Premium",
    },
    {
      provider: "CityPower",
      price: 82,
      features: ["urban network", "no exit fees", "weekly billing"],
      planName: "CityPower Urban Plan",
    },
  ];

  // Filter out current provider
  const alternatives = offers
    .filter((o) => o.provider.toLowerCase() !== provider.toLowerCase())
    .sort((a, b) => a.price - b.price);

  if (alternatives.length === 0) {
    return "AI recommends: keep (savings: $0.00/month)";
  }

  const cheapest = alternatives[0];
  const savings = current_price - cheapest.price;
  const savingsPercentage =
    current_price > 0 ? (savings / current_price) * 100 : 0;

  if (savingsPercentage > 10) {
    return (
      `AI recommends: switch ` +
      `(savings: $${savings.toFixed(2)}/month, ` +
      `${savingsPercentage.toFixed(1)}% with ${cheapest.planName})`
    );
  }

  return `AI recommends: keep (savings: $${Math.max(0, savings).toFixed(2)}/month, below 10% threshold)`;
}
