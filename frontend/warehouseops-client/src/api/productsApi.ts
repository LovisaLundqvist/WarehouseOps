import apiClient from "./apiClient";
import type {
  CreateProductRequest,
  Product,
  ProductFilters,
  UpdateProductRequest,
} from "../types/product";

export async function getProducts(filters: ProductFilters = {}): Promise<Product[]> {
  const params = new URLSearchParams();

  if (filters.search?.trim()) {
    params.append("search", filters.search.trim());
  }

  if (filters.category?.trim()) {
    params.append("category", filters.category.trim());
  }

  const response = await apiClient.get<Product[]>("/Products", { params });

  return response.data;
}

export async function createProduct(request: CreateProductRequest): Promise<Product> {
  const response = await apiClient.post<Product>("/Products", request);

  return response.data;
}

export async function updateProduct(
  id: string,
  request: UpdateProductRequest,
): Promise<Product> {
  const response = await apiClient.put<Product>(`/Products/${id}`, request);

  return response.data;
}
