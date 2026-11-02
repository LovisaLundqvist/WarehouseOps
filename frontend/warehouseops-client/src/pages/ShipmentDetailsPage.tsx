import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, ArrowLeft, ArrowRight, CheckCircle2, Truck } from "lucide-react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { getShipments, updateShipmentStatus } from "../api/shipmentsApi";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";
import { badgeClass, getShipmentWorkflow, shortId } from "../utils/orderShipmentWorkflow";

function displayDate(value: string | null) {
  return value ? new Date(value).toLocaleString("sv-SE") : "Not yet recorded";
}

export default function ShipmentDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const canManage = user?.role === "Admin" || user?.role === "WarehouseStaff";
  const query = useQuery({ queryKey: ["shipments"], queryFn: getShipments });
  const shipment = query.data?.find((entry) => entry.id === id);
  const actions = shipment ? getShipmentWorkflow(shipment) : [];

  const mutation = useMutation({
    mutationFn: (status: string) => updateShipmentStatus(id!, { status }),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["shipments"] }),
        queryClient.invalidateQueries({ queryKey: ["orders"] }),
        queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] }),
      ]);
    },
  });

  function applyStatus(status: string) {
    if (status === "Cancelled" && !window.confirm("Cancel this shipment? The order will not be cancelled automatically.")) return;
    mutation.mutate(status);
  }

  return (
    <div className="max-w-4xl space-y-6">
      <Link to="/shipments" className="inline-flex items-center gap-2 text-sm font-semibold text-[#3e5d51] hover:underline"><ArrowLeft size={16} /> Back to shipments</Link>
      {query.isLoading && <p className="border border-slate-200 bg-white p-6 text-sm">Loading shipment...</p>}
      {query.isError && <p role="alert" className="border border-red-200 bg-red-50 p-5 text-sm text-red-800">{getApiErrorMessage(query.error, "Could not load shipment.")}</p>}
      {!query.isLoading && !query.isError && !shipment && <p className="border border-slate-200 bg-white p-6 text-sm">Shipment not found.</p>}
      {shipment && (
        <>
          <header className="flex flex-wrap items-start justify-between gap-4 border-b border-slate-300 pb-6"><div><p className="ops-eyebrow text-[#60766a]">Shipments / {shortId(shipment.id, "Shipment")}</p><h1 className="ops-display mt-2 text-4xl">{shipment.customerName}</h1><p className="mt-2 text-sm text-slate-600">Tracking number: {shipment.trackingNumber}</p></div><span className={`border px-3 py-2 text-xs font-semibold ${badgeClass(shipment.status)}`}>{shipment.status}</span></header>
          <section className="border border-slate-200 bg-white p-6">
            <div className="flex items-center gap-2"><Truck size={20} className="text-[#3e5d51]" /><h2 className="text-lg font-semibold">Delivery information</h2></div>
            <dl className="mt-5 grid gap-6 sm:grid-cols-2">
              <div><dt className="text-xs uppercase tracking-widest text-slate-500">Related order</dt><dd className="mt-2"><Link to={`/orders/${shipment.orderId}`} className="inline-flex items-center gap-2 text-sm font-semibold text-[#3e5d51] hover:underline">{shortId(shipment.orderId, "Order")} <ArrowRight size={15} /></Link></dd></div>
              <div><dt className="text-xs uppercase tracking-widest text-slate-500">Order status</dt><dd className="mt-2 text-sm font-semibold">{shipment.orderStatus}</dd></div>
              <div><dt className="text-xs uppercase tracking-widest text-slate-500">Dispatched</dt><dd className="mt-2 text-sm">{displayDate(shipment.shippedDate)}</dd></div>
              <div><dt className="text-xs uppercase tracking-widest text-slate-500">Delivered</dt><dd className="mt-2 text-sm">{displayDate(shipment.deliveredDate)}</dd></div>
            </dl>
          </section>
          <section className="border border-slate-200 bg-white p-6">
            <div className="flex items-center gap-2"><CheckCircle2 size={20} className="text-[#3e5d51]" /><h2 className="text-lg font-semibold">Next step</h2></div>
            {shipment.status === "Delivered" && <p className="mt-3 text-sm text-slate-600">Delivery confirmed. The order should now be completed automatically.</p>}
            {shipment.status === "Cancelled" && <p className="mt-3 text-sm text-slate-600">Shipment cancelled. Open its order to complete the order cancellation if necessary.</p>}
            {actions.length > 0 && <p className="mt-3 text-sm text-slate-600">Choose one of the actions currently allowed for this shipment. The API checks all status transitions again.</p>}
            {canManage ? <div className="mt-5 flex flex-wrap gap-3">
              {actions.map((action) => <button key={action.status} type="button" title={action.description} disabled={mutation.isPending} onClick={() => applyStatus(action.status)} className={action.status === "Cancelled" ? "border border-red-300 px-5 py-3 text-sm font-semibold text-red-800 hover:bg-red-50 disabled:opacity-50" : action.status === "Delayed" ? "border border-amber-300 px-5 py-3 text-sm font-semibold text-amber-900 hover:bg-amber-50 disabled:opacity-50" : "bg-[#3e5d51] px-5 py-3 text-sm font-semibold text-white hover:bg-[#314a40] disabled:opacity-50"}>{mutation.isPending ? "Updating..." : action.label}</button>)}
              {shipment.status === "Cancelled" && <Link to={`/orders/${shipment.orderId}`} className="inline-flex items-center gap-2 border border-slate-300 px-5 py-3 text-sm font-semibold">Open order <ArrowRight size={16} /></Link>}
            </div> : <p className="mt-4 text-xs text-slate-500">Your role has read-only access to shipments.</p>}
            {mutation.isError && <p role="alert" className="mt-4 flex items-start gap-2 border border-red-200 bg-red-50 p-4 text-sm text-red-800"><AlertTriangle size={18} className="shrink-0" />{getApiErrorMessage(mutation.error, "Shipment could not be updated.")}</p>}
            {mutation.isSuccess && !mutation.isError && <p role="status" className="mt-4 text-sm text-green-800">Shipment updated. The related order status has been refreshed.</p>}
          </section>
        </>
      )}
    </div>
  );
}
