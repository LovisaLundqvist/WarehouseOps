import type { Order } from "../types/order";
import type { Shipment } from "../types/shipment";

export type WorkflowAction = { status: string; label: string; description: string };

// Presentation guidance only. The API is authoritative and revalidates every change.
export function getOrderWorkflow(order: Order, shipment?: Shipment) {
  if (order.status === "Cancelled") {
    return { message: "This order was cancelled.", actions: [] as WorkflowAction[], canCancel: false };
  }

  if (order.status === "Completed") {
    return { message: "This order has been delivered and completed.", actions: [] as WorkflowAction[], canCancel: false };
  }

  if (order.status === "Shipped") {
    return { message: "The shipment is on its way. Complete the order by marking its shipment as delivered.", actions: [] as WorkflowAction[], canCancel: false };
  }

  if (order.status === "Packed") {
    return {
      message: shipment
        ? shipment.status === "Cancelled"
          ? "The shipment was cancelled. You can now cancel the order, or review the existing shipment."
          : "Manage the shipment to dispatch this packed order. The order will update automatically."
        : "This order is ready for a shipment. Create a shipment to continue.",
      actions: [] as WorkflowAction[],
      canCancel: !shipment || shipment.status === "Cancelled",
    };
  }

  if (order.status === "Pending") {
    return {
      message: "The order has been registered. Start processing to prepare it for packing.",
      actions: [{ status: "Processing", label: "Start processing", description: "Begin preparing this order." }],
      canCancel: !shipment || shipment.status === "Cancelled",
    };
  }

  if (order.status === "Processing") {
    return {
      message: "The order is being processed. Mark it as packed when all items are ready.",
      actions: [{ status: "Packed", label: "Mark as packed", description: "Confirm the order is packed." }],
      canCancel: !shipment || shipment.status === "Cancelled",
    };
  }

  return { message: "No actions are available for this status.", actions: [] as WorkflowAction[], canCancel: false };
}

export function getShipmentWorkflow(shipment: Shipment): WorkflowAction[] {
  switch (shipment.status) {
    case "Pending":
      return [
        { status: "Packed", label: "Mark as packed", description: "Confirm the shipment is packed." },
        { status: "Delayed", label: "Mark as delayed", description: "Record a delay before dispatch." },
        { status: "Cancelled", label: "Cancel shipment", description: "Stop this shipment before dispatch." },
      ];
    case "Packed":
      return [
        { status: "Shipped", label: "Dispatch shipment", description: "Automatically marks the order as shipped." },
        { status: "Delayed", label: "Mark as delayed", description: "Record a delay in dispatch." },
        { status: "Cancelled", label: "Cancel shipment", description: "Stop this shipment before dispatch." },
      ];
    case "Shipped":
      return [
        { status: "Delivered", label: "Confirm delivery", description: "Automatically completes the order." },
        { status: "Delayed", label: "Mark as delayed", description: "Track a shipment delayed in transit." },
      ];
    case "Delayed":
      return shipment.shippedDate
        ? [{ status: "Shipped", label: "Resume transit", description: "Return the shipment to in-transit status." }]
        : [
            { status: "Packed", label: "Confirm packing after delay", description: "Reconfirm packing before the shipment can be dispatched." },
            { status: "Cancelled", label: "Cancel shipment", description: "Only possible before dispatch." },
          ];
    default:
      return [];
  }
}

export function badgeClass(status: string) {
  if (status === "Cancelled") return "border-red-200 bg-red-50 text-red-800";
  if (status === "Delivered" || status === "Completed") return "border-green-200 bg-green-50 text-green-800";
  if (status === "Delayed") return "border-amber-200 bg-amber-50 text-amber-800";
  if (status === "Shipped" || status === "Processing") return "border-blue-200 bg-blue-50 text-blue-800";
  return "border-slate-200 bg-slate-100 text-slate-800";
}

export function shortId(id: string, label: string) {
  return `${label} #${id.slice(0, 8).toUpperCase()}`;
}

export const currency = new Intl.NumberFormat("sv-SE", { style: "currency", currency: "SEK" });
