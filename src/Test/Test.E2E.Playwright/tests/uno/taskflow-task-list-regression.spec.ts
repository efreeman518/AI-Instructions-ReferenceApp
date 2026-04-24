import { expect, test } from "@playwright/test";
import {
  clickVisibleText,
  expectBodyToContainAll,
  navigateToTaskList,
} from "../../utils/unoTestUtils";

test.describe("TaskFlow UI — task list regression coverage", () => {
  test.beforeEach(async ({ page }) => {
    page.setDefaultTimeout(30_000);
  });

  test("desktop task list renders the richer pager and wide layout", async ({ page }) => {
    await navigateToTaskList(page);

    await expectBodyToContainAll(page, [
      "Tasks",
      "Manage and track all your tasks",
      "Showing 1 to 10 of 14 tasks",
      "Page 1 of 2",
      "Choose 10, 20, or 50 above and press Search to update the list size.",
      "First Previous 1 2 Next Last",
      "Task",
      "Status",
      "Category",
      "Priority",
      "Start",
      "Due",
    ]);
  });

  test("task detail still opens from the list after the layout changes", async ({ page }) => {
    await navigateToTaskList(page);
    await clickVisibleText(page, "Build dashboard UI");

    await expectBodyToContainAll(page, [
      "Edit Task",
      "Back to Tasks",
      "Checklist",
      "Comments",
      "Attachments",
      "Design mockups",
      "Looking good so far!",
      "design.pdf",
    ]);
  });
});

test.describe("TaskFlow UI — mobile task list", () => {
  test.use({ viewport: { width: 390, height: 844 } });

  test("mobile task list renders stacked metadata labels", async ({ page }) => {
    await navigateToTaskList(page);

    await expectBodyToContainAll(page, [
      "Find the task you need, then jump between pages without losing context.",
      "Page 1 of 2",
      "Category",
      "Priority",
      "Start",
      "Due",
      "First Previous 1 2 Next Last",
    ]);
  });
});
