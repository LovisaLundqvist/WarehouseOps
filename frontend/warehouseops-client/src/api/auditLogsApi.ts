import apiClient from "./apiClient";
import type { AuditLog } from "../types/auditLog";

export async function getAuditLogs(): Promise<AuditLog[]> {
  const response = await apiClient.get<AuditLog[]>("/AuditLogs");

  return response.data;
}
