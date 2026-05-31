import { NavLink } from "react-router-dom";
import {
  AlertTriangle,
  BarChart3,
  Boxes,
  ClipboardList,
  History,
  LayoutDashboard,
  Package,
  Truck,
  Users,
  type LucideIcon,
} from "lucide-react";
import { useAuth } from "../auth/AuthContext";
import type { UserRole } from "../types/auth";

type NavigationItem = {
  name: string;
  path: string;
  icon: LucideIcon;
  roles?: UserRole[];
};

const navigationItems: NavigationItem[] = [
  { name: "Dashboard", path: "/", icon: LayoutDashboard },
  { name: "Products", path: "/products", icon: Package },
  { name: "Inventory", path: "/inventory", icon: Boxes },
  { name: "Customers", path: "/customers", icon: Users },
  { name: "Orders", path: "/orders", icon: ClipboardList },
  { name: "Shipments", path: "/shipments", icon: Truck },
  { name: "Incidents", path: "/incidents", icon: AlertTriangle },
  { name: "Change History", path: "/audit-logs", icon: History, roles: ["Admin", "Manager"] },
];

export default function Sidebar() {
  const { user } = useAuth();

  const allowedNavigationItems = navigationItems.filter((item) => {
    if (!item.roles) {
      return true;
    }

    return user ? item.roles.includes(user.role) : false;
  });

  return (
    <aside className="fixed inset-y-0 left-0 z-20 hidden w-72 border-r border-slate-800 bg-slate-950 px-4 py-5 text-white lg:block">
      <div className="mb-8 flex items-center gap-3 px-2">
        <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-blue-500">
          <BarChart3 size={24} />
        </div>

        <div>
          <p className="text-lg font-bold tracking-tight">WarehouseOps</p>
          <p className="text-xs text-slate-400">Logistics management</p>
        </div>
      </div>

      <nav className="space-y-1">
        {allowedNavigationItems.map((item) => {
          const Icon = item.icon;

          return (
            <NavLink
              key={item.name}
              to={item.path}
              end={item.path === "/"}
              className={({ isActive }) =>
                [
                  "flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition",
                  isActive
                    ? "bg-blue-500 text-white shadow-sm"
                    : "text-slate-300 hover:bg-slate-900 hover:text-white",
                ].join(" ")
              }
            >
              <Icon size={18} />
              <span>{item.name}</span>
            </NavLink>
          );
        })}
      </nav>

      <div className="absolute bottom-5 left-4 right-4 rounded-2xl border border-slate-800 bg-slate-900 p-4">
        <p className="text-sm font-semibold text-white">Signed in as</p>
        <p className="mt-1 text-xs leading-5 text-slate-400">
          {user ? `${user.displayName} | ${user.role}` : "No user"}
        </p>
      </div>
    </aside>
  );
}
