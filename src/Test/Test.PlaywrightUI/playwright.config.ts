import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  outputDir: "./node_modules/.cache/playwright-results",
  reporter: [["list"]],
  workers: 1,
  retries: 1,
  timeout: 60_000,

  projects: [
    {
      name: "blazor",
      testDir: "./tests/blazor",
      use: {
        baseURL: "https://localhost:7201",
        ignoreHTTPSErrors: true,
        screenshot: "only-on-failure",
        trace: "on-first-retry",
        ...devices["Desktop Chrome"],
      },
    },
    {
      name: "uno",
      testDir: "./tests/uno",
      use: {
        baseURL: "https://localhost:7069",
        ignoreHTTPSErrors: true,
        screenshot: "only-on-failure",
        trace: "on-first-retry",
        ...devices["Desktop Chrome"],
      },
    },
  ],
});
