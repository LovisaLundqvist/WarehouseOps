import { Outlet } from "react-router-dom";
import { Bell, Search } from "lucide-react";
import Sidebar from "../components/Sidebar";

export default function AppLayout() {
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

            <div className="hidden items-center gap-3 md:flex">
              <div className="flex items-center gap-2 rounded-full border border-slate-200 bg-slate-50 px-4 py-2 text-sm text-slate-500">
                <Search size={16} />
                <span>Search operations</span>
              </div>

              <button className="rounded-full border border-slate-200 bg-white p-2 text-slate-600 shadow-sm hover:bg-slate-50">
                <Bell size={18} />
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
