import React, { createContext, useContext, useEffect, useMemo, useState } from "react";
import type { AuthResultDto, LoginRequestDto, MinimalUserDto, SignUpRequestDto } from "../types/auth";
import { authService } from "../services/authService";
import { clearAuthStorage, getExpiresAtUtc, getStoredUser, getToken, isExpired, setAuthStorage } from "../lib/storage";

type AuthState = {
    user: MinimalUserDto | null;
    token: string | null;
    expiresAtUtc: string | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    error: string | null;

    login: (dto: LoginRequestDto) => Promise<boolean>;
    signup: (dto: SignUpRequestDto) => Promise<boolean>;
    logout: () => void;
};

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [user, setUser] = useState<MinimalUserDto | null>(null);
    const [token, setToken] = useState<string | null>(null);
    const [expiresAtUtc, setExpiresAtUtc] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // bootstrap from localStorage
    useEffect(() => {
        const t = getToken();
        const u = getStoredUser<MinimalUserDto>();
        const exp = getExpiresAtUtc();

        if (t && u && !isExpired(exp)) {
            setToken(t);
            setUser(u);
            setExpiresAtUtc(exp);
        } else {
            clearAuthStorage();
        }

        setIsLoading(false);
    }, []);

    async function applyAuthResult(data: AuthResultDto) {
        setAuthStorage({ token: data.accessToken, user: data.user, expiresAtUtc: data.expiresAtUtc });
        setToken(data.accessToken);
        setUser(data.user);
        setExpiresAtUtc(data.expiresAtUtc);
    }

    async function login(dto: LoginRequestDto) {
        setError(null);
        setIsLoading(true);
        try {
            const data = await authService.login(dto);
            await applyAuthResult(data);
            return true;
        } catch (e: any) {
            setError(e?.message ?? "Login failed");
            return false;
        } finally {
            setIsLoading(false);
        }
    }

    async function signup(dto: SignUpRequestDto) {
        setError(null);
        setIsLoading(true);
        try {
            const data = await authService.signup(dto);
            await applyAuthResult(data);
            return true;
        } catch (e: any) {
            setError(e?.message ?? "Sign up failed");
            return false;
        } finally {
            setIsLoading(false);
        }
    }

    function logout() {
        clearAuthStorage();
        setUser(null);
        setToken(null);
        setExpiresAtUtc(null);
        setError(null);
    }

    const value = useMemo<AuthState>(
        () => ({
            user,
            token,
            expiresAtUtc,
            isAuthenticated: !!user && !!token && !isExpired(expiresAtUtc),
            isLoading,
            error,
            login,
            signup,
            logout,
        }),
        [user, token, expiresAtUtc, isLoading, error]
    );

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
    const ctx = useContext(AuthContext);
    if (!ctx) throw new Error("useAuth must be used inside <AuthProvider />");
    return ctx;
}
