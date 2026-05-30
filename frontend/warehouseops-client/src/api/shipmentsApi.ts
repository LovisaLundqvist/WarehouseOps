import apiClient from "./apiClient";
import type { Shipment } from "../types/shipment";

export async function getShipments(): Promise<Shipment[]> {
  const response = await apiClient.get<Shipment[]>("/Shipments");

  return response.data;
}
