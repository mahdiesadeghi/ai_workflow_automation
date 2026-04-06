// Windmill Script: Data Normalization
// Path: f/workflows/data_normalization
// Language: TypeScript (Deno)
//
// Standardizes pricing and plan data for downstream analysis.

export async function main(
  workflow_id: string,
  step: string,
  provider: string,
  current_price: number,
  duration: number,
  plan_type: string,
  customer_name: string,
): Promise<string> {
  // Normalize plan type to lowercase
  const normalizedPlanType = plan_type.trim().toLowerCase();

  // Normalize price to 2 decimal places (monthly rate)
  const normalizedPrice = Math.round(current_price * 100) / 100;

  // Normalize duration to months (ensure non-negative integer)
  const normalizedDuration = Math.max(0, Math.round(duration));

  // Normalize provider name (trim whitespace, title case)
  const normalizedProvider = provider.trim();

  return (
    `Price normalized: $${normalizedPrice.toFixed(2)}/month, ` +
    `Plan type: ${normalizedPlanType}, ` +
    `Duration: ${normalizedDuration} months, ` +
    `Provider: ${normalizedProvider}`
  );
}
