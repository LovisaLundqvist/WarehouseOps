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

export type UpdateInventoryItemRequest = {
  quantityInStock: number;
  minimumStockLevel: number;
};
