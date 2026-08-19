import { defineConfig, devices } from "@playwright/test";

// Assumes backend (+ Postgres + MinIO) and frontend are already running —
// see e2e/README.md. CI starts them explicitly as separate workflow steps
// so failures are attributable to a specific service, not a shared webServer block.
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  // In CI every spec file hits the same backend instance from the same IP, and the auth rate
  // limiter is partitioned by IP — running spec files in parallel there would let one file's
  // auth calls count against another's budget (see auth-flow.spec.ts's rate-limiting case),
  // causing unrelated flakiness. Serial only in CI; local runs keep default parallelism.
  workers: process.env.CI ? 1 : undefined,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? "list" : "html",
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3000",
    trace: "on-first-retry",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
