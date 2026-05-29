export type Product = {
  id: string;
  name: string;
  sku: string;
  category: string;
  description: string;
  price: number;
  createdAt: string;
  updatedAt: string | null;
};

export type ProductFilters = {
  search?: string;
  category?: string;
};
