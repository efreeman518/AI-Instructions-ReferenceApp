import { expect, test, Page, BrowserContext } from "@playwright/test";
import {
  clickVisibleText,
  expectBodyToContain,
  getBodyText,
  navigateToTaskList,
  uniqueTitle,
  waitForApp,
} from "../../utils/unoTestUtils";

/**
 * Full Task CRUD lifecycle exercised through the Uno WASM UI.
 *
 * Prerequisites:
 *   1. Uno WASM app running on https://localhost:7069
 *   2. API running with seed data (Aspire AppHost or standalone)
 *   3. `npx playwright install --with-deps chromium`
 *
 * Run:  npm run test:uno
 */

// ---------------------------------------------------------------------------
// Uno-specific helpers — all use coordinate-based interaction because
// Uno WASM renders elements invisible to Playwright's standard click checks.
// ---------------------------------------------------------------------------

/** Get the bounding box of a Uno WASM element via evaluate (bypasses Playwright visibility checks). */
async function getVisibleUnoElementBox(locator: import("@playwright/test").Locator) {
  // Uno WASM keeps off-screen copies of old pages. Iterate to find the on-screen element.
  const count = await locator.count();
  for (let i = 0; i < count; i++) {
    const box = await locator.nth(i).evaluate((el) => {
      const r = el.getBoundingClientRect();
      return { x: r.x, y: r.y, width: r.width, height: r.height };
    });
    if (box.x >= 0 && box.y >= 0 && box.width > 0 && box.height > 0) {
      return box;
    }
  }
  throw new Error("No on-screen element found for locator");
}

/** Type into a Uno WASM TextBox identified by text it contains (Header or Placeholder). */
async function fillUnoTextBox(page: Page, containsText: string, value: string) {
  const containers = page.locator('[xamltype="Microsoft.UI.Xaml.Controls.TextBox"]').filter({ hasText: containsText });
  const box = await getVisibleUnoElementBox(containers);
  await page.mouse.click(box.x + box.width / 2, box.y + box.height / 2);
  await page.waitForTimeout(200);
  await page.keyboard.press("Control+a");
  await page.keyboard.type(value);
  // Click the page heading area to blur / commit value (avoid clickable elements)
  await page.mouse.click(400, 100);
  await page.waitForTimeout(300);
}

/** Select an item from a Uno WASM ComboBox identified by its placeholder label. */
async function selectUnoComboBox(page: Page, placeholderLabel: string, option: string) {
  const combos = page.locator('[xamltype="Microsoft.UI.Xaml.Controls.ComboBox"]').filter({ hasText: placeholderLabel });
  const comboBox = await getVisibleUnoElementBox(combos);
  await page.mouse.click(comboBox.x + comboBox.width / 2, comboBox.y + comboBox.height / 2);
  await page.waitForTimeout(500);

  // Find the dropdown option by text and click at its coordinates
  const optionCoords = await page.evaluate((optText) => {
    const allP = document.querySelectorAll("p");
    for (const p of allP) {
      if (p.textContent?.trim() === optText) {
        const r = p.getBoundingClientRect();
        if (r.width > 0 && r.height > 0 && r.y > 0) {
          return { x: r.x + r.width / 2, y: r.y + r.height / 2 };
        }
      }
    }
    return null;
  }, option);
  if (!optionCoords) throw new Error(`ComboBox option "${option}" not found`);
  await page.mouse.click(optionCoords.x, optionCoords.y);
  await page.waitForTimeout(300);
}

