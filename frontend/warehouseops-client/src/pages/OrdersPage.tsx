import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AlertTriangle,
  ChevronDown,
  ChevronUp,
  ClipboardList,
  Package,
  Plus,
  ReceiptText,
  Settings,
  ShoppingCart,
  Trash2,
  User,
  X,
} from "lucide-react";
import { z } from "zod";
import { getCustomers } from "../api/customersApi";
import { createOrder, cancelOrder, getOrders, updateOrderStatus } from "../api/ordersApi";
import { getProducts } from "../api/productsApi";
import type { Order } from "../types/order";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";
import { formatShortId } from "../utils/formatShortId";

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

const createOrderSchema = z.object({
  customerId: z.string().trim().min(1, "Customer is required."),
  items: z
    .array(
      z.object({
        productId: z.string().trim().min(1, "Product is required."),
        quantity: z.number().int().min(1, "Quantity must be greater than zero."),
      }),
    )
    .min(1, "An order must contain at least one order item.")
    .superRefine((items, context) => {
      const productIds = new Set<string>();

      items.forEach((item, index) => {
        if (!item.productId) {
          return;
        }

        if (productIds.has(item.productId)) {
          context.addIssue({
            code: "custom",
            message: "The same product cannot be added more than once.",
            path: [index, "productId"],
          });
        }

        productIds.add(item.productId);
      });
    }),
});

type CreateOrderFormValues = z.infer<typeof createOrderSchema>;

const initialCreateOrderFormValues: CreateOrderFormValues = {
  customerId: "",
  items: [
    {
      productId: "",
      quantity: 1,
    },
  ],
};

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

function mapValidationErrors(error: z.ZodError<CreateOrderFormValues>) {
  const validationErrors: Record<string, string> = {};

  error.issues.forEach((issue) => {
    const path = issue.path.join(".");

    if (path) {
      validationErrors[path] = issue.message;
    }
  });

  return validationErrors;
}

