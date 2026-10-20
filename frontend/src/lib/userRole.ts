// Mirrors the backend's UserRole enum (serialized as a string via JsonStringEnumConverter) — a
// plain const object instead of a TS `enum` keyword, matching this codebase's existing pattern
// for closed string sets (see SPOT_CATEGORIES/SpotCategory in spotsApi.ts). Gives named access
// (UserRole.Admin) instead of comparing against bare "Admin"/"User" string literals.
export const UserRole = {
  User: "User",
  Admin: "Admin",
} as const;

export type UserRole = (typeof UserRole)[keyof typeof UserRole];
