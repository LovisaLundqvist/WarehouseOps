import { useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Truck } from "lucide-react";
import { Link, Navigate, useNavigate, useSearchParams } from "react-router-dom";
import { z } from "zod";
import { useAuth } from "../auth/AuthContext";
import { getOrders } from "../api/ordersApi";
import { createShipment, getShipments } from "../api/shipmentsApi";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";
import { shortId } from "../utils/orderShipmentWorkflow";

const schema = z.object({
  orderId: z.guid("Select a packed order."),
  trackingNumber: z.string().trim().min(1, "Tracking number is required.").max(100, "Maximum 100 characters.").refine((text) => Array.from(text).every((char) => char.charCodeAt(0) >= 32 && char.charCodeAt(0) !== 127), "Tracking number contains an invalid character."),
});

export default function NewShipmentPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const preselectedOrderId = params.get("orderId") ?? "";
  const [orderId, setOrderId] = useState(preselectedOrderId);
  const [trackingNumber, setTrackingNumber] = useState("");
  const [formError, setFormError] = useState("");
  const queryClient = useQueryClient();
  const canManage = user?.role === "Admin" || user?.role === "WarehouseStaff";

  const ordersQuery = useQuery({ queryKey: ["orders"], queryFn: getOrders, enabled: canManage });
  const shipmentsQuery = useQuery({ queryKey: ["shipments"], queryFn: getShipments, enabled: canManage });
  const orders = ordersQuery.data ?? [];
  const shipments = shipmentsQuery.data ?? [];
  const linkedOrderIds = new Set(shipments.map((shipment) => shipment.orderId));
  const eligibleOrders = orders.filter((order) => order.status === "Packed" && !linkedOrderIds.has(order.id));

  const mutation = useMutation({
    mutationFn: createShipment,
    onSuccess: async (shipment) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["shipments"] }),
        queryClient.invalidateQueries({ queryKey: ["orders"] }),
        queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] }),
      ]);
      navigate(`/shipments/${shipment.id}`, { replace: true });
    },
  });

  if (!canManage) return <Navigate to="/shipments" replace />;

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError("");
    const result = schema.safeParse({ orderId, trackingNumber });
    if (!result.success) { setFormError(result.error.issues[0]?.message ?? "Invalid shipment."); return; }
    if (!eligibleOrders.some((order) => order.id === result.data.orderId)) {
      setFormError("Only packed orders without an existing shipment can be selected.");
      return;
    }
    mutation.mutate(result.data);
  }

  return (
    <div className="max-w-3xl space-y-6">
      <Link to="/shipments" className="inline-flex items-center gap-2 text-sm font-semibold text-[#3e5d51] hover:underline"><ArrowLeft size={16} /> Back to shipments</Link>
      <header className="border-b border-slate-300 pb-6"><p className="ops-eyebrow text-[#60766a]">Shipments / New shipment</p><h1 className="ops-display mt-2 text-4xl">New shipment</h1><p className="mt-2 text-sm text-slate-600">Attach a shipment to a packed order. Each order can have one shipment.</p></header>
      {ordersQuery.isLoading || shipmentsQuery.isLoading ? <p className="border border-slate-200 bg-white p-6 text-sm">Loading eligible orders...</p> : null}
      {ordersQuery.isError || shipmentsQuery.isError ? <p role="alert" className="border border-red-200 bg-red-50 p-4 text-sm text-red-800">{getApiErrorMessage(ordersQuery.error ?? shipmentsQuery.error, "Could not load orders.")}</p> : null}
      {!ordersQuery.isLoading && !shipmentsQuery.isLoading && !ordersQuery.isError && !shipmentsQuery.isError && (
        <form onSubmit={submit} className="space-y-5 border border-slate-200 bg-white p-6" noValidate>
          <div className="flex items-center gap-2"><Truck size={20} className="text-[#3e5d51]" /><h2 className="font-semibold">Shipment information</h2></div>
          <label className="block text-sm font-semibold" htmlFor="shipment-order">Packed order</label>
          <select id="shipment-order" value={orderId} onChange={(event) => setOrderId(event.target.value)} className="w-full border border-slate-300 bg-white p-3 text-sm" required>
            <option value="">Select packed order</option>
            {eligibleOrders.map((order) => <option key={order.id} value={order.id}>{order.customerName} · {shortId(order.id, "Order")}</option>)}
          </select>
          {eligibleOrders.length === 0 && <p className="text-sm text-amber-800">No packed orders without shipments. Pack an order on the Orders page first.</p>}
          {preselectedOrderId && !eligibleOrders.some((order) => order.id === preselectedOrderId) && <p className="text-sm text-amber-800">The selected order is not currently eligible for a new shipment.</p>}
          <label className="block text-sm font-semibold" htmlFor="shipment-tracking">Tracking number</label>
          <input id="shipment-tracking" value={trackingNumber} onChange={(event) => setTrackingNumber(event.target.value)} placeholder="WH-TRK-1001" maxLength={100} className="w-full border border-slate-300 bg-white p-3 text-sm" required />
          {formError && <p role="alert" className="border border-red-200 bg-red-50 p-4 text-sm text-red-800">{formError}</p>}
          {mutation.isError && <p role="alert" className="border border-red-200 bg-red-50 p-4 text-sm text-red-800">{getApiErrorMessage(mutation.error, "Could not create shipment.")}</p>}
          <div className="flex flex-wrap justify-end gap-3"><Link to="/shipments" className="border border-slate-300 px-5 py-3 text-sm font-semibold">Cancel</Link><button type="submit" disabled={mutation.isPending || eligibleOrders.length === 0} className="bg-[#3e5d51] px-5 py-3 text-sm font-semibold text-white disabled:opacity-50">{mutation.isPending ? "Creating..." : "Create shipment"}</button></div>
        </form>
      )}
    </div>
  );
}
