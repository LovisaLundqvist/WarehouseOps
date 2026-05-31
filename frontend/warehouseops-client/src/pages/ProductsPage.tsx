import { type FormEvent, useMemo, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AlertTriangle,
  CheckCircle2,
  Package,
  PackagePlus,
  Pencil,
  Plus,
  Search,
  SlidersHorizontal,
  Trash2,
  X,
} from "lucide-react";
import { type SubmitHandler, useForm } from "react-hook-form";
import { z } from "zod";
import { useAuth } from "../auth/AuthContext";
import { createInventoryItem, getInventoryItems } from "../api/inventoryApi";
import { createProduct, deleteProduct, getProducts, updateProduct } from "../api/productsApi";
import type {
  CreateProductRequest,
  Product,
  ProductFilters,
  UpdateProductRequest,
} from "../types/product";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";

const currencyFormatter = new Intl.NumberFormat("sv-SE", {
  style: "currency",
  currency: "SEK",
});

const productSchema = z.object({
  name: z.string().trim().min(1, "Name is required."),
  sku: z.string().trim().min(1, "SKU is required."),
  category: z.string().trim().min(1, "Category is required."),
  description: z.string().optional(),
  price: z.number().min(0, "Price cannot be negative."),
});

type ProductFormValues = z.infer<typeof productSchema>;

const initialProductFormValues: ProductFormValues = {
  name: "",
  sku: "",
  category: "",
  description: "",
  price: 0,
};

