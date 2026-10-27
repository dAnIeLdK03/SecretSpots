"use client";

import type { ReactNode } from "react";
import { useRequireAdmin } from "@/hooks/useRequireAdmin";

// Renders nothing until the current user is confirmed to be an admin, so the admin pages it
// wraps never mount (and never start fetching) for anyone else. Access control itself is
// enforced by the API (AuthorizationPolicies.Admin); this only keeps non-admins off broken pages.
export function AdminGuard({ children }: { children: ReactNode }) {
  const isAdmin = useRequireAdmin();
  return isAdmin ? children : null;
}
