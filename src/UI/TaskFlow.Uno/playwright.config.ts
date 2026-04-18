import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e-tests",
  outputDir: "./node_modules/.cache/playwright-results",
  reporter: [["list"]],
  workers: 1,
  use: {
    baseURL: "https://localhost:55551",
    ignoreHTTPSErrors: true,
    screenshot: "only-on-failure",
    trace: "on-first-retry",
  },
  retries: 1,
  timeout: 60_000,
});
