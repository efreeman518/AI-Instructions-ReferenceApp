import { expect, Page } from "@playwright/test";

export const WasmBootMs = 20_000;

export function normalizeUnoText(value: string) {
  return value.replace(/\u200B/g, "").replace(/\s+/g, " ").trim();
}

export async function getBodyText(page: Page) {
  return normalizeUnoText(await page.locator("body").innerText());
}

export async function expectBodyToContain(page: Page, expectedText: string, timeout = 30_000) {
  await expect.poll(() => getBodyText(page), { timeout }).toContain(expectedText);
}

export async function expectBodyToContainAll(page: Page, expectedTexts: string[], timeout = 30_000) {
  await expect.poll(async () => {
    const bodyText = await getBodyText(page);
    return expectedTexts.every((expectedText) => bodyText.includes(expectedText));
  }, { timeout }).toBe(true);
}

type Occurrence = "first" | "last";

async function clickUnoButtonByText(page: Page, text: string, occurrence: Occurrence) {
  // Uno WASM renders Button as <div xamltype="Microsoft.UI.Xaml.Controls.Button">; filter to
  // the visible buttons whose textContent matches the requested label exactly.
  for (let attempt = 0; attempt < 30; attempt++) {
    const clicked = await page.evaluate(({ label, order }) => {
      const buttons = Array.from(document.querySelectorAll('[xamltype="Microsoft.UI.Xaml.Controls.Button"]'));
      const visible = buttons.filter((b) => {
        const r = b.getBoundingClientRect();
        if (r.width <= 0 || r.height <= 0 || r.x < 0 || r.y < 0) return false;
        const style = getComputedStyle(b);
        if (style.visibility === "hidden" || style.display === "none") return false;
        const txt = (b.textContent ?? "").replace(/\s+/g, " ").trim();
        // match whole-word to avoid Task vs Tasks ambiguity
        const re = new RegExp(`(^|[^A-Za-z])${label}([^A-Za-z]|$)`);
        return re.test(txt);
      });
      if (!visible.length) return null;
      const target = order === "last" ? visible[visible.length - 1] : visible[0];
      const r = target.getBoundingClientRect();
      return { x: r.x + r.width / 2, y: r.y + r.height / 2 };
    }, { label: text, order: occurrence });
    if (clicked) {
      await page.mouse.move(clicked.x, clicked.y);
      await page.mouse.down();
      await page.mouse.up();
      return true;
    }
    await page.waitForTimeout(500);
  }
  return false;
}

export async function clickVisibleText(page: Page, text: string, occurrence: Occurrence = "first") {
  const ok = await clickUnoButtonByText(page, text, occurrence);
  if (ok) return;

  // Fallback: try any clickable element with exact text (legacy flows).
  const locator = page.getByText(text, { exact: true });
  const target = occurrence === "last" ? locator.last() : locator.first();
  await expect(target).toBeVisible({ timeout: 30_000 });
  await target.scrollIntoViewIfNeeded();
  await target.click({ force: true });
}

export async function waitForApp(page: Page) {
  await page.goto("/", { waitUntil: "domcontentloaded" });
  await expectBodyToContain(page, "TaskFlow", WasmBootMs);
}

export async function navigateToTaskList(page: Page) {
  await waitForApp(page);
  await clickVisibleText(page, "Tasks", "last");
  await expectBodyToContain(page, "Manage and track all your tasks");
}

// ---------------------------------------------------------------------------
// Unique test data
// ---------------------------------------------------------------------------

export function uniqueTitle(prefix: string) {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
}
