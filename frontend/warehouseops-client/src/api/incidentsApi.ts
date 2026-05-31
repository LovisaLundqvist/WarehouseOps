import apiClient from "./apiClient";
import type {
  CreateIncidentRequest,
  Incident,
  IncidentFilters,
  ResolveIncidentRequest,
  UpdateIncidentStatusRequest,
} from "../types/incident";

export async function getIncidents(filters: IncidentFilters = {}): Promise<Incident[]> {
  const params = new URLSearchParams();

  if (filters.status?.trim()) {
    params.append("status", filters.status.trim());
  }

  const response = await apiClient.get<Incident[]>("/Incidents", { params });

  return response.data;
}

export async function createIncident(request: CreateIncidentRequest): Promise<Incident> {
  const response = await apiClient.post<Incident>("/Incidents", request);

  return response.data;
}

export async function updateIncidentStatus(
  id: string,
  request: UpdateIncidentStatusRequest,
): Promise<Incident> {
  const response = await apiClient.put<Incident>(`/Incidents/${id}/status`, request);

  return response.data;
}

export async function resolveIncident(
  id: string,
  request: ResolveIncidentRequest,
): Promise<Incident> {
  const response = await apiClient.put<Incident>(`/Incidents/${id}/resolve`, request);

  return response.data;
}
