import { type FormEvent, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AlertTriangle,
  CheckCircle2,
  CircleAlert,
  Link2,
  Plus,
  Search,
  Settings,
  X,
} from "lucide-react";
import { type SubmitHandler, useForm } from "react-hook-form";
import { z } from "zod";
import { useAuth } from "../auth/AuthContext";
import {
  createIncident,
  getIncidents,
  resolveIncident,
  updateIncidentStatus,
} from "../api/incidentsApi";
import type {
  CreateIncidentRequest,
  Incident,
  IncidentFilters,
  ResolveIncidentRequest,
} from "../types/incident";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";
import { formatShortId } from "../utils/formatShortId";

const incidentStatuses = ["Open", "InProgress", "Resolved", "Closed"];
const incidentSeverities = ["Low", "Medium", "High", "Critical"];
const relatedEntityTypes = ["General", "Product", "Inventory", "Customer", "Order", "Shipment"];

const createIncidentSchema = z
  .object({
    title: z.string().trim().min(1, "Title is required."),
    description: z.string().trim().min(1, "Description is required."),
    severity: z.string().trim().min(1, "Severity is required."),
    relatedEntityType: z.string().trim().min(1, "Area is required."),
    relatedEntityId: z
      .string()
      .trim()
      .max(100, "Reference number cannot be longer than 100 characters."),
  })
  .refine(
    (values) => values.relatedEntityType === "General" || values.relatedEntityId.trim().length > 0,
    {
      message: "Reference number is required when the incident is connected to another area.",
      path: ["relatedEntityId"],
    },
  );

type CreateIncidentFormValues = z.infer<typeof createIncidentSchema>;

const initialCreateIncidentValues: CreateIncidentFormValues = {
  title: "",
  description: "",
  severity: "Medium",
  relatedEntityType: "General",
  relatedEntityId: "",
};

function getStatusBadgeClass(status: string) {
  switch (status) {
    case "Open":
      return "bg-red-50 text-red-700";
    case "InProgress":
      return "bg-blue-50 text-blue-700";
    case "Resolved":
      return "bg-amber-50 text-amber-700";
    case "Closed":
      return "bg-green-50 text-green-700";
    default:
      return "bg-slate-100 text-slate-700";
  }
}

function getSeverityBadgeClass(severity: string) {
  switch (severity) {
    case "Low":
      return "bg-slate-100 text-slate-700";
    case "Medium":
      return "bg-blue-50 text-blue-700";
    case "High":
      return "bg-amber-50 text-amber-700";
    case "Critical":
      return "bg-red-50 text-red-700";
    default:
      return "bg-slate-100 text-slate-700";
  }
}

function canManageIncident(incident: Incident) {
  return incident.status !== "Closed";
}

function formatRelatedEntity(incident: Incident) {
  if (incident.relatedEntityType === "General") {
    return "General incident";
  }

  if (!incident.relatedEntityId) {
    return incident.relatedEntityType;
  }

  return `${incident.relatedEntityType}: ${incident.relatedEntityId}`;
}

