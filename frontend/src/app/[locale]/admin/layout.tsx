import { AdminGuard } from "@/components/AdminGuard";

// Every page under /admin is admin-only by virtue of living here — pages don't (and shouldn't)
// repeat the check themselves.
export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return <AdminGuard>{children}</AdminGuard>;
}
