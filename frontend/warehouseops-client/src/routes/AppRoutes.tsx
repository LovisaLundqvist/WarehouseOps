import { Navigate, Route, Routes } from "react-router-dom";
import AppLayout from "../layouts/AppLayout";
import AuditLogsPage from "../pages/AuditLogsPage";
import CustomersPage from "../pages/CustomersPage";
import DashboardPage from "../pages/DashboardPage";
import IncidentsPage from "../pages/IncidentsPage";
import InventoryPage from "../pages/InventoryPage";
import LoginPage from "../pages/LoginPage";
import NewOrderPage from "../pages/NewOrderPage";
import NewShipmentPage from "../pages/NewShipmentPage";
import OrderDetailsPage from "../pages/OrderDetailsPage";
import OrdersPage from "../pages/OrdersPage";
import ProductsPage from "../pages/ProductsPage";
import ShipmentDetailsPage from "../pages/ShipmentDetailsPage";
import ShipmentsPage from "../pages/ShipmentsPage";
import ProtectedRoute from "./ProtectedRoute";

export default function AppRoutes() {
  return (
    <Routes>
      <Route path="login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="products" element={<ProductsPage />} />
          <Route path="inventory" element={<InventoryPage />} />
          <Route path="customers" element={<CustomersPage />} />
          <Route path="orders" element={<OrdersPage />} />
          <Route path="orders/new" element={<ProtectedRoute allowedRoles={["Admin", "WarehouseStaff"]} />}>
            <Route index element={<NewOrderPage />} />
          </Route>
          <Route path="orders/:id" element={<OrderDetailsPage />} />
          <Route path="shipments" element={<ShipmentsPage />} />
          <Route path="shipments/new" element={<ProtectedRoute allowedRoles={["Admin", "WarehouseStaff"]} />}>
            <Route index element={<NewShipmentPage />} />
          </Route>
          <Route path="shipments/:id" element={<ShipmentDetailsPage />} />
          <Route path="incidents" element={<IncidentsPage />} />
          <Route element={<ProtectedRoute allowedRoles={["Admin", "Manager"]} />}>
            <Route path="audit-logs" element={<AuditLogsPage />} />
          </Route>
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
