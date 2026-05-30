import { useQuery } from "@tanstack/react-query";
import {
  CalendarCheck,
  Clock,
  PackageCheck,
  Route,
  Truck,
} from "lucide-react";
import { getShipments } from "../api/shipmentsApi";
import type { Shipment } from "../types/shipment";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";

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

export default function ShipmentsPage() {
  const {
    data: shipments = [],
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ["shipments"],
    queryFn: getShipments,
  });

  const activeShipmentsCount = getActiveShipmentsCount(shipments);
  const deliveredShipmentsCount = shipments.filter(
    (shipment) => shipment.status === "Delivered",
  ).length;
  const delayedShipmentsCount = shipments.filter(
    (shipment) => shipment.status === "Delayed",
  ).length;

  const errorMessage = getApiErrorMessage(error, "Could not load shipments.");

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
            View shipments, tracking numbers, delivery status and order connections from the backend API.
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

      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="flex items-center gap-3">
          <div className="rounded-xl bg-slate-50 p-3 text-slate-600">
            <Route size={20} />
          </div>

          <div>
            <h3 className="text-base font-semibold text-slate-950">Shipment overview</h3>
            <p className="mt-1 text-sm text-slate-500">
              Read-only overview. Shipment status updates will be added after the list is stable.
            </p>
          </div>
        </div>
      </section>

      <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-200 px-5 py-4">
          <h3 className="text-base font-semibold text-slate-950">Shipment list</h3>
          <p className="mt-1 text-sm text-slate-500">
            Data is loaded from the ASP.NET Core Shipments API.
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
                            {shipment.id}
                          </p>
                          <p className="mt-1 text-xs text-slate-500">
                            Carrier: {shipment.carrier ?? "Not set"}
                          </p>
                        </div>
                      </div>
                    </td>

                    <td className="px-5 py-4">
                      <p className="text-sm font-medium text-slate-950">
                        {shipment.orderNumber ?? shipment.orderId}
                      </p>
                      <p className="mt-1 text-xs text-slate-500">
                        {shipment.customerName ?? "Customer not shown"}
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
                      {shipment.trackingNumber ?? "Not assigned"}
                    </td>

                    <td className="whitespace-nowrap px-5 py-4">
                      <div className="flex items-center gap-2 text-sm text-slate-600">
                        <Clock size={15} />
                        {formatDate(shipment.shippedAt)}
                      </div>
                    </td>

                    <td className="whitespace-nowrap px-5 py-4">
                      <div className="flex items-center gap-2 text-sm text-slate-600">
                        <CalendarCheck size={15} />
                        {formatDate(shipment.deliveredAt)}
                      </div>
                    </td>
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
