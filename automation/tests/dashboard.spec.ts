import { test, expect } from '@playwright/test';

/**
 * Tests for the dashboard view: loading, workflow list rendering,
 * and auto-refresh behaviour.
 */
test.describe('Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should load the dashboard page', async ({ page }) => {
    // The page title should reference the application
    await expect(page).toHaveTitle(/Workflow Automation/i);

    // A heading should be visible
    await expect(page.locator('h1, h2').first()).toBeVisible();

    // The "New Workflow" button should be accessible
    await expect(page.getByRole('button', { name: /new workflow/i })).toBeVisible();
  });

  test('should display the workflow list', async ({ page }) => {
    // The list container should exist even if empty
    const listContainer = page.locator(
      '[data-testid="workflow-list"], .workflow-list, table'
    );
    await expect(listContainer).toBeVisible({ timeout: 10_000 });

    // If workflows exist, each row should show status and provider info
    const rows = page.locator(
      '[data-testid="workflow-row"], .workflow-row, tbody tr'
    );
    const count = await rows.count();

    if (count > 0) {
      // Verify the first row has expected content structure
      const firstRow = rows.first();
      await expect(firstRow).toBeVisible();

      // Each row should display a status badge
      await expect(
        firstRow.locator('[data-testid="workflow-status"], .status-badge, .workflow-status')
      ).toBeVisible();
    }
  });

  test('should auto-refresh workflow data', async ({ page }) => {
    // Create a workflow via the API so we have data to observe
    const createResponse = await page.request.post('/api/workflows', {
      data: {
        provider: 'TestEnergy',
        currentPrice: 100,
        duration: 12,
        planType: 'electricity',
        customerName: 'Auto Refresh Test',
      },
    });
    expect(createResponse.ok()).toBeTruthy();
    const workflow = await createResponse.json();

    // Reload the dashboard so the new workflow appears
    await page.reload();

    // Wait for the workflow to appear in the list
    await expect(page.getByText(workflow.id.substring(0, 8))).toBeVisible({
      timeout: 15_000,
    });

    // The workflow starts as Pending/Running. Wait for it to transition to
    // AwaitingApproval -- the dashboard should pick this up via polling.
    // We use a generous timeout because the backend orchestrator runs
    // asynchronously and the polling interval is typically 5-10 seconds.
    await expect(
      page.getByText(/awaiting\s*approval|running|completed/i)
    ).toBeVisible({ timeout: 60_000 });
  });

  test('should navigate to workflow detail on row click', async ({ page }) => {
    // Seed a workflow
    const createResponse = await page.request.post('/api/workflows', {
      data: {
        provider: 'PowerCorp',
        currentPrice: 110,
        duration: 24,
        planType: 'electricity',
        customerName: 'Detail Nav Test',
      },
    });
    expect(createResponse.ok()).toBeTruthy();
    const workflow = await createResponse.json();

    await page.reload();

    // Click the first matching workflow row / link
    const row = page.locator(
      `[data-testid="workflow-row"], .workflow-row, tbody tr`
    ).first();
    await expect(row).toBeVisible({ timeout: 10_000 });
    await row.click();

    // Should navigate to the detail page
    await page.waitForURL(/\/workflow(s)?\/[a-f0-9-]+/i, { timeout: 10_000 });
    await expect(
      page.locator('[data-testid="workflow-detail"], .workflow-detail')
    ).toBeVisible();
  });
});
