export const storageKeys = {
    token: "us_access_token",
    user: "us_user",
    expiresAt: "us_expires_at",
} as const;

export function setAuthStorage(args: { token: string; user: unknown; expiresAtUtc?: string }) {
    localStorage.setItem(storageKeys.token, args.token);
    localStorage.setItem(storageKeys.user, JSON.stringify(args.user));
    if (args.expiresAtUtc) localStorage.setItem(storageKeys.expiresAt, args.expiresAtUtc);
}

export function clearAuthStorage() {
    localStorage.removeItem(storageKeys.token);
    localStorage.removeItem(storageKeys.user);
    localStorage.removeItem(storageKeys.expiresAt);
}

export function getToken(): string | null {
    return localStorage.getItem(storageKeys.token);
}

export function getStoredUser<T>(): T | null {
    const raw = localStorage.getItem(storageKeys.user);
    if (!raw) return null;
    try {
        return JSON.parse(raw) as T;
    } catch {
        return null;
    }
}

export function getExpiresAtUtc(): string | null {
    return localStorage.getItem(storageKeys.expiresAt);
}

export function isExpired(expiresAtUtc?: string | null) {
    if (!expiresAtUtc) return false;
    const t = Date.parse(expiresAtUtc);
    if (Number.isNaN(t)) return false;
    return Date.now() >= t;
}
