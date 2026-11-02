import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, Plus, Search, Truck } from "lucide-react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { getShipments } from "../api/shipmentsApi";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";
import { badgeClass, shortId } from "../utils/orderShipmentWorkflow";

const statuses = ["All", "Pending", "Packed", "Shipped", "Delayed", "Delivered", "Cancelled"];

export default function ShipmentsPage() {
  const { user } = useAuth();
  const canManage = user?.role === "Admin" || user?.role === "WarehouseStaff";
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("All");
  const { data: shipments = [], isLoading, isError, error } = useQuery({ queryKey: ["shipments"], queryFn: getShipments });

  const term = search.trim().toLowerCase();
  const filtered = shipments.filter((shipment) => (
    (status === "All" || shipment.status === status)
    && (!term || shipment.customerName.toLowerCase().includes(term)
      || shipment.trackingNumber.toLowerCase().includes(term)
      || shipment.orderId.toLowerCase().includes(term))
  ));
  const active = shipments.filter((item) => item.status !== "Cancelled" && item.status !== "Delivered").length;
  const delayed = shipments.filter((item) => item.status === "Delayed").length;
  const delivered = shipments.filter((item) => item.status === "Delivered").length;

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-end justify-between gap-4 border-b border-slate-300 pb-6">
        <div><p className="ops-eyebrow text-[#60766a]">Operations / Shipments</p><h1 className="ops-display mt-2 text-4xl">Shipments</h1><p className="mt-2 text-sm text-slate-600">Track deliveries and take the next valid action for each shipment.</p></div>
        {canManage && <Link to="/shipments/new" className="inline-flex items-center gap-2 bg-[#3e5d51] px-5 py-3 text-sm font-semibold text-white hover:bg-[#314a40]"><Plus size={17} /> New shipment</Link>}
      </header>
      <div className="grid gap-3 sm:grid-cols-3">
        {[{ label: "Active shipments", value: active }, { label: "Delayed", value: delayed }, { label: "Delivered", value: delivered }].map((item) => (
          <div key={item.label} className="border border-slate-200 bg-white p-5"><p className="text-xs font-semibold uppercase tracking-widest text-slate-500">{item.label}</p><p className="ops-display mt-3 text-4xl">{item.value}</p></div>
        ))}
      </div>
      <section className="border border-slate-200 bg-white">
        <div className="flex flex-wrap items-center justify-between gap-4 border-b border-slate-200 p-5">
          <div className="flex items-center gap-2"><Truck size={19} className="text-[#3e5d51]" /><div><h2 className="font-semibold">Shipment list</h2><p className="text-xs text-slate-500">{filtered.length} of {shipments.length} shipments</p></div></div>
          <div className="flex w-full flex-wrap gap-3 sm:w-auto">
            <label className="flex min-w-0 flex-1 items-center gap-2 border border-slate-300 px-3 sm:w-64"><Search size={16} className="shrink-0 text-slate-500" /><span className="sr-only">Search shipments</span><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Customer, tracking or order" className="min-w-0 flex-1 bg-transparent py-3 text-sm outline-none" /></label>
            <label><span className="sr-only">Filter shipment status</span><select className="h-full border border-slate-300 bg-white px-3 text-sm" value={status} onChange={(event) => setStatus(event.target.value)}>{statuses.map((value) => <option value={value} key={value}>{value === "All" ? "All statuses" : value}</option>)}</select></label>
          </div>
        </div>
        {isLoading && <p className="p-6 text-sm text-slate-500">Loading shipments...</p>}
        {isError && <p role="alert" className="m-5 border border-red-200 bg-red-50 p-4 text-sm text-red-800">{getApiErrorMessage(error, "Could not load shipments.")}</p>}
        {!isLoading && !isError && filtered.length === 0 && <p className="p-6 text-sm text-slate-500">No shipments match the selected filters.</p>}
        {!isLoading && !isError && filtered.length > 0 && (
          <div className="divide-y divide-slate-200">
            {filtered.map((shipment) => <article key={shipment.id} className="flex flex-wrap items-center justify-between gap-4 p-5 hover:bg-[#f1f2eb]">
              <div className="min-w-0 flex-1"><Link to={`/shipments/${shipment.id}`} className="text-sm font-semibold hover:underline">{shipment.customerName}</Link><p className="mt-1 text-xs text-slate-500">{shortId(shipment.id, "Shipment")} · {shortId(shipment.orderId, "Order")}</p><p className="mt-1 text-xs text-slate-600">Tracking: {shipment.trackingNumber}</p></div>
              <span className={`border px-2.5 py-1 text-xs font-semibold ${badgeClass(shipment.status)}`}>{shipment.status}</span>
              <div className="hidden text-xs text-slate-500 lg:block"><p>Created {new Date(shipment.createdAt).toLocaleDateString("sv-SE")}</p><p>Order: {shipment.orderStatus}</p></div>
              <Link to={`/shipments/${shipment.id}`} className="inline-flex shrink-0 items-center gap-2 border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-800 hover:bg-slate-100">Open shipment <ArrowRight size={16} /></Link>
            </article>)}
          </div>
        )}
      </section>
    </div>
  );
}
