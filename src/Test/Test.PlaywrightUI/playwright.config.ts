import { defineConfig, devices } from "@playwright/test";

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
      },
    },
  ],
});
