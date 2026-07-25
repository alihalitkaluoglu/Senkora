import { create } from "zustand";

interface User {
  userId: string;
  email: string;
  fullName: string;
  roles: string[];
}

interface AuthState {
  token: string | null;
  user: User | null;
  isAuthenticated: boolean;
  setAuth: (token: string, user: User) => void;
  clearAuth: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  token: null,
  user: null,
  isAuthenticated: false,
  setAuth: (token, user) => {
    if (typeof window !== "undefined") {
      localStorage.setItem("senkora_token", token);
      localStorage.setItem("senkora_user", JSON.stringify(user));
    }
    set({ token, user, isAuthenticated: true });
  },
  clearAuth: () => {
    if (typeof window !== "undefined") {
      localStorage.removeItem("senkora_token");
      localStorage.removeItem("senkora_user");
    }
    set({ token: null, user: null, isAuthenticated: false });
  },
}));
