import apiClient from "./apiClient";
import type { Order } from "../types/order";

export async function getOrders(): Promise<Order[]> {
  const response = await apiClient.get<Order[]>("/Orders");

  return response.data;
}
