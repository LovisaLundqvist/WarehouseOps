import { type FormEvent, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Mail,
  MapPin,
  Pencil,
  Phone,
  Plus,
  Search,
  UserRound,
  Users,
  X,
} from "lucide-react";
import { type SubmitHandler, useForm } from "react-hook-form";
import { z } from "zod";
import { createCustomer, getCustomers, updateCustomer } from "../api/customersApi";
import type {
  CreateCustomerRequest,
  Customer,
  CustomerFilters,
  UpdateCustomerRequest,
} from "../types/customer";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";

const customerSchema = z.object({
  name: z.string().trim().min(1, "Name is required."),
  email: z.string().trim().min(1, "Email is required.").email("Email must be valid."),
  phoneNumber: z.string().optional(),
  address: z.string().optional(),
});

type CustomerFormValues = z.infer<typeof customerSchema>;

const initialCustomerFormValues: CustomerFormValues = {
  name: "",
  email: "",
  phoneNumber: "",
  address: "",
};

export default function CustomersPage() {
  const queryClient = useQueryClient();

  const [draftFilters, setDraftFilters] = useState<CustomerFilters>({
    search: "",
  });

  const [filters, setFilters] = useState<CustomerFilters>({
    search: "",
  });

  const [successMessage, setSuccessMessage] = useState("");
  const [editingCustomerId, setEditingCustomerId] = useState<string | null>(null);
  const [editingCustomerName, setEditingCustomerName] = useState("");

  const isEditing = editingCustomerId !== null;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CustomerFormValues>({
    resolver: zodResolver(customerSchema),
    defaultValues: initialCustomerFormValues,
  });

  const {
    data: customers = [],
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ["customers", filters],
    queryFn: () => getCustomers(filters),
  });

  const createCustomerMutation = useMutation({
    mutationFn: createCustomer,
    onSuccess: (createdCustomer) => {
      setSuccessMessage(`${createdCustomer.name} was created.`);
      reset(initialCustomerFormValues);
      queryClient.invalidateQueries({ queryKey: ["customers"] });
    },
  });

  const updateCustomerMutation = useMutation({
    mutationFn: (data: { id: string; request: UpdateCustomerRequest }) =>
      updateCustomer(data.id, data.request),
    onSuccess: (updatedCustomer) => {
      setSuccessMessage(`${updatedCustomer.name} was updated.`);
      setEditingCustomerId(null);
      setEditingCustomerName("");
      reset(initialCustomerFormValues);
      queryClient.invalidateQueries({ queryKey: ["customers"] });
    },
  });

  function handleFilterSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setFilters({
      search: draftFilters.search?.trim() ?? "",
    });
  }

  function handleClearFilters() {
    const emptyFilters = {
      search: "",
    };

    setDraftFilters(emptyFilters);
    setFilters(emptyFilters);
  }

  function handleStartEdit(customer: Customer) {
    setSuccessMessage("");
    createCustomerMutation.reset();
    updateCustomerMutation.reset();

    setEditingCustomerId(customer.id);
    setEditingCustomerName(customer.name);

    reset({
      name: customer.name,
      email: customer.email,
      phoneNumber: customer.phoneNumber ?? "",
      address: customer.address ?? "",
    });

    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function handleCancelEdit() {
    setSuccessMessage("");
    createCustomerMutation.reset();
    updateCustomerMutation.reset();

    setEditingCustomerId(null);
    setEditingCustomerName("");
    reset(initialCustomerFormValues);
  }

  const handleSaveCustomer: SubmitHandler<CustomerFormValues> = (values) => {
    setSuccessMessage("");
    createCustomerMutation.reset();
    updateCustomerMutation.reset();

    const request: CreateCustomerRequest | UpdateCustomerRequest = {
      name: values.name.trim(),
      email: values.email.trim(),
      phoneNumber: values.phoneNumber?.trim() ?? "",
      address: values.address?.trim() ?? "",
    };

    if (editingCustomerId) {
      updateCustomerMutation.mutate({
        id: editingCustomerId,
        request,
      });

      return;
    }

    createCustomerMutation.mutate(request);
  };

  const listErrorMessage = getApiErrorMessage(error, "Could not load customers.");

  const saveErrorMessage = getApiErrorMessage(
    isEditing ? updateCustomerMutation.error : createCustomerMutation.error,
    isEditing ? "Could not update customer." : "Could not create customer.",
  );

  const isSaving = createCustomerMutation.isPending || updateCustomerMutation.isPending;
  const hasSaveError = createCustomerMutation.isError || updateCustomerMutation.isError;

  return (
    <div className="space-y-6">
      <section className="flex flex-col justify-between gap-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm lg:flex-row lg:items-center">
        <div>
          <div className="flex items-center gap-3">
            <div className="rounded-xl bg-blue-50 p-3 text-blue-600">
              <Users size={24} />
            </div>

            <div>
              <p className="text-sm font-medium text-blue-600">Customer management</p>
              <h2 className="text-2xl font-bold text-slate-950">Customers</h2>
            </div>
          </div>

          <p className="mt-4 max-w-2xl text-sm leading-6 text-slate-500">
            Create, update and view customers that can be connected to warehouse orders.
          </p>
        </div>

        <div className="rounded-2xl bg-slate-50 px-5 py-4">
          <p className="text-sm font-medium text-slate-500">Customers loaded</p>
          <p className="mt-1 text-2xl font-bold text-slate-950">{customers.length}</p>
        </div>
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="mb-5 flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="rounded-xl bg-blue-50 p-3 text-blue-600">
              {isEditing ? <Pencil size={20} /> : <Plus size={20} />}
            </div>

            <div>
              <h3 className="text-base font-semibold text-slate-950">
                {isEditing ? "Update customer" : "Create customer"}
              </h3>
              <p className="mt-1 text-sm text-slate-500">
                {isEditing
                  ? `Editing ${editingCustomerName}. Save changes or cancel editing.`
                  : "Add a new customer to the customer register."}
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

        <form onSubmit={handleSubmit(handleSaveCustomer)} className="grid gap-4 lg:grid-cols-2">
          <label className="block">
            <span className="text-sm font-medium text-slate-700">Name</span>
            <input
              {...register("name")}
              placeholder="Example: Nordic Retail AB"
              className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            />
            {errors.name && <p className="mt-1 text-sm text-red-600">{errors.name.message}</p>}
          </label>

          <label className="block">
            <span className="text-sm font-medium text-slate-700">Email</span>
            <input
              {...register("email")}
              placeholder="Example: logistics@nordic-retail.se"
              className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            />
            {errors.email && <p className="mt-1 text-sm text-red-600">{errors.email.message}</p>}
          </label>

          <label className="block">
            <span className="text-sm font-medium text-slate-700">Phone number</span>
            <input
              {...register("phoneNumber")}
              placeholder="Example: +46 31 123 456"
              className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            />
          </label>

          <label className="block">
            <span className="text-sm font-medium text-slate-700">Address</span>
            <input
              {...register("address")}
              placeholder="Example: Lagergatan 12, Göteborg"
              className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
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
                  ? "Update customer"
                  : "Create customer"}
            </button>
          </div>
        </form>
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <form onSubmit={handleFilterSubmit} className="grid gap-4 lg:grid-cols-[1fr_auto_auto]">
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
                placeholder="Name, email or phone"
                className="w-full border-0 bg-transparent text-sm text-slate-950 outline-none placeholder:text-slate-400"
              />
            </div>
          </label>

          <button
            type="submit"
            className="self-end rounded-xl bg-blue-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-blue-700"
          >
            Apply search
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
          <h3 className="text-base font-semibold text-slate-950">Customer list</h3>
          <p className="mt-1 text-sm text-slate-500">
            Data is loaded from the ASP.NET Core Customers API.
          </p>
        </div>

        {isLoading && (
          <div className="p-6 text-sm text-slate-500">
            Loading customers...
          </div>
        )}

        {isError && (
          <div className="m-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            {listErrorMessage}
          </div>
        )}

        {!isLoading && !isError && customers.length === 0 && (
          <div className="p-6 text-sm text-slate-500">
            No customers found.
          </div>
        )}

        {!isLoading && !isError && customers.length > 0 && (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Customer
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Contact
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Address
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
                {customers.map((customer) => (
                  <tr key={customer.id} className="hover:bg-slate-50">
                    <td className="px-5 py-4">
                      <div className="flex items-start gap-3">
                        <div className="rounded-xl bg-slate-50 p-2 text-slate-600">
                          <UserRound size={18} />
                        </div>

                        <div>
                          <p className="text-sm font-semibold text-slate-950">{customer.name}</p>
                          <p className="mt-1 text-xs text-slate-500">{customer.id}</p>
                        </div>
                      </div>
                    </td>

                    <td className="px-5 py-4">
                      <div className="space-y-2">
                        <p className="flex items-center gap-2 text-sm text-slate-700">
                          <Mail size={15} className="text-slate-400" />
                          {customer.email}
                        </p>
                        <p className="flex items-center gap-2 text-sm text-slate-500">
                          <Phone size={15} className="text-slate-400" />
                          {customer.phoneNumber || "No phone number"}
                        </p>
                      </div>
                    </td>

                    <td className="px-5 py-4">
                      <p className="flex items-center gap-2 text-sm text-slate-600">
                        <MapPin size={15} className="text-slate-400" />
                        {customer.address || "No address"}
                      </p>
                    </td>

                    <td className="whitespace-nowrap px-5 py-4 text-sm text-slate-500">
                      {new Date(customer.createdAt).toLocaleDateString("sv-SE")}
                    </td>

                    <td className="whitespace-nowrap px-5 py-4 text-right">
                      <button
                        type="button"
                        onClick={() => handleStartEdit(customer)}
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
