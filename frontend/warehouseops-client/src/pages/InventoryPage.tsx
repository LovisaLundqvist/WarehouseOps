import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AlertTriangle,
  Boxes,
  CheckCircle2,
  PackageSearch,
  Pencil,
  Save,
  X,
} from "lucide-react";
import { type SubmitHandler, useForm } from "react-hook-form";
import { z } from "zod";
import {
  getInventoryItems,
  getLowStockInventoryItems,
  updateInventoryItem,
} from "../api/inventoryApi";
import type { InventoryItem, UpdateInventoryItemRequest } from "../types/inventory";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";

const inventorySchema = z.object({
  quantityInStock: z
    .number()
    .int("Quantity must be a whole number.")
    .min(0, "Quantity cannot be negative."),
  minimumStockLevel: z
    .number()
    .int("Minimum stock level must be a whole number.")
    .min(0, "Minimum stock level cannot be negative."),
});

type InventoryFormValues = z.infer<typeof inventorySchema>;

const initialInventoryFormValues: InventoryFormValues = {
  quantityInStock: 0,
  minimumStockLevel: 0,
};

export default function InventoryPage() {
  const queryClient = useQueryClient();

  const [showLowStockOnly, setShowLowStockOnly] = useState(false);
  const [editingInventoryItem, setEditingInventoryItem] = useState<InventoryItem | null>(null);
  const [successMessage, setSuccessMessage] = useState("");

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<InventoryFormValues>({
    resolver: zodResolver(inventorySchema),
    defaultValues: initialInventoryFormValues,
  });

  const inventoryQuery = useQuery({
    queryKey: ["inventory"],
    queryFn: getInventoryItems,
  });

  const lowStockQuery = useQuery({
    queryKey: ["inventory", "low-stock"],
    queryFn: getLowStockInventoryItems,
  });

  const updateInventoryMutation = useMutation({
    mutationFn: (data: { id: string; request: UpdateInventoryItemRequest }) =>
      updateInventoryItem(data.id, data.request),
    onSuccess: (updatedItem) => {
      setSuccessMessage(`${updatedItem.productName} inventory was updated.`);
      setEditingInventoryItem(null);
      reset(initialInventoryFormValues);
      queryClient.invalidateQueries({ queryKey: ["inventory"] });
    },
  });

  const inventoryItems = inventoryQuery.data ?? [];
  const lowStockItems = lowStockQuery.data ?? [];
  const displayedItems = showLowStockOnly ? lowStockItems : inventoryItems;

  const isLoading = showLowStockOnly ? lowStockQuery.isLoading : inventoryQuery.isLoading;
  const isError = showLowStockOnly ? lowStockQuery.isError : inventoryQuery.isError;
  const error = showLowStockOnly ? lowStockQuery.error : inventoryQuery.error;

  const totalQuantity = inventoryItems.reduce(
    (sum, item) => sum + item.quantityInStock,
    0,
  );

  function handleStartEdit(item: InventoryItem) {
    setSuccessMessage("");
    updateInventoryMutation.reset();

    setEditingInventoryItem(item);

    reset({
      quantityInStock: item.quantityInStock,
      minimumStockLevel: item.minimumStockLevel,
    });

    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function handleCancelEdit() {
    setSuccessMessage("");
    updateInventoryMutation.reset();

    setEditingInventoryItem(null);
    reset(initialInventoryFormValues);
  }

  const handleUpdateInventory: SubmitHandler<InventoryFormValues> = (values) => {
    if (!editingInventoryItem) {
      return;
    }

    setSuccessMessage("");
    updateInventoryMutation.reset();

    updateInventoryMutation.mutate({
      id: editingInventoryItem.id,
      request: {
        quantityInStock: values.quantityInStock,
        minimumStockLevel: values.minimumStockLevel,
      },
    });
  };

  const errorMessage = getApiErrorMessage(error, "Could not load inventory.");
  const updateErrorMessage = getApiErrorMessage(
    updateInventoryMutation.error,
    "Could not update inventory item.",
  );

  return (
    <div className="space-y-6">
      <section className="flex flex-col justify-between gap-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm lg:flex-row lg:items-center">
        <div>
          <div className="flex items-center gap-3">
            <div className="rounded-xl bg-blue-50 p-3 text-blue-600">
              <Boxes size={24} />
            </div>

            <div>
              <p className="text-sm font-medium text-blue-600">Stock control</p>
              <h2 className="text-2xl font-bold text-slate-950">Inventory</h2>
            </div>
          </div>

          <p className="mt-4 max-w-2xl text-sm leading-6 text-slate-500">
            Monitor and update warehouse stock levels, minimum stock limits and low-stock items.
          </p>
        </div>

        <div className="grid gap-3 sm:grid-cols-3">
          <div className="rounded-2xl bg-slate-50 px-5 py-4">
            <p className="text-sm font-medium text-slate-500">Inventory items</p>
            <p className="mt-1 text-2xl font-bold text-slate-950">{inventoryItems.length}</p>
          </div>

          <div className="rounded-2xl bg-slate-50 px-5 py-4">
            <p className="text-sm font-medium text-slate-500">Total quantity</p>
            <p className="mt-1 text-2xl font-bold text-slate-950">{totalQuantity}</p>
          </div>

          <div className="rounded-2xl bg-red-50 px-5 py-4">
            <p className="text-sm font-medium text-red-700">Low stock</p>
            <p className="mt-1 text-2xl font-bold text-red-700">{lowStockItems.length}</p>
          </div>
        </div>
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="mb-5 flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="rounded-xl bg-blue-50 p-3 text-blue-600">
              <Save size={20} />
            </div>

            <div>
              <h3 className="text-base font-semibold text-slate-950">Update inventory</h3>
              <p className="mt-1 text-sm text-slate-500">
                {editingInventoryItem
                  ? `Editing stock levels for ${editingInventoryItem.productName}.`
                  : "Select an inventory item from the table to update stock levels."}
              </p>
            </div>
          </div>

          {editingInventoryItem && (
            <button
              type="button"
              onClick={handleCancelEdit}
              className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50"
            >
              <X size={16} />
              Cancel edit
            </button>
          )}
        </div>

        {editingInventoryItem ? (
          <form onSubmit={handleSubmit(handleUpdateInventory)} className="grid gap-4 lg:grid-cols-2">
            <div className="rounded-xl bg-slate-50 p-4 lg:col-span-2">
              <p className="text-sm font-semibold text-slate-950">
                {editingInventoryItem.productName}
              </p>
              <p className="mt-1 text-sm text-slate-500">
                SKU: {editingInventoryItem.productSku}
              </p>
            </div>

            <label className="block">
              <span className="text-sm font-medium text-slate-700">Quantity in stock</span>
              <input
                {...register("quantityInStock", { valueAsNumber: true })}
                type="number"
                min="0"
                step="1"
                className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              />
              {errors.quantityInStock && (
                <p className="mt-1 text-sm text-red-600">
                  {errors.quantityInStock.message}
                </p>
              )}
            </label>

            <label className="block">
              <span className="text-sm font-medium text-slate-700">Minimum stock level</span>
              <input
                {...register("minimumStockLevel", { valueAsNumber: true })}
                type="number"
                min="0"
                step="1"
                className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              />
              {errors.minimumStockLevel && (
                <p className="mt-1 text-sm text-red-600">
                  {errors.minimumStockLevel.message}
                </p>
              )}
            </label>

            {successMessage && (
              <div className="rounded-xl border border-green-200 bg-green-50 p-4 text-sm text-green-700 lg:col-span-2">
                {successMessage}
              </div>
            )}

            {updateInventoryMutation.isError && (
              <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 lg:col-span-2">
                {updateErrorMessage}
              </div>
            )}

            <div className="flex justify-end lg:col-span-2">
              <button
                type="submit"
                disabled={updateInventoryMutation.isPending}
                className="rounded-xl bg-blue-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-300"
              >
                {updateInventoryMutation.isPending ? "Updating..." : "Update inventory"}
              </button>
            </div>
          </form>
        ) : (
          <div className="rounded-xl border border-dashed border-slate-300 bg-slate-50 p-5 text-sm text-slate-500">
            No inventory item selected.
          </div>
        )}
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
          <div>
            <h3 className="text-base font-semibold text-slate-950">Inventory overview</h3>
            <p className="mt-1 text-sm text-slate-500">
              Switch between all inventory items and products that are at or below their minimum stock level.
            </p>
          </div>

          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setShowLowStockOnly(false)}
              className={
                showLowStockOnly
                  ? "rounded-xl border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50"
                  : "rounded-xl bg-blue-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-blue-700"
              }
            >
              All inventory
            </button>

            <button
              type="button"
              onClick={() => setShowLowStockOnly(true)}
              className={
                showLowStockOnly
                  ? "rounded-xl bg-red-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-red-700"
                  : "rounded-xl border border-red-200 bg-white px-4 py-2 text-sm font-semibold text-red-700 shadow-sm hover:bg-red-50"
              }
            >
              Low stock only
            </button>
          </div>
        </div>
      </section>

      <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-200 px-5 py-4">
          <div className="flex items-center gap-3">
            <PackageSearch size={20} className="text-slate-500" />
            <div>
              <h3 className="text-base font-semibold text-slate-950">
                {showLowStockOnly ? "Low-stock inventory" : "Inventory list"}
              </h3>
              <p className="mt-1 text-sm text-slate-500">
                Data is loaded from the ASP.NET Core Inventory API.
              </p>
            </div>
          </div>
        </div>

        {isLoading && (
          <div className="p-6 text-sm text-slate-500">
            Loading inventory...
          </div>
        )}

        {isError && (
          <div className="m-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            {errorMessage}
          </div>
        )}

        {!isLoading && !isError && displayedItems.length === 0 && (
          <div className="p-6 text-sm text-slate-500">
            No inventory items found.
          </div>
        )}

        {!isLoading && !isError && displayedItems.length > 0 && (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Product
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    SKU
                  </th>
                  <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Quantity
                  </th>
                  <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Minimum
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Status
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Updated
                  </th>
                  <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Actions
                  </th>
                </tr>
              </thead>

              <tbody className="divide-y divide-slate-100 bg-white">
                {displayedItems.map((item) => (
                  <tr key={item.id} className="hover:bg-slate-50">
                    <td className="px-5 py-4">
                      <p className="text-sm font-semibold text-slate-950">{item.productName}</p>
                      <p className="mt-1 text-xs text-slate-500">{item.productId}</p>
                    </td>

                    <td className="whitespace-nowrap px-5 py-4 text-sm font-medium text-slate-700">
                      {item.productSku}
                    </td>

                    <td className="whitespace-nowrap px-5 py-4 text-right text-sm font-semibold text-slate-950">
                      {item.quantityInStock}
                    </td>

                    <td className="whitespace-nowrap px-5 py-4 text-right text-sm text-slate-700">
                      {item.minimumStockLevel}
                    </td>

                    <td className="whitespace-nowrap px-5 py-4">
                      {item.isLowStock ? (
                        <span className="inline-flex items-center gap-2 rounded-full bg-red-50 px-3 py-1 text-xs font-semibold text-red-700">
                          <AlertTriangle size={14} />
                          Low stock
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-2 rounded-full bg-green-50 px-3 py-1 text-xs font-semibold text-green-700">
                          <CheckCircle2 size={14} />
                          In stock
                        </span>
                      )}
                    </td>

                    <td className="whitespace-nowrap px-5 py-4 text-sm text-slate-500">
                      {item.updatedAt
                        ? new Date(item.updatedAt).toLocaleDateString("sv-SE")
                        : new Date(item.createdAt).toLocaleDateString("sv-SE")}
                    </td>

                    <td className="whitespace-nowrap px-5 py-4 text-right">
                      <button
                        type="button"
                        onClick={() => handleStartEdit(item)}
                        className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50"
                      >
                        <Pencil size={15} />
                        Edit
                      </button>
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
