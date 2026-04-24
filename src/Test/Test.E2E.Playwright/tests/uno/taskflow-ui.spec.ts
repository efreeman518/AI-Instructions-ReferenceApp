import { test } from "@playwright/test";
import {
  clickVisibleText,
  expectBodyToContainAll,
  waitForApp,
} from "../../utils/unoTestUtils";

test.describe("TaskFlow UI — smoke coverage", () => {
  test.beforeEach(async ({ page }) => {
    page.setDefaultTimeout(30_000);
  });

  test("dashboard renders core summary content", async ({ page }) => {
    await waitForApp(page);

    await expectBodyToContainAll(page, [
      "TaskFlow",
      "Recent Activity",
      "View All Tasks",
      "Build dashboard UI",
      "Open",
      "In Progress",
      "Completed",
    ]);
  });

  test("categories page renders its management surface", async ({ page }) => {
    await waitForApp(page);
    await clickVisibleText(page, "Categories", "last");

    await expectBodyToContainAll(page, [
      "Categories",
      "Organize tasks with hierarchical categories",
      "ADD CATEGORY",
      "Category name",
      "Development",
    ]);
  });

  test("tags page renders its management surface", async ({ page }) => {
    await waitForApp(page);
    await clickVisibleText(page, "Tags", "last");

    await expectBodyToContainAll(page, [
      "Tags",
      "Label and color-code your tasks",
      "ADD TAG",
      "Tag name",
      "frontend",
    ]);
  });
});
