import apiClient from "./apiClient";
import type {
  CreateInventoryItemRequest,
  InventoryItem,
  UpdateInventoryItemRequest,
} from "../types/inventory";

export async function getInventoryItems(): Promise<InventoryItem[]> {
  const response = await apiClient.get<InventoryItem[]>("/Inventory");

  return response.data;
}

export async function getLowStockInventoryItems(): Promise<InventoryItem[]> {
  const response = await apiClient.get<InventoryItem[]>("/Inventory/low-stock");

  return response.data;
}

export async function createInventoryItem(
  request: CreateInventoryItemRequest,
): Promise<InventoryItem> {
  const response = await apiClient.post<InventoryItem>("/Inventory", request);

  return response.data;
}

export async function updateInventoryItem(
  id: string,
  request: UpdateInventoryItemRequest,
): Promise<InventoryItem> {
  const response = await apiClient.put<InventoryItem>(`/Inventory/${id}`, request);

  return response.data;
}
