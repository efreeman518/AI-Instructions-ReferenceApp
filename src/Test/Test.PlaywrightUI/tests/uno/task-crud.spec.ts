import { test } from "@playwright/test";
import {
  clickAppChrome,
  expectVisualChangeAfter,
  waitForApp,
} from "../../utils/unoTestUtils";

test.describe("TaskFlow Uno WASM new-task smoke", () => {
  test("new task chrome action changes the painted surface", async ({ page }) => {
    await waitForApp(page);

    await expectVisualChangeAfter(page, () => clickAppChrome(page, "addTask"));
  });
});
