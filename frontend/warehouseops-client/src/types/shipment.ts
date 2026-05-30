export type Shipment = {
  id: string;
  orderId: string;
  orderNumber?: string | null;
  customerName?: string | null;
  status: string;
  trackingNumber?: string | null;
  carrier?: string | null;
  shippedAt?: string | null;
  deliveredAt?: string | null;
  createdAt: string;
  updatedAt: string | null;
};
