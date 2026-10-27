"use client";

import { useEffect } from "react";
import { useAuthStore } from "@/store/useAuthStore";
import { useRouter } from "@/i18n/navigation";
import { useRequireAuth } from "@/hooks/useRequireAuth";
import { UserRole } from "@/lib/userRole";

// useRequireAuth plus a role check: signed-out users go to /login (via useRequireAuth), signed-in
// non-admins go home. Returns true only once the current user is confirmed to be an admin.
export function useRequireAdmin(): boolean {
  const isAuthenticated = useRequireAuth();
  const user = useAuthStore((state) => state.user);
  const router = useRouter();

  const isAdmin = isAuthenticated && user?.role === UserRole.Admin;

  useEffect(() => {
    if (isAuthenticated && user && !isAdmin) {
      router.replace("/");
    }
  }, [isAuthenticated, user, isAdmin, router]);

  return isAdmin;
}
