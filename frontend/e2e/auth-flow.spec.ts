import { test, expect } from "@playwright/test";
import { randomTestPassword } from "./helpers";

const API_BASE_URL = process.env.PLAYWRIGHT_API_URL ?? "http://localhost:5193";

test("repeated failed logins eventually get rate-limited", async ({ request }) => {
  const configResponse = await request.get(`${API_BASE_URL}/internal/test/rate-limit-config`);
  expect(configResponse.ok()).toBe(true);
  const { authPermitLimit, authWindowSeconds } = (await configResponse.json()) as {
    authPermitLimit: number;
    authWindowSeconds: number;
  };
  // Reading the live config (instead of hardcoding a number) keeps this correct whether it runs
  // against CI's tightened RateLimiting__AuthPermitLimit override or an unmodified local backend.
  test.setTimeout(Math.max(30_000, (authWindowSeconds + 5) * 1000));

  const email = `e2e-ratelimit-${Date.now()}@example.com`;
  const statuses: number[] = [];
  for (let i = 0; i < authPermitLimit + 1; i++) {
    const response = await request.post(`${API_BASE_URL}/auth/login`, {
      data: { email, password: "wrong-password-does-not-matter" },
    });
    statuses.push(response.status());
  }

  // Every request under the limit fails with "invalid credentials" (401) — proving each one
  // actually reached the handler rather than being rejected for some unrelated reason — and only
  // the one that exceeds the permit count comes back 429.
  expect(statuses.slice(0, authPermitLimit)).toEqual(Array(authPermitLimit).fill(401));
  expect(statuses.at(-1)).toBe(429);

  // Wait the window out before finishing, so this test doesn't leave every other spec's
  // login/register calls rate-limited for the remainder of the fixed window — see the comment
  // on RateLimiting__AuthWindowSeconds in ci.yml.
  await new Promise((resolve) => setTimeout(resolve, (authWindowSeconds + 1) * 1000));
});

test("refresh rotates the httpOnly cookie and revokes the previous token", async ({ page, context }) => {
  const email = `e2e-refresh-${Date.now()}@example.com`;
  const password = randomTestPassword();

  await page.goto("/bg/register");
  await page.getByLabel("Име за показване").fill("E2E Refresh");
  await page.getByLabel("Имейл").fill(email);
  await page.getByLabel("Парола").fill(password);
  await page.getByRole("button", { name: "Регистрация" }).click();
  await expect(page.getByRole("heading", { name: "Моят акаунт" })).toBeVisible();

  const cookieName = "secretspots_refresh_token";
  const originalCookie = (await context.cookies()).find((c) => c.name === cookieName);
  expect(originalCookie).toBeDefined();

  // Same browser context, so the httpOnly cookie is sent automatically — this call rotates it.
  // X-Requested-With is required by CsrfProtection.RequireCsrfHeader (see Program.cs) — /auth/
  // refresh authenticates via this ambient cookie rather than a bearer token, so a raw request
  // without it would otherwise be indistinguishable from a cross-site CSRF attempt.
  const csrfHeaders = { "X-Requested-With": "XMLHttpRequest" };
  const refreshResponse = await context.request.post(`${API_BASE_URL}/auth/refresh`, { headers: csrfHeaders });
  expect(refreshResponse.ok()).toBe(true);

  const rotatedCookie = (await context.cookies()).find((c) => c.name === cookieName);
  expect(rotatedCookie?.value).toBeDefined();
  expect(rotatedCookie?.value).not.toBe(originalCookie?.value);

  // The old token was revoked by the rotation above — reusing it explicitly (bypassing the
  // context's cookie jar, which now only holds the new one) must now be rejected.
  const reuseResponse = await context.request.post(`${API_BASE_URL}/auth/refresh`, {
    headers: { ...csrfHeaders, Cookie: `${cookieName}=${originalCookie?.value}` },
  });
  expect(reuseResponse.status()).toBe(401);
});

test("forgot password lets a user set a new password and log in with it", async ({ page, context, request }) => {
  const email = `e2e-reset-${Date.now()}@example.com`;
  const oldPassword = randomTestPassword();
  const newPassword = randomTestPassword();

  await page.goto("/bg/register");
  await page.getByLabel("Име за показване").fill("E2E Reset");
  await page.getByLabel("Имейл").fill(email);
  await page.getByLabel("Парола").fill(oldPassword);
  await page.getByRole("button", { name: "Регистрация" }).click();
  await expect(page.getByRole("heading", { name: "Моят акаунт" })).toBeVisible();

  await page.getByRole("button", { name: "Меню" }).click();
  await page.getByRole("button", { name: "Изход" }).click();

  await page.goto("/bg/forgot-password");
  await page.getByLabel("Имейл").fill(email);
  await page.getByRole("button", { name: "Изпрати връзка" }).click();
  await expect(page.getByText("Ако имейлът съществува в системата")).toBeVisible();

  // InMemoryEmailSender (Development-only, see Program.cs) captured the email instead of
  // calling the real Resend API — read it back to pull out the reset link's token.
  const emailResponse = await request.get(`${API_BASE_URL}/internal/test/emails?to=${encodeURIComponent(email)}`);
  expect(emailResponse.ok()).toBe(true);
  const { htmlBody } = (await emailResponse.json()) as { htmlBody: string };
  const token = new URL(htmlBody.match(/href="([^"]+)"/)?.[1] ?? "").searchParams.get("token");
  expect(token).toBeTruthy();

  await page.goto(`/bg/reset-password?token=${token}`);
  await page.getByLabel("Нова парола").fill(newPassword);
  await page.getByLabel("Потвърди паролата").fill(newPassword);
  await page.getByRole("button", { name: "Смени паролата" }).click();
  await expect(page.getByText("Паролата беше сменена успешно.")).toBeVisible();

  // Resetting revokes every existing refresh token (see ResetPassword.Handler) — a fresh
  // context proves the new password works independently of any leftover session cookie.
  const freshPage = await context.newPage();
  await freshPage.goto("/bg/login");
  await freshPage.getByLabel("Имейл").fill(email);
  await freshPage.getByLabel("Парола").fill(newPassword);
  await freshPage.getByRole("button", { name: "Вход" }).click();
  await expect(freshPage.getByRole("heading", { name: "Моят акаунт" })).toBeVisible();
});
