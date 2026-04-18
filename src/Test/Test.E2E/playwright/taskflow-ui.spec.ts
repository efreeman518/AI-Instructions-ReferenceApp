import { test, expect, Page } from "@playwright/test";

const BASE = "https://localhost:55551";
const WASM_BOOT_MS = 15_000;
const NAV_WAIT_MS = 3_000;

// Helper: waits for Uno WASM to fully bootstrap (spinner → content)
async function waitForApp(page: Page) {
  await page.goto(BASE, { waitUntil: "networkidle" });
  await page.waitForTimeout(WASM_BOOT_MS);
}

// Helper: click sidebar nav item by content text (wide viewport)
async function navigateTo(page: Page, tabName: string) {
  // Wide layout uses the vertical TabBar inside AutoLayout
  const tab = page.locator(`text=${tabName}`).first();
  await tab.click();
  await page.waitForTimeout(NAV_WAIT_MS);
}

test.describe("TaskFlow UI — Smoke Tests", () => {
  test.beforeEach(async ({ page }) => {
    page.setDefaultTimeout(30_000);
  });

  test("Dashboard renders with header, stats, and recent activity", async ({ page }) => {
    await waitForApp(page);

    // Header always visible
    await expect(page.locator("text=TaskFlow").first()).toBeVisible();

    // Stats cards
    await expect(page.locator("text=Total").first()).toBeVisible();
    await expect(page.locator("text=Open").first()).toBeVisible();
    await expect(page.locator("text=In Progress").first()).toBeVisible();
    await expect(page.locator("text=Completed").first()).toBeVisible();

    // Recent Activity section
    await expect(page.locator("text=Recent Activity").first()).toBeVisible();
    await expect(page.locator("text=Build dashboard UI").first()).toBeVisible();

    await page.screenshot({ path: "playwright-screenshots/e2e-01-dashboard.png" });
  });

  test("Navigation to Task Detail via recent activity click", async ({ page }) => {
    await waitForApp(page);

    // Click on a task in recent activity
    await page.locator("text=Build dashboard UI").first().click();
    await page.waitForTimeout(NAV_WAIT_MS);

    // Task detail should show
    await expect(page.locator("text=Build dashboard UI").first()).toBeVisible();
    await expect(page.locator("text=Back to Tasks").first()).toBeVisible();
    await expect(page.locator("text=Edit").first()).toBeVisible();
    await expect(page.locator("text=Delete").first()).toBeVisible();

    // Description
    await expect(page.locator("text=Description").first()).toBeVisible();

    // Checklist
    await expect(page.locator("text=Checklist").first()).toBeVisible();
    await expect(page.locator("text=Design mockups").first()).toBeVisible();

    // Comments
    await expect(page.locator("text=Comments").first()).toBeVisible();
    await expect(page.locator("text=Looking good so far!").first()).toBeVisible();

    await page.screenshot({ path: "playwright-screenshots/e2e-02-task-detail.png" });
  });

  test("Task Form renders for new task", async ({ page }) => {
    await waitForApp(page);

    // Click New Task button
    await page.locator("text=New Task").first().click();
    await page.waitForTimeout(NAV_WAIT_MS);

    // Form elements
    await expect(page.locator("text=New Task").first()).toBeVisible();
    await expect(page.locator("[placeholder='What needs to be done?']").first()).toBeVisible();
    await expect(page.locator("text=Save Task").first()).toBeVisible();
    await expect(page.locator("text=Cancel").first()).toBeVisible();

    await page.screenshot({ path: "playwright-screenshots/e2e-03-task-form.png" });
  });

  test("Categories page renders with CRUD controls", async ({ page }) => {
    await waitForApp(page);
    await navigateTo(page, "Categories");

    await expect(page.locator("text=Categories").first()).toBeVisible();
    await expect(page.locator("text=ADD CATEGORY").first()).toBeVisible();
    await expect(page.locator("[placeholder='Category name']").first()).toBeVisible();
    await expect(page.locator("text=Development").first()).toBeVisible();
    await expect(page.locator("text=Documentation").first()).toBeVisible();
    await expect(page.locator("text=Active").first()).toBeVisible();

    await page.screenshot({ path: "playwright-screenshots/e2e-04-categories.png" });
  });

  test("Tags page renders with CRUD controls", async ({ page }) => {
    await waitForApp(page);
    await navigateTo(page, "Tags");

    await expect(page.locator("text=Tags").first()).toBeVisible();
    await expect(page.locator("text=ADD TAG").first()).toBeVisible();
    await expect(page.locator("[placeholder='Tag name']").first()).toBeVisible();
    await expect(page.locator("text=frontend").first()).toBeVisible();
    await expect(page.locator("text=backend").first()).toBeVisible();

    await page.screenshot({ path: "playwright-screenshots/e2e-05-tags.png" });
  });

  test("Settings page renders", async ({ page }) => {
    await waitForApp(page);

    // Click gear icon (settings button in header)
    const settingsBtn = page.locator("button").filter({ has: page.locator("[class*='FontIcon']") }).last();
    await settingsBtn.click();
    await page.waitForTimeout(NAV_WAIT_MS);

    await expect(page.locator("text=Settings").first()).toBeVisible();
    await expect(page.locator("text=API Configuration").first()).toBeVisible();
    await expect(page.locator("text=About").first()).toBeVisible();

    await page.screenshot({ path: "playwright-screenshots/e2e-06-settings.png" });
  });
});

test.describe("TaskFlow UI — Mobile Responsive", () => {
  test.use({ viewport: { width: 375, height: 812 } });

  test("Mobile shows bottom tab bar, hides sidebar", async ({ page }) => {
    await waitForApp(page);

    // Header visible
    await expect(page.locator("text=TaskFlow").first()).toBeVisible();

    // Bottom tabs should be visible
    await expect(page.locator("text=Home").first()).toBeVisible();
    await expect(page.locator("text=Tasks").first()).toBeVisible();

    await page.screenshot({ path: "playwright-screenshots/e2e-07-mobile.png" });
  });
});

test.describe("TaskFlow UI — CRUD Operations", () => {
  test("Add comment on task detail", async ({ page }) => {
    await waitForApp(page);

    // Navigate to task detail
    await page.locator("text=Build dashboard UI").first().click();
    await page.waitForTimeout(NAV_WAIT_MS);

    // Type a comment
    const commentInput = page.locator("[placeholder='Write a comment...']").first();
    await commentInput.fill("This is a test comment");

    // Click Post
    await page.locator("text=Post").first().click();
    await page.waitForTimeout(2000);

    await page.screenshot({ path: "playwright-screenshots/e2e-08-comment-added.png" });
  });

  test("Add checklist item on task detail", async ({ page }) => {
    await waitForApp(page);

    // Navigate to task detail
    await page.locator("text=Build dashboard UI").first().click();
    await page.waitForTimeout(NAV_WAIT_MS);

    // Type a checklist item
    const checklistInput = page.locator("[placeholder='Add checklist item...']").first();
    await checklistInput.fill("New checklist item");

    // Click Add
    await page.locator("text=Add").first().click();
    await page.waitForTimeout(2000);

    await page.screenshot({ path: "playwright-screenshots/e2e-09-checklist-added.png" });
  });
});
