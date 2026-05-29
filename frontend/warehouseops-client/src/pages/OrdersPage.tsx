import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  ChevronDown,
  ChevronUp,
  ClipboardList,
  Package,
  ReceiptText,
  User,
} from "lucide-react";
import { getOrders } from "../api/ordersApi";
import type { Order } from "../types/order";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";

const currencyFormatter = new Intl.NumberFormat("sv-SE", {
  style: "currency",
  currency: "SEK",
});

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

export default function OrdersPage() {
  const [expandedOrderId, setExpandedOrderId] = useState<string | null>(null);

  const {
    data: orders = [],
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ["orders"],
    queryFn: getOrders,
  });

  const activeOrdersCount = getActiveOrdersCount(orders);
  const completedOrdersCount = orders.filter((order) => order.status === "Completed").length;
  const totalOrderValue = orders.reduce((sum, order) => sum + order.totalAmount, 0);

  const errorMessage = getApiErrorMessage(error, "Could not load orders.");

  function toggleOrderDetails(orderId: string) {
    setExpandedOrderId((current) => (current === orderId ? null : orderId));
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
            View customer orders, order status, total value and order lines from the backend API.
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
                Read-only overview. Status changes and cancel actions will be added after the list is stable.
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
                    Details
                  </th>
                </tr>
              </thead>

              <tbody className="divide-y divide-slate-100 bg-white">
                {orders.map((order) => {
                  const isExpanded = expandedOrderId === order.id;

                  return (
                    <tr key={order.id} className="align-top">
                      <td colSpan={6} className="p-0">
                        <div className="grid grid-cols-1 border-b border-slate-100 lg:grid-cols-[1.6fr_1fr_0.7fr_1fr_1fr_0.8fr]">
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

                          <div className="px-5 py-4 text-right">
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
