export type DashboardStatusCount = {
  status: string;
  count: number;
};

export type DashboardActivity = {
  title: string;
  description: string;
  performedBy: string;
  performedAt: string;
};

export type DashboardRecentShipment = {
  id: string;
  orderId: string;
  customerName: string;
  status: string;
  trackingNumber: string;
  createdAt: string;
};

export type DashboardSummary = {
  activeOrdersCount: number;
  lowStockItemsCount: number;
  openIncidentsCount: number;
  activeShipmentsCount: number;
  orderStatusCounts: DashboardStatusCount[];
  recentActivities: DashboardActivity[];
  recentShipments: DashboardRecentShipment[];
};
