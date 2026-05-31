import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { login as loginRequest } from "../api/authApi";
import {
  clearStoredAuthState,
  getStoredAuthState,
  saveAuthState,
} from "./authStorage";
import type { AuthState, AuthUser, LoginRequest } from "../types/auth";

type AuthContextValue = {
  authState: AuthState | null;
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (request: LoginRequest) => Promise<void>;
  logout: () => void;
};

type AuthProviderProps = {
  children: ReactNode;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: AuthProviderProps) {
  const [authState, setAuthState] = useState<AuthState | null>(() => getStoredAuthState());

  const isAuthenticated =
    Boolean(authState?.token) &&
    Boolean(authState?.expiresAt) &&
    new Date(authState?.expiresAt ?? "").getTime() > Date.now();

  const user = authState?.user ?? null;

  const login = useCallback(async (request: LoginRequest) => {
    const response = await loginRequest(request);

    const nextAuthState: AuthState = {
      token: response.token,
      expiresAt: response.expiresAt,
      user: {
        email: response.email,
        displayName: response.displayName,
        role: response.role,
      },
    };

    saveAuthState(nextAuthState);
    setAuthState(nextAuthState);
  }, []);

  const logout = useCallback(() => {
    clearStoredAuthState();
    setAuthState(null);
  }, []);

  const value = useMemo(
    () => ({
      authState,
      user,
      isAuthenticated,
      login,
      logout,
    }),
    [authState, user, isAuthenticated, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used inside AuthProvider.");
  }

  return context;
}
