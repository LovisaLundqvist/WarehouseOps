import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, ClipboardList, Plus, Search } from "lucide-react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { getOrders } from "../api/ordersApi";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";
import { badgeClass, currency, shortId } from "../utils/orderShipmentWorkflow";

const filters = ["All", "Pending", "Processing", "Packed", "Shipped", "Completed", "Cancelled"];

export default function OrdersPage() {
  const { user } = useAuth();
  const canManage = user?.role === "Admin" || user?.role === "WarehouseStaff";
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("All");

  const { data: orders = [], isLoading, isError, error } = useQuery({
    queryKey: ["orders"],
    queryFn: getOrders,
  });

  const term = search.trim().toLowerCase();
  const shown = orders.filter((order) => {
    const matchesSearch = !term || order.customerName.toLowerCase().includes(term)
      || order.id.toLowerCase().includes(term);
    return matchesSearch && (status === "All" || order.status === status);
  });

  const pending = orders.filter((order) => order.status === "Pending").length;
  const ready = orders.filter((order) => order.status === "Packed").length;
  const active = orders.filter((order) => order.status !== "Completed" && order.status !== "Cancelled").length;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4 border-b border-slate-300 pb-6">
        <div>
          <p className="ops-eyebrow text-[#60766a]">Operations / Orders</p>
          <h1 className="ops-display mt-2 text-4xl text-slate-950">Orders</h1>
          <p className="mt-2 text-sm text-slate-600">Find an order, review its progress and continue the next valid step.</p>
        </div>
        {canManage && (
          <Link to="/orders/new" className="inline-flex items-center gap-2 bg-[#3e5d51] px-5 py-3 text-sm font-semibold text-white hover:bg-[#314a40]">
            <Plus size={17} /> New order
          </Link>
        )}
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        {[
          { label: "Active orders", value: active },
          { label: "Awaiting processing", value: pending },
          { label: "Ready for shipment", value: ready },
        ].map((item) => (
          <div key={item.label} className="border border-slate-200 bg-white p-5">
            <p className="text-xs font-semibold uppercase tracking-widest text-slate-500">{item.label}</p>
            <p className="ops-display mt-3 text-4xl text-slate-950">{item.value}</p>
          </div>
        ))}
      </div>

      <section className="border border-slate-200 bg-white">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-200 p-5">
          <div className="flex items-center gap-2">
            <ClipboardList size={19} className="text-[#527266]" />
            <div>
              <h2 className="font-semibold text-slate-950">Order list</h2>
              <p className="text-xs text-slate-500">{shown.length} of {orders.length} orders</p>
            </div>
          </div>
          <div className="flex w-full flex-wrap gap-3 sm:w-auto">
            <label className="flex min-w-0 flex-1 items-center gap-2 border border-slate-300 bg-white px-3 sm:w-64">
              <Search size={16} className="shrink-0 text-slate-500" />
              <span className="sr-only">Search orders or customers</span>
              <input className="w-full min-w-0 bg-transparent py-3 text-sm outline-none" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Customer or order ID" />
            </label>
            <label>
              <span className="sr-only">Filter by status</span>
              <select value={status} onChange={(event) => setStatus(event.target.value)} className="h-full border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800">
                {filters.map((value) => <option key={value} value={value}>{value === "All" ? "All statuses" : value}</option>)}
              </select>
            </label>
          </div>
        </div>

        {isLoading && <p className="p-6 text-sm text-slate-500">Loading orders...</p>}
        {isError && <p role="alert" className="m-5 border border-red-200 bg-red-50 p-4 text-sm text-red-700">{getApiErrorMessage(error, "Could not load orders.")}</p>}
        {!isLoading && !isError && shown.length === 0 && <p className="p-6 text-sm text-slate-500">No orders match the selected filters.</p>}
        {!isLoading && !isError && shown.length > 0 && (
          <div className="divide-y divide-slate-200">
            {shown.map((order) => (
              <article key={order.id} className="flex flex-wrap items-center justify-between gap-4 p-5 hover:bg-[#f1f2eb]">
                <div className="min-w-0 flex-1">
                  <Link to={`/orders/${order.id}`} className="font-semibold text-slate-950 hover:underline">{order.customerName}</Link>
                  <p className="mt-1 text-xs text-slate-500">{shortId(order.id, "Order")} · {new Date(order.createdAt).toLocaleDateString("sv-SE")}</p>
                </div>
                <span className={`border px-2.5 py-1 text-xs font-semibold ${badgeClass(order.status)}`}>{order.status}</span>
                <div className="text-right sm:w-36">
                  <p className="text-sm font-semibold text-slate-900">{currency.format(order.totalAmount)}</p>
                  <p className="text-xs text-slate-500">{order.items.length} order lines</p>
                </div>
                <Link to={`/orders/${order.id}`} className="inline-flex shrink-0 items-center gap-2 border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-800 hover:bg-slate-100" aria-label={`Open order for ${order.customerName}`}>
                  Open order <ArrowRight size={16} />
                </Link>
              </article>
            ))}
          </div>
        )}
      </section>
      <p className="text-xs leading-5 text-slate-500">Amounts are registered order values, not recognized revenue. Cancelled orders remain visible for traceability.</p>
    </div>
  );
}