export default function IncidentsPage() {
  const queryClient = useQueryClient();
  const { user } = useAuth();

  const canManageIncidents = user?.role === "Admin" || user?.role === "WarehouseStaff";

  const [draftFilters, setDraftFilters] = useState<IncidentFilters>({
    status: "",
  });

  const [filters, setFilters] = useState<IncidentFilters>({
    status: "",
  });

  const [selectedIncident, setSelectedIncident] = useState<Incident | null>(null);
  const [selectedStatus, setSelectedStatus] = useState("");
  const [resolutionNotes, setResolutionNotes] = useState("");
  const [resolutionValidationMessage, setResolutionValidationMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CreateIncidentFormValues>({
    resolver: zodResolver(createIncidentSchema),
    defaultValues: initialCreateIncidentValues,
  });

  const {
    data: incidents = [],
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ["incidents", filters],
    queryFn: () => getIncidents(filters),
  });

  const createIncidentMutation = useMutation({
    mutationFn: createIncident,
    onSuccess: (createdIncident) => {
      setSuccessMessage(`${createdIncident.title} was reported.`);
      reset(initialCreateIncidentValues);
      queryClient.invalidateQueries({ queryKey: ["incidents"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
    },
  });

  const updateStatusMutation = useMutation({
    mutationFn: (data: { id: string; status: string }) =>
      updateIncidentStatus(data.id, { status: data.status }),
    onSuccess: (updatedIncident) => {
      setSuccessMessage(`Incident status was updated to ${updatedIncident.status}.`);
      setSelectedIncident(updatedIncident);
      setSelectedStatus(updatedIncident.status);
      queryClient.invalidateQueries({ queryKey: ["incidents"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
    },
  });

  const resolveIncidentMutation = useMutation({
    mutationFn: (data: { id: string; request: ResolveIncidentRequest }) =>
      resolveIncident(data.id, data.request),
    onSuccess: (resolvedIncident) => {
      setSuccessMessage("Incident was closed.");
      setSelectedIncident(resolvedIncident);
      setSelectedStatus(resolvedIncident.status);
      setResolutionNotes("");
      setResolutionValidationMessage("");
      queryClient.invalidateQueries({ queryKey: ["incidents"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
    },
  });

  const openIncidentsCount = incidents.filter((incident) => incident.status === "Open").length;
  const inProgressIncidentsCount = incidents.filter(
    (incident) => incident.status === "InProgress",
  ).length;
  const criticalIncidentsCount = incidents.filter(
    (incident) => incident.severity === "Critical",
  ).length;

  const listErrorMessage = getApiErrorMessage(error, "Could not load incidents.");

  const createErrorMessage = getApiErrorMessage(
    createIncidentMutation.error,
    "Could not create incident.",
  );

  const actionErrorMessage = getApiErrorMessage(
    updateStatusMutation.error ?? resolveIncidentMutation.error,
    "Could not update incident.",
  );

  const hasActionError = updateStatusMutation.isError || resolveIncidentMutation.isError;

  function handleFilterSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setFilters({
      status: draftFilters.status?.trim() ?? "",
    });
  }

  function handleClearFilters() {
    const emptyFilters = {
      status: "",
    };

    setDraftFilters(emptyFilters);
    setFilters(emptyFilters);
  }

  const handleCreateIncident: SubmitHandler<CreateIncidentFormValues> = (values) => {
    if (!canManageIncidents) {
      return;
    }

    setSuccessMessage("");
    createIncidentMutation.reset();

    const request: CreateIncidentRequest = {
      title: values.title.trim(),
      description: values.description.trim(),
      severity: values.severity.trim(),
      relatedEntityType: values.relatedEntityType.trim(),
      relatedEntityId: values.relatedEntityId.trim(),
    };

    createIncidentMutation.mutate(request);
  };

  function handleSelectIncident(incident: Incident) {
    if (!canManageIncidents) {
      return;
    }

    setSuccessMessage("");
    setResolutionValidationMessage("");
    updateStatusMutation.reset();
    resolveIncidentMutation.reset();

    setSelectedIncident(incident);
    setSelectedStatus(incident.status);
    setResolutionNotes("");

    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function handleClearSelectedIncident() {
    setSuccessMessage("");
    setResolutionValidationMessage("");
    updateStatusMutation.reset();
    resolveIncidentMutation.reset();

    setSelectedIncident(null);
    setSelectedStatus("");
    setResolutionNotes("");
  }

  function handleUpdateStatus() {
    if (!canManageIncidents || !selectedIncident || !selectedStatus) {
      return;
    }

    setSuccessMessage("");
    setResolutionValidationMessage("");
    updateStatusMutation.reset();
    resolveIncidentMutation.reset();

    updateStatusMutation.mutate({
      id: selectedIncident.id,
      status: selectedStatus,
    });
  }

  function handleResolveIncident() {
    if (!canManageIncidents || !selectedIncident) {
      return;
    }

    if (!resolutionNotes.trim()) {
      setResolutionValidationMessage("Resolution notes are required.");
      return;
    }

    setSuccessMessage("");
    setResolutionValidationMessage("");
    updateStatusMutation.reset();
    resolveIncidentMutation.reset();

    resolveIncidentMutation.mutate({
      id: selectedIncident.id,
      request: {
        resolutionNotes: resolutionNotes.trim(),
      },
    });
  }

  return (
    <div className="space-y-6">
      <section className="flex flex-col justify-between gap-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm lg:flex-row lg:items-center">
        <div>
          <div className="flex items-center gap-3">
            <div className="rounded-xl bg-red-50 p-3 text-red-600">
              <AlertTriangle size={24} />
            </div>

            <div>
              <p className="text-sm font-medium text-red-600">Incident management</p>
              <h2 className="text-2xl font-bold text-slate-950">Incidents</h2>
            </div>
          </div>

          <p className="mt-4 max-w-2xl text-sm leading-6 text-slate-500">
            View operational issues, affected areas and current handling status. Incident changes are available based on your role.
          </p>
        </div>

        <div className="grid gap-3 sm:grid-cols-3">
          <div className="rounded-2xl bg-red-50 px-5 py-4">
            <p className="text-sm font-medium text-red-700">Open</p>
            <p className="mt-1 text-2xl font-bold text-red-700">{openIncidentsCount}</p>
          </div>

          <div className="rounded-2xl bg-blue-50 px-5 py-4">
            <p className="text-sm font-medium text-blue-700">In progress</p>
            <p className="mt-1 text-2xl font-bold text-blue-700">{inProgressIncidentsCount}</p>
          </div>

          <div className="rounded-2xl bg-red-50 px-5 py-4">
            <p className="text-sm font-medium text-red-700">Critical</p>
            <p className="mt-1 text-2xl font-bold text-red-700">{criticalIncidentsCount}</p>
          </div>
        </div>
      </section>

      {canManageIncidents ? (
        <>
          <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <div className="mb-5 flex items-center gap-3">
              <div className="rounded-xl bg-red-50 p-3 text-red-600">
                <Plus size={20} />
              </div>

              <div>
                <h3 className="text-base font-semibold text-slate-950">Report incident</h3>
                <p className="mt-1 text-sm text-slate-500">
                  Create a new incident and connect it to a product, stock item, customer, order or shipment when needed.
                </p>
              </div>
            </div>

            <form onSubmit={handleSubmit(handleCreateIncident)} className="grid gap-4 lg:grid-cols-3">
              <label className="block lg:col-span-2">
                <span className="text-sm font-medium text-slate-700">Title</span>
                <input
                  {...register("title")}
                  placeholder="Example: Damaged package"
                  className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                />
                {errors.title && <p className="mt-1 text-sm text-red-600">{errors.title.message}</p>}
              </label>

              <label className="block">
                <span className="text-sm font-medium text-slate-700">Severity</span>
                <select
                  {...register("severity")}
                  className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-950 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                >
                  {incidentSeverities.map((severity) => (
                    <option key={severity} value={severity}>
                      {severity}
                    </option>
                  ))}
                </select>
                {errors.severity && (
                  <p className="mt-1 text-sm text-red-600">{errors.severity.message}</p>
                )}
              </label>

              <label className="block">
                <span className="text-sm font-medium text-slate-700">Area</span>
                <select
                  {...register("relatedEntityType")}
                  className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-950 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                >
                  {relatedEntityTypes.map((type) => (
                    <option key={type} value={type}>
                      {type}
                    </option>
                  ))}
                </select>
                {errors.relatedEntityType && (
                  <p className="mt-1 text-sm text-red-600">{errors.relatedEntityType.message}</p>
                )}
              </label>

              <label className="block lg:col-span-2">
                <span className="text-sm font-medium text-slate-700">Reference number</span>
                <input
                  {...register("relatedEntityId")}
                  placeholder="Example: order number, product code or tracking number"
                  className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                />
                {errors.relatedEntityId && (
                  <p className="mt-1 text-sm text-red-600">{errors.relatedEntityId.message}</p>
                )}
              </label>

              <label className="block lg:col-span-3">
                <span className="text-sm font-medium text-slate-700">Description</span>
                <textarea
                  {...register("description")}
                  rows={4}
                  placeholder="Describe what happened and what needs attention."
                  className="mt-2 w-full resize-none rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                />
                {errors.description && (
                  <p className="mt-1 text-sm text-red-600">{errors.description.message}</p>
                )}
              </label>

              {successMessage && (
                <div className="rounded-xl border border-green-200 bg-green-50 p-4 text-sm text-green-700 lg:col-span-3">
                  {successMessage}
                </div>
              )}

              {createIncidentMutation.isError && (
                <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 lg:col-span-3">
                  {createErrorMessage}
                </div>
              )}

              <div className="flex justify-end lg:col-span-3">
                <button
                  type="submit"
                  disabled={createIncidentMutation.isPending}
                  className="rounded-xl bg-red-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-red-700 disabled:cursor-not-allowed disabled:bg-red-300"
                >
                  {createIncidentMutation.isPending ? "Reporting..." : "Report incident"}
                </button>
              </div>
            </form>
          </section>

          <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <div className="mb-5 flex items-center justify-between gap-4">
              <div className="flex items-center gap-3">
                <div className="rounded-xl bg-blue-50 p-3 text-blue-600">
                  <Settings size={20} />
                </div>

                <div>
                  <h3 className="text-base font-semibold text-slate-950">Incident actions</h3>
                  <p className="mt-1 text-sm text-slate-500">
                    {selectedIncident
                      ? `Managing incident ${selectedIncident.title}.`
                      : "Select an incident from the table to update or close it."}
                  </p>
                </div>
              </div>

              {selectedIncident && (
                <button
                  type="button"
                  onClick={handleClearSelectedIncident}
                  className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50"
                >
                  <X size={16} />
                  Clear
                </button>
              )}
            </div>

            {selectedIncident ? (
              <div className="grid gap-4 lg:grid-cols-[1fr_auto]">
                <div>
                  <label className="block">
                    <span className="text-sm font-medium text-slate-700">Incident status</span>
                    <select
                      value={selectedStatus}
                      onChange={(event) => setSelectedStatus(event.target.value)}
                      disabled={!canManageIncident(selectedIncident)}
                      className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-950 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-500"
                    >
                      {incidentStatuses.map((status) => (
                        <option key={status} value={status}>
                          {status}
                        </option>
                      ))}
                    </select>
                  </label>

                  <p className="mt-2 text-sm text-slate-500">
                    Current status:{" "}
                    <span className={`rounded-full px-3 py-1 text-xs font-semibold ${getStatusBadgeClass(selectedIncident.status)}`}>
                      {selectedIncident.status}
                    </span>
                  </p>
                </div>

                <button
                  type="button"
                  onClick={handleUpdateStatus}
                  disabled={
                    updateStatusMutation.isPending ||
                    !canManageIncident(selectedIncident) ||
                    selectedStatus === selectedIncident.status
                  }
                  className="self-end rounded-xl bg-blue-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-300"
                >
                  {updateStatusMutation.isPending ? "Updating..." : "Update status"}
                </button>

                <label className="block lg:col-span-2">
                  <span className="text-sm font-medium text-slate-700">Resolution notes</span>
                  <textarea
                    value={resolutionNotes}
                    onChange={(event) => setResolutionNotes(event.target.value)}
                    disabled={!canManageIncident(selectedIncident)}
                    rows={3}
                    placeholder="Required when closing an incident."
                    className="mt-2 w-full resize-none rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:border-blue-500 focus:ring-2 focus:ring-blue-100 disabled:cursor-not-allowed disabled:bg-slate-100"
                  />
                  {resolutionValidationMessage && (
                    <p className="mt-1 text-sm text-red-600">{resolutionValidationMessage}</p>
                  )}
                </label>

                <div className="flex justify-end lg:col-span-2">
                  <button
                    type="button"
                    onClick={handleResolveIncident}
                    disabled={resolveIncidentMutation.isPending || !canManageIncident(selectedIncident)}
                    className="rounded-xl bg-green-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-green-700 disabled:cursor-not-allowed disabled:bg-green-300"
                  >
                    {resolveIncidentMutation.isPending ? "Closing..." : "Close incident"}
                  </button>
                </div>

                {hasActionError && (
                  <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 lg:col-span-2">
                    <div className="flex gap-2">
                      <AlertTriangle size={18} />
                      <span>{actionErrorMessage}</span>
                    </div>
                  </div>
                )}

                {!canManageIncident(selectedIncident) && (
                  <div className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800 lg:col-span-2">
                    This incident cannot be changed because it is already closed.
                  </div>
                )}
              </div>
            ) : (
              <div className="rounded-xl border border-dashed border-slate-300 bg-slate-50 p-5 text-sm text-slate-500">
                No incident selected.
              </div>
            )}
          </section>
        </>
      ) : (
        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h3 className="text-base font-semibold text-slate-950">Read only incident access</h3>
          <p className="mt-1 text-sm leading-6 text-slate-500">
            Your role can view incidents, but only Admin and WarehouseStaff users can report incidents, update status or close incidents.
          </p>
        </section>
      )}

      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <form onSubmit={handleFilterSubmit} className="grid gap-4 lg:grid-cols-[1fr_auto_auto]">
          <label className="block">
            <span className="text-sm font-medium text-slate-700">Status filter</span>
            <div className="mt-2 flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2">
              <Search size={16} className="text-slate-400" />
              <select
                value={draftFilters.status}
                onChange={(event) =>
                  setDraftFilters({
                    status: event.target.value,
                  })
                }
                className="w-full border-0 bg-transparent text-sm text-slate-950 outline-none"
              >
                <option value="">All statuses</option>
                {incidentStatuses.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            </div>
          </label>

          <button
            type="submit"
            className="self-end rounded-xl bg-blue-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-blue-700"
          >
            Apply filter
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
          <h3 className="text-base font-semibold text-slate-950">Incident list</h3>
          <p className="mt-1 text-sm text-slate-500">
            Shows reported issues and their current handling status.
          </p>
        </div>

        {isLoading && (
          <div className="p-6 text-sm text-slate-500">
            Loading incidents...
          </div>
        )}

        {isError && (
          <div className="m-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            {listErrorMessage}
          </div>
        )}

        {!isLoading && !isError && incidents.length === 0 && (
          <div className="p-6 text-sm text-slate-500">
            No incidents found.
          </div>
        )}

        {!isLoading && !isError && incidents.length > 0 && (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Incident
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Severity
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Area
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Status
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Resolution
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Created
                  </th>
                  {canManageIncidents && (
                    <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                      Actions
                    </th>
                  )}
                </tr>
              </thead>

              <tbody className="divide-y divide-slate-100 bg-white">
                {incidents.map((incident) => (
                  <tr key={incident.id} className="hover:bg-slate-50">
                    <td className="px-5 py-4">
                      <div className="flex items-start gap-3">
                        <div className="rounded-xl bg-slate-50 p-2 text-slate-600">
                          <CircleAlert size={18} />
                        </div>

                        <div>
                          <p className="text-sm font-semibold text-slate-950">{incident.title}</p>
                          <p className="mt-1 max-w-xl text-sm text-slate-500">
                            {incident.description}
                          </p>
                          <p className="mt-1 text-xs text-slate-400">
                            {formatShortId(incident.id, "Incident")}
                          </p>
                        </div>
                      </div>
                    </td>

                    <td className="whitespace-nowrap px-5 py-4">
                      <span
                        className={`rounded-full px-3 py-1 text-xs font-semibold ${getSeverityBadgeClass(
                          incident.severity,
                        )}`}
                      >
                        {incident.severity}
                      </span>
                    </td>

                    <td className="px-5 py-4">
                      <p className="flex items-center gap-2 text-sm text-slate-700">
                        <Link2 size={15} className="text-slate-400" />
                        {formatRelatedEntity(incident)}
                      </p>
                    </td>

                    <td className="whitespace-nowrap px-5 py-4">
                      <span
                        className={`rounded-full px-3 py-1 text-xs font-semibold ${getStatusBadgeClass(
                          incident.status,
                        )}`}
                      >
                        {incident.status}
                      </span>
                    </td>

                    <td className="px-5 py-4">
                      {incident.resolutionNotes ? (
                        <div>
                          <p className="flex items-center gap-2 text-sm font-medium text-green-700">
                            <CheckCircle2 size={15} />
                            Resolution added
                          </p>
                          <p className="mt-1 max-w-xl text-sm text-slate-500">
                            {incident.resolutionNotes}
                          </p>
                        </div>
                      ) : (
                        <p className="text-sm text-slate-500">No resolution notes</p>
                      )}
                    </td>

                    <td className="whitespace-nowrap px-5 py-4 text-sm text-slate-500">
                      {new Date(incident.createdAt).toLocaleDateString("sv-SE")}
                    </td>

                    {canManageIncidents && (
                      <td className="whitespace-nowrap px-5 py-4 text-right">
                        <button
                          type="button"
                          onClick={() => handleSelectIncident(incident)}
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
