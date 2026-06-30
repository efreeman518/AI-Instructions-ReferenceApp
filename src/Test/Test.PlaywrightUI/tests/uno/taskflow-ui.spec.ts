import { expect, test } from "@playwright/test";
import {
  canvasFingerprint,
  clickAppChrome,
  expectVisualChangeAfter,
  waitForApp,
} from "../../utils/unoTestUtils";

test.describe("TaskFlow Uno WASM canvas smoke", () => {
  test("dashboard paints a stable canvas", async ({ page }) => {
    await waitForApp(page);

    await expect(await canvasFingerprint(page)).toHaveLength(64);
  });

  test("desktop chrome navigation changes the painted surface", async ({ page }) => {
    await waitForApp(page);

    await expectVisualChangeAfter(page, () => clickAppChrome(page, "tasks"));
    await expectVisualChangeAfter(page, () => clickAppChrome(page, "categories"));
    await expectVisualChangeAfter(page, () => clickAppChrome(page, "tags"));
  });
});
