import { test, expect } from "@playwright/test";
import {
  clickDeleteOnRow,
  clickEditOnRow,
  clickSave,
  confirmDeleteDialog,
  expectTaskInTable,
  expectTaskNotInTable,
  fillTextField,
  navigateToNewTask,
  navigateToTaskList,
  searchForTask,
  selectOption,
  uniqueTitle,
  waitForApp,
} from "../../utils/blazorTestUtils";

/**
 * Full Task CRUD lifecycle exercised through the Blazor MudBlazor UI.
 *
 * Prerequisites:
 *   1. Blazor app running on https://localhost:7201
 *   2. API running with seed data (Aspire AppHost or standalone)
 *   3. `npx playwright install --with-deps chromium`
 *
 * Run:  npm run test:blazor
 */

test.describe("TaskFlow Blazor — Task CRUD lifecycle", () => {
  // Tests must run in order — each depends on the previous test's side effects
  test.describe.configure({ mode: "serial" });
  let taskTitle: string;
  let updatedTitle: string;

  test.beforeAll(() => {
    taskTitle = uniqueTitle("E2E-Create");
    updatedTitle = uniqueTitle("E2E-Updated");
  });

  test.beforeEach(async ({ page }) => {
    page.setDefaultTimeout(30_000);
  });

  // ── CREATE ──────────────────────────────────────────────────────────
  test("1. create a new task", async ({ page }) => {
    await waitForApp(page);
    await navigateToNewTask(page);

    await fillTextField(page, "Title", taskTitle);
    await fillTextField(page, "Description", "Automated Playwright E2E test task");
    // Status=Open and Priority=Medium are defaults — no need to select them

    await clickSave(page);

    // Should redirect back to list or show success
    await navigateToTaskList(page);
    await searchForTask(page, taskTitle);
    await expectTaskInTable(page, taskTitle);
  });

  // ── READ ────────────────────────────────────────────────────────────
  test("2. read the created task in the list", async ({ page }) => {
    await navigateToTaskList(page);
    await searchForTask(page, taskTitle);
    await expectTaskInTable(page, taskTitle);

    // Verify row contains expected metadata
    const row = page.locator(`.mud-table-body tr:has-text("${taskTitle}")`);
    await expect(row).toContainText("Open");
    await expect(row).toContainText("Medium");
  });

  // ── UPDATE ──────────────────────────────────────────────────────────
  test("3. update the task title and priority", async ({ page }) => {
    await navigateToTaskList(page);
    await searchForTask(page, taskTitle);
    await clickEditOnRow(page, taskTitle);

    // Clear and re-fill the title
    await fillTextField(page, "Title", updatedTitle);
    await selectOption(page, "Priority", "High");

    await clickSave(page);

    await navigateToTaskList(page);
    await searchForTask(page, updatedTitle);
    await expectTaskInTable(page, updatedTitle);
    await expectTaskNotInTable(page, taskTitle);

    const row = page.locator(`.mud-table-body tr:has-text("${updatedTitle}")`);
    await expect(row).toContainText("High");
  });

  // ── DELETE ──────────────────────────────────────────────────────────
  test("4. delete the task", async ({ page }) => {
    await navigateToTaskList(page);
    await searchForTask(page, updatedTitle);
    await clickDeleteOnRow(page, updatedTitle);
    await confirmDeleteDialog(page);

    // Task should vanish from the table
    await expectTaskNotInTable(page, updatedTitle);
  });
});
