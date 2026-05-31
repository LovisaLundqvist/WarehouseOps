import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AlertTriangle,
  CalendarCheck,
  Clock,
  PackageCheck,
  Plus,
  Route,
  Settings,
  Truck,
  X,
} from "lucide-react";
import { z } from "zod";
import { useAuth } from "../auth/AuthContext";
import { getOrders } from "../api/ordersApi";
import {
  createShipment,
  getShipments,
  updateShipmentStatus,
} from "../api/shipmentsApi";
import type { Order } from "../types/order";
import type { Shipment } from "../types/shipment";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";
import { formatShortId } from "../utils/formatShortId";

const shipmentStatuses = [
  "Pending",
  "Packed",
  "Shipped",
  "Delivered",
  "Delayed",
  "Cancelled",
];

const createShipmentSchema = z.object({
  orderId: z.string().trim().min(1, "Order is required."),
  trackingNumber: z.string().trim().min(1, "Tracking number is required."),
});

type CreateShipmentFormValues = z.infer<typeof createShipmentSchema>;

const initialCreateShipmentFormValues: CreateShipmentFormValues = {
  orderId: "",
  trackingNumber: "",
};

function getStatusBadgeClass(status: string) {
  switch (status) {
    case "Pending":
      return "bg-slate-100 text-slate-700";
    case "Packed":
      return "bg-indigo-50 text-indigo-700";
    case "Shipped":
      return "bg-blue-50 text-blue-700";
    case "Delivered":
      return "bg-green-50 text-green-700";
    case "Delayed":
      return "bg-amber-50 text-amber-700";
    case "Cancelled":
      return "bg-red-50 text-red-700";
    default:
      return "bg-slate-100 text-slate-700";
  }
}

function formatDate(value?: string | null) {
  if (!value) {
    return "Not set";
  }

  return new Date(value).toLocaleDateString("sv-SE");
}

function getActiveShipmentsCount(shipments: Shipment[]) {
  return shipments.filter(
    (shipment) =>
      shipment.status !== "Delivered" &&
      shipment.status !== "Cancelled",
  ).length;
}

function canUpdateShipment(shipment: Shipment) {
  return shipment.status !== "Delivered" && shipment.status !== "Cancelled";
}

function getOrdersWithoutShipment(orders: Order[], shipments: Shipment[]) {
  const orderIdsWithShipment = new Set(shipments.map((shipment) => shipment.orderId));

  return orders.filter(
    (order) =>
      order.status !== "Cancelled" &&
      !orderIdsWithShipment.has(order.id),
  );
}

function mapValidationErrors(error: z.ZodError<CreateShipmentFormValues>) {
  const validationErrors: Record<string, string> = {};

  error.issues.forEach((issue) => {
    const path = issue.path.join(".");

    if (path) {
      validationErrors[path] = issue.message;
    }
  });

  return validationErrors;
}

