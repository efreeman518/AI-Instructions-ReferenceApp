import { expect, Page } from "@playwright/test";

/**
 * MudBlazor renders standard HTML. Key selectors:
 *   - MudTable rows: .mud-table-body tr
 *   - MudButton:     button.mud-button-root
 *   - MudTextField:  .mud-input-control input
 *   - MudSelect:     .mud-select (click to open popover, then .mud-popover .mud-list-item)
 *   - Dialog:        .mud-dialog
 */

// ---------------------------------------------------------------------------
// Navigation
// ---------------------------------------------------------------------------

export async function waitForApp(page: Page) {
  await page.goto("/tasks", { waitUntil: "networkidle" });
  await expect(page.getByRole("heading", { name: "Tasks" })).toBeVisible({ timeout: 15_000 });
}

export async function navigateToNewTask(page: Page) {
  await page.getByRole("button", { name: /new task/i }).click();
  await expect(page.getByRole("heading", { name: /new task/i })).toBeVisible({ timeout: 10_000 });
}

export async function navigateToTaskList(page: Page) {
  await page.goto("/tasks", { waitUntil: "networkidle" });
  await expect(page.locator(".mud-table")).toBeVisible({ timeout: 10_000 });
}

export async function searchForTask(page: Page, term: string) {
  const searchInput = page.getByPlaceholder(/title or description/i);
  await searchInput.click();
  await searchInput.fill(term);
  await page.getByRole("button", { name: /^search$/i }).click();
  await page.waitForLoadState("networkidle");
}

// ---------------------------------------------------------------------------
// Form helpers
// ---------------------------------------------------------------------------

export async function fillTextField(page: Page, label: string, value: string) {
  const field = page.locator(`.mud-input-control:has(label:has-text("${label}")) input, .mud-input-control:has(label:has-text("${label}")) textarea`);
  await field.first().click();
  await field.first().fill(value);
}

export async function selectOption(page: Page, label: string, option: string) {
  // MudSelect renders a hidden <input> inside .mud-input-control.
  // Click the visible .mud-input wrapper to open the dropdown.
  const wrapper = page.locator(`.mud-input-control:has(label:has-text("${label}"))`);
  await wrapper.locator(".mud-input").first().click();
  // Wait for popover and click the option
  const popover = page.locator(".mud-popover-open");
  await popover.locator(`.mud-list-item:has-text("${option}")`).click();
}

export async function clickSave(page: Page) {
  await page.getByRole("button", { name: /save/i }).click();
}

// ---------------------------------------------------------------------------
// Table helpers
// ---------------------------------------------------------------------------

export async function getTableRowByText(page: Page, text: string) {
  return page.locator(`.mud-table-body tr:has-text("${text}")`);
}

export async function expectTaskInTable(page: Page, title: string, timeout = 10_000) {
  await expect(page.locator(`.mud-table-body`)).toContainText(title, { timeout });
}

export async function expectTaskNotInTable(page: Page, title: string, timeout = 10_000) {
  await expect(page.locator(`.mud-table-body`)).not.toContainText(title, { timeout });
}

export async function clickEditOnRow(page: Page, title: string) {
  const row = page.locator(`.mud-table-body tr:has-text("${title}")`);
  // Actions cell has 3 icon buttons: checkmark (0), edit (1), delete (2)
  await row.locator("td").last().locator("button").nth(1).click();
  await expect(page.getByRole("heading", { name: /edit task/i })).toBeVisible({ timeout: 10_000 });
}

export async function clickDeleteOnRow(page: Page, title: string) {
  const row = page.locator(`.mud-table-body tr:has-text("${title}")`);
  // Actions cell has 3 icon buttons: checkmark (0), edit (1), delete (2)
  await row.locator("td").last().locator("button").nth(2).click();
}

// ---------------------------------------------------------------------------
// Dialog helpers
// ---------------------------------------------------------------------------

export async function confirmDeleteDialog(page: Page) {
  const dialog = page.locator(".mud-overlay-dialog, .mud-dialog-container").first();
  await expect(dialog).toBeVisible({ timeout: 5_000 });
  await dialog.getByRole("button", { name: /delete/i }).click();
}

// ---------------------------------------------------------------------------
// Snackbar / feedback
// ---------------------------------------------------------------------------

export async function expectSnackbar(page: Page, textFragment: string, timeout = 5_000) {
  await expect(page.locator(".mud-snackbar")).toContainText(textFragment, { timeout });
}

// ---------------------------------------------------------------------------
// Unique test data
// ---------------------------------------------------------------------------

export function uniqueTitle(prefix: string) {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
}
