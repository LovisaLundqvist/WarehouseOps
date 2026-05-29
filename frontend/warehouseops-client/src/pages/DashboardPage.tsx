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

const summaryCards = [
  {
    title: "Active orders",
    value: "18",
    description: "Orders currently being processed",
    icon: ClipboardList,
  },
  {
    title: "Low stock items",
    value: "4",
    description: "Products at or below minimum stock level",
    icon: Boxes,
  },
  {
    title: "Open incidents",
    value: "3",
    description: "Operational issues that need attention",
    icon: AlertTriangle,
  },
  {
    title: "Active shipments",
    value: "11",
    description: "Shipments not yet delivered",
    icon: Truck,
  },
];

const orderStatusData = [
  { status: "Pending", count: 5 },
  { status: "Processing", count: 7 },
  { status: "Packed", count: 4 },
  { status: "Shipped", count: 6 },
  { status: "Completed", count: 12 },
];

const recentActivities = [
  {
    title: "Order status updated",
    description: "Order 53049767 moved to Completed",
    time: "Today",
  },
  {
    title: "Shipment delivered",
    description: "Tracking number SE-TRK-1001 was marked as Delivered",
    time: "Today",
  },
  {
    title: "Incident closed",
    description: "Damaged package incident was resolved",
    time: "Today",
  },
];

export default function DashboardPage() {
  return (
    <div className="space-y-6">
      <section className="rounded-3xl bg-slate-950 p-6 text-white shadow-sm">
        <div className="flex flex-col justify-between gap-6 lg:flex-row lg:items-center">
          <div>
            <p className="text-sm font-medium text-blue-300">Warehouse overview</p>
            <h2 className="mt-2 text-3xl font-bold tracking-tight">Operational control center</h2>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-300">
              Monitor orders, inventory, shipments and incidents from one business-focused dashboard.
            </p>
          </div>

          <div className="rounded-2xl border border-slate-800 bg-slate-900 p-4">
            <div className="flex items-center gap-3">
              <PackageCheck className="text-blue-300" size={28} />
              <div>
                <p className="text-sm text-slate-400">Backend status</p>
                <p className="text-base font-semibold text-white">API layer ready</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {summaryCards.map((card) => {
          const Icon = card.icon;

          return (
            <article key={card.title} className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="text-sm font-medium text-slate-500">{card.title}</p>
                  <p className="mt-3 text-3xl font-bold text-slate-950">{card.value}</p>
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
              <p className="mt-1 text-sm text-slate-500">Static dashboard data for the first frontend version.</p>
            </div>

            <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-medium text-slate-600">
              Demo data
            </span>
          </div>

          <div className="h-80">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={orderStatusData}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                <XAxis dataKey="status" />
                <YAxis allowDecimals={false} />
                <Tooltip />
                <Bar dataKey="count" fill="#2563eb" radius={[8, 8, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </article>

        <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-5 flex items-center justify-between">
            <div>
              <h3 className="text-base font-semibold text-slate-950">Recent activity</h3>
              <p className="mt-1 text-sm text-slate-500">Latest operational changes</p>
            </div>

            <ArrowUpRight size={18} className="text-slate-400" />
          </div>

          <div className="space-y-4">
            {recentActivities.map((activity) => (
              <div key={activity.title} className="rounded-xl border border-slate-100 bg-slate-50 p-4">
                <div className="flex items-start justify-between gap-3">
                  <p className="text-sm font-semibold text-slate-950">{activity.title}</p>
                  <span className="text-xs text-slate-400">{activity.time}</span>
                </div>
                <p className="mt-2 text-sm leading-5 text-slate-500">{activity.description}</p>
              </div>
            ))}
          </div>
        </article>
      </section>
    </div>
  );
}
