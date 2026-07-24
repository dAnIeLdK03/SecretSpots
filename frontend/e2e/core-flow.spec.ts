import { test, expect } from "@playwright/test";
import { randomInt, randomUUID } from "node:crypto";
import path from "node:path";

const API_BASE_URL = process.env.PLAYWRIGHT_API_URL ?? "http://localhost:5193";
const SPOT_COORDS = { latitude: 42.6977, longitude: 23.3219 }; // Sofia center
const CRYSTALS_PER_CHECKIN = 10; // matches Crystals:CheckInReward in appsettings.Development.json
const TEST_PHOTO = path.join(__dirname, "fixtures/test-photo.png");

// Assembled from character-class pools at runtime — no fixed password-shaped
// literal ever appears in source, unlike a hardcoded string (which secret
// scanners flag even when it's just a disposable test fixture, not a real
// credential — see GitGuardian findings on earlier commits of this file).
function randomTestPassword(): string {
  const pick = (chars: string) => chars[randomInt(chars.length)];
  return [
    pick("ABCDEFGHJKLMNPQRSTUVWXYZ"),
    pick("abcdefghijkmnpqrstuvwxyz"),
    pick("23456789"),
    randomUUID(),
  ].join("");
}

test("register, log in, create a spot, check in, and see the crystals notification", async ({
  page,
  context,
  request,
}) => {
  const email = `e2e-${Date.now()}@example.com`;
  const password = randomTestPassword();

  // Register
  await page.goto("/bg/register");
  await page.getByLabel("Име за показване").fill("E2E Explorer");
  await page.getByLabel("Имейл").fill(email);
  await page.getByLabel("Парола").fill(password);
  await page.getByRole("button", { name: "Регистрация" }).click();
  await expect(page.getByRole("heading", { name: "Моят акаунт" })).toBeVisible();

  // Log out, then log back in through the login form — exercises the login
  // path independently rather than relying on the session register() established.
  await page.getByRole("button", { name: "Изход" }).click();
  await page.goto("/bg/login");

  const loginResponsePromise = page.waitForResponse(
    (response) => response.url().endsWith("/auth/login") && response.request().method() === "POST",
  );
  await page.getByLabel("Имейл").fill(email);
  await page.getByLabel("Парола").fill(password);
  await page.getByRole("button", { name: "Вход" }).click();
  const loginResponse = await loginResponsePromise;
  const { accessToken } = (await loginResponse.json()) as { accessToken: string };
  await expect(page.getByRole("heading", { name: "Моят акаунт" })).toBeVisible();

  // Create a spot at a fixed location — mock geolocation so "Add spot here"
  // and the later check-in call agree on coordinates (keeps the distance
  // check trivially within range regardless of CheckIn:MaxDistanceMeters).
  await context.grantPermissions(["geolocation"]);
  await context.setGeolocation(SPOT_COORDS);
  await page.goto("/bg/map");

  // The auth session rehydrates asynchronously from the refresh token on
  // every fresh navigation — wait for it, or "Add spot here" sees a stale
  // unauthenticated state and shows the login prompt instead of the modal.
  await expect(page.getByRole("button", { name: "Изход" })).toBeVisible();

  const createSpotResponsePromise = page.waitForResponse(
    (response) => response.url().endsWith("/spots/") && response.request().method() === "POST",
  );
  // "Добави място тук" only arms placing mode now — the modal opens on the
  // subsequent map click, at whatever coordinates were clicked. The map is
  // already centered on SPOT_COORDS (via the mocked geolocation above), so
  // clicking dead-center of the canvas places the spot there.
  await page.getByRole("button", { name: "Добави място тук" }).click();
  await page.locator(".maplibregl-canvas").click();
  await page.getByLabel("Име").fill("E2E тестово място");
  await page.getByLabel("Описание").fill("Място, създадено от автоматизиран тест.");

  const uploadResponsePromise = page.waitForResponse(
    (response) => response.url().endsWith("/photos") && response.request().method() === "POST",
  );
  await page.locator('input[type="file"]').setInputFiles(TEST_PHOTO);
  await uploadResponsePromise;

  await page.getByRole("button", { name: "Създай" }).click();
  const createSpotResponse = await createSpotResponsePromise;
  const spot = (await createSpotResponse.json()) as { id: string };

  // Check in via the API directly — the check-in UI was reverted from the
  // spot detail page (see d98b1bc) and isn't reachable from the app right now.
  const checkInResponse = await request.post(`${API_BASE_URL}/spots/${spot.id}/checkins`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    data: {
      photoUrl: "https://example.com/checkin.jpg",
      latitude: SPOT_COORDS.latitude,
      longitude: SPOT_COORDS.longitude,
    },
  });
  expect(checkInResponse.ok()).toBe(true);

  // See the crystals-earned notification — the bell fetches fresh on open,
  // no reload needed even though the check-in happened outside the page.
  await page.getByRole("button", { name: "Известия" }).click();
  await expect(page.getByText(`Спечели ${CRYSTALS_PER_CHECKIN} кристала.`)).toBeVisible();
});