export default function ShipmentsPage() {
  const queryClient = useQueryClient();
  const { user } = useAuth();

  const canManageShipments = user?.role === "Admin" || user?.role === "WarehouseStaff";

  const [selectedShipment, setSelectedShipment] = useState<Shipment | null>(null);
  const [selectedStatus, setSelectedStatus] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [createShipmentForm, setCreateShipmentForm] = useState<CreateShipmentFormValues>(
    initialCreateShipmentFormValues,
  );
  const [createShipmentValidationErrors, setCreateShipmentValidationErrors] = useState<
    Record<string, string>
  >({});

  const {
    data: shipments = [],
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ["shipments"],
    queryFn: getShipments,
  });

  const {
    data: orders = [],
    isLoading: isLoadingOrders,
    isError: isOrdersError,
    error: ordersError,
  } = useQuery({
    queryKey: ["orders", "shipment-form"],
    queryFn: getOrders,
    enabled: canManageShipments,
  });

  const createShipmentMutation = useMutation({
    mutationFn: createShipment,
    onSuccess: (createdShipment) => {
      setSuccessMessage(`Shipment was created with tracking number ${createdShipment.trackingNumber}.`);
      setCreateShipmentForm(initialCreateShipmentFormValues);
      setCreateShipmentValidationErrors({});
      setSelectedShipment(createdShipment);
      setSelectedStatus(createdShipment.status);
      queryClient.invalidateQueries({ queryKey: ["shipments"] });
      queryClient.invalidateQueries({ queryKey: ["orders"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
    },
  });

  const updateStatusMutation = useMutation({
    mutationFn: (data: { id: string; status: string }) =>
      updateShipmentStatus(data.id, { status: data.status }),
    onSuccess: (updatedShipment) => {
      setSuccessMessage(`Shipment status was updated to ${updatedShipment.status}.`);
      setSelectedShipment(updatedShipment);
      setSelectedStatus(updatedShipment.status);
      queryClient.invalidateQueries({ queryKey: ["shipments"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
    },
  });

  const activeShipmentsCount = getActiveShipmentsCount(shipments);
  const deliveredShipmentsCount = shipments.filter(
    (shipment) => shipment.status === "Delivered",
  ).length;
  const delayedShipmentsCount = shipments.filter(
    (shipment) => shipment.status === "Delayed",
  ).length;

  const availableOrders = getOrdersWithoutShipment(orders, shipments);

  const errorMessage = getApiErrorMessage(error, "Could not load shipments.");

  const ordersErrorMessage = getApiErrorMessage(
    ordersError,
    "Could not load orders for the shipment form.",
  );

  const createShipmentErrorMessage = getApiErrorMessage(
    createShipmentMutation.error,
    "Could not create shipment.",
  );

  const updateErrorMessage = getApiErrorMessage(
    updateStatusMutation.error,
    "Could not update shipment status.",
  );

  function handleSelectShipment(shipment: Shipment) {
    if (!canManageShipments) {
      return;
    }

    setSuccessMessage("");
    createShipmentMutation.reset();
    updateStatusMutation.reset();

    setSelectedShipment(shipment);
    setSelectedStatus(shipment.status);

    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function handleClearSelectedShipment() {
    setSuccessMessage("");
    updateStatusMutation.reset();

    setSelectedShipment(null);
    setSelectedStatus("");
  }

  function handleCreateShipmentOrderChange(orderId: string) {
    setCreateShipmentForm((current) => ({
      ...current,
      orderId,
    }));
  }

  function handleCreateShipmentTrackingNumberChange(trackingNumber: string) {
    setCreateShipmentForm((current) => ({
      ...current,
      trackingNumber,
    }));
  }

  function handleCreateShipmentSubmit() {
    if (!canManageShipments) {
      return;
    }

    setSuccessMessage("");
    createShipmentMutation.reset();
    updateStatusMutation.reset();

    const result = createShipmentSchema.safeParse(createShipmentForm);

    if (!result.success) {
      setCreateShipmentValidationErrors(mapValidationErrors(result.error));
      return;
    }

    setCreateShipmentValidationErrors({});
    createShipmentMutation.mutate(result.data);
  }

  function handleUpdateStatus() {
    if (!canManageShipments || !selectedShipment || !selectedStatus) {
      return;
    }

    setSuccessMessage("");
    createShipmentMutation.reset();
    updateStatusMutation.reset();

    updateStatusMutation.mutate({
      id: selectedShipment.id,
      status: selectedStatus,
    });
  }

  return (
    <div className="space-y-6">
      <section className="flex flex-col justify-between gap-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm lg:flex-row lg:items-center">
        <div>
          <div className="flex items-center gap-3">
            <div className="rounded-xl bg-blue-50 p-3 text-blue-600">
              <Truck size={24} />
            </div>

            <div>
              <p className="text-sm font-medium text-blue-600">Shipment tracking</p>
              <h2 className="text-2xl font-bold text-slate-950">Shipments</h2>
            </div>
          </div>

          <p className="mt-4 max-w-2xl text-sm leading-6 text-slate-500">
            View shipments, tracking numbers and delivery progress. Shipment changes are available based on your role.
          </p>
        </div>

        <div className="grid gap-3 sm:grid-cols-3">
          <div className="rounded-2xl bg-blue-50 px-5 py-4">
            <p className="text-sm font-medium text-blue-700">Active</p>
            <p className="mt-1 text-2xl font-bold text-blue-700">{activeShipmentsCount}</p>
          </div>

          <div className="rounded-2xl bg-green-50 px-5 py-4">
            <p className="text-sm font-medium text-green-700">Delivered</p>
            <p className="mt-1 text-2xl font-bold text-green-700">{deliveredShipmentsCount}</p>
          </div>

          <div className="rounded-2xl bg-amber-50 px-5 py-4">
            <p className="text-sm font-medium text-amber-700">Delayed</p>
            <p className="mt-1 text-2xl font-bold text-amber-700">{delayedShipmentsCount}</p>
          </div>
        </div>
      </section>

      {canManageShipments ? (
        <>
          <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <div className="mb-5 flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
              <div className="flex items-center gap-3">
                <div className="rounded-xl bg-blue-50 p-3 text-blue-600">
                  <Plus size={20} />
                </div>

                <div>
                  <h3 className="text-base font-semibold text-slate-950">Create shipment</h3>
                  <p className="mt-1 text-sm text-slate-500">
                    Select an order without an existing shipment and enter a tracking number.
                  </p>
                </div>
              </div>

              <div className="rounded-2xl bg-slate-50 px-5 py-4">
                <p className="text-sm font-medium text-slate-500">Available orders</p>
                <p className="mt-1 text-2xl font-bold text-slate-950">{availableOrders.length}</p>
              </div>
            </div>

            {isOrdersError && (
              <div className="mb-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
                <div className="flex gap-2">
                  <AlertTriangle size={18} />
                  <span>{ordersErrorMessage}</span>
                </div>
              </div>
            )}

            <div className="grid gap-4 lg:grid-cols-[1.4fr_1fr_auto] lg:items-start">
              <div>
                <label className="block">
                  <span className="text-sm font-medium text-slate-700">Order</span>
                  <select
                    value={createShipmentForm.orderId}
                    onChange={(event) => handleCreateShipmentOrderChange(event.target.value)}
                    disabled={isLoadingOrders || isOrdersError || availableOrders.length === 0}
                    className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-950 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 disabled:cursor-not-allowed disabled:bg-slate-100"
                  >
                    <option value="">Select order</option>
                    {availableOrders.map((order) => (
                      <option key={order.id} value={order.id}>
                        {order.customerName} | {order.status} | {formatShortId(order.id, "Order")}
                      </option>
                    ))}
                  </select>
                </label>

                {createShipmentValidationErrors.orderId && (
                  <p className="mt-2 text-sm text-red-600">
                    {createShipmentValidationErrors.orderId}
                  </p>
                )}

                {!isLoadingOrders && !isOrdersError && availableOrders.length === 0 && (
                  <p className="mt-2 text-sm text-amber-700">
                    There are no available orders. Cancelled orders and orders that already have shipments are excluded.
                  </p>
                )}
              </div>

              <div>
                <label className="block">
                  <span className="text-sm font-medium text-slate-700">Tracking number</span>
                  <input
                    type="text"
                    value={createShipmentForm.trackingNumber}
                    onChange={(event) => handleCreateShipmentTrackingNumberChange(event.target.value)}
                    placeholder="Example: WH-TRK-1001"
                    className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-950 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                  />
                </label>

                {createShipmentValidationErrors.trackingNumber && (
                  <p className="mt-2 text-sm text-red-600">
                    {createShipmentValidationErrors.trackingNumber}
                  </p>
                )}
              </div>

              <button
                type="button"
                onClick={handleCreateShipmentSubmit}
                disabled={
                  createShipmentMutation.isPending ||
                  isLoadingOrders ||
                  isOrdersError ||
                  availableOrders.length === 0
                }
                className="mt-7 rounded-xl bg-blue-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-300"
              >
                {createShipmentMutation.isPending ? "Creating..." : "Create shipment"}
              </button>

              {createShipmentMutation.isError && (
                <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 lg:col-span-3">
                  <div className="flex gap-2">
                    <AlertTriangle size={18} />
                    <span>{createShipmentErrorMessage}</span>
                  </div>
                </div>
              )}
            </div>
          </section>

          <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <div className="mb-5 flex items-center justify-between gap-4">
              <div className="flex items-center gap-3">
                <div className="rounded-xl bg-blue-50 p-3 text-blue-600">
                  <Settings size={20} />
                </div>

                <div>
                  <h3 className="text-base font-semibold text-slate-950">Shipment actions</h3>
                  <p className="mt-1 text-sm text-slate-500">
                    {selectedShipment
                      ? `Managing ${formatShortId(selectedShipment.id, "Shipment")}.`
                      : "Select a shipment from the table to update its status."}
                  </p>
                </div>
              </div>

              {selectedShipment && (
                <button
                  type="button"
                  onClick={handleClearSelectedShipment}
                  className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50"
                >
                  <X size={16} />
                  Clear
                </button>
              )}
            </div>

            {selectedShipment ? (
              <div className="grid gap-4 lg:grid-cols-[1fr_auto]">
                <div>
                  <label className="block">
                    <span className="text-sm font-medium text-slate-700">Shipment status</span>
                    <select
                      value={selectedStatus}
                      onChange={(event) => setSelectedStatus(event.target.value)}
                      disabled={!canUpdateShipment(selectedShipment)}
                      className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-950 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-500"
                    >
                      {shipmentStatuses.map((status) => (
                        <option key={status} value={status}>
                          {status}
                        </option>
                      ))}
                    </select>
                  </label>

                  <p className="mt-2 text-sm text-slate-500">
                    Current status:{" "}
                    <span className={`rounded-full px-3 py-1 text-xs font-semibold ${getStatusBadgeClass(selectedShipment.status)}`}>
                      {selectedShipment.status}
                    </span>
                  </p>

                  <p className="mt-2 text-sm text-slate-500">
                    Order status:{" "}
                    <span className="font-semibold text-slate-700">
                      {selectedShipment.orderStatus || "Not shown"}
                    </span>
                  </p>
                </div>

                <button
                  type="button"
                  onClick={handleUpdateStatus}
                  disabled={
                    updateStatusMutation.isPending ||
                    selectedStatus === selectedShipment.status ||
                    !canUpdateShipment(selectedShipment)
                  }
                  className="self-end rounded-xl bg-blue-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-300"
                >
                  {updateStatusMutation.isPending ? "Updating..." : "Update status"}
                </button>

                {successMessage && (
                  <div className="rounded-xl border border-green-200 bg-green-50 p-4 text-sm text-green-700 lg:col-span-2">
                    {successMessage}
                  </div>
                )}

                {updateStatusMutation.isError && (
                  <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 lg:col-span-2">
                    <div className="flex gap-2">
                      <AlertTriangle size={18} />
                      <span>{updateErrorMessage}</span>
                    </div>
                  </div>
                )}

                {!canUpdateShipment(selectedShipment) && (
                  <div className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800 lg:col-span-2">
                    This shipment cannot be updated because it is already delivered or cancelled.
                  </div>
                )}
              </div>
            ) : (
              <div className="rounded-xl border border-dashed border-slate-300 bg-slate-50 p-5 text-sm text-slate-500">
                No shipment selected.
              </div>
            )}
          </section>
        </>
      ) : (
        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h3 className="text-base font-semibold text-slate-950">Read only shipment access</h3>
          <p className="mt-1 text-sm leading-6 text-slate-500">
            Your role can view shipments, but only Admin and WarehouseStaff users can create shipments or update shipment status.
          </p>
        </section>
      )}

      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="flex items-center gap-3">
          <div className="rounded-xl bg-slate-50 p-3 text-slate-600">
            <Route size={20} />
          </div>

          <div>
            <h3 className="text-base font-semibold text-slate-950">Shipment overview</h3>
            <p className="mt-1 text-sm text-slate-500">
              Shows shipment progress, tracking numbers and delivery dates.
            </p>
          </div>
        </div>
      </section>

      <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-200 px-5 py-4">
          <h3 className="text-base font-semibold text-slate-950">Shipment list</h3>
          <p className="mt-1 text-sm text-slate-500">
            Shows current shipments, tracking numbers and delivery progress.
          </p>
        </div>

        {isLoading && (
          <div className="p-6 text-sm text-slate-500">
            Loading shipments...
          </div>
        )}

        {isError && (
          <div className="m-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            {errorMessage}
          </div>
        )}

        {!isLoading && !isError && shipments.length === 0 && (
          <div className="p-6 text-sm text-slate-500">
            No shipments found.
          </div>
        )}

        {!isLoading && !isError && shipments.length > 0 && (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Shipment
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Order
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Status
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Tracking
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Shipped
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Delivered
                  </th>
                  {canManageShipments && (
                    <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                      Actions
                    </th>
                  )}
                </tr>
              </thead>

              <tbody className="divide-y divide-slate-100 bg-white">
                {shipments.map((shipment) => (
                  <tr key={shipment.id} className="hover:bg-slate-50">
                    <td className="px-5 py-4">
                      <div className="flex items-start gap-3">
                        <div className="rounded-xl bg-slate-50 p-2 text-slate-600">
                          <PackageCheck size={18} />
                        </div>

                        <div>
                          <p className="text-sm font-semibold text-slate-950">
                            {formatShortId(shipment.id, "Shipment")}
                          </p>
                          <p className="mt-1 text-xs text-slate-500">
                            Created: {formatDate(shipment.createdAt)}
                          </p>
                        </div>
                      </div>
                    </td>

                    <td className="px-5 py-4">
                      <p className="text-sm font-medium text-slate-950">
                        {formatShortId(shipment.orderId, "Order")}
                      </p>
                      <p className="mt-1 text-xs text-slate-500">
                        {shipment.customerName || "Customer not shown"}
                      </p>
                    </td>

                    <td className="whitespace-nowrap px-5 py-4">
                      <span
                        className={`rounded-full px-3 py-1 text-xs font-semibold ${getStatusBadgeClass(
                          shipment.status,
                        )}`}
                      >
                        {shipment.status}
                      </span>
                    </td>

                    <td className="whitespace-nowrap px-5 py-4 text-sm font-medium text-slate-700">
                      {shipment.trackingNumber || "Not assigned"}
                    </td>

                    <td className="whitespace-nowrap px-5 py-4">
                      <div className="flex items-center gap-2 text-sm text-slate-600">
                        <Clock size={15} />
                        {formatDate(shipment.shippedDate)}
                      </div>
                    </td>

                    <td className="whitespace-nowrap px-5 py-4">
                      <div className="flex items-center gap-2 text-sm text-slate-600">
                        <CalendarCheck size={15} />
                        {formatDate(shipment.deliveredDate)}
                      </div>
                    </td>

                    {canManageShipments && (
                      <td className="whitespace-nowrap px-5 py-4 text-right">
                        <button
                          type="button"
                          onClick={() => handleSelectShipment(shipment)}
                          className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50"
                        >
                          <Settings size={15} />
                          Manage
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
