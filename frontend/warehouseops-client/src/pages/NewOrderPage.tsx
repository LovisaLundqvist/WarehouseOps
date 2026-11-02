import { useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, ArrowLeft, Plus, Trash2 } from "lucide-react";
import { Link, Navigate, useNavigate } from "react-router-dom";
import { z } from "zod";
import { useAuth } from "../auth/AuthContext";
import { getCustomers } from "../api/customersApi";
import { getProducts } from "../api/productsApi";
import { getInventoryItems } from "../api/inventoryApi";
import { createOrder } from "../api/ordersApi";
import type { CreateOrderItemRequest } from "../types/order";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";
import { currency } from "../utils/orderShipmentWorkflow";

const schema = z.object({
  customerId: z.guid("Select a customer."),
  items: z.array(z.object({
    productId: z.guid("Select a product."),
    quantity: z.number().int().positive("Quantity must be a positive whole number."),
  })).min(1, "Add at least one product."),
}).superRefine((value, context) => {
  const productIds = value.items.map((item) => item.productId);
  if (new Set(productIds).size !== productIds.length) {
    context.addIssue({ code: "custom", message: "Each product can only appear once in an order.", path: ["items"] });
  }
});

export default function NewOrderPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [customerId, setCustomerId] = useState("");
  const [items, setItems] = useState<CreateOrderItemRequest[]>([{ productId: "", quantity: 1 }]);
  const [formError, setFormError] = useState("");
  const canManage = user?.role === "Admin" || user?.role === "WarehouseStaff";

  const customersQuery = useQuery({ queryKey: ["customers", "order-form"], queryFn: () => getCustomers(), enabled: canManage });
  const productsQuery = useQuery({ queryKey: ["products", "order-form"], queryFn: () => getProducts(), enabled: canManage });
  const inventoryQuery = useQuery({ queryKey: ["inventory", "order-form"], queryFn: getInventoryItems, enabled: canManage });

  const customers = customersQuery.data ?? [];
  const products = productsQuery.data ?? [];
  const inventory = inventoryQuery.data ?? [];
  const stockedProducts = products.filter((product) =>
    inventory.some((stock) => stock.productId === product.id && stock.quantityInStock > 0));
  const estimate = items.reduce((total, item) => total + (products.find((product) => product.id === item.productId)?.price ?? 0) * item.quantity, 0);

  const mutation = useMutation({
    mutationFn: createOrder,
    onSuccess: async (order) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["orders"] }),
        queryClient.invalidateQueries({ queryKey: ["inventory"] }),
        queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] }),
      ]);
      navigate(`/orders/${order.id}`, { replace: true });
    },
  });

  if (!canManage) return <Navigate to="/orders" replace />;

  function changeItem(index: number, updated: Partial<CreateOrderItemRequest>) {
    setItems((current) => current.map((item, i) => (i === index ? { ...item, ...updated } : item)));
    setFormError("");
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError("");
    const parsed = schema.safeParse({ customerId, items });
    if (!parsed.success) {
      setFormError(parsed.error.issues[0]?.message ?? "Invalid order.");
      return;
    }
    for (const item of parsed.data.items) {
      const stock = inventory.find((entry) => entry.productId === item.productId);
      if (!stock || item.quantity > stock.quantityInStock) {
        const productName = products.find((product) => product.id === item.productId)?.name ?? "Selected product";
        setFormError(`${productName}: only ${stock?.quantityInStock ?? 0} items available.`);
        return;
      }
    }
    mutation.mutate(parsed.data);
  }

  const loading = customersQuery.isLoading || productsQuery.isLoading || inventoryQuery.isLoading;
  const loadingError = customersQuery.error ?? productsQuery.error ?? inventoryQuery.error;

  return (
    <div className="max-w-4xl space-y-6">
      <Link to="/orders" className="inline-flex items-center gap-2 text-sm font-semibold text-[#3e5d51] hover:underline"><ArrowLeft size={16} /> Back to orders</Link>
      <div className="border-b border-slate-300 pb-6">
        <p className="ops-eyebrow text-[#60766a]">Orders / New order</p>
        <h1 className="ops-display mt-2 text-4xl text-slate-950">New order</h1>
        <p className="mt-2 text-sm text-slate-600">Create an order for a customer. Stock will be reserved when the order is saved.</p>
      </div>
      {loading ? <p className="border border-slate-200 bg-white p-6 text-sm">Loading customers and stock...</p> : null}
      {loadingError ? <p role="alert" className="border border-red-200 bg-red-50 p-4 text-sm text-red-800">{getApiErrorMessage(loadingError, "Could not load order data.")}</p> : null}
      {!loading && !loadingError && (
        <form onSubmit={submit} className="space-y-6" noValidate>
          <section className="border border-slate-200 bg-white p-6">
            <h2 className="text-lg font-semibold text-slate-950">Customer</h2>
            <label className="mt-4 block text-sm font-medium text-slate-800" htmlFor="order-customer">Customer account</label>
            <select id="order-customer" value={customerId} onChange={(event) => { setCustomerId(event.target.value); setFormError(""); }} className="mt-2 w-full border border-slate-300 bg-white p-3 text-sm text-slate-900" required>
              <option value="">Choose customer</option>
              {customers.map((customer) => <option value={customer.id} key={customer.id}>{customer.name}</option>)}
            </select>
          </section>
          <section className="border border-slate-200 bg-white p-6">
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div><h2 className="text-lg font-semibold text-slate-950">Order lines</h2><p className="mt-1 text-xs text-slate-500">Only products in stock are available.</p></div>
              <button type="button" className="inline-flex items-center gap-2 border border-slate-300 px-3 py-2 text-sm font-semibold hover:bg-slate-100" onClick={() => setItems((current) => [...current, { productId: "", quantity: 1 }])}>
                <Plus size={16} /> Add product
              </button>
            </div>
            <div className="mt-5 space-y-4">
              {items.map((item, index) => {
                const stock = inventory.find((entry) => entry.productId === item.productId);
                const product = products.find((entry) => entry.id === item.productId);
                return (
                  <div className="grid gap-3 border-b border-slate-200 pb-4 sm:grid-cols-[minmax(0,1fr)_110px_110px_auto] sm:items-end" key={index}>
                    <label className="block text-xs font-semibold text-slate-700">Product
                      <select value={item.productId} onChange={(event) => changeItem(index, { productId: event.target.value })} className="mt-2 w-full border border-slate-300 bg-white p-3 text-sm text-slate-900" required>
                        <option value="">Select product</option>
                        {stockedProducts.filter((entry) => entry.id === item.productId || !items.some((other, i) => i !== index && other.productId === entry.id)).map((entry) => (
                          <option value={entry.id} key={entry.id}>{entry.name}</option>
                        ))}
                      </select>
                      {stock && <span className="mt-1 block text-xs font-normal text-slate-500">Available: {stock.quantityInStock}</span>}
                    </label>
                    <label className="block text-xs font-semibold text-slate-700">Quantity
                      <input type="number" min="1" max={stock?.quantityInStock} value={item.quantity} onChange={(event) => changeItem(index, { quantity: Number(event.target.value) })} className="mt-2 w-full border border-slate-300 bg-white p-3 text-sm text-slate-900" required />
                    </label>
                    <div><p className="text-xs font-semibold text-slate-700">Line value</p><p className="mt-3 text-sm font-semibold">{currency.format((product?.price ?? 0) * item.quantity)}</p></div>
                    <button type="button" disabled={items.length === 1} className="border border-slate-300 p-3 text-red-800 hover:bg-red-50 disabled:opacity-40" aria-label={`Remove order line ${index + 1}`} onClick={() => setItems((current) => current.filter((_, i) => i !== index))}><Trash2 size={16} /></button>
                  </div>
                );
              })}
            </div>
          </section>
          <section className="flex flex-wrap items-center justify-between gap-4 border border-slate-200 bg-white p-6">
            <div><p className="text-xs uppercase tracking-widest text-slate-500">Estimated order value</p><p className="ops-display mt-2 text-3xl">{currency.format(estimate)}</p><p className="mt-1 text-xs text-slate-500">Final prices and stock are validated by the API.</p></div>
            <div className="flex flex-wrap gap-3"><Link to="/orders" className="border border-slate-300 px-5 py-3 text-sm font-semibold">Cancel</Link><button type="submit" disabled={mutation.isPending || stockedProducts.length === 0} className="bg-[#3e5d51] px-5 py-3 text-sm font-semibold text-white disabled:opacity-50">{mutation.isPending ? "Creating..." : "Create order"}</button></div>
          </section>
          {formError && <p role="alert" className="flex items-center gap-2 border border-red-200 bg-red-50 p-4 text-sm text-red-700"><AlertTriangle size={17} />{formError}</p>}
          {mutation.isError && <p role="alert" className="border border-red-200 bg-red-50 p-4 text-sm text-red-700">{getApiErrorMessage(mutation.error, "Could not create the order.")}</p>}
        </form>
      )}
    </div>
  );
}
