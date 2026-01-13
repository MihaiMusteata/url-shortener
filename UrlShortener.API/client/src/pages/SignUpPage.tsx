import React, { useState } from "react";

function clsx(...parts: Array<string | boolean | undefined | null>) {
    return parts.filter(Boolean).join(" ");
}

function isValidEmail(email: string) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
}

function isValidUsername(username: string) {
    return /^[a-zA-Z0-9._-]{3,24}$/.test(username.trim());
}

export default function SignUpPage() {
    const [firstName, setFirstName] = useState("");
    const [lastName, setLastName] = useState("");
    const [username, setUsername] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");

    const [busy, setBusy] = useState(false);
    const [error, setError] = useState<string | null>(null);

    async function onSubmit(e: React.FormEvent) {
        e.preventDefault();
        setError(null);

        if (firstName.trim().length < 2) {
            setError("First name is required.");
            return;
        }
        if (lastName.trim().length < 2) {
            setError("Last name is required.");
            return;
        }
        if (!isValidUsername(username)) {
            setError("Username must be 3–24 chars (letters, numbers, dot, dash, underscore).");
            return;
        }
        if (!isValidEmail(email)) {
            setError("Please enter a valid email address.");
            return;
        }
        if (password.trim().length < 6) {
            setError("Password must be at least 6 characters.");
            return;
        }

        setBusy(true);
        try {
            await new Promise((r) => setTimeout(r, 500)); // mock
            alert(`Account created: ${email} (@${username})`);
        } catch {
            setError("Sign up failed. Please try again.");
        } finally {
            setBusy(false);
        }
    }

    return (
        <div className="min-h-screen w-full bg-[#070A12] text-white">
            {/* background */}
            <div className="pointer-events-none fixed inset-0 overflow-hidden">
                <div className="absolute left-1/2 top-[-240px] h-[520px] w-[720px] -translate-x-1/2 rounded-full bg-gradient-to-r from-indigo-500/20 via-fuchsia-500/20 to-cyan-400/20 blur-3xl sm:h-[620px] sm:w-[920px] 2xl:h-[720px] 2xl:w-[1100px]" />
                <div className="absolute bottom-[-240px] right-[-240px] h-[420px] w-[420px] rounded-full bg-gradient-to-r from-cyan-400/10 to-indigo-500/10 blur-3xl sm:h-[520px] sm:w-[520px]" />
                <div className="absolute top-[35%] left-[-220px] h-[360px] w-[360px] rounded-full bg-gradient-to-r from-fuchsia-500/10 to-indigo-500/10 blur-3xl" />
            </div>

            <div className="relative mx-auto w-full max-w-5xl px-4 py-10 sm:px-6 sm:py-14 lg:max-w-6xl lg:px-8 2xl:max-w-7xl 2xl:px-10">
                {/* header */}
                <header className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                        <div className="h-10 w-10 rounded-2xl bg-white/10 ring-1 ring-white/15 backdrop-blur flex items-center justify-center">
                            <span className="text-sm font-semibold">US</span>
                        </div>
                        <div className="leading-tight">
                            <div className="text-sm font-semibold tracking-wide">UrlShortener</div>
                            <div className="text-xs text-white/60">Create your account</div>
                        </div>
                    </div>

                    <a
                        href="/login"
                        className="rounded-2xl px-4 py-2 text-sm font-semibold text-white/85 ring-1 ring-white/10 hover:bg-white/10"
                    >
                        Sign in
                    </a>
                </header>

                <main className="mt-12 grid gap-8 lg:grid-cols-2 lg:items-center">
                    {/* left */}
                    <section className="max-w-xl">
                        <div className="inline-flex items-center gap-2 rounded-full bg-white/5 px-4 py-2 ring-1 ring-white/10">
                            <span className="h-2 w-2 rounded-full bg-emerald-400" />
                            <span className="text-xs font-semibold tracking-wide text-white/80">
                Start free, upgrade anytime
              </span>
                        </div>

                        <h1 className="mt-6 text-3xl font-semibold tracking-tight sm:text-5xl">
                            Create an account
                        </h1>

                        <p className="mt-4 text-base text-white/70">
                            Save links, generate QR codes, and manage everything from one dashboard.
                        </p>

                        <div className="mt-8 rounded-3xl bg-white/5 p-5 ring-1 ring-white/10">
                            <div className="text-sm font-semibold">What you get</div>
                            <ul className="mt-3 space-y-2 text-sm text-white/70">
                                <li className="flex items-center gap-2">
                  <span className="h-5 w-5 rounded-lg bg-emerald-400/15 ring-1 ring-emerald-400/20 text-emerald-300 flex items-center justify-center">
                    ✓
                  </span>
                                    Short links with optional QR codes
                                </li>
                                <li className="flex items-center gap-2">
                  <span className="h-5 w-5 rounded-lg bg-emerald-400/15 ring-1 ring-emerald-400/20 text-emerald-300 flex items-center justify-center">
                    ✓
                  </span>
                                    Clean UI, fast creation
                                </li>
                                <li className="flex items-center gap-2">
                  <span className="h-5 w-5 rounded-lg bg-emerald-400/15 ring-1 ring-emerald-400/20 text-emerald-300 flex items-center justify-center">
                    ✓
                  </span>
                                    Upgrade to unlock more features
                                </li>
                            </ul>
                        </div>
                    </section>

                    {/* form card */}
                    <section className="rounded-3xl bg-white/5 p-6 ring-1 ring-white/10 backdrop-blur sm:p-8">
                        <form onSubmit={onSubmit} className="space-y-5">
                            <div className="grid gap-4 sm:grid-cols-2">
                                <div>
                                    <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">First name</div>
                                    <input
                                        value={firstName}
                                        onChange={(e) => setFirstName(e.target.value)}
                                        placeholder="Mihail"
                                        autoComplete="given-name"
                                        className="mt-2 w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-white placeholder:text-white/35 shadow-inner outline-none focus:bg-white/10 focus:ring-2 focus:ring-white/30"
                                    />
                                </div>

                                <div>
                                    <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Last name</div>
                                    <input
                                        value={lastName}
                                        onChange={(e) => setLastName(e.target.value)}
                                        placeholder="Musteata"
                                        autoComplete="family-name"
                                        className="mt-2 w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-white placeholder:text-white/35 shadow-inner outline-none focus:bg-white/10 focus:ring-2 focus:ring-white/30"
                                    />
                                </div>
                            </div>

                            <div>
                                <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Username</div>
                                <div className="mt-2 flex items-center gap-2 rounded-2xl border border-white/10 bg-white/5 px-3 py-2.5 shadow-inner focus-within:ring-2 focus-within:ring-white/30">
                                    <span className="text-white/40 text-sm">@</span>
                                    <input
                                        value={username}
                                        onChange={(e) => setUsername(e.target.value)}
                                        placeholder="mihail.dev"
                                        autoComplete="username"
                                        className="w-full bg-transparent text-sm text-white placeholder:text-white/35 outline-none"
                                    />
                                </div>
                                <div className="mt-2 text-xs text-white/50">3–24 chars. Letters, numbers, dot, dash, underscore.</div>
                            </div>

                            <div>
                                <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Email</div>
                                <input
                                    value={email}
                                    onChange={(e) => setEmail(e.target.value)}
                                    placeholder="you@example.com"
                                    autoComplete="email"
                                    inputMode="email"
                                    className="mt-2 w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-white placeholder:text-white/35 shadow-inner outline-none focus:bg-white/10 focus:ring-2 focus:ring-white/30"
                                />
                            </div>

                            <div>
                                <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Password</div>
                                <input
                                    type="password"
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    placeholder="••••••••"
                                    autoComplete="new-password"
                                    className="mt-2 w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-white placeholder:text-white/35 shadow-inner outline-none focus:bg-white/10 focus:ring-2 focus:ring-white/30"
                                />
                                <div className="mt-2 text-xs text-white/50">Minimum 6 characters.</div>
                            </div>

                            {error && (
                                <div className="rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-100">
                                    {error}
                                </div>
                            )}

                            <button
                                type="submit"
                                disabled={busy}
                                className={clsx(
                                    "w-full rounded-2xl px-5 py-3 text-sm font-semibold",
                                    busy ? "bg-white/60 text-[#070A12]" : "bg-white text-[#070A12] hover:bg-white/90"
                                )}
                            >
                                {busy ? "Creating account..." : "Create account"}
                            </button>

                            <div className="pt-2 text-center text-sm text-white/60">
                                Already have an account?{" "}
                                <a href="/login" className="font-semibold text-white/80 hover:text-white">
                                    Sign in
                                </a>
                            </div>
                        </form>
                    </section>
                </main>

                <footer className="mt-14 border-t border-white/10 pt-8">
                    <div className="text-sm text-white/60">© 2026 UrlShortener</div>
                </footer>
            </div>
        </div>
    );
}
