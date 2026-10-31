import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { login as loginRequest } from "../api/authApi";
import type { AuthState, LoginRequest } from "../types/auth";
import {
  clearStoredAuthState,
  getStoredAuthState,
  saveAuthState,
} from "./authStorage";
import { AuthContext } from "./AuthContext";

type AuthProviderProps = {
  children: ReactNode;
};

const maxTimeoutMs = 2_147_483_647;

export function AuthProvider({ children }: AuthProviderProps) {
  const [authState, setAuthState] = useState<AuthState | null>(getStoredAuthState);

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

  useEffect(() => {
    if (!authState) {
      return;
    }

    let timeoutId: number;
    const expiresAt = authState.expiresAt;

    function scheduleExpirationCheck() {
      const expiration = new Date(expiresAt).getTime();
      const remainingTime = expiration - Date.now();

      if (!Number.isFinite(remainingTime) || remainingTime <= 0) {
        timeoutId = window.setTimeout(logout, 0);
        return;
      }

      timeoutId = window.setTimeout(
        scheduleExpirationCheck,
        Math.min(remainingTime, maxTimeoutMs),
      );
    }

    scheduleExpirationCheck();

    return () => window.clearTimeout(timeoutId);
  }, [authState, logout]);

  const user = authState?.user ?? null;
  const isAuthenticated = Boolean(authState?.token && authState.expiresAt && user);

  const value = useMemo(
    () => ({ authState, user, isAuthenticated, login, logout }),
    [authState, user, isAuthenticated, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
