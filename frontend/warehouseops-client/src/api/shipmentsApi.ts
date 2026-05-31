import apiClient from "./apiClient";
import type {
  CreateShipmentRequest,
  Shipment,
  UpdateShipmentStatusRequest,
} from "../types/shipment";

export async function getShipments(): Promise<Shipment[]> {
  const response = await apiClient.get<Shipment[]>("/Shipments");

  return response.data;
}

export async function createShipment(request: CreateShipmentRequest): Promise<Shipment> {
  const response = await apiClient.post<Shipment>("/Shipments", request);

  return response.data;
}

export async function updateShipmentStatus(
  id: string,
  request: UpdateShipmentStatusRequest,
): Promise<Shipment> {
  const response = await apiClient.put<Shipment>(`/Shipments/${id}/status`, request);

  return response.data;
}
