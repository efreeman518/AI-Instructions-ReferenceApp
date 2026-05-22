import { expect, type Page } from "@playwright/test";

export async function waitForReactApp(page: Page) {
  await page.goto("/tasks", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { exact: true, name: "Tasks" })).toBeVisible({ timeout: 15_000 });
  await expect(page.getByRole("navigation")).toContainText("TaskFlow");
}

export async function navigateToTaskList(page: Page) {
  await page.goto("/tasks", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { exact: true, name: "Tasks" })).toBeVisible({ timeout: 10_000 });
}

export async function navigateToNewTask(page: Page) {
  await page.getByRole("link", { name: /new task/i }).or(page.getByRole("button", { name: /new task/i })).first().click();
  await expect(page.getByRole("heading", { name: /new task/i })).toBeVisible({ timeout: 10_000 });
}

export async function searchForTask(page: Page, term: string) {
  await page.getByLabel("Search").fill(term);
  await page.getByRole("button", { name: /^search$/i }).click();
}

export async function expectTaskInTable(page: Page, title: string, timeout = 15_000) {
  await expect(page.locator("table")).toContainText(title, { timeout });
}

export async function expectTaskNotInTable(page: Page, title: string, timeout = 15_000) {
  await expect(page.locator("body")).not.toContainText(title, { timeout });
}

export async function openTaskFromList(page: Page, title: string) {
  const row = page.locator("tbody tr", { hasText: title }).first();
  await expect(row).toBeVisible({ timeout: 10_000 });
  await row.getByRole("link", { name: /edit task/i }).click();
  await expect(page.getByRole("heading", { name: /edit task/i })).toBeVisible({ timeout: 10_000 });
}

export async function deleteTaskFromList(page: Page, title: string) {
  const row = page.locator("tbody tr", { hasText: title }).first();
  await expect(row).toBeVisible({ timeout: 10_000 });
  await row.getByRole("button", { name: /delete task/i }).click();
  await confirmDialog(page, "Delete");
}

export async function fillTaskForm(page: Page, values: {
  title?: string;
  description?: string;
  priority?: string;
  status?: string;
}) {
  if (values.title !== undefined) await page.getByLabel("Title").fill(values.title);
  if (values.description !== undefined) await page.getByLabel("Description").fill(values.description);
  if (values.priority !== undefined) await selectMuiOption(page, "Priority", values.priority);
  if (values.status !== undefined) await selectMuiOption(page, "Status", values.status);
}

export async function addChecklistItem(page: Page, title: string) {
  await page.getByRole("button", { name: /checklist/i }).click();
  const input = page.locator('input[placeholder="Add item"]:visible');
  await input.fill(title);
  await input.press("Enter");
  await expect(page.getByText(title)).toBeVisible({ timeout: 5_000 });
}

export async function addComment(page: Page, body: string) {
  await page.getByRole("button", { name: /comments/i }).click();
  await page.locator('textarea[placeholder="Add a comment"]:visible').fill(body);
  await page.getByRole("button", { name: /^add$/i }).last().click();
  await expect(page.getByText(body)).toBeVisible({ timeout: 5_000 });
}

export async function saveTask(page: Page) {
  await page.getByRole("button", { name: /^save$/i }).click();
}

export async function confirmDialog(page: Page, confirmLabel: string) {
  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible({ timeout: 10_000 });
  await dialog.getByRole("button", { name: confirmLabel }).click();
}

export async function selectMuiOption(page: Page, label: string, option: string) {
  await page.getByLabel(label).click();
  await page.getByRole("option", { name: option }).click();
}

export async function expectNoPageErrors(errors: Error[]) {
  expect(errors.map((error) => error.message)).toEqual([]);
}

export function uniqueTitle(prefix: string) {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
}