export default function ProductsPage() {
  const queryClient = useQueryClient();
  const { user } = useAuth();

  const canManageProducts = user?.role === "Admin";
  const canAddProductsToInventory = user?.role === "Admin" || user?.role === "WarehouseStaff";

  const [draftFilters, setDraftFilters] = useState<ProductFilters>({
    search: "",
    category: "",
  });

  const [filters, setFilters] = useState<ProductFilters>({
    search: "",
    category: "",
  });

  const [successMessage, setSuccessMessage] = useState("");
  const [editingProductId, setEditingProductId] = useState<string | null>(null);
  const [editingProductName, setEditingProductName] = useState("");
  const [productToDelete, setProductToDelete] = useState<Product | null>(null);

  const isEditing = editingProductId !== null;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ProductFormValues>({
    resolver: zodResolver(productSchema),
    defaultValues: initialProductFormValues,
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

  const {
    data: inventoryItems = [],
    isLoading: isInventoryLoading,
    isError: isInventoryError,
    error: inventoryError,
  } = useQuery({
    queryKey: ["inventory", "products-page"],
    queryFn: getInventoryItems,
  });

  const inventoryProductIds = useMemo(() => {
    return new Set(inventoryItems.map((inventoryItem) => inventoryItem.productId));
  }, [inventoryItems]);

  const createProductMutation = useMutation({
    mutationFn: createProduct,
    onSuccess: (createdProduct) => {
      setSuccessMessage(`${createdProduct.name} was created.`);
      reset(initialProductFormValues);
      queryClient.invalidateQueries({ queryKey: ["products"] });
    },
  });

  const updateProductMutation = useMutation({
    mutationFn: (data: { id: string; request: UpdateProductRequest }) =>
      updateProduct(data.id, data.request),
    onSuccess: () => {
      setSuccessMessage("Product was updated.");
      setEditingProductId(null);
      setEditingProductName("");
      reset(initialProductFormValues);
      queryClient.invalidateQueries({ queryKey: ["products"] });
    },
  });

  const deleteProductMutation = useMutation({
    mutationFn: deleteProduct,
    onSuccess: (_data, deletedProductId) => {
      setSuccessMessage("Product was deleted.");
      setProductToDelete(null);

      if (editingProductId === deletedProductId) {
        setEditingProductId(null);
        setEditingProductName("");
        reset(initialProductFormValues);
      }

      queryClient.invalidateQueries({ queryKey: ["products"] });
    },
  });

  const addToInventoryMutation = useMutation({
    mutationFn: (productId: string) =>
      createInventoryItem({
        productId,
        quantityInStock: 0,
        minimumStockLevel: 0,
      }),
    onSuccess: (inventoryItem) => {
      setSuccessMessage(`${inventoryItem.productName} was added to inventory.`);
      queryClient.invalidateQueries({ queryKey: ["inventory"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
    },
  });

  function handleFilterSubmit(event: FormEvent<HTMLFormElement>) {
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

  function handleStartEdit(product: Product) {
    if (!canManageProducts) {
      return;
    }

    setSuccessMessage("");
    setProductToDelete(null);
    createProductMutation.reset();
    updateProductMutation.reset();
    deleteProductMutation.reset();
    addToInventoryMutation.reset();

    setEditingProductId(product.id);
    setEditingProductName(product.name);

    reset({
      name: product.name,
      sku: product.sku,
      category: product.category,
      description: product.description ?? "",
      price: product.price,
    });

    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function handleCancelEdit() {
    setSuccessMessage("");
    createProductMutation.reset();
    updateProductMutation.reset();

    setEditingProductId(null);
    setEditingProductName("");
    reset(initialProductFormValues);
  }

  function handleRequestDelete(product: Product) {
    if (!canManageProducts) {
      return;
    }

    setSuccessMessage("");
    createProductMutation.reset();
    updateProductMutation.reset();
    deleteProductMutation.reset();
    addToInventoryMutation.reset();

    setProductToDelete(product);
  }

  function handleCancelDelete() {
    setProductToDelete(null);
    deleteProductMutation.reset();
  }

  function handleConfirmDelete() {
    if (!canManageProducts || !productToDelete) {
      return;
    }

    deleteProductMutation.mutate(productToDelete.id);
  }

  function handleAddToInventory(product: Product) {
    if (!canAddProductsToInventory || inventoryProductIds.has(product.id)) {
      return;
    }

    setSuccessMessage("");
    createProductMutation.reset();
    updateProductMutation.reset();
    deleteProductMutation.reset();
    addToInventoryMutation.reset();

    addToInventoryMutation.mutate(product.id);
  }

  const handleSaveProduct: SubmitHandler<ProductFormValues> = (values) => {
    if (!canManageProducts) {
      return;
    }

    setSuccessMessage("");
    createProductMutation.reset();
    updateProductMutation.reset();
    deleteProductMutation.reset();
    addToInventoryMutation.reset();

    const request: CreateProductRequest | UpdateProductRequest = {
      name: values.name.trim(),
      sku: values.sku.trim(),
      category: values.category.trim(),
      description: values.description?.trim() ?? "",
      price: values.price,
    };

    if (editingProductId) {
      updateProductMutation.mutate({
        id: editingProductId,
        request,
      });

      return;
    }

    createProductMutation.mutate(request);
  };

  const listErrorMessage = getApiErrorMessage(error, "Could not load products.");

  const inventoryErrorMessage = getApiErrorMessage(
    inventoryError,
    "Could not load inventory status for products.",
  );

  const saveErrorMessage = getApiErrorMessage(
    isEditing ? updateProductMutation.error : createProductMutation.error,
    isEditing ? "Could not update product." : "Could not create product.",
  );

  const deleteErrorMessage = getApiErrorMessage(
    deleteProductMutation.error,
    "Could not delete product.",
  );

  const addToInventoryErrorMessage = getApiErrorMessage(
    addToInventoryMutation.error,
    "Could not add product to inventory.",
  );

  const isSaving = createProductMutation.isPending || updateProductMutation.isPending;
  const hasSaveError = createProductMutation.isError || updateProductMutation.isError;
  const showActionsColumn = canManageProducts || canAddProductsToInventory;

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
            View warehouse products, add products to inventory and manage product details based on your role.
          </p>
        </div>

        <div className="rounded-2xl bg-slate-50 px-5 py-4">
          <p className="text-sm font-medium text-slate-500">Products loaded</p>
          <p className="mt-1 text-2xl font-bold text-slate-950">{products.length}</p>
        </div>
      </section>

      {canManageProducts ? (
        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-5 flex items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <div className="rounded-xl bg-blue-50 p-3 text-blue-600">
                {isEditing ? <Pencil size={20} /> : <Plus size={20} />}
              </div>

              <div>
                <h3 className="text-base font-semibold text-slate-950">
                  {isEditing ? "Update product" : "Create product"}
                </h3>
                <p className="mt-1 text-sm text-slate-500">
                  {isEditing
                    ? `Editing ${editingProductName}. Save changes or cancel editing.`
                    : "Add a new product to the warehouse catalog."}
                </p>
              </div>
            </div>

            {isEditing && (
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

          <form onSubmit={handleSubmit(handleSaveProduct)} className="grid gap-4 lg:grid-cols-2">
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Name</span>
              <input
                {...register("name")}
                placeholder="Example: Laptop Dell XPS 15"
                className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              />
              {errors.name && <p className="mt-1 text-sm text-red-600">{errors.name.message}</p>}
            </label>

            <label className="block">
              <span className="text-sm font-medium text-slate-700">SKU</span>
              <input
                {...register("sku")}
                placeholder="Example: LAP-DELL-XPS15"
                className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              />
              {errors.sku && <p className="mt-1 text-sm text-red-600">{errors.sku.message}</p>}
            </label>

            <label className="block">
              <span className="text-sm font-medium text-slate-700">Category</span>
              <input
                {...register("category")}
                placeholder="Example: Electronics"
                className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              />
              {errors.category && (
                <p className="mt-1 text-sm text-red-600">{errors.category.message}</p>
              )}
            </label>

            <label className="block">
              <span className="text-sm font-medium text-slate-700">Price</span>
              <input
                {...register("price", { valueAsNumber: true })}
                type="number"
                min="0"
                step="0.01"
                placeholder="Example: 18999"
                className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              />
              {errors.price && <p className="mt-1 text-sm text-red-600">{errors.price.message}</p>}
            </label>

            <label className="block lg:col-span-2">
              <span className="text-sm font-medium text-slate-700">Description</span>
              <textarea
                {...register("description")}
                rows={3}
                placeholder="Short product description"
                className="mt-2 w-full resize-none rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              />
            </label>

            {successMessage && (
              <div className="rounded-xl border border-green-200 bg-green-50 p-4 text-sm text-green-700 lg:col-span-2">
                {successMessage}
              </div>
            )}

            {hasSaveError && (
              <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 lg:col-span-2">
                {saveErrorMessage}
              </div>
            )}

            {addToInventoryMutation.isError && (
              <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 lg:col-span-2">
                {addToInventoryErrorMessage}
              </div>
            )}

            <div className="flex justify-end lg:col-span-2">
              <button
                type="submit"
                disabled={isSaving}
                className="rounded-xl bg-blue-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-300"
              >
                {isSaving
                  ? isEditing
                    ? "Updating..."
                    : "Creating..."
                  : isEditing
                    ? "Update product"
                    : "Create product"}
              </button>
            </div>
          </form>
        </section>
      ) : (
        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h3 className="text-base font-semibold text-slate-950">Read only product access</h3>
          <p className="mt-1 text-sm leading-6 text-slate-500">
            Your role can view products. Product creation, editing and deletion are only available for Admin users.
          </p>

          {addToInventoryMutation.isError && (
            <div className="mt-4 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
              {addToInventoryErrorMessage}
            </div>
          )}

          {successMessage && (
            <div className="mt-4 rounded-xl border border-green-200 bg-green-50 p-4 text-sm text-green-700">
              {successMessage}
            </div>
          )}
        </section>
      )}

      {canManageProducts && productToDelete && (
        <section className="rounded-2xl border border-red-200 bg-red-50 p-5 shadow-sm">
          <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
            <div className="flex gap-3">
              <div className="mt-1 rounded-xl bg-red-100 p-3 text-red-700">
                <AlertTriangle size={22} />
              </div>

              <div>
                <h3 className="text-base font-semibold text-red-950">Confirm delete</h3>
                <p className="mt-1 text-sm leading-6 text-red-800">
                  You are about to delete <span className="font-semibold">{productToDelete.name}</span> with SKU{" "}
                  <span className="font-semibold">{productToDelete.sku}</span>. This action should only be used when
                  the product is no longer needed.
                </p>

                {deleteProductMutation.isError && (
                  <p className="mt-3 rounded-xl border border-red-200 bg-white p-3 text-sm text-red-700">
                    {deleteErrorMessage}
                  </p>
                )}
              </div>
            </div>

            <div className="flex gap-3">
              <button
                type="button"
                onClick={handleCancelDelete}
                disabled={deleteProductMutation.isPending}
                className="rounded-xl border border-red-200 bg-white px-4 py-2 text-sm font-semibold text-red-700 shadow-sm hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-60"
              >
                Cancel
              </button>

              <button
                type="button"
                onClick={handleConfirmDelete}
                disabled={deleteProductMutation.isPending}
                className="rounded-xl bg-red-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-red-700 disabled:cursor-not-allowed disabled:bg-red-300"
              >
                {deleteProductMutation.isPending ? "Deleting..." : "Delete product"}
              </button>
            </div>
          </div>
        </section>
      )}

      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <form onSubmit={handleFilterSubmit} className="grid gap-4 lg:grid-cols-[1fr_1fr_auto_auto]">
          <label className="block">
            <span className="text-sm font-medium text-slate-700">Search</span>
            <div className="mt-2 flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2">
              <Search size={16} className="text-slate-400" />
              <input
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
            <div className="mt-2 flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2">
              <SlidersHorizontal size={16} className="text-slate-400" />
              <input
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
            Shows all products currently registered in the warehouse.
          </p>

          {isInventoryError && (
            <p className="mt-3 rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800">
              {inventoryErrorMessage}
            </p>
          )}
        </div>

        {isLoading && (
          <div className="p-6 text-sm text-slate-500">
            Loading products...
          </div>
        )}

        {isError && (
          <div className="m-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            {listErrorMessage}
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
                    Inventory
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Created
                  </th>
                  {showActionsColumn && (
                    <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                      Actions
                    </th>
                  )}
                </tr>
              </thead>

              <tbody className="divide-y divide-slate-100 bg-white">
                {products.map((product) => {
                  const isInInventory = inventoryProductIds.has(product.id);
                  const isAddingThisProduct =
                    addToInventoryMutation.isPending &&
                    addToInventoryMutation.variables === product.id;

                  return (
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

                      <td className="whitespace-nowrap px-5 py-4">
                        {isInventoryLoading ? (
                          <span className="text-sm text-slate-500">Checking...</span>
                        ) : isInInventory ? (
                          <span className="inline-flex items-center gap-2 rounded-full bg-green-50 px-3 py-1 text-xs font-semibold text-green-700">
                            <CheckCircle2 size={14} />
                            In inventory
                          </span>
                        ) : (
                          <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-700">
                            Not in inventory
                          </span>
                        )}
                      </td>

                      <td className="whitespace-nowrap px-5 py-4 text-sm text-slate-500">
                        {new Date(product.createdAt).toLocaleDateString("sv-SE")}
                      </td>

                      {showActionsColumn && (
                        <td className="whitespace-nowrap px-5 py-4 text-right">
                          <div className="flex justify-end gap-2">
                            {canAddProductsToInventory && !isInInventory && (
                              <button
                                type="button"
                                onClick={() => handleAddToInventory(product)}
                                disabled={isAddingThisProduct || isInventoryLoading || isInventoryError}
                                className="inline-flex items-center gap-2 rounded-xl border border-blue-200 bg-white px-3 py-2 text-sm font-semibold text-blue-700 shadow-sm hover:bg-blue-50 disabled:cursor-not-allowed disabled:opacity-60"
                              >
                                <PackagePlus size={15} />
                                {isAddingThisProduct ? "Adding..." : "Add to inventory"}
                              </button>
                            )}

                            {canManageProducts && (
                              <>
                                <button
                                  type="button"
                                  onClick={() => handleStartEdit(product)}
                                  className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50"
                                >
                                  <Pencil size={15} />
                                  Edit
                                </button>

                                <button
                                  type="button"
                                  onClick={() => handleRequestDelete(product)}
                                  className="inline-flex items-center gap-2 rounded-xl border border-red-200 bg-white px-3 py-2 text-sm font-semibold text-red-700 shadow-sm hover:bg-red-50"
                                >
                                  <Trash2 size={15} />
                                  Delete
                                </button>
                              </>
                            )}
                          </div>
                        </td>
                      )}
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
