import { useQuery } from "@tanstack/react-query";
import {
  AlertTriangle,
  ArrowUpRight,
  Boxes,
  ClipboardList,
  PackageCheck,
  Truck,
} from "lucide-react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { getDashboardSummary } from "../api/dashboardApi";
import type { DashboardSummary } from "../types/dashboard";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";

const emptyDashboardSummary: DashboardSummary = {
  activeOrdersCount: 0,
  lowStockItemsCount: 0,
  openIncidentsCount: 0,
  activeShipmentsCount: 0,
  orderStatusCounts: [],
  recentActivities: [],
  recentShipments: [],
};

function formatDateTime(value: string) {
  return new Date(value).toLocaleString("sv-SE", {
    dateStyle: "short",
    timeStyle: "short",
  });
}

function formatShortId(id: string) {
  return `#${id.slice(0, 8).toUpperCase()}`;
}

export default function DashboardPage() {
  const {
    data: dashboardSummary = emptyDashboardSummary,
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ["dashboard-summary"],
    queryFn: getDashboardSummary,
  });

  const summaryCards = [
    {
      title: "Active orders",
      value: dashboardSummary.activeOrdersCount,
      description: "Orders currently being handled",
      icon: ClipboardList,
    },
    {
      title: "Low stock items",
      value: dashboardSummary.lowStockItemsCount,
      description: "Products at or below minimum stock level",
      icon: Boxes,
    },
    {
      title: "Open incidents",
      value: dashboardSummary.openIncidentsCount,
      description: "Reported issues that are not closed",
      icon: AlertTriangle,
    },
    {
      title: "Active shipments",
      value: dashboardSummary.activeShipmentsCount,
      description: "Shipments not yet delivered",
      icon: Truck,
    },
  ];

  const errorMessage = getApiErrorMessage(error, "Could not load dashboard data.");

  return (
    <div className="space-y-6">
      <section className="rounded-3xl bg-slate-950 p-6 text-white shadow-sm">
        <div className="flex flex-col justify-between gap-6 lg:flex-row lg:items-center">
          <div>
            <p className="text-sm font-medium text-blue-300">Warehouse overview</p>
            <h2 className="mt-2 text-3xl font-bold tracking-tight">Operational control center</h2>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-300">
              Monitor orders, stock, shipments and incidents from one business-focused dashboard.
            </p>
          </div>

          <div className="rounded-2xl border border-slate-800 bg-slate-900 p-4">
            <div className="flex items-center gap-3">
              <PackageCheck className="text-blue-300" size={28} />
              <div>
                <p className="text-sm text-slate-400">Dashboard status</p>
                <p className="text-base font-semibold text-white">
                  {isLoading ? "Loading current data" : "Showing current data"}
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {isError && (
        <section className="rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          {errorMessage}
        </section>
      )}

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {summaryCards.map((card) => {
          const Icon = card.icon;

          return (
            <article key={card.title} className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="text-sm font-medium text-slate-500">{card.title}</p>
                  <p className="mt-3 text-3xl font-bold text-slate-950">
                    {isLoading ? "..." : card.value}
                  </p>
                </div>

                <div className="rounded-xl bg-blue-50 p-3 text-blue-600">
                  <Icon size={22} />
                </div>
              </div>

              <p className="mt-4 text-sm leading-5 text-slate-500">{card.description}</p>
            </article>
          );
        })}
      </section>

      <section className="grid gap-6 xl:grid-cols-3">
        <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm xl:col-span-2">
          <div className="mb-5 flex items-center justify-between">
            <div>
              <h3 className="text-base font-semibold text-slate-950">Orders by status</h3>
              <p className="mt-1 text-sm text-slate-500">
                Shows how current orders are distributed by status.
              </p>
            </div>

            <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-medium text-slate-600">
              Live data
            </span>
          </div>

          <div className="h-80">
            {dashboardSummary.orderStatusCounts.length === 0 && !isLoading ? (
              <div className="flex h-full items-center justify-center rounded-2xl bg-slate-50 text-sm text-slate-500">
                No order data available.
              </div>
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={dashboardSummary.orderStatusCounts}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} />
                  <XAxis dataKey="status" />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Bar dataKey="count" fill="#2563eb" radius={[8, 8, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </article>

        <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-5 flex items-center justify-between">
            <div>
              <h3 className="text-base font-semibold text-slate-950">Recent activity</h3>
              <p className="mt-1 text-sm text-slate-500">Latest important changes</p>
            </div>

            <ArrowUpRight size={18} className="text-slate-400" />
          </div>

          <div className="space-y-4">
            {isLoading && <p className="text-sm text-slate-500">Loading recent activity...</p>}

            {!isLoading && dashboardSummary.recentActivities.length === 0 && (
              <p className="text-sm text-slate-500">No recent activity yet.</p>
            )}

            {dashboardSummary.recentActivities.map((activity) => (
              <div key={`${activity.title}-${activity.performedAt}`} className="rounded-xl border border-slate-100 bg-slate-50 p-4">
                <div className="flex items-start justify-between gap-3">
                  <p className="text-sm font-semibold text-slate-950">{activity.title}</p>
                  <span className="text-xs text-slate-400">{formatDateTime(activity.performedAt)}</span>
                </div>
                <p className="mt-2 text-sm leading-5 text-slate-500">{activity.description}</p>
                <p className="mt-2 text-xs text-slate-400">Changed by {activity.performedBy}</p>
              </div>
            ))}
          </div>
        </article>
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="mb-5 flex items-center justify-between">
          <div>
            <h3 className="text-base font-semibold text-slate-950">Recent shipments</h3>
            <p className="mt-1 text-sm text-slate-500">
              Latest shipments created in the warehouse flow.
            </p>
          </div>

          <Truck size={18} className="text-slate-400" />
        </div>

        {isLoading && <p className="text-sm text-slate-500">Loading recent shipments...</p>}

        {!isLoading && dashboardSummary.recentShipments.length === 0 && (
          <p className="text-sm text-slate-500">No shipments found.</p>
        )}

        {!isLoading && dashboardSummary.recentShipments.length > 0 && (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Shipment
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Customer
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Status
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Tracking
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Created
                  </th>
                </tr>
              </thead>

              <tbody className="divide-y divide-slate-100 bg-white">
                {dashboardSummary.recentShipments.map((shipment) => (
                  <tr key={shipment.id}>
                    <td className="px-4 py-3 text-sm font-semibold text-slate-950">
                      {formatShortId(shipment.id)}
                    </td>
                    <td className="px-4 py-3 text-sm text-slate-600">
                      {shipment.customerName || "Customer not shown"}
                    </td>
                    <td className="px-4 py-3 text-sm text-slate-600">
                      {shipment.status}
                    </td>
                    <td className="px-4 py-3 text-sm text-slate-600">
                      {shipment.trackingNumber || "Not assigned"}
                    </td>
                    <td className="px-4 py-3 text-sm text-slate-600">
                      {formatDateTime(shipment.createdAt)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
