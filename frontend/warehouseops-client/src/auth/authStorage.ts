import type { AuthState } from "../types/auth";

const storageKey = "warehouseops.auth";

export function getStoredAuthState(): AuthState | null {
  const storedValue = localStorage.getItem(storageKey);

  if (!storedValue) {
    return null;
  }

  try {
    const parsedValue = JSON.parse(storedValue) as AuthState;

    if (!parsedValue.token || !parsedValue.expiresAt || !parsedValue.user) {
      clearStoredAuthState();
      return null;
    }

    const expiresAt = new Date(parsedValue.expiresAt).getTime();

    if (Number.isNaN(expiresAt) || expiresAt <= Date.now()) {
      clearStoredAuthState();
      return null;
    }

    return parsedValue;
  } catch {
    clearStoredAuthState();
    return null;
  }
}

export function saveAuthState(authState: AuthState) {
  localStorage.setItem(storageKey, JSON.stringify(authState));
}

export function clearStoredAuthState() {
  localStorage.removeItem(storageKey);
}
