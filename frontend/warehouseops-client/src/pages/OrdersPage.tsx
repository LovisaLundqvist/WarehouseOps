import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AlertTriangle,
  ChevronDown,
  ChevronUp,
  ClipboardList,
  Package,
  ReceiptText,
  Settings,
  User,
  X,
} from "lucide-react";
import { cancelOrder, getOrders, updateOrderStatus } from "../api/ordersApi";
import type { Order } from "../types/order";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";

const currencyFormatter = new Intl.NumberFormat("sv-SE", {
  style: "currency",
  currency: "SEK",
});

const orderStatuses = [
  "Pending",
  "Processing",
  "Packed",
  "Shipped",
  "Completed",
  "Cancelled",
];

function getStatusBadgeClass(status: string) {
  switch (status) {
    case "Pending":
      return "bg-slate-100 text-slate-700";
    case "Processing":
      return "bg-blue-50 text-blue-700";
    case "Packed":
      return "bg-indigo-50 text-indigo-700";
    case "Shipped":
      return "bg-amber-50 text-amber-700";
    case "Completed":
      return "bg-green-50 text-green-700";
    case "Cancelled":
      return "bg-red-50 text-red-700";
    default:
      return "bg-slate-100 text-slate-700";
  }
}

function getActiveOrdersCount(orders: Order[]) {
  return orders.filter(
    (order) => order.status !== "Completed" && order.status !== "Cancelled",
  ).length;
}

function canCancelOrder(order: Order) {
  return order.status !== "Shipped" && order.status !== "Completed" && order.status !== "Cancelled";
}

