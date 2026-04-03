import { test, expect } from '@playwright/test';

/**
 * End-to-end tests for the complete workflow lifecycle:
 * create -> execute -> await approval -> approve -> complete.
 */
test.describe('Workflow Lifecycle', () => {
  test('should create, execute, approve, and complete a workflow', async ({ page }) => {
    // ---------------------------------------------------------------
    // Step 1: Navigate to the dashboard
    // ---------------------------------------------------------------
    await page.goto('/');
    await expect(page).toHaveTitle(/Workflow Automation/i);
    await expect(page.locator('h1, h2').first()).toBeVisible();

    // ---------------------------------------------------------------
    // Step 2: Click "New Workflow" to open the creation form
    // ---------------------------------------------------------------
    const newWorkflowButton = page.getByRole('button', { name: /new workflow/i });
    await expect(newWorkflowButton).toBeVisible();
    await newWorkflowButton.click();

    // Wait for the form to appear
    await expect(page.locator('form')).toBeVisible();

    // ---------------------------------------------------------------
    // Step 3: Fill in the workflow creation form
    // ---------------------------------------------------------------
    await page.getByLabel(/provider/i).fill('TestEnergy');
    await page.getByLabel(/price/i).fill('95');
    await page.getByLabel(/duration/i).fill('12');

    // Select the plan type -- handle both <select> and <input> variants
    const planTypeField = page.getByLabel(/type/i);
    const tagName = await planTypeField.evaluate((el) => el.tagName.toLowerCase());
    if (tagName === 'select') {
      await planTypeField.selectOption('electricity');
    } else {
      await planTypeField.fill('electricity');
    }

    await page.getByLabel(/customer/i).fill('John Doe');

    // ---------------------------------------------------------------
    // Step 4: Submit the form
    // ---------------------------------------------------------------
    const submitButton = page.getByRole('button', { name: /submit|create|start/i });
    await expect(submitButton).toBeEnabled();
    await submitButton.click();

    // ---------------------------------------------------------------
    // Step 5: Verify redirect to the workflow detail page
    // ---------------------------------------------------------------
    await page.waitForURL(/\/workflow(s)?\/[a-f0-9-]+/i, { timeout: 10_000 });
    await expect(page.locator('[data-testid="workflow-detail"], .workflow-detail')).toBeVisible();

    // ---------------------------------------------------------------
    // Step 6: Wait for workflow steps to appear
    // ---------------------------------------------------------------
    const stepsContainer = page.locator(
      '[data-testid="workflow-steps"], .workflow-steps, .step-list'
    );
    await expect(stepsContainer).toBeVisible({ timeout: 15_000 });

    // Verify the expected step names are present
    await expect(page.getByText(/Scrape Provider Offers/i)).toBeVisible();
    await expect(page.getByText(/AI Contract Analysis/i)).toBeVisible();
    await expect(page.getByText(/Human Approval/i)).toBeVisible();
    await expect(page.getByText(/Finalize Recommendation/i)).toBeVisible();

    // ---------------------------------------------------------------
    // Step 7: Wait for AwaitingApproval status and check for approval buttons
    // ---------------------------------------------------------------
    const approveButton = page.getByRole('button', { name: /approve/i });
    await expect(approveButton).toBeVisible({ timeout: 30_000 });

    // A reject button should also be available
    const rejectButton = page.getByRole('button', { name: /reject/i });
    await expect(rejectButton).toBeVisible();

    // ---------------------------------------------------------------
    // Step 8: Approve the workflow
    // ---------------------------------------------------------------
    await approveButton.click();

    // ---------------------------------------------------------------
    // Step 9: Verify the workflow reaches Completed status
    // ---------------------------------------------------------------
    const completedBadge = page.locator(
      '[data-testid="workflow-status"], .workflow-status, .status-badge'
    );
    await expect(completedBadge).toContainText(/completed/i, { timeout: 30_000 });

    // Verify the result section is displayed with a recommendation
    await expect(
      page.locator('[data-testid="workflow-result"], .workflow-result')
    ).toBeVisible();
  });

  test('should reject a workflow when reject is clicked', async ({ page }) => {
    // Create a workflow via the API for speed
    const response = await page.request.post('/api/workflows', {
      data: {
        provider: 'TestEnergy',
        currentPrice: 95,
        duration: 12,
        planType: 'electricity',
        customerName: 'Jane Doe',
      },
    });
    expect(response.ok()).toBeTruthy();
    const workflow = await response.json();

    // Navigate to the workflow detail page
    await page.goto(`/workflows/${workflow.id}`);

    // Wait for AwaitingApproval status
    const rejectButton = page.getByRole('button', { name: /reject/i });
    await expect(rejectButton).toBeVisible({ timeout: 30_000 });

    // Reject the workflow
    await rejectButton.click();

    // Verify the workflow reaches Rejected status
    const statusBadge = page.locator(
      '[data-testid="workflow-status"], .workflow-status, .status-badge'
    );
    await expect(statusBadge).toContainText(/rejected/i, { timeout: 15_000 });
  });
});
