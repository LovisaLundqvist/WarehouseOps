import { type FormEvent, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Package, Search, SlidersHorizontal } from "lucide-react";
import { getProducts } from "../api/productsApi";
import type { ProductFilters } from "../types/product";

const currencyFormatter = new Intl.NumberFormat("sv-SE", {
  style: "currency",
  currency: "SEK",
});

export default function ProductsPage() {
  const searchInputRef = useRef<HTMLInputElement>(null);
  const categoryInputRef = useRef<HTMLInputElement>(null);

  const [draftFilters, setDraftFilters] = useState<ProductFilters>({
    search: "",
    category: "",
  });

  const [filters, setFilters] = useState<ProductFilters>({
    search: "",
    category: "",
  });

  const {
    data: products = [],
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ["products", filters],
    queryFn: () => getProducts(filters),
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setFilters({
      search: draftFilters.search?.trim() ?? "",
      category: draftFilters.category?.trim() ?? "",
    });
  }

  function handleClearFilters() {
    const emptyFilters = {
      search: "",
      category: "",
    };

    setDraftFilters(emptyFilters);
    setFilters(emptyFilters);
  }

  const errorMessage = error instanceof Error ? error.message : "Could not load products.";

  return (
    <div className="space-y-6">
      <section className="flex flex-col justify-between gap-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm lg:flex-row lg:items-center">
        <div>
          <div className="flex items-center gap-3">
            <div className="rounded-xl bg-blue-50 p-3 text-blue-600">
              <Package size={24} />
            </div>

            <div>
              <p className="text-sm font-medium text-blue-600">Product catalog</p>
              <h2 className="text-2xl font-bold text-slate-950">Products</h2>
            </div>
          </div>

          <p className="mt-4 max-w-2xl text-sm leading-6 text-slate-500">
            View warehouse products, search by name or SKU and filter by category.
          </p>
        </div>

        <div className="rounded-2xl bg-slate-50 px-5 py-4">
          <p className="text-sm font-medium text-slate-500">Products loaded</p>
          <p className="mt-1 text-2xl font-bold text-slate-950">{products.length}</p>
        </div>
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <form onSubmit={handleSubmit} className="grid gap-4 lg:grid-cols-[1fr_1fr_auto_auto]">
          <label className="block">
            <span className="text-sm font-medium text-slate-700">Search</span>
            <div
              onClick={() => searchInputRef.current?.focus()}
              className="mt-2 flex cursor-text items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2"
            >
              <Search size={16} className="pointer-events-none text-slate-400" />
              <input
                ref={searchInputRef}
                value={draftFilters.search}
                onChange={(event) =>
                  setDraftFilters((current) => ({
                    ...current,
                    search: event.target.value,
                  }))
                }
                placeholder="Name or SKU"
                className="w-full border-0 bg-transparent text-sm text-slate-950 outline-none placeholder:text-slate-400"
              />
            </div>
          </label>

          <label className="block">
            <span className="text-sm font-medium text-slate-700">Category</span>
            <div
              onClick={() => categoryInputRef.current?.focus()}
              className="mt-2 flex cursor-text items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2"
            >
              <SlidersHorizontal size={16} className="pointer-events-none text-slate-400" />
              <input
                ref={categoryInputRef}
                value={draftFilters.category}
                onChange={(event) =>
                  setDraftFilters((current) => ({
                    ...current,
                    category: event.target.value,
                  }))
                }
                placeholder="Example: Electronics"
                className="w-full border-0 bg-transparent text-sm text-slate-950 outline-none placeholder:text-slate-400"
              />
            </div>
          </label>

          <button
            type="submit"
            className="self-end rounded-xl bg-blue-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-blue-700"
          >
            Apply filters
          </button>

          <button
            type="button"
            onClick={handleClearFilters}
            className="self-end rounded-xl border border-slate-200 bg-white px-5 py-2.5 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50"
          >
            Clear
          </button>
        </form>
      </section>

      <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-200 px-5 py-4">
          <h3 className="text-base font-semibold text-slate-950">Product list</h3>
          <p className="mt-1 text-sm text-slate-500">
            Data is loaded from the ASP.NET Core Product API.
          </p>
        </div>

        {isLoading && (
          <div className="p-6 text-sm text-slate-500">
            Loading products...
          </div>
        )}

        {isError && (
          <div className="m-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            {errorMessage}
          </div>
        )}

        {!isLoading && !isError && products.length === 0 && (
          <div className="p-6 text-sm text-slate-500">
            No products found.
          </div>
        )}

        {!isLoading && !isError && products.length > 0 && (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Name
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    SKU
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Category
                  </th>
                  <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Price
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Created
                  </th>
                </tr>
              </thead>

              <tbody className="divide-y divide-slate-100 bg-white">
                {products.map((product) => (
                  <tr key={product.id} className="hover:bg-slate-50">
                    <td className="px-5 py-4">
                      <div>
                        <p className="text-sm font-semibold text-slate-950">{product.name}</p>
                        <p className="mt-1 max-w-xl text-sm text-slate-500">{product.description}</p>
                      </div>
                    </td>

                    <td className="whitespace-nowrap px-5 py-4 text-sm font-medium text-slate-700">
                      {product.sku}
                    </td>

                    <td className="whitespace-nowrap px-5 py-4">
                      <span className="rounded-full bg-blue-50 px-3 py-1 text-xs font-medium text-blue-700">
                        {product.category}
                      </span>
                    </td>

                    <td className="whitespace-nowrap px-5 py-4 text-right text-sm font-semibold text-slate-950">
                      {currencyFormatter.format(product.price)}
                    </td>

                    <td className="whitespace-nowrap px-5 py-4 text-sm text-slate-500">
                      {new Date(product.createdAt).toLocaleDateString("sv-SE")}
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
