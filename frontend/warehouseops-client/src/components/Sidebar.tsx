import { NavLink } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import type { UserRole } from "../types/auth";

type NavigationItem = {
  label: string;
  path: string;
  roles?: UserRole[];
};

type SidebarProps = {
  isOpen: boolean;
  onClose: () => void;
};

const navigationItems: NavigationItem[] = [
  { label: "Overview", path: "/" },
  { label: "Products", path: "/products" },
  { label: "Inventory", path: "/inventory" },
  { label: "Customers", path: "/customers" },
  { label: "Orders", path: "/orders" },
  { label: "Shipments", path: "/shipments" },
  { label: "Incidents", path: "/incidents" },
  { label: "Change history", path: "/audit-logs", roles: ["Admin", "Manager"] },
];

export default function Sidebar({ isOpen, onClose }: SidebarProps) {
  const { user } = useAuth();

  const visibleItems = navigationItems.filter(
    (item) => !item.roles || (user && item.roles.includes(user.role)),
  );

  return (
    <aside
      className={`ops-sidebar${isOpen ? " ops-sidebar--open" : ""}`}
      id="primary-navigation"
      aria-label="Main navigation"
    >
      <div className="ops-sidebar__brand">
        <button
          type="button"
          className="ops-sidebar__close"
          onClick={onClose}
          aria-label="Close navigation"
        >
          Close
        </button>
        <span className="ops-eyebrow ops-sidebar__brand-label">
          Internal / Logistics
        </span>
        <span className="ops-display ops-sidebar__brand-name">WarehouseOps</span>
        <span className="ops-sidebar__brand-meta">Operations management</span>
      </div>

      <nav className="ops-sidebar__nav" aria-label="Workspace sections">
        <p className="ops-eyebrow ops-sidebar__heading">Workspace</p>
        {visibleItems.map((item, index) => (
          <NavLink
            key={item.path}
            to={item.path}
            end={item.path === "/"}
            onClick={onClose}
            className={({ isActive }) =>
              `ops-navlink${isActive ? " ops-navlink--active" : ""}`
            }
          >
            <span className="ops-navlink__index" aria-hidden="true">
              {String(index + 1).padStart(2, "0")}
            </span>
            <span>{item.label}</span>
          </NavLink>
        ))}
      </nav>

      <div className="ops-sidebar__account">
        <p className="ops-eyebrow ops-sidebar__account-label">Current session</p>
        <p className="ops-sidebar__account-name">{user?.displayName ?? "Unknown user"}</p>
        <p className="ops-sidebar__account-role">{user?.role ?? "No assigned role"}</p>
      </div>
    </aside>
  );
}
