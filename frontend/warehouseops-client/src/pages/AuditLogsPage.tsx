import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Activity,
  ClipboardList,
  Clock,
  Database,
  Filter,
  Search,
  ShieldCheck,
  UserRound,
} from "lucide-react";
import { getAuditLogs } from "../api/auditLogsApi";
import type { AuditLog } from "../types/auditLog";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";

function getActionBadgeClass(action: string) {
  switch (action) {
    case "Created":
      return "bg-green-50 text-green-700";
    case "Updated":
      return "bg-blue-50 text-blue-700";
    case "Deleted":
      return "bg-red-50 text-red-700";
    case "Cancelled":
      return "bg-red-50 text-red-700";
    case "Closed":
      return "bg-green-50 text-green-700";
    default:
      return "bg-slate-100 text-slate-700";
  }
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString("sv-SE", {
    dateStyle: "short",
    timeStyle: "short",
  });
}

export default function AuditLogsPage() {
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedEntity, setSelectedEntity] = useState("");

  const {
    data: auditLogs = [],
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ["audit-logs"],
    queryFn: getAuditLogs,
  });

  const entityNames = useMemo(() => {
    return Array.from(new Set(auditLogs.map((auditLog) => auditLog.entityName))).sort();
  }, [auditLogs]);

  const filteredAuditLogs = useMemo(() => {
    return auditLogs.filter((auditLog) => {
      const matchesEntity = selectedEntity
        ? auditLog.entityName === selectedEntity
        : true;

      const normalizedSearch = searchTerm.trim().toLowerCase();

      const matchesSearch = normalizedSearch
        ? auditLog.entityName.toLowerCase().includes(normalizedSearch) ||
          auditLog.action.toLowerCase().includes(normalizedSearch) ||
          auditLog.performedBy.toLowerCase().includes(normalizedSearch) ||
          auditLog.changes.toLowerCase().includes(normalizedSearch)
        : true;

      return matchesEntity && matchesSearch;
    });
  }, [auditLogs, searchTerm, selectedEntity]);

  const createdCount = auditLogs.filter((auditLog) => auditLog.action === "Created").length;
  const updatedCount = auditLogs.filter((auditLog) => auditLog.action === "Updated").length;
  const deletedCount = auditLogs.filter((auditLog) => auditLog.action === "Deleted").length;

  const errorMessage = getApiErrorMessage(error, "Could not load audit logs.");

  function handleClearFilters() {
    setSearchTerm("");
    setSelectedEntity("");
  }

  return (
    <div className="space-y-6">
      <section className="flex flex-col justify-between gap-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm lg:flex-row lg:items-center">
        <div>
          <div className="flex items-center gap-3">
            <div className="rounded-xl bg-blue-50 p-3 text-blue-600">
              <ShieldCheck size={24} />
            </div>

            <div>
              <p className="text-sm font-medium text-blue-600">System traceability</p>
              <h2 className="text-2xl font-bold text-slate-950">Audit Logs</h2>
            </div>
          </div>

          <p className="mt-4 max-w-2xl text-sm leading-6 text-slate-500">
            View important system changes, who performed them, when they happened and what changed.
          </p>
        </div>

        <div className="grid gap-3 sm:grid-cols-3">
          <div className="rounded-2xl bg-green-50 px-5 py-4">
            <p className="text-sm font-medium text-green-700">Created</p>
            <p className="mt-1 text-2xl font-bold text-green-700">{createdCount}</p>
          </div>

          <div className="rounded-2xl bg-blue-50 px-5 py-4">
            <p className="text-sm font-medium text-blue-700">Updated</p>
            <p className="mt-1 text-2xl font-bold text-blue-700">{updatedCount}</p>
          </div>

          <div className="rounded-2xl bg-red-50 px-5 py-4">
            <p className="text-sm font-medium text-red-700">Deleted</p>
            <p className="mt-1 text-2xl font-bold text-red-700">{deletedCount}</p>
          </div>
        </div>
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="mb-5 flex items-center gap-3">
          <div className="rounded-xl bg-slate-50 p-3 text-slate-600">
            <Filter size={20} />
          </div>

          <div>
            <h3 className="text-base font-semibold text-slate-950">Filter audit logs</h3>
            <p className="mt-1 text-sm text-slate-500">
              Filter by entity type or search in action, user and change details.
            </p>
          </div>
        </div>

        <div className="grid gap-4 lg:grid-cols-[1fr_1fr_auto]">
          <label className="block">
            <span className="text-sm font-medium text-slate-700">Search</span>
            <div className="mt-2 flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2">
              <Search size={16} className="text-slate-400" />
              <input
                value={searchTerm}
                onChange={(event) => setSearchTerm(event.target.value)}
                placeholder="Search action, user or changes"
                className="w-full border-0 bg-transparent text-sm text-slate-950 outline-none placeholder:text-slate-400"
              />
            </div>
          </label>

          <label className="block">
            <span className="text-sm font-medium text-slate-700">Entity</span>
            <select
              value={selectedEntity}
              onChange={(event) => setSelectedEntity(event.target.value)}
              className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-950 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            >
              <option value="">All entities</option>
              {entityNames.map((entityName) => (
                <option key={entityName} value={entityName}>
                  {entityName}
                </option>
              ))}
            </select>
          </label>

          <button
            type="button"
            onClick={handleClearFilters}
            className="self-end rounded-xl border border-slate-200 bg-white px-5 py-2.5 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50"
          >
            Clear
          </button>
        </div>
      </section>

      <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-200 px-5 py-4">
          <h3 className="text-base font-semibold text-slate-950">Audit log list</h3>
          <p className="mt-1 text-sm text-slate-500">
            Data is loaded from the ASP.NET Core Audit Logs API.
          </p>
        </div>

        {isLoading && (
          <div className="p-6 text-sm text-slate-500">
            Loading audit logs...
          </div>
        )}

        {isError && (
          <div className="m-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            {errorMessage}
          </div>
        )}

        {!isLoading && !isError && filteredAuditLogs.length === 0 && (
          <div className="p-6 text-sm text-slate-500">
            No audit logs found.
          </div>
        )}

        {!isLoading && !isError && filteredAuditLogs.length > 0 && (
          <div className="divide-y divide-slate-100">
            {filteredAuditLogs.map((auditLog: AuditLog) => (
              <article key={auditLog.id} className="p-5 hover:bg-slate-50">
                <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
                  <div className="flex gap-3">
                    <div className="rounded-xl bg-slate-50 p-3 text-slate-600">
                      <Activity size={20} />
                    </div>

                    <div>
                      <div className="flex flex-wrap items-center gap-2">
                        <span
                          className={`rounded-full px-3 py-1 text-xs font-semibold ${getActionBadgeClass(
                            auditLog.action,
                          )}`}
                        >
                          {auditLog.action}
                        </span>

                        <span className="inline-flex items-center gap-2 rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-700">
                          <Database size={13} />
                          {auditLog.entityName}
                        </span>
                      </div>

                      <p className="mt-3 max-w-4xl text-sm leading-6 text-slate-700">
                        {auditLog.changes}
                      </p>

                      <div className="mt-3 flex flex-wrap gap-4 text-xs text-slate-500">
                        <span className="inline-flex items-center gap-2">
                          <UserRound size={14} />
                          {auditLog.performedBy}
                        </span>

                        <span className="inline-flex items-center gap-2">
                          <Clock size={14} />
                          {formatDateTime(auditLog.performedAt)}
                        </span>

                        <span className="inline-flex items-center gap-2">
                          <ClipboardList size={14} />
                          {auditLog.id}
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
