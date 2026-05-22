import { expect, test } from "@playwright/test";
import { expectNoPageErrors, waitForReactApp } from "../../utils/reactTestUtils";

test.describe("TaskFlow React - shell behavior", () => {
  test("persists theme choice and exposes primary navigation", async ({ page }) => {
    const pageErrors: Error[] = [];
    page.on("pageerror", (error) => pageErrors.push(error));
    await page.goto("/", { waitUntil: "domcontentloaded" });
    await page.evaluate(() => localStorage.removeItem("taskflow.react.theme"));

    await waitForReactApp(page);

    await expect(page.getByRole("navigation")).toContainText("Dashboard");
    await expect(page.getByRole("navigation")).toContainText("Tasks");
    await expect(page.getByRole("navigation")).toContainText("Categories");
    await expect(page.getByRole("navigation")).toContainText("Tags");
    await expect(page.getByRole("navigation")).toContainText("Settings");

    await page.getByRole("link", { name: /settings/i }).click();
    await expect(page.getByRole("heading", { name: "Settings" })).toBeVisible();

    const themeSwitch = page.locator('input[type="checkbox"]').first();
    await expect(themeSwitch).toBeChecked();

    await themeSwitch.click();
    await expect(themeSwitch).not.toBeChecked();
    await expect.poll(() => page.evaluate(() => localStorage.getItem("taskflow.react.theme"))).toBe("light");

    await page.reload();
    await expect(page.getByRole("heading", { name: "Settings" })).toBeVisible();
    await expect(page.locator('input[type="checkbox"]').first()).not.toBeChecked();

    await expectNoPageErrors(pageErrors);
  });
});
