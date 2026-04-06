// Windmill Script: Decision
// Path: f/workflows/decision
// Language: TypeScript (Deno)
//
// Makes a keep/switch decision based on the analysis results.
// Uses the same deterministic logic as the AI analysis step.

export async function main(
  workflow_id: string,
  step: string,
  provider: string,
  current_price: number,
  duration: number,
  plan_type: string,
  customer_name: string,
): Promise<string> {
  // Re-evaluate cheapest alternative (mirrors .NET decision logic)
  const alternatives = [
    { provider: "SolarDirect", price: 75, planName: "SolarDirect Smart Electricity" },
    { provider: "NordGas", price: 68, planName: "NordGas Premium" },
    { provider: "CityPower", price: 82, planName: "CityPower Urban Plan" },
  ]
    .filter((o) => o.provider.toLowerCase() !== provider.toLowerCase())
    .sort((a, b) => a.price - b.price);

  if (alternatives.length === 0) {
    return "Decision: Keep current contract";
  }

  const cheapest = alternatives[0];
  const savingsPercentage =
    current_price > 0
      ? ((current_price - cheapest.price) / current_price) * 100
      : 0;

  if (savingsPercentage > 10) {
    return `Decision: Switch to ${cheapest.provider}`;
  }

  return "Decision: Keep current contract";
}
