import { LogOut } from "lucide-react";
import { Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import Sidebar from "../components/Sidebar";

export default function AppLayout() {
  const { user, logout } = useAuth();

  return (
    <div className="min-h-screen bg-slate-100">
      <Sidebar />

      <div className="lg:pl-72">
        <header className="sticky top-0 z-10 border-b border-slate-200 bg-white/90 backdrop-blur">
          <div className="flex h-16 items-center justify-between px-6">
            <div>
              <p className="text-sm font-medium text-slate-500">WarehouseOps</p>
              <h1 className="text-lg font-semibold text-slate-950">Operations dashboard</h1>
            </div>

            <div className="flex items-center gap-3">
              {user && (
                <div className="hidden rounded-full border border-slate-200 bg-slate-50 px-4 py-2 text-sm font-medium text-slate-600 md:block">
                  {user.displayName} | {user.role}
                </div>
              )}

              <button
                type="button"
                onClick={logout}
                className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50"
              >
                <LogOut size={16} />
                Logout
              </button>
            </div>
          </div>
        </header>

        <main className="px-6 py-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
