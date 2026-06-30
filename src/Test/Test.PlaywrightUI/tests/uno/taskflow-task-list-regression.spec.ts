import { expect, test } from "@playwright/test";
import {
  canvasFingerprint,
  clickAppChrome,
  expectVisualChangeAfter,
  waitForApp,
} from "../../utils/unoTestUtils";

test.describe("TaskFlow Uno WASM task-list smoke", () => {
  test("task navigation paints a different desktop surface", async ({ page }) => {
    await waitForApp(page);

    await expectVisualChangeAfter(page, () => clickAppChrome(page, "tasks"));
  });

  test("mobile viewport boots and paints", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await waitForApp(page);

    await expect(await canvasFingerprint(page)).toHaveLength(64);
  });
});
