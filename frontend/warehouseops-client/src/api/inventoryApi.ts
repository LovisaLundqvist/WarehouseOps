import apiClient from "./apiClient";
import type { InventoryItem, UpdateInventoryItemRequest } from "../types/inventory";

export async function getInventoryItems(): Promise<InventoryItem[]> {
  const response = await apiClient.get<InventoryItem[]>("/Inventory");

  return response.data;
}

export async function getLowStockInventoryItems(): Promise<InventoryItem[]> {
  const response = await apiClient.get<InventoryItem[]>("/Inventory/low-stock");

  return response.data;
}

export async function updateInventoryItem(
  id: string,
  request: UpdateInventoryItemRequest,
): Promise<InventoryItem> {
  const response = await apiClient.put<InventoryItem>(`/Inventory/${id}`, request);

  return response.data;
}