/** Click a Uno WASM element by finding its on-screen coordinates via evaluate. */
async function clickOnScreenElement(page: Page, xamltype: string, textMatch: RegExp) {
  for (let attempt = 0; attempt < 20; attempt++) {
    const coords = await page.evaluate(({ selector, pattern }) => {
      const elements = document.querySelectorAll(`[xamltype="${selector}"]`);
      const re = new RegExp(pattern);
      for (const el of elements) {
        const txt = (el.textContent ?? "").replace(/\s+/g, " ").trim();
        if (re.test(txt)) {
          const r = el.getBoundingClientRect();
          if (r.width > 0 && r.height > 0 && r.x >= 0 && r.y >= 0) {
            return { x: r.x + r.width / 2, y: r.y + r.height / 2 };
          }
        }
      }
      return null;
    }, { selector: xamltype, pattern: textMatch.source });
    if (coords) {
      await page.mouse.click(coords.x, coords.y);
      return;
    }
    await page.waitForTimeout(500);
  }
  throw new Error(`Could not find on-screen ${xamltype} matching ${textMatch}`);
}

/** Search for a task in the task list using the search TextBox and button. */
async function searchForTask(page: Page, searchTerm: string) {
  const searchBoxes = page.locator('[xamltype="Microsoft.UI.Xaml.Controls.TextBox"]').filter({ hasText: "Search tasks" });
  const box = await getVisibleUnoElementBox(searchBoxes);
  await page.mouse.click(box.x + box.width / 2, box.y + box.height / 2);
  await page.waitForTimeout(200);
  await page.keyboard.press("Control+a");
  await page.keyboard.type(searchTerm);
  await page.mouse.click(400, 100);
  await page.waitForTimeout(300);

  // Click the "Search" button directly via coordinate-based approach
  await clickOnScreenElement(page, "Microsoft.UI.Xaml.Controls.Button", /Search/);
  await page.waitForTimeout(2_000);
}

/** Navigate to the task list via sidebar (assumes app is already running). */
async function goToTaskList(page: Page) {
  await clickVisibleText(page, "Tasks", "last");
  await expectBodyToContain(page, "Manage and track all your tasks");
}

/** Navigate to the new task form via "Add Task" sidebar button. */
async function navigateToNewTask(page: Page) {
  await navigateToTaskList(page);
  for (let attempt = 0; attempt < 3; attempt++) {
    await clickVisibleText(page, "Add Task", "last");
    try {
      await expectBodyToContain(page, "Fill in the details to create a new task", 5_000);
      return;
    }
    catch
    {
      await page.waitForTimeout(400);
    }
  }
  await clickVisibleText(page, "Add Task", "last");
  await expectBodyToContain(page, "Fill in the details to create a new task", 10_000);
}

/** Click a task row by its title to open the edit form (coordinate-based). */
async function openTaskByTitle(page: Page, title: string) {
  // Find the element with the task title text and click at its coordinates
  const coords = await page.evaluate((t) => {
    const allP = document.querySelectorAll("p");
    for (const p of allP) {
      if (p.textContent?.trim() === t) {
        const r = p.getBoundingClientRect();
        if (r.width > 0 && r.height > 0 && r.y > 0) {
          return { x: r.x + r.width / 2, y: r.y + r.height / 2 };
        }
      }
    }
    return null;
  }, title);
  if (!coords) throw new Error(`Task "${title}" not found in task list`);
  await page.mouse.click(coords.x, coords.y);
  await expectBodyToContain(page, "Edit Task", 10_000);
}

// ---------------------------------------------------------------------------
// Tests — share a single page/context to avoid re-booting WASM each test.
// Uno WASM's async default-route resolution can override manual sidebar
// navigation on a freshly loaded page.  Sharing the page means the route
// resolves once in test 1 and stays stable for tests 2-4.
// ---------------------------------------------------------------------------

