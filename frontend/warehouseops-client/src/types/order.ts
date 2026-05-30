export type OrderItem = {
  productId: string;
  productName: string;
  productSku: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
};

export type Order = {
  id: string;
  customerId: string;
  customerName: string;
  status: string;
  totalAmount: number;
  items: OrderItem[];
  createdAt: string;
  updatedAt: string | null;
};

export type CreateOrderItemRequest = {
  productId: string;
  quantity: number;
};

export type CreateOrderRequest = {
  customerId: string;
  items: CreateOrderItemRequest[];
};

export type UpdateOrderStatusRequest = {
  status: string;
};
