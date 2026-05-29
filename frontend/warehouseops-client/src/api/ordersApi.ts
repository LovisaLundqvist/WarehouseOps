import apiClient from "./apiClient";
import type { Order, UpdateOrderStatusRequest } from "../types/order";

export async function getOrders(): Promise<Order[]> {
  const response = await apiClient.get<Order[]>("/Orders");

  return response.data;
}

export async function updateOrderStatus(
  id: string,
  request: UpdateOrderStatusRequest,
): Promise<Order> {
  const response = await apiClient.put<Order>(`/Orders/${id}/status`, request);

  return response.data;
}

export async function cancelOrder(id: string): Promise<Order> {
  const response = await apiClient.put<Order>(`/Orders/${id}/cancel`);

  return response.data;
}
