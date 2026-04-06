// Windmill Script: Input Validation
// Path: f/workflows/input_validation
// Language: TypeScript (Deno)
//
// Validates the contract input data for completeness and correctness.

export async function main(
  workflow_id: string,
  step: string,
  provider: string,
  current_price: number,
  duration: number,
  plan_type: string,
  customer_name: string,
): Promise<string> {
  const errors: string[] = [];

  if (!provider || provider.trim().length === 0) {
    errors.push("Provider is required.");
  }

  if (!current_price || current_price <= 0) {
    errors.push("Current price must be greater than zero.");
  }

  if (duration < 0) {
    errors.push("Duration cannot be negative.");
  }

  if (!plan_type || plan_type.trim().length === 0) {
    errors.push("Plan type is required.");
  }

  if (!customer_name || customer_name.trim().length === 0) {
    errors.push("Customer name is required.");
  }

  if (errors.length > 0) {
    throw new Error(`Validation failed: ${errors.join(" ")}`);
  }

  return `Input validated successfully for workflow ${workflow_id}`;
}
