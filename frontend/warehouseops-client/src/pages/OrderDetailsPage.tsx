import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, ArrowLeft, ArrowRight, CheckCircle2, Package, Truck } from "lucide-react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { cancelOrder, getOrders, updateOrderStatus } from "../api/ordersApi";
import { getShipments } from "../api/shipmentsApi";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";
import { badgeClass, currency, getOrderWorkflow, shortId } from "../utils/orderShipmentWorkflow";

export default function OrderDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const canManage = user?.role === "Admin" || user?.role === "WarehouseStaff";

  const ordersQuery = useQuery({ queryKey: ["orders"], queryFn: getOrders });
  const shipmentsQuery = useQuery({ queryKey: ["shipments"], queryFn: getShipments });
  const order = ordersQuery.data?.find((item) => item.id === id);
  const shipment = shipmentsQuery.data?.find((item) => item.orderId === id);
  const workflow = order ? getOrderWorkflow(order, shipment) : null;

  async function refresh() {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["orders"] }),
      queryClient.invalidateQueries({ queryKey: ["shipments"] }),
      queryClient.invalidateQueries({ queryKey: ["inventory"] }),
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] }),
    ]);
  }

  const update = useMutation({
    mutationFn: (status: string) => updateOrderStatus(id!, { status }),
    onSuccess: refresh,
  });
  const cancel = useMutation({
    mutationFn: () => cancelOrder(id!),
    onSuccess: refresh,
  });
  const pending = update.isPending || cancel.isPending;
  const actionError = update.error ?? cancel.error;

  return (
    <div className="max-w-5xl space-y-6">
      <Link to="/orders" className="inline-flex items-center gap-2 text-sm font-semibold text-[#3e5d51] hover:underline"><ArrowLeft size={16} /> Back to orders</Link>
      {ordersQuery.isLoading || shipmentsQuery.isLoading ? <p className="border border-slate-200 bg-white p-6 text-sm">Loading order...</p> : null}
      {ordersQuery.isError || shipmentsQuery.isError ? <p role="alert" className="border border-red-200 bg-red-50 p-5 text-sm text-red-800">{getApiErrorMessage(ordersQuery.error ?? shipmentsQuery.error, "Could not load order details.")}</p> : null}
      {!ordersQuery.isLoading && !shipmentsQuery.isLoading && !ordersQuery.isError && !shipmentsQuery.isError && !order && <p className="border border-slate-200 bg-white p-6 text-sm">Order not found.</p>}
      {order && workflow && !ordersQuery.isError && !shipmentsQuery.isError && !ordersQuery.isLoading && !shipmentsQuery.isLoading && (
        <>
          <header className="flex flex-wrap items-start justify-between gap-4 border-b border-slate-300 pb-6">
            <div><p className="ops-eyebrow text-[#60766a]">Orders / {shortId(order.id, "Order")}</p><h1 className="ops-display mt-2 text-4xl text-slate-950">{order.customerName}</h1><p className="mt-2 text-sm text-slate-500">Created {new Date(order.createdAt).toLocaleDateString("sv-SE")}</p></div>
            <span className={`border px-3 py-2 text-xs font-semibold ${badgeClass(order.status)}`}>{order.status}</span>
          </header>

          <div className="grid gap-5 lg:grid-cols-[minmax(0,1.3fr)_minmax(280px,0.7fr)]">
            <section className="border border-slate-200 bg-white p-6">
              <div className="flex items-center gap-3"><Package size={19} className="text-[#3e5d51]" /><h2 className="text-lg font-semibold">Order lines</h2></div>
              <div className="mt-5 divide-y divide-slate-200">
                {order.items.map((item) => <div key={item.productId} className="flex flex-wrap items-center justify-between gap-3 py-4 text-sm"><div><p className="font-semibold text-slate-950">{item.productName}</p><p className="mt-1 text-xs text-slate-500">{item.productSku} · Quantity {item.quantity} · {currency.format(item.unitPrice)} each</p></div><span className="font-semibold">{currency.format(item.lineTotal)}</span></div>)}
              </div>
              <div className="flex items-center justify-between border-t border-slate-300 pt-4"><span className="text-sm font-semibold">Order total</span><span className="ops-display text-2xl">{currency.format(order.totalAmount)}</span></div>
            </section>

            <div className="space-y-5">
              <section className="border border-slate-200 bg-white p-6">
                <div className="flex items-center gap-2"><Truck size={19} className="text-[#3e5d51]" /><h2 className="text-lg font-semibold">Shipment</h2></div>
                {shipment ? <div className="mt-4 space-y-3"><p className="text-sm text-slate-600">{shortId(shipment.id, "Shipment")}</p><span className={`inline-block border px-3 py-1 text-xs font-semibold ${badgeClass(shipment.status)}`}>{shipment.status}</span><p className="text-xs text-slate-500">Tracking: {shipment.trackingNumber}</p><Link to={`/shipments/${shipment.id}`} className="inline-flex items-center gap-2 border border-slate-300 px-4 py-2 text-sm font-semibold hover:bg-slate-100">Open shipment <ArrowRight size={16} /></Link></div>
                  : <div className="mt-4"><p className="text-sm text-slate-600">No shipment registered for this order.</p>{canManage && order.status === "Packed" && <Link to={`/shipments/new?orderId=${encodeURIComponent(order.id)}`} className="mt-4 inline-flex items-center gap-2 bg-[#3e5d51] px-4 py-2 text-sm font-semibold text-white">Create shipment <ArrowRight size={16} /></Link>}</div>}
              </section>
            </div>
          </div>

          <section className="border border-slate-200 bg-white p-6">
            <div className="flex items-center gap-2"><CheckCircle2 size={20} className="text-[#3e5d51]" /><h2 className="text-lg font-semibold">Next step</h2></div>
            <p className="mt-3 max-w-3xl text-sm leading-6 text-slate-600">{workflow.message}</p>
            {canManage && (
              <div className="mt-5 flex flex-wrap gap-3">
                {workflow.actions.map((action) => (
                  <button type="button" key={action.status} disabled={pending} onClick={() => update.mutate(action.status)} title={action.description} className="bg-[#3e5d51] px-5 py-3 text-sm font-semibold text-white hover:bg-[#314a40] disabled:opacity-50">{pending ? "Updating..." : action.label}</button>
                ))}
                {order.status === "Packed" && shipment && shipment.status !== "Cancelled" && <Link to={`/shipments/${shipment.id}`} className="inline-flex items-center gap-2 bg-[#3e5d51] px-5 py-3 text-sm font-semibold text-white">Manage shipment <ArrowRight size={16} /></Link>}
                {workflow.canCancel && <button type="button" disabled={pending} onClick={() => { if (window.confirm("Cancel this order and return reserved stock?")) cancel.mutate(); }} className="border border-red-300 px-5 py-3 text-sm font-semibold text-red-800 hover:bg-red-50 disabled:opacity-50">Cancel order</button>}
              </div>
            )}
            {!canManage && <p className="mt-3 text-xs text-slate-500">Your role has read-only access to this order.</p>}
            {actionError && <p role="alert" className="mt-4 flex items-start gap-2 border border-red-200 bg-red-50 p-4 text-sm text-red-800"><AlertTriangle size={18} className="shrink-0" />{getApiErrorMessage(actionError, "The operation could not be completed.")}</p>}
            {(update.isSuccess || cancel.isSuccess) && !actionError && <p role="status" className="mt-4 text-sm text-green-800">Order updated. The latest status has been loaded.</p>}
          </section>
        </>
      )}
    </div>
  );
}
