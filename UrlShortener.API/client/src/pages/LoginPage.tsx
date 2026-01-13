import React, { useState } from "react";
import { Link, useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

function clsx(...parts: Array<string | boolean | undefined | null>) {
    return parts.filter(Boolean).join(" ");
}

function isValidEmail(email: string) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
}

export default function LoginPage() {
    const nav = useNavigate();
    const location = useLocation();
    const { login, error: authError, isLoading, isAuthenticated } = useAuth();

    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [remember, setRemember] = useState(true);

    const [busy, setBusy] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const redirectTo =
        (location.state as any)?.from?.pathname ||
        (location.state as any)?.from ||
        "/profile";

    async function onSubmit(e: React.FormEvent) {
        e.preventDefault();
        setError(null);

        if (!isValidEmail(email)) {
            setError("Please enter a valid email address.");
            return;
        }
        if (password.length < 6) {
            setError("Password must be at least 6 characters.");
            return;
        }

        setBusy(true);
        try {
            const ok = await login({ email, password });
            if (ok) nav(redirectTo, { replace: true });
        } finally {
            setBusy(false);
        }
    }

    // dacă deja e logat
    if (!isLoading && isAuthenticated) {
        nav("/profile", { replace: true });
    }

    return (
        <>
            {/* background */}
            <div className="pointer-events-none fixed inset-0 overflow-hidden">
                <div className="absolute left-1/2 top-[-240px] h-[520px] w-[720px] -translate-x-1/2 rounded-full bg-gradient-to-r from-indigo-500/20 via-fuchsia-500/20 to-cyan-400/20 blur-3xl sm:h-[620px] sm:w-[920px] 2xl:h-[720px] 2xl:w-[1100px]" />
                <div className="absolute bottom-[-240px] right-[-240px] h-[420px] w-[420px] rounded-full bg-gradient-to-r from-cyan-400/10 to-indigo-500/10 blur-3xl sm:h-[520px] sm:w-[520px]" />
                <div className="absolute top-[35%] left-[-220px] h-[360px] w-[360px] rounded-full bg-gradient-to-r from-fuchsia-500/10 to-indigo-500/10 blur-3xl" />
            </div>

            <div className="relative mx-auto w-full max-w-5xl px-4 py-10 sm:px-6 sm:py-14 lg:max-w-6xl lg:px-8 2xl:max-w-7xl 2xl:px-10">
                <main className="mt-12 grid gap-8 lg:grid-cols-2 lg:items-center">
                    {/* left */}
                    <section className="max-w-xl">
                        <div className="inline-flex items-center gap-2 rounded-full bg-white/5 px-4 py-2 ring-1 ring-white/10">
                            <span className="h-2 w-2 rounded-full bg-emerald-400" />
                            <span className="text-xs font-semibold tracking-wide text-white/80">Secure login</span>
                        </div>

                        <h1 className="mt-6 text-3xl font-semibold tracking-tight sm:text-5xl">
                            Sign in to your account
                        </h1>

                        <p className="mt-4 text-base text-white/70">
                            Manage your links, generate QR codes, and keep everything organized in one place.
                        </p>

                        <div className="mt-8 grid gap-3 sm:grid-cols-2">
                            <MiniStat title="Fast" value="Create links in seconds" />
                            <MiniStat title="Clean" value="Simple interface" />
                            <MiniStat title="Track" value="Analytics ready" />
                            <MiniStat title="Share" value="QR in one click" />
                        </div>
                    </section>

                    {/* form card */}
                    <section className="rounded-3xl bg-white/5 p-6 ring-1 ring-white/10 backdrop-blur sm:p-8">
                        <form onSubmit={onSubmit} className="space-y-5">
                            <div>
                                <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Email</div>
                                <div className="mt-2">
                                    <input
                                        value={email}
                                        onChange={(e) => setEmail(e.target.value)}
                                        placeholder="you@example.com"
                                        autoComplete="email"
                                        inputMode="email"
                                        className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-white placeholder:text-white/35 shadow-inner outline-none focus:bg-white/10 focus:ring-2 focus:ring-white/30"
                                    />
                                </div>
                            </div>

                            <div>
                                <div className="flex items-center justify-between gap-3">
                                    <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Password</div>
                                    <button
                                        type="button"
                                        className="text-xs font-semibold text-white/70 hover:text-white"
                                        onClick={() => alert("TODO: forgot password")}
                                    >
                                        Forgot?
                                    </button>
                                </div>

                                <div className="mt-2">
                                    <input
                                        type="password"
                                        value={password}
                                        onChange={(e) => setPassword(e.target.value)}
                                        placeholder="••••••••"
                                        autoComplete="current-password"
                                        className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-white placeholder:text-white/35 shadow-inner outline-none focus:bg-white/10 focus:ring-2 focus:ring-white/30"
                                    />
                                </div>
                            </div>

                            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                                <label className="inline-flex items-center gap-3 text-sm text-white/70">
                                    <input
                                        type="checkbox"
                                        checked={remember}
                                        onChange={(e) => setRemember(e.target.checked)}
                                        className="h-5 w-5 rounded-md border-white/10 bg-white/10 text-white focus:ring-2 focus:ring-white/30"
                                    />
                                    Remember me
                                </label>

                                <Link to="/signup" className="text-sm font-semibold text-white/80 hover:text-white">
                                    Create account
                                </Link>
                            </div>

                            {(error || authError) && (
                                <div className="rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-100">
                                    {error ?? authError}
                                </div>
                            )}

                            <button
                                type="submit"
                                disabled={busy || isLoading}
                                className={clsx(
                                    "w-full rounded-2xl px-5 py-3 text-sm font-semibold transition",
                                    busy || isLoading
                                        ? "bg-white/60 text-[#070A12] cursor-not-allowed"
                                        : "bg-white text-[#070A12] hover:bg-white/90"
                                )}
                            >
                                {busy || isLoading ? "Signing in..." : "Sign in"}
                            </button>

                            <div className="pt-2 text-center text-sm text-white/60">
                                By continuing, you agree to our{" "}
                                <a href="#" className="font-semibold text-white/80 hover:text-white">
                                    Terms
                                </a>{" "}
                                and{" "}
                                <a href="#" className="font-semibold text-white/80 hover:text-white">
                                    Privacy Policy
                                </a>
                                .
                            </div>

                            {/* NOTE: remember checkbox
                  Momentan token-ul e salvat în localStorage de AuthContext.
                  Dacă vrei să respecți "remember=false", schimbă storage-ul în sessionStorage
                  sau condiționează setAuthStorage în context. */}
                        </form>
                    </section>
                </main>
            </div>
        </>
    );
}

function MiniStat({ title, value }: { title: string; value: string }) {
    return (
        <div className="rounded-3xl bg-white/5 p-4 ring-1 ring-white/10">
            <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">{title}</div>
            <div className="mt-2 text-sm font-semibold text-white">{value}</div>
        </div>
    );
}
