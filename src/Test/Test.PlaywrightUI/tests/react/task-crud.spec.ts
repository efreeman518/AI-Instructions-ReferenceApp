import { expect, test } from "@playwright/test";
import {
  addChecklistItem,
  addComment,
  deleteTaskFromList,
  expectNoPageErrors,
  expectTaskInTable,
  expectTaskNotInTable,
  fillTaskForm,
  navigateToNewTask,
  navigateToTaskList,
  openTaskFromList,
  saveTask,
  searchForTask,
  uniqueTitle,
  waitForReactApp,
} from "../../utils/reactTestUtils";

/**
 * Full Task CRUD lifecycle exercised through the React + TypeScript UI.
 *
 * Prerequisites:
 *   1. React app running on http://localhost:5178, preferably via Aspire AppHost
 *   2. Gateway/API running with the normal TaskFlow Aspire stack
 *   3. `npx playwright install chromium`
 *
 * Run:  npm run test:react
 */

test.describe("TaskFlow React - Task CRUD lifecycle", () => {
  test.describe.configure({ mode: "serial" });

  let taskTitle: string;
  let updatedTitle: string;
  let checklistTitle: string;
  let commentBody: string;
  let pageErrors: Error[];

  test.beforeAll(() => {
    taskTitle = uniqueTitle("E2E-React-Create");
    updatedTitle = uniqueTitle("E2E-React-Updated");
    checklistTitle = uniqueTitle("E2E-React-Checklist");
    commentBody = uniqueTitle("E2E-React-Comment");
  });

  test.beforeEach(async ({ page }) => {
    pageErrors = [];
    page.on("pageerror", (error) => pageErrors.push(error));
    page.setDefaultTimeout(30_000);
  });

  test.afterEach(async () => {
    await expectNoPageErrors(pageErrors);
  });

  /** Verifies the Playwright scenario for 1. create a new task. */
  test("1. create a new task", async ({ page }) => {
    await waitForReactApp(page);
    await navigateToNewTask(page);

    await fillTaskForm(page, {
      description: "Automated Playwright E2E test task (React)",
      title: taskTitle,
    });
    await addChecklistItem(page, checklistTitle);
    await addComment(page, commentBody);

    await saveTask(page);
    await expect(page.getByRole("heading", { name: /edit task/i })).toBeVisible({ timeout: 15_000 });

    await navigateToTaskList(page);
    await searchForTask(page, taskTitle);
    await expectTaskInTable(page, taskTitle, 20_000);
  });

  /** Verifies the Playwright scenario for 2. read the created task and child details. */
  test("2. read the created task and child details", async ({ page }) => {
    await navigateToTaskList(page);
    await searchForTask(page, taskTitle);
    await expectTaskInTable(page, taskTitle);

    const row = page.locator("tbody tr", { hasText: taskTitle }).first();
    await expect(row).toContainText("Open");
    await expect(row).toContainText("Medium");

    await openTaskFromList(page, taskTitle);
    await expect(page.getByText(checklistTitle)).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText(commentBody)).toBeVisible({ timeout: 10_000 });
  });

  /** Verifies the Playwright scenario for 3. update the task title and priority. */
  test("3. update the task title and priority", async ({ page }) => {
    await navigateToTaskList(page);
    await searchForTask(page, taskTitle);
    await openTaskFromList(page, taskTitle);

    await fillTaskForm(page, {
      priority: "High",
      title: updatedTitle,
    });
    await saveTask(page);
    await expect(page.getByText("Task saved.")).toBeVisible({ timeout: 15_000 });

    await navigateToTaskList(page);
    await searchForTask(page, updatedTitle);
    await expectTaskInTable(page, updatedTitle);
    await expectTaskNotInTable(page, taskTitle);
    await expect(page.locator("tbody tr", { hasText: updatedTitle }).first()).toContainText("High");
  });

  /** Verifies the Playwright scenario for 4. delete the task. */
  test("4. delete the task", async ({ page }) => {
    await navigateToTaskList(page);
    await searchForTask(page, updatedTitle);
    await deleteTaskFromList(page, updatedTitle);
    await expect(page.getByText("Task deleted.")).toBeVisible({ timeout: 15_000 });
    await expectTaskNotInTable(page, updatedTitle);
  });
});
