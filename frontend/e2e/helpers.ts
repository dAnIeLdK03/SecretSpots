import { randomInt, randomUUID } from "node:crypto";

// Assembled from character-class pools at runtime — no fixed password-shaped
// literal ever appears in source, unlike a hardcoded string (which secret
// scanners flag even when it's just a disposable test fixture, not a real
// credential — see GitGuardian findings on earlier commits of this file).
export function randomTestPassword(): string {
  const pick = (chars: string) => chars[randomInt(chars.length)];
  return [
    pick("ABCDEFGHJKLMNPQRSTUVWXYZ"),
    pick("abcdefghijkmnpqrstuvwxyz"),
    pick("23456789"),
    randomUUID(),
  ].join("");
}