export default function OrdersPage() {
  const queryClient = useQueryClient();

  const [expandedOrderId, setExpandedOrderId] = useState<string | null>(null);
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);
  const [selectedStatus, setSelectedStatus] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  const {
    data: orders = [],
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ["orders"],
    queryFn: getOrders,
  });

  const updateStatusMutation = useMutation({
    mutationFn: (data: { id: string; status: string }) =>
      updateOrderStatus(data.id, { status: data.status }),
    onSuccess: (updatedOrder) => {
      setSuccessMessage(`Order status was updated to ${updatedOrder.status}.`);
      setSelectedOrder(updatedOrder);
      setSelectedStatus(updatedOrder.status);
      queryClient.invalidateQueries({ queryKey: ["orders"] });
    },
  });

  const cancelOrderMutation = useMutation({
    mutationFn: cancelOrder,
    onSuccess: (cancelledOrder) => {
      setSuccessMessage("Order was cancelled.");
      setSelectedOrder(cancelledOrder);
      setSelectedStatus(cancelledOrder.status);
      queryClient.invalidateQueries({ queryKey: ["orders"] });
    },
  });

  const activeOrdersCount = getActiveOrdersCount(orders);
  const completedOrdersCount = orders.filter((order) => order.status === "Completed").length;
  const totalOrderValue = orders.reduce((sum, order) => sum + order.totalAmount, 0);

  const errorMessage = getApiErrorMessage(error, "Could not load orders.");

  const actionErrorMessage = getApiErrorMessage(
    updateStatusMutation.error ?? cancelOrderMutation.error,
    "Could not update order.",
  );

  const hasActionError = updateStatusMutation.isError || cancelOrderMutation.isError;
  const isActionPending = updateStatusMutation.isPending || cancelOrderMutation.isPending;

  function toggleOrderDetails(orderId: string) {
    setExpandedOrderId((current) => (current === orderId ? null : orderId));
  }

  function handleSelectOrder(order: Order) {
    setSuccessMessage("");
    updateStatusMutation.reset();
    cancelOrderMutation.reset();

    setSelectedOrder(order);
    setSelectedStatus(order.status);

    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function handleClearSelectedOrder() {
    setSuccessMessage("");
    updateStatusMutation.reset();
    cancelOrderMutation.reset();

    setSelectedOrder(null);
    setSelectedStatus("");
  }

  function handleUpdateStatus() {
    if (!selectedOrder || !selectedStatus) {
      return;
    }

    setSuccessMessage("");
    updateStatusMutation.reset();
    cancelOrderMutation.reset();

    updateStatusMutation.mutate({
      id: selectedOrder.id,
      status: selectedStatus,
    });
  }

  function handleCancelOrder() {
    if (!selectedOrder) {
      return;
    }

    const confirmed = window.confirm(
      `Are you sure you want to cancel order ${selectedOrder.id}?`,
    );

    if (!confirmed) {
      return;
    }

    setSuccessMessage("");
    updateStatusMutation.reset();
    cancelOrderMutation.reset();

    cancelOrderMutation.mutate(selectedOrder.id);
  }

  return (
    <div className="space-y-6">
      <section className="flex flex-col justify-between gap-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm lg:flex-row lg:items-center">
        <div>
          <div className="flex items-center gap-3">
            <div className="rounded-xl bg-blue-50 p-3 text-blue-600">
              <ClipboardList size={24} />
            </div>

            <div>
              <p className="text-sm font-medium text-blue-600">Order management</p>
              <h2 className="text-2xl font-bold text-slate-950">Orders</h2>
            </div>
          </div>

          <p className="mt-4 max-w-2xl text-sm leading-6 text-slate-500">
            View customer orders, inspect order lines, update order status and cancel orders when business rules allow it.
          </p>
        </div>

        <div className="grid gap-3 sm:grid-cols-3">
          <div className="rounded-2xl bg-slate-50 px-5 py-4">
            <p className="text-sm font-medium text-slate-500">Orders</p>
            <p className="mt-1 text-2xl font-bold text-slate-950">{orders.length}</p>
          </div>

          <div className="rounded-2xl bg-blue-50 px-5 py-4">
            <p className="text-sm font-medium text-blue-700">Active</p>
            <p className="mt-1 text-2xl font-bold text-blue-700">{activeOrdersCount}</p>
          </div>

          <div className="rounded-2xl bg-green-50 px-5 py-4">
            <p className="text-sm font-medium text-green-700">Completed</p>
            <p className="mt-1 text-2xl font-bold text-green-700">{completedOrdersCount}</p>
          </div>
        </div>
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
          <div className="flex items-center gap-3">
            <div className="rounded-xl bg-slate-50 p-3 text-slate-600">
              <ReceiptText size={20} />
            </div>

            <div>
              <h3 className="text-base font-semibold text-slate-950">Order summary</h3>
              <p className="mt-1 text-sm text-slate-500">
                Status transitions are validated by the ASP.NET Core backend.
              </p>
            </div>
          </div>

          <div className="rounded-2xl bg-slate-50 px-5 py-4">
            <p className="text-sm font-medium text-slate-500">Total order value</p>
            <p className="mt-1 text-2xl font-bold text-slate-950">
              {currencyFormatter.format(totalOrderValue)}
            </p>
          </div>
        </div>
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="mb-5 flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="rounded-xl bg-blue-50 p-3 text-blue-600">
              <Settings size={20} />
            </div>

            <div>
              <h3 className="text-base font-semibold text-slate-950">Order actions</h3>
              <p className="mt-1 text-sm text-slate-500">
                {selectedOrder
                  ? `Managing order ${selectedOrder.id} for ${selectedOrder.customerName}.`
                  : "Select an order from the table to update status or cancel it."}
              </p>
            </div>
          </div>

          {selectedOrder && (
            <button
              type="button"
              onClick={handleClearSelectedOrder}
              className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50"
            >
              <X size={16} />
              Clear
            </button>
          )}
        </div>

        {selectedOrder ? (
          <div className="grid gap-4 lg:grid-cols-[1fr_auto_auto]">
            <div>
              <label className="block">
                <span className="text-sm font-medium text-slate-700">Order status</span>
                <select
                  value={selectedStatus}
                  onChange={(event) => setSelectedStatus(event.target.value)}
                  className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-950 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                >
                  {orderStatuses.map((status) => (
                    <option key={status} value={status}>
                      {status}
                    </option>
                  ))}
                </select>
              </label>

              <p className="mt-2 text-sm text-slate-500">
                Current status:{" "}
                <span className={`rounded-full px-3 py-1 text-xs font-semibold ${getStatusBadgeClass(selectedOrder.status)}`}>
                  {selectedOrder.status}
                </span>
              </p>
            </div>

            <button
              type="button"
              onClick={handleUpdateStatus}
              disabled={isActionPending || selectedStatus === selectedOrder.status}
              className="self-end rounded-xl bg-blue-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-300"
            >
              {updateStatusMutation.isPending ? "Updating..." : "Update status"}
            </button>

            <button
              type="button"
              onClick={handleCancelOrder}
              disabled={isActionPending || !canCancelOrder(selectedOrder)}
              className="self-end rounded-xl bg-red-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-red-700 disabled:cursor-not-allowed disabled:bg-red-300"
            >
              {cancelOrderMutation.isPending ? "Cancelling..." : "Cancel order"}
            </button>

            {successMessage && (
              <div className="rounded-xl border border-green-200 bg-green-50 p-4 text-sm text-green-700 lg:col-span-3">
                {successMessage}
              </div>
            )}

            {hasActionError && (
              <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 lg:col-span-3">
                <div className="flex gap-2">
                  <AlertTriangle size={18} />
                  <span>{actionErrorMessage}</span>
                </div>
              </div>
            )}

            {!canCancelOrder(selectedOrder) && (
              <div className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800 lg:col-span-3">
                This order cannot be cancelled because it is already shipped, completed or cancelled.
              </div>
            )}
          </div>
        ) : (
          <div className="rounded-xl border border-dashed border-slate-300 bg-slate-50 p-5 text-sm text-slate-500">
            No order selected.
          </div>
        )}
      </section>

      <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-200 px-5 py-4">
          <h3 className="text-base font-semibold text-slate-950">Order list</h3>
          <p className="mt-1 text-sm text-slate-500">
            Data is loaded from the ASP.NET Core Orders API.
          </p>
        </div>

        {isLoading && (
          <div className="p-6 text-sm text-slate-500">
            Loading orders...
          </div>
        )}

        {isError && (
          <div className="m-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            {errorMessage}
          </div>
        )}

        {!isLoading && !isError && orders.length === 0 && (
          <div className="p-6 text-sm text-slate-500">
            No orders found.
          </div>
        )}

        {!isLoading && !isError && orders.length > 0 && (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Customer
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Status
                  </th>
                  <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Items
                  </th>
                  <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Total
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Created
                  </th>
                  <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Actions
                  </th>
                </tr>
              </thead>

              <tbody className="divide-y divide-slate-100 bg-white">
                {orders.map((order) => {
                  const isExpanded = expandedOrderId === order.id;

                  return (
                    <tr key={order.id} className="align-top">
                      <td colSpan={6} className="p-0">
                        <div className="grid grid-cols-1 border-b border-slate-100 lg:grid-cols-[1.6fr_1fr_0.7fr_1fr_1fr_1.1fr]">
                          <div className="px-5 py-4">
                            <div className="flex items-start gap-3">
                              <div className="rounded-xl bg-slate-50 p-2 text-slate-600">
                                <User size={18} />
                              </div>

                              <div>
                                <p className="text-sm font-semibold text-slate-950">
                                  {order.customerName}
                                </p>
                                <p className="mt-1 text-xs text-slate-500">{order.id}</p>
                              </div>
                            </div>
                          </div>

                          <div className="px-5 py-4">
                            <span
                              className={`rounded-full px-3 py-1 text-xs font-semibold ${getStatusBadgeClass(
                                order.status,
                              )}`}
                            >
                              {order.status}
                            </span>
                          </div>

                          <div className="px-5 py-4 text-right text-sm font-semibold text-slate-950">
                            {order.items.length}
                          </div>

                          <div className="px-5 py-4 text-right text-sm font-semibold text-slate-950">
                            {currencyFormatter.format(order.totalAmount)}
                          </div>

                          <div className="px-5 py-4 text-sm text-slate-500">
                            {new Date(order.createdAt).toLocaleDateString("sv-SE")}
                          </div>

                          <div className="flex justify-end gap-2 px-5 py-4">
                            <button
                              type="button"
                              onClick={() => handleSelectOrder(order)}
                              className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50"
                            >
                              <Settings size={15} />
                              Manage
                            </button>

                            <button
                              type="button"
                              onClick={() => toggleOrderDetails(order.id)}
                              className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50"
                            >
                              {isExpanded ? <ChevronUp size={15} /> : <ChevronDown size={15} />}
                              {isExpanded ? "Hide" : "View"}
                            </button>
                          </div>
                        </div>

                        {isExpanded && (
                          <div className="border-t border-slate-100 bg-slate-50 px-5 py-4">
                            <div className="mb-3 flex items-center gap-2">
                              <Package size={17} className="text-slate-500" />
                              <h4 className="text-sm font-semibold text-slate-950">Order items</h4>
                            </div>

                            <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white">
                              <table className="min-w-full divide-y divide-slate-200">
                                <thead className="bg-white">
                                  <tr>
                                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                                      Product
                                    </th>
                                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                                      SKU
                                    </th>
                                    <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                                      Quantity
                                    </th>
                                    <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                                      Unit price
                                    </th>
                                    <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                                      Line total
                                    </th>
                                  </tr>
                                </thead>

                                <tbody className="divide-y divide-slate-100">
                                  {order.items.map((item) => (
                                    <tr key={`${order.id}-${item.productId}`}>
                                      <td className="px-4 py-3 text-sm font-medium text-slate-950">
                                        {item.productName}
                                      </td>
                                      <td className="px-4 py-3 text-sm text-slate-600">
                                        {item.productSku}
                                      </td>
                                      <td className="px-4 py-3 text-right text-sm text-slate-700">
                                        {item.quantity}
                                      </td>
                                      <td className="px-4 py-3 text-right text-sm text-slate-700">
                                        {currencyFormatter.format(item.unitPrice)}
                                      </td>
                                      <td className="px-4 py-3 text-right text-sm font-semibold text-slate-950">
                                        {currencyFormatter.format(item.lineTotal)}
                                      </td>
                                    </tr>
                                  ))}
                                </tbody>
                              </table>
                            </div>
                          </div>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}

