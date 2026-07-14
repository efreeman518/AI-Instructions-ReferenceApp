import { expect, Page } from "@playwright/test";
import { createHash } from "node:crypto";

export type UnoChromeTarget = "dashboard" | "tasks" | "categories" | "tags" | "addTask";

const desktopChrome: Record<UnoChromeTarget, { x: number; y: number }> = {
  dashboard: { x: 84, y: 130 },
  tasks: { x: 84, y: 276 },
  categories: { x: 84, y: 420 },
  tags: { x: 84, y: 566 },
  addTask: { x: 84, y: 681 },
};

function readTimeoutMs(name: string, defaultMs: number) {
  const value = process.env[name];
  if (!value) return defaultMs;
  const seconds = Number.parseInt(value, 10);
  if (!Number.isFinite(seconds) || seconds <= 0) return defaultMs;
  return seconds * 1000;
}

export const WasmBootMs = readTimeoutMs("TASKFLOW_WASM_STARTUP_TIMEOUT_SECONDS", 120_000);
export const WasmPageLoadMs = readTimeoutMs("TASKFLOW_WASM_PAGE_LOAD_TIMEOUT_SECONDS", 60_000);

function requireBaseUrl() {
  if (!process.env.PLAYWRIGHT_UNO_URL) {
    throw new Error("PLAYWRIGHT_UNO_URL is required for direct Uno WASM Playwright runs. Use the C# Aspire wrapper or set PLAYWRIGHT_UNO_URL.");
  }
}

export async function waitForApp(page: Page) {
  requireBaseUrl();
  await page.goto("/", { waitUntil: "domcontentloaded", timeout: WasmPageLoadMs });
  await waitForCanvasPaint(page, WasmBootMs);
}

export async function waitForCanvasPaint(page: Page, timeout = WasmBootMs) {
  await expect.poll(async () => page.evaluate(() => {
    const canvas = Array.from(document.querySelectorAll("canvas"))
      .find((candidate) => {
        const rect = candidate.getBoundingClientRect();
        return rect.width >= 100 && rect.height >= 100;
      });

    if (!canvas) return false;

    try {
      return canvas.toDataURL("image/png").length > 1000;
    } catch {
      const rect = canvas.getBoundingClientRect();
      return rect.width >= 100 && rect.height >= 100;
    }
  }), { timeout }).toBe(true);
}

export async function canvasFingerprint(page: Page) {
  await waitForCanvasPaint(page);
  const screenshot = await page.screenshot({ fullPage: false });
  return createHash("sha256").update(screenshot).digest("hex");
}

export async function clickAppChrome(page: Page, target: UnoChromeTarget) {
  const viewport = page.viewportSize() ?? { width: 1280, height: 720 };
  const point = desktopChrome[target];
  const x = Math.min(point.x, Math.max(24, viewport.width - 24));
  const y = Math.min(point.y, Math.max(24, viewport.height - 24));

  await page.mouse.click(x, y);
  await waitForCanvasPaint(page, 30_000);
}

export async function expectVisualChangeAfter(page: Page, action: () => Promise<void>, timeout = 30_000) {
  const before = await canvasFingerprint(page);
  await action();
  await expect.poll(async () => canvasFingerprint(page), { timeout }).not.toBe(before);
}

export async function navigateToTaskList(page: Page) {
  await waitForApp(page);
  await clickAppChrome(page, "tasks");
}

export function uniqueTitle(prefix: string) {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
}