test.describe("TaskFlow Uno WASM — Task CRUD lifecycle", () => {
  test.describe.configure({ mode: "serial" });

  let sharedPage: Page;
  let sharedContext: BrowserContext;
  let taskTitle: string;
  let updatedTitle: string;
  let checklistTitle: string;
  let commentBody: string;

  test.beforeAll(async ({ browser }) => {
    sharedContext = await browser.newContext({ ignoreHTTPSErrors: true });
    sharedPage = await sharedContext.newPage();
    sharedPage.setDefaultTimeout(30_000);
    taskTitle = uniqueTitle("E2E-Uno-Create");
    updatedTitle = uniqueTitle("E2E-Uno-Updated");
    checklistTitle = uniqueTitle("E2E-Uno-Checklist");
    commentBody = uniqueTitle("E2E-Uno-Comment");
  });

  test.afterAll(async () => {
    await sharedPage.close();
    await sharedContext.close();
  });

  // ── CREATE ──────────────────────────────────────────────────────────
  test("1. create a new task", async () => {
    await navigateToNewTask(sharedPage);
    await sharedPage.waitForTimeout(1_000);

    await fillUnoTextBox(sharedPage, "Title", taskTitle);
    await fillUnoTextBox(sharedPage, "Description", "Automated Playwright E2E test task (Uno)");
    await selectUnoComboBox(sharedPage, "Select priority", "Medium");

    await fillUnoTextBox(sharedPage, "Add checklist item...", checklistTitle);
    await clickOnScreenElement(sharedPage, "Microsoft.UI.Xaml.Controls.Button", /^Add$/);

    // Comments controls are near the lower portion of the form; scroll first
    // so coordinate-based clicks reliably target the live textbox/button.
    await sharedPage.mouse.wheel(0, 700);
    await sharedPage.waitForTimeout(300);
    await fillUnoTextBox(sharedPage, "Write a comment...", commentBody);
    await clickOnScreenElement(sharedPage, "Microsoft.UI.Xaml.Controls.Button", /^Post$/);
    // Wait for the comment to appear in the buffered list before saving
    await expectBodyToContain(sharedPage, commentBody, 5_000);

    await clickVisibleText(sharedPage, "Save Task");

    // Save auto-navigates to task list — wait for it, then search for the task
    await expectBodyToContain(sharedPage, "Manage and track all your tasks", 15_000);
    await searchForTask(sharedPage, taskTitle);
    await expectBodyToContain(sharedPage, taskTitle, 10_000);
  });

  // ── READ ────────────────────────────────────────────────────────────
  test("2. read the created task in the list", async () => {
    // Navigate via sidebar (no fresh boot — route is already stable)
    await goToTaskList(sharedPage);
    await searchForTask(sharedPage, taskTitle);
    const body = await getBodyText(sharedPage);
    expect(body).toContain(taskTitle);
    expect(body).toContain("Medium");

    await openTaskByTitle(sharedPage, taskTitle);
    await expectBodyToContain(sharedPage, checklistTitle, 10_000);
    await expectBodyToContain(sharedPage, commentBody, 10_000);
  });

  // ── UPDATE ──────────────────────────────────────────────────────────
  test("3. update the task title and priority", async () => {
    await goToTaskList(sharedPage);
    await searchForTask(sharedPage, taskTitle);
    await openTaskByTitle(sharedPage, taskTitle);

    await fillUnoTextBox(sharedPage, "Title", updatedTitle);
    await selectUnoComboBox(sharedPage, "Select priority", "High");

    await clickVisibleText(sharedPage, "Update Task");
    await sharedPage.waitForTimeout(1_000);

    // Update navigates back — verify we reach the task list
    await goToTaskList(sharedPage);
    await searchForTask(sharedPage, updatedTitle);
    await expectBodyToContain(sharedPage, updatedTitle, 10_000);

    // Old title should be gone
    const body = await getBodyText(sharedPage);
    expect(body).not.toContain(taskTitle);
  });

  // ── DELETE ──────────────────────────────────────────────────────────
  test("4. delete the task", async () => {
    await goToTaskList(sharedPage);
    await searchForTask(sharedPage, updatedTitle);
    await openTaskByTitle(sharedPage, updatedTitle);

    await clickVisibleText(sharedPage, "Delete");
    await sharedPage.waitForTimeout(1_000);

    // Should return to list; search for the deleted task — it should be gone
    await goToTaskList(sharedPage);
    await searchForTask(sharedPage, updatedTitle);
    const body = await getBodyText(sharedPage);
    expect(body).not.toContain(updatedTitle);
  });
});
