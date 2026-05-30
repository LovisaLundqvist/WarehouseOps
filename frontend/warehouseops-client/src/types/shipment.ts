export type Shipment = {
  id: string;
  orderId: string;
  orderStatus: string;
  customerName: string;
  status: string;
  trackingNumber: string;
  shippedDate: string | null;
  deliveredDate: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type UpdateShipmentStatusRequest = {
  status: string;
};
