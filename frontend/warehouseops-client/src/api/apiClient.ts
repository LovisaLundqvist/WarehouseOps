import axios from "axios";
import { getStoredAuthState } from "../auth/authStorage";

const apiClient = axios.create({
  baseURL: "http://localhost:5059/api",
  headers: {
    "Content-Type": "application/json",
  },
});

apiClient.interceptors.request.use((config) => {
  const authState = getStoredAuthState();

  if (authState?.token) {
    config.headers.Authorization = `Bearer ${authState.token}`;
  }

  return config;
});

export default apiClient;
