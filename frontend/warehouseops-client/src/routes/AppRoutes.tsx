import { Navigate, Route, Routes } from "react-router-dom";
import AppLayout from "../layouts/AppLayout";
import DashboardPage from "../pages/DashboardPage";
import InventoryPage from "../pages/InventoryPage";
import OrdersPage from "../pages/OrdersPage";
import PlaceholderPage from "../pages/PlaceholderPage";
import ProductsPage from "../pages/ProductsPage";
import ShipmentsPage from "../pages/ShipmentsPage";

export default function AppRoutes() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<DashboardPage />} />
        <Route path="products" element={<ProductsPage />} />
        <Route path="inventory" element={<InventoryPage />} />
        <Route path="orders" element={<OrdersPage />} />
        <Route path="shipments" element={<ShipmentsPage />} />
        <Route path="customers" element={<PlaceholderPage title="Customers" description="Customer management will be implemented after the core layout is working." />} />
        <Route path="incidents" element={<PlaceholderPage title="Incidents" description="Incident reporting and incident status handling will be implemented in the next frontend step." />} />
        <Route path="audit-logs" element={<PlaceholderPage title="Audit Logs" description="Audit log viewing will be connected after authentication and roles are added." />} />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
