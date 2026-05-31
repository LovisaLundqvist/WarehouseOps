export type UserRole = "Admin" | "WarehouseStaff" | "Manager";

export type LoginRequest = {
  email: string;
  password: string;
};

export type LoginResponse = {
  token: string;
  expiresAt: string;
  email: string;
  displayName: string;
  role: UserRole;
};

export type AuthUser = {
  email: string;
  displayName: string;
  role: UserRole;
};

export type AuthState = {
  token: string;
  expiresAt: string;
  user: AuthUser;
};
