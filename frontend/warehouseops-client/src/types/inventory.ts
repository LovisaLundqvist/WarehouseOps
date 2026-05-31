export type InventoryItem = {
  id: string;
  productId: string;
  productName: string;
  productSku: string;
  quantityInStock: number;
  minimumStockLevel: number;
  isLowStock: boolean;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateInventoryItemRequest = {
  productId: string;
  quantityInStock: number;
  minimumStockLevel: number;
};

export type UpdateInventoryItemRequest = {
  quantityInStock: number;
  minimumStockLevel: number;
};
