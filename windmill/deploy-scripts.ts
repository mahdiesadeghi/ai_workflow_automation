#!/usr/bin/env -S npx tsx
/**
 * Windmill Script Deployment Tool
 *
 * Deploys all workflow scripts to a self-hosted Windmill instance via its REST API.
 *
 * Usage:
 *   npx tsx windmill/deploy-scripts.ts
 *
 * Environment variables:
 *   WINDMILL_URL       - Windmill base URL (default: http://localhost:8000)
 *   WINDMILL_TOKEN     - Windmill API token (required)
 *   WINDMILL_WORKSPACE - Windmill workspace (default: default)
 */

import * as fs from "fs";
import * as path from "path";

const WINDMILL_URL = (
  process.env.WINDMILL_URL || "http://localhost:8000"
).replace(/\/$/, "");
const WINDMILL_TOKEN = process.env.WINDMILL_TOKEN || "";
const WINDMILL_WORKSPACE = process.env.WINDMILL_WORKSPACE || "default";

interface ScriptDefinition {
  /** Windmill path without the f/ prefix, e.g. "workflows/input_validation" */
  windmillPath: string;
  /** Local file path relative to this script's directory */
  localFile: string;
  /** Human-readable summary */
  summary: string;
  /** Description for Windmill UI */
  description: string;
}

const SCRIPTS: ScriptDefinition[] = [
  {
    windmillPath: "workflows/input_validation",
    localFile: "scripts/workflows/input_validation.ts",
    summary: "Input Validation",
    description:
      "Validates contract input data (provider, price, duration, plan type, customer name).",
  },
  {
    windmillPath: "workflows/data_normalization",
    localFile: "scripts/workflows/data_normalization.ts",
    summary: "Data Normalization",
    description:
      "Normalizes pricing, plan type, and duration for downstream analysis.",
  },
  {
    windmillPath: "workflows/provider_scraping",
    localFile: "scripts/workflows/provider_scraping.ts",
    summary: "Provider Scraping",
    description:
      "Scrapes competitor provider offers from comparison websites (simulated).",
  },
  {
    windmillPath: "workflows/ai_analysis",
    localFile: "scripts/workflows/ai_analysis.ts",
    summary: "AI Analysis",
    description:
      "Analyzes current contract against alternatives and recommends keep/switch.",
  },
  {
    windmillPath: "workflows/decision",
    localFile: "scripts/workflows/decision.ts",
    summary: "Decision",
    description:
      "Makes a keep/switch decision based on AI analysis results.",
  },
  {
    windmillPath: "workflows/execution",
    localFile: "scripts/workflows/execution.ts",
    summary: "Execution",
    description:
      "Executes the provider switch after human approval (simulated).",
  },
];

async function windmillFetch(
  endpoint: string,
  options: RequestInit = {},
): Promise<Response> {
  const url = `${WINDMILL_URL}/api/w/${WINDMILL_WORKSPACE}${endpoint}`;
  const response = await fetch(url, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${WINDMILL_TOKEN}`,
      ...((options.headers as Record<string, string>) || {}),
    },
  });
  return response;
}

async function checkHealth(): Promise<boolean> {
  try {
    const res = await fetch(`${WINDMILL_URL}/api/version`);
    if (!res.ok) return false;

    const authRes = await windmillFetch("/scripts/list?per_page=1");
    if (authRes.status === 401) {
      console.error(
        "ERROR: Windmill is reachable but authentication failed (401).",
      );
      console.error("       Set WINDMILL_TOKEN to a valid API token.");
      return false;
    }
    return authRes.ok;
  } catch {
    return false;
  }
}

async function scriptExists(scriptPath: string): Promise<boolean> {
  const res = await windmillFetch(
    `/scripts/get/p/f/${scriptPath}`,
  );
  return res.ok;
}

async function createOrUpdateScript(script: ScriptDefinition): Promise<void> {
  const scriptDir = path.dirname(new URL(import.meta.url).pathname).replace(/^\/([A-Za-z]:)/, '$1');
  const filePath = path.join(scriptDir, script.localFile);
  const content = fs.readFileSync(filePath, "utf-8");

  const exists = await scriptExists(script.windmillPath);

  const body = {
    path: `f/${script.windmillPath}`,
    summary: script.summary,
    description: script.description,
    content: content,
    language: "deno" as const,
    is_template: false,
    kind: "script" as const,
  };

  let res: Response;

  if (exists) {
    // Update existing script by creating a new version
    res = await windmillFetch(`/scripts/create`, {
      method: "POST",
      body: JSON.stringify({
        ...body,
        parent_hash: undefined,
      }),
    });
  } else {
    // Create new script
    res = await windmillFetch(`/scripts/create`, {
      method: "POST",
      body: JSON.stringify(body),
    });
  }

  if (!res.ok) {
    const errorText = await res.text();
    throw new Error(
      `Failed to deploy ${script.windmillPath}: ${res.status} ${errorText}`,
    );
  }

  console.log(
    `  ${exists ? "Updated" : "Created"}: f/${script.windmillPath}`,
  );
}

async function main(): Promise<void> {
  console.log("Windmill Script Deployment");
  console.log("=========================");
  console.log(`URL:       ${WINDMILL_URL}`);
  console.log(`Workspace: ${WINDMILL_WORKSPACE}`);
  console.log();

  if (!WINDMILL_TOKEN) {
    console.error("ERROR: WINDMILL_TOKEN environment variable is required.");
    console.error("");
    console.error("To get a token:");
    console.error("  1. Open Windmill UI at " + WINDMILL_URL);
    console.error('  2. Go to Settings > "Tokens" or your user menu');
    console.error("  3. Create a new token");
    console.error("");
    console.error("Then run:");
    console.error(
      "  WINDMILL_TOKEN=your_token npx tsx windmill/deploy-scripts.ts",
    );
    process.exit(1);
  }

  console.log("Checking Windmill connectivity...");
  const healthy = await checkHealth();
  if (!healthy) {
    console.error(
      `ERROR: Cannot connect to Windmill at ${WINDMILL_URL}`,
    );
    console.error("Make sure Windmill is running: docker compose up -d");
    process.exit(1);
  }
  console.log("Windmill is reachable and authenticated.\n");

  console.log(`Deploying ${SCRIPTS.length} scripts...\n`);

  let succeeded = 0;
  let failed = 0;

  for (const script of SCRIPTS) {
    try {
      await createOrUpdateScript(script);
      succeeded++;
    } catch (err) {
      console.error(`  FAILED: f/${script.windmillPath} - ${err}`);
      failed++;
    }
  }

  console.log();
  console.log(
    `Done: ${succeeded} deployed, ${failed} failed out of ${SCRIPTS.length} total.`,
  );

  if (failed > 0) {
    process.exit(1);
  }
}

main();
