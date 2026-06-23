import { defineConfig, devices } from "@playwright/test";
import { existsSync, readFileSync } from "node:fs";
import { homedir } from "node:os";
import { join } from "node:path";

function getManagedHeadlessShellPath() {
  if (process.platform !== "win32") return undefined;

  const browsersJson = JSON.parse(readFileSync(join(process.cwd(), "node_modules", "playwright-core", "browsers.json"), "utf8"));
  const revision = browsersJson.browsers.find((browser: { name: string }) => browser.name === "chromium-headless-shell")?.revision;
  if (!revision) return undefined;

  const browserRoot = process.env.PLAYWRIGHT_BROWSERS_PATH === "0"
    ? join(process.cwd(), "node_modules", "playwright-core", ".local-browsers")
    : process.env.PLAYWRIGHT_BROWSERS_PATH ?? join(process.env.LOCALAPPDATA ?? join(homedir(), "AppData", "Local"), "ms-playwright");

  return join(browserRoot, `chromium_headless_shell-${revision}`, "chrome-headless-shell-win64", "chrome-headless-shell.exe");
}

const systemChromePath = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const managedHeadlessShellPath = getManagedHeadlessShellPath();
const managedHeadlessShellMissing = !managedHeadlessShellPath || !existsSync(managedHeadlessShellPath);
const useSystemChrome = process.platform === "win32" &&
  managedHeadlessShellMissing &&
  process.env.PLAYWRIGHT_USE_SYSTEM_CHROME !== "false" &&
  existsSync(systemChromePath);

const chromeFallback = useSystemChrome ? { channel: "chrome" } : {};

export default defineConfig({
  outputDir: "./node_modules/.cache/playwright-results",
  reporter: [["list"]],
  workers: 1,
  retries: 1,
  timeout: 180_000,

  projects: [
    {
      name: "blazor",
      testDir: "./tests/blazor",
      use: {
        baseURL: process.env.PLAYWRIGHT_BLAZOR_URL ?? "https://localhost:7201",
        ignoreHTTPSErrors: true,
        screenshot: "only-on-failure",
        trace: "on-first-retry",
        ...devices["Desktop Chrome"],
        ...chromeFallback,
      },
    },
    {
      name: "uno",
      testDir: "./tests/uno",
      use: {
        baseURL: process.env.PLAYWRIGHT_UNO_URL ?? "https://localhost:7069",
        ignoreHTTPSErrors: true,
        screenshot: "only-on-failure",
        trace: "on-first-retry",
        ...devices["Desktop Chrome"],
        ...chromeFallback,
      },
    },
    {
      name: "react",
      testDir: "./tests/react",
      use: {
        baseURL: process.env.PLAYWRIGHT_REACT_URL ?? process.env.TASKFLOW_REACT_BASE_URL ?? "http://localhost:5178",
        screenshot: "only-on-failure",
        trace: "on-first-retry",
        ...devices["Desktop Chrome"],
        ...chromeFallback,
      },
    },
  ],
});