export default function OrdersPage() {
  const queryClient = useQueryClient();

  const [expandedOrderId, setExpandedOrderId] = useState<string | null>(null);
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);
  const [selectedStatus, setSelectedStatus] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [createOrderForm, setCreateOrderForm] = useState<CreateOrderFormValues>(
    initialCreateOrderFormValues,
  );
  const [createOrderValidationErrors, setCreateOrderValidationErrors] = useState<
    Record<string, string>
  >({});

  const {
    data: orders = [],
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ["orders"],
    queryFn: getOrders,
  });

  const {
    data: customers = [],
    isLoading: isLoadingCustomers,
    isError: isCustomersError,
    error: customersError,
  } = useQuery({
    queryKey: ["customers", "order-form"],
    queryFn: () => getCustomers(),
  });

  const {
    data: products = [],
    isLoading: isLoadingProducts,
    isError: isProductsError,
    error: productsError,
  } = useQuery({
    queryKey: ["products", "order-form"],
    queryFn: () => getProducts(),
  });

  const createOrderMutation = useMutation({
    mutationFn: createOrder,
    onSuccess: (createdOrder) => {
      setSuccessMessage(`Order was created for ${createdOrder.customerName}.`);
      setCreateOrderForm(initialCreateOrderFormValues);
      setCreateOrderValidationErrors({});
      setSelectedOrder(createdOrder);
      setSelectedStatus(createdOrder.status);
      queryClient.invalidateQueries({ queryKey: ["orders"] });
      queryClient.invalidateQueries({ queryKey: ["inventory"] });
    },
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
      queryClient.invalidateQueries({ queryKey: ["inventory"] });
    },
  });

  const activeOrdersCount = getActiveOrdersCount(orders);
  const completedOrdersCount = orders.filter((order) => order.status === "Completed").length;
  const totalOrderValue = orders.reduce((sum, order) => sum + order.totalAmount, 0);

  const estimatedOrderTotal = createOrderForm.items.reduce((sum, item) => {
    const product = products.find((currentProduct) => currentProduct.id === item.productId);

    if (!product) {
      return sum;
    }

    return sum + product.price * item.quantity;
  }, 0);

  const errorMessage = getApiErrorMessage(error, "Could not load orders.");

  const customerLoadErrorMessage = getApiErrorMessage(
    customersError,
    "Could not load customers for the order form.",
  );

  const productLoadErrorMessage = getApiErrorMessage(
    productsError,
    "Could not load products for the order form.",
  );

  const createOrderErrorMessage = getApiErrorMessage(
    createOrderMutation.error,
    "Could not create order.",
  );

  const actionErrorMessage = getApiErrorMessage(
    updateStatusMutation.error ?? cancelOrderMutation.error,
    "Could not update order.",
  );

  const hasActionError = updateStatusMutation.isError || cancelOrderMutation.isError;
  const isActionPending = updateStatusMutation.isPending || cancelOrderMutation.isPending;
  const isCreateOrderPending = createOrderMutation.isPending;
  const isCreateOrderDataLoading = isLoadingCustomers || isLoadingProducts;
  const hasCreateOrderDataError = isCustomersError || isProductsError;

  function toggleOrderDetails(orderId: string) {
    setExpandedOrderId((current) => (current === orderId ? null : orderId));
  }

  function handleSelectOrder(order: Order) {
    setSuccessMessage("");
    createOrderMutation.reset();
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

  function handleCreateOrderCustomerChange(customerId: string) {
    setCreateOrderForm((current) => ({
      ...current,
      customerId,
    }));
  }

  function handleCreateOrderItemProductChange(index: number, productId: string) {
    setCreateOrderForm((current) => ({
      ...current,
      items: current.items.map((item, itemIndex) =>
        itemIndex === index
          ? {
              ...item,
              productId,
            }
          : item,
      ),
    }));
  }

  function handleCreateOrderItemQuantityChange(index: number, quantity: number) {
    setCreateOrderForm((current) => ({
      ...current,
      items: current.items.map((item, itemIndex) =>
        itemIndex === index
          ? {
              ...item,
              quantity,
            }
          : item,
      ),
    }));
  }

  function handleAddOrderItem() {
    setCreateOrderForm((current) => ({
      ...current,
      items: [
        ...current.items,
        {
          productId: "",
          quantity: 1,
        },
      ],
    }));
  }

  function handleRemoveOrderItem(index: number) {
    setCreateOrderForm((current) => ({
      ...current,
      items:
        current.items.length === 1
          ? current.items
          : current.items.filter((_item, itemIndex) => itemIndex !== index),
    }));
  }

  function handleCreateOrderSubmit() {
    setSuccessMessage("");
    createOrderMutation.reset();
    updateStatusMutation.reset();
    cancelOrderMutation.reset();

    const result = createOrderSchema.safeParse(createOrderForm);

    if (!result.success) {
      setCreateOrderValidationErrors(mapValidationErrors(result.error));
      return;
    }

    setCreateOrderValidationErrors({});
    createOrderMutation.mutate(result.data);
  }

  function handleUpdateStatus() {
    if (!selectedOrder || !selectedStatus) {
      return;
    }

    setSuccessMessage("");
    createOrderMutation.reset();
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
      `Are you sure you want to cancel ${formatShortId(selectedOrder.id, "Order")}?`,
    );

    if (!confirmed) {
      return;
    }

    setSuccessMessage("");
    createOrderMutation.reset();
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
            Create customer orders, inspect order lines, update order status and cancel orders when business rules allow it.
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
                Order creation checks customer, product and stock rules before the order is saved.
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
        <div className="mb-5 flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
          <div className="flex items-center gap-3">
            <div className="rounded-xl bg-blue-50 p-3 text-blue-600">
              <ShoppingCart size={20} />
            </div>

            <div>
              <h3 className="text-base font-semibold text-slate-950">Create order</h3>
              <p className="mt-1 text-sm text-slate-500">
                Select a customer and one or more products. Stock is reduced when the order is created.
              </p>
            </div>
          </div>

          <div className="rounded-2xl bg-slate-50 px-5 py-4">
            <p className="text-sm font-medium text-slate-500">Estimated total</p>
            <p className="mt-1 text-xl font-bold text-slate-950">
              {currencyFormatter.format(estimatedOrderTotal)}
            </p>
          </div>
        </div>

        {hasCreateOrderDataError && (
          <div className="mb-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            <div className="flex gap-2">
              <AlertTriangle size={18} />
              <span>
                {isCustomersError ? customerLoadErrorMessage : productLoadErrorMessage}
              </span>
            </div>
          </div>
        )}

        <div className="grid gap-4">
          <label className="block">
            <span className="text-sm font-medium text-slate-700">Customer</span>
            <select
              value={createOrderForm.customerId}
              onChange={(event) => handleCreateOrderCustomerChange(event.target.value)}
              disabled={isCreateOrderDataLoading || hasCreateOrderDataError}
              className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-950 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 disabled:bg-slate-100"
            >
              <option value="">Select customer</option>
              {customers.map((customer) => (
                <option key={customer.id} value={customer.id}>
                  {customer.name}
                </option>
              ))}
            </select>
          </label>

          {createOrderValidationErrors.customerId && (
            <p className="text-sm text-red-600">{createOrderValidationErrors.customerId}</p>
          )}

          <div className="space-y-3">
            <div className="flex items-center justify-between gap-3">
              <h4 className="text-sm font-semibold text-slate-950">Order items</h4>

              <button
                type="button"
                onClick={handleAddOrderItem}
                disabled={isCreateOrderDataLoading || hasCreateOrderDataError}
                className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50 disabled:cursor-not-allowed disabled:bg-slate-100"
              >
                <Plus size={15} />
                Add item
              </button>
            </div>

            {createOrderForm.items.map((item, index) => {
              const product = products.find((currentProduct) => currentProduct.id === item.productId);
              const lineTotal = product ? product.price * item.quantity : 0;

              return (
                <div
                  key={`create-order-item-${index}`}
                  className="grid gap-3 rounded-2xl border border-slate-200 bg-slate-50 p-4 lg:grid-cols-[1fr_160px_160px_auto] lg:items-start"
                >
                  <div>
                    <label className="block">
                      <span className="text-sm font-medium text-slate-700">Product</span>
                      <select
                        value={item.productId}
                        onChange={(event) => handleCreateOrderItemProductChange(index, event.target.value)}
                        disabled={isCreateOrderDataLoading || hasCreateOrderDataError}
                        className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-950 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 disabled:bg-slate-100"
                      >
                        <option value="">Select product</option>
                        {products.map((currentProduct) => (
                          <option key={currentProduct.id} value={currentProduct.id}>
                            {currentProduct.name} ({currentProduct.sku})
                          </option>
                        ))}
                      </select>
                    </label>

                    {createOrderValidationErrors[`items.${index}.productId`] && (
                      <p className="mt-2 text-sm text-red-600">
                        {createOrderValidationErrors[`items.${index}.productId`]}
                      </p>
                    )}
                  </div>

                  <div>
                    <label className="block">
                      <span className="text-sm font-medium text-slate-700">Quantity</span>
                      <input
                        type="number"
                        min="1"
                        value={item.quantity}
                        onChange={(event) =>
                          handleCreateOrderItemQuantityChange(index, Number(event.target.value))
                        }
                        disabled={isCreateOrderDataLoading || hasCreateOrderDataError}
                        className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-950 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 disabled:bg-slate-100"
                      />
                    </label>

                    {createOrderValidationErrors[`items.${index}.quantity`] && (
                      <p className="mt-2 text-sm text-red-600">
                        {createOrderValidationErrors[`items.${index}.quantity`]}
                      </p>
                    )}
                  </div>

                  <div>
                    <p className="text-sm font-medium text-slate-700">Line total</p>
                    <p className="mt-3 text-sm font-semibold text-slate-950">
                      {currencyFormatter.format(lineTotal)}
                    </p>
                  </div>

                  <button
                    type="button"
                    onClick={() => handleRemoveOrderItem(index)}
                    disabled={createOrderForm.items.length === 1}
                    className="mt-7 inline-flex items-center justify-center rounded-xl border border-red-200 bg-white px-3 py-2 text-sm font-semibold text-red-700 shadow-sm hover:bg-red-50 disabled:cursor-not-allowed disabled:border-slate-200 disabled:text-slate-300"
                  >
                    <Trash2 size={16} />
                  </button>
                </div>
              );
            })}
          </div>

          {createOrderMutation.isError && (
            <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
              <div className="flex gap-2">
                <AlertTriangle size={18} />
                <span>{createOrderErrorMessage}</span>
              </div>
            </div>
          )}

          <button
            type="button"
            onClick={handleCreateOrderSubmit}
            disabled={isCreateOrderPending || isCreateOrderDataLoading || hasCreateOrderDataError}
            className="justify-self-start rounded-xl bg-blue-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-300"
          >
            {isCreateOrderPending ? "Creating..." : "Create order"}
          </button>
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
                  ? `Managing ${formatShortId(selectedOrder.id, "Order")} for ${selectedOrder.customerName}.`
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
            Shows customer orders, status, items and total value.
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
                                <p className="mt-1 text-xs text-slate-500">{formatShortId(order.id, "Order")}</p>
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


