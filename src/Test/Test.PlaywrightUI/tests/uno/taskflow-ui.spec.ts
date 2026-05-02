import { test, Page, BrowserContext } from "@playwright/test";
import {
  clickVisibleText,
  expectBodyToContainAll,
  waitForApp,
} from "../../utils/unoTestUtils";

test.describe("TaskFlow UI — smoke coverage", () => {
  test.describe.configure({ mode: "serial" });

  let sharedPage: Page;
  let sharedContext: BrowserContext;

  test.beforeAll(async ({ browser }) => {
    sharedContext = await browser.newContext({ ignoreHTTPSErrors: true });
    sharedPage = await sharedContext.newPage();
    sharedPage.setDefaultTimeout(30_000);
    await waitForApp(sharedPage); // Boot WASM once for all tests in this describe
  });

  test.afterAll(async () => {
    await sharedPage.close();
    await sharedContext.close();
  });

  test("dashboard renders core summary content", async () => {
    await waitForApp(sharedPage);

    await expectBodyToContainAll(sharedPage, [
      "TaskFlow",
      "Recent Activity",
      "Open",
      "In Progress",
      "Completed",
    ]);
  });

  test("categories page renders its management surface", async () => {
    await waitForApp(sharedPage);
    await clickVisibleText(sharedPage, "Categories", "last");

    await expectBodyToContainAll(sharedPage, [
      "Categories",
      "Organize tasks with hierarchical categories",
      "ADD CATEGORY",
      "Category name",
      "Development",
    ], 60_000);
  });

  test("tags page renders its management surface", async () => {
    await waitForApp(sharedPage);
    await clickVisibleText(sharedPage, "Tags", "last");

    await expectBodyToContainAll(sharedPage, [
      "Tags",
      "Label and color-code your tasks",
      "ADD TAG",
      "Tag name",
      "frontend",
    ], 60_000);
  });
});
