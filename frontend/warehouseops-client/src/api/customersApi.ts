import apiClient from "./apiClient";
import type {
  CreateCustomerRequest,
  Customer,
  CustomerFilters,
  UpdateCustomerRequest,
} from "../types/customer";

export async function getCustomers(filters: CustomerFilters = {}): Promise<Customer[]> {
  const params = new URLSearchParams();

  if (filters.search?.trim()) {
    params.append("search", filters.search.trim());
  }

  const response = await apiClient.get<Customer[]>("/Customers", { params });

  return response.data;
}

export async function createCustomer(request: CreateCustomerRequest): Promise<Customer> {
  const response = await apiClient.post<Customer>("/Customers", request);

  return response.data;
}

export async function updateCustomer(
  id: string,
  request: UpdateCustomerRequest,
): Promise<Customer> {
  const response = await apiClient.put<Customer>(`/Customers/${id}`, request);

  return response.data;
}
