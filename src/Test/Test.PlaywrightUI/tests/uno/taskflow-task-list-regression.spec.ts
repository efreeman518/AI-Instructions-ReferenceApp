import { test, Page, BrowserContext } from "@playwright/test";
import {
  expectBodyToContainAll,
  navigateToTaskList,
} from "../../utils/unoTestUtils";

test.describe("TaskFlow UI — task list regression coverage", () => {
  test.describe.configure({ mode: "serial" });

  let sharedPage: Page;
  let sharedContext: BrowserContext;

  test.beforeAll(async ({ browser }) => {
    sharedContext = await browser.newContext({ ignoreHTTPSErrors: true });
    sharedPage = await sharedContext.newPage();
    sharedPage.setDefaultTimeout(30_000);
    await navigateToTaskList(sharedPage); // Boot WASM once, land on task list
  });

  test.afterAll(async () => {
    await sharedPage.close();
    await sharedContext.close();
  });

  test("desktop task list renders the richer pager and wide layout", async () => {
    await navigateToTaskList(sharedPage);

    await expectBodyToContainAll(sharedPage, [
      "Tasks",
      "Manage and track all your tasks",
      "Choose 10, 20, or 50 above and press Search to update the list size.",
      "Task",
      "Status",
      "Category",
      "Priority",
      "Start",
      "Due",
    ]);
  });

  test("task detail still opens from the list after the layout changes", async () => {
    await navigateToTaskList(sharedPage);
    // Click first E2E-prefixed task title — these are always present from prior test runs
    for (let attempt = 0; attempt < 20; attempt++) {
      const clicked = await sharedPage.evaluate(() => {
        for (const p of Array.from(document.querySelectorAll("p"))) {
          const txt = (p.textContent ?? "").trim();
          if (!txt.startsWith("E2E-")) continue;
          const r = p.getBoundingClientRect();
          if (r.width > 0 && r.height > 0 && r.y > 0 && r.x > 0) {
            return { x: r.x + r.width / 2, y: r.y + r.height / 2 };
          }
        }
        return null;
      });
      if (clicked) { await sharedPage.mouse.click(clicked.x, clicked.y); break; }
      await sharedPage.waitForTimeout(500);
    }

    await expectBodyToContainAll(sharedPage, [
      "Edit Task",
      "Back to Tasks",
      "Checklist",
      "Comments",
      "Attachments",
    ], 60_000);
  });
});

test.describe("TaskFlow UI — mobile task list", () => {
  let sharedPage: Page;
  let sharedContext: BrowserContext;

  test.beforeAll(async ({ browser }) => {
    sharedContext = await browser.newContext({
      ignoreHTTPSErrors: true,
      viewport: { width: 390, height: 844 },
    });
    sharedPage = await sharedContext.newPage();
    sharedPage.setDefaultTimeout(30_000);
    await navigateToTaskList(sharedPage); // Boot WASM once with mobile viewport
  });

  test.afterAll(async () => {
    await sharedPage.close();
    await sharedContext.close();
  });

  test("mobile task list renders stacked metadata labels", async () => {
    await navigateToTaskList(sharedPage);

    await expectBodyToContainAll(sharedPage, [
      "Find the task you need, then jump between pages without losing context.",
      "Category",
      "Priority",
      "Start",
      "Due",
    ]);
  });
});
