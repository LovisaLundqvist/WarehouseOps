export type Customer = {
  id: string;
  name: string;
  email: string;
  phoneNumber: string;
  address: string;
  createdAt: string;
  updatedAt: string | null;
};

export type CustomerFilters = {
  search?: string;
};

export type CreateCustomerRequest = {
  name: string;
  email: string;
  phoneNumber: string;
  address: string;
};

export type UpdateCustomerRequest = {
  name: string;
  email: string;
  phoneNumber: string;
  address: string;
};
