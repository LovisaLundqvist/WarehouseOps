import { useEffect, useState } from "react";
import { Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import Sidebar from "../components/Sidebar";

const pageNames: Record<string, string> = {
  "/": "Overview",
  "/products": "Products",
  "/inventory": "Inventory",
  "/customers": "Customers",
  "/orders": "Orders",
  "/orders/new": "New order",
  "/shipments": "Shipments",
  "/shipments/new": "New shipment",
  "/incidents": "Incidents",
  "/audit-logs": "Change history",
};

export default function AppLayout() {
  const { user, logout } = useAuth();
  const { pathname } = useLocation();
  const [isNavigationOpen, setIsNavigationOpen] = useState(false);

  useEffect(() => {
    if (!isNavigationOpen) return;
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") setIsNavigationOpen(false);
    }
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isNavigationOpen]);

  const currentPage = pageNames[pathname]
    ?? (pathname.startsWith("/orders/") ? "Order details"
      : pathname.startsWith("/shipments/") ? "Shipment details" : "Operations");

  return (
    <div className="ops-app">
      <a href="#main-content" className="sr-only focus:not-sr-only focus:fixed focus:left-3 focus:top-3 focus:z-50 focus:bg-[#F8F6F0] focus:p-3">Skip to content</a>
      {isNavigationOpen && <button type="button" className="ops-overlay" aria-label="Close navigation overlay" onClick={() => setIsNavigationOpen(false)} />}
      <Sidebar isOpen={isNavigationOpen} onClose={() => setIsNavigationOpen(false)} />
      <div className="ops-main-column">
        <header className="ops-header">
          <div className="ops-header__start">
            <button type="button" className="ops-button-quiet ops-menu-button" aria-expanded={isNavigationOpen} aria-controls="primary-navigation" onClick={() => setIsNavigationOpen(true)}>Menu</button>
            <div><p className="ops-eyebrow ops-header__eyebrow">WarehouseOps / Workspace</p><p className="ops-header__label">{currentPage}</p></div>
          </div>
          <div className="ops-header__end">
            {user && <div className="ops-header__user"><p className="ops-header__user-name">{user.displayName}</p><p className="ops-header__role">{user.role}</p></div>}
            <button type="button" className="ops-button-quiet" onClick={logout}>Sign out</button>
          </div>
        </header>
        <main className="ops-content" id="main-content"><Outlet /></main>
      </div>
    </div>
  );
}
