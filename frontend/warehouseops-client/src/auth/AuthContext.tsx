import { createContext, useContext } from "react";
import type { AuthState, AuthUser, LoginRequest } from "../types/auth";

export type AuthContextValue = {
  authState: AuthState | null;
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (request: LoginRequest) => Promise<void>;
  logout: () => void;
};

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used inside AuthProvider.");
  }

  return context;
}
