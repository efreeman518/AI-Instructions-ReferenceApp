import { expect, Page, test } from "@playwright/test";
import { waitForCanvasPaint, WasmBootMs, WasmPageLoadMs } from "../../utils/unoTestUtils";

type StartupWindow = typeof globalThis & { __taskflowStartupErrors?: string[] };

test("published Release renders on its first visit", async ({ browser }, testInfo) => {
  const baseURL = process.env.PLAYWRIGHT_UNO_URL;
  expect(baseURL, "PLAYWRIGHT_UNO_URL must point to the published Release host").toBeTruthy();

  const diagnostics: string[] = [];
  const context = await browser.newContext({ baseURL });
  const initialStorage = await context.storageState();
  expect(initialStorage.cookies).toEqual([]);
  expect(initialStorage.origins).toEqual([]);

  await context.addInitScript(() => {
    const startupWindow = globalThis as StartupWindow;
    startupWindow.__taskflowStartupErrors = [];
    addEventListener("error", event => {
      const managedDetail = event.error instanceof Error
        ? `${event.error.name}: ${event.error.message}${event.error.stack ? `\n${event.error.stack}` : ""}`
        : String(event.error ?? "");
      startupWindow.__taskflowStartupErrors?.push(
        `window.error: ${event.message} at ${event.filename}:${event.lineno}:${event.colno}${managedDetail ? `\n${managedDetail}` : ""}`);
    });
    addEventListener("unhandledrejection", event => {
      startupWindow.__taskflowStartupErrors?.push(`unhandledrejection: ${String(event.reason)}`);
    });
  });

  const page = await context.newPage();
  page.on("console", message => diagnostics.push(`console.${message.type()}: ${message.text()}`));
  page.on("pageerror", error => {
    const detail = [error.name, error.message, error.stack].filter(Boolean).join("\n");
    diagnostics.push(`pageerror: ${detail}`);
  });
  page.on("requestfailed", request => {
    diagnostics.push(`requestfailed: ${request.method()} ${request.url()} ${request.failure()?.errorText ?? "unknown"}`);
  });
  page.on("response", response => {
    if (response.status() >= 400) {
      diagnostics.push(`response.${response.status()}: ${response.request().method()} ${response.url()}`);
    }
  });

  try {
    await page.goto("/", { waitUntil: "domcontentloaded", timeout: WasmPageLoadMs });
    await waitForCanvasPaint(page, WasmBootMs);
    await expect(page.locator(".uno-loader"), "Uno splash must clear on the first visit")
      .toBeHidden({ timeout: WasmBootMs });

    const startupErrors = await readStartupErrors(page);
    const startupEvidence = [...diagnostics, ...startupErrors];
    const hasRendererFrame = startupEvidence.some(entry => /BrowserRenderer\.requestRender/i.test(entry));
    const hasManagedNullReference = startupEvidence.some(entry =>
      /ManagedError|(?:Arg_)?NullReferenceException/i.test(entry));
    const rendererRaceEvidence = hasRendererFrame && hasManagedNullReference
      ? startupEvidence.filter(entry =>
        /BrowserRenderer\.requestRender|ManagedError|(?:Arg_)?NullReferenceException/i.test(entry))
      : [];

    // A stackless managed error can be nonfatal after the renderer is ready. Fail the known
    // upstream race when first render/splash fails above or the paired renderer evidence is present.
    expect(rendererRaceEvidence, "Uno BrowserRenderer cold-start race detected").toEqual([]);
  } catch (error) {
    diagnostics.push(`failure: ${error instanceof Error ? error.stack ?? error.message : String(error)}`);
    diagnostics.push(`page-state: ${JSON.stringify(await readPageState(page))}`);
    await testInfo.attach("uno-release-cold-start.png", {
      body: await page.screenshot({ fullPage: false }),
      contentType: "image/png",
    });
    throw new Error(`Uno published Release cold start failed.\n${diagnostics.join("\n")}`);
  } finally {
    await testInfo.attach("uno-release-cold-start.txt", {
      body: Buffer.from(diagnostics.join("\n"), "utf8"),
      contentType: "text/plain",
    });
    await context.close();
  }
});

async function readStartupErrors(page: Page) {
  return page.evaluate(() =>
    [...((globalThis as StartupWindow).__taskflowStartupErrors ?? [])]);
}

async function readPageState(page: Page) {
  try {
    return await page.evaluate(() => ({
      url: location.href,
      readyState: document.readyState,
      html: document.documentElement.outerHTML.slice(0, 4_000),
      scripts: Array.from(document.scripts, script => script.src),
      canvasCount: document.querySelectorAll("canvas").length,
      bodyText: document.body?.innerText.slice(0, 2_000) ?? "",
      startupErrors: [...((globalThis as StartupWindow).__taskflowStartupErrors ?? [])],
    }));
  } catch (error) {
    return { diagnosticsError: error instanceof Error ? error.message : String(error) };
  }
}
