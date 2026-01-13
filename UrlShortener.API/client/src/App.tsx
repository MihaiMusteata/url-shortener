import { Routes, Route, Navigate, Link, useLocation } from "react-router-dom";
import PricingPage from "./pages/PricingPage";
import HeroShortenerPage from "./pages/HeroShortenerPage";
import LoginPage from "./pages/LoginPage";
import SignUpPage from "./pages/SignUpPage";
import ProfilePage from "./pages/ProfilePage";
import LinkDetailsPage from "./pages/LinkDetailsPage";
import { useAuth } from "./auth/AuthContext";
import { RequireAuth } from "./auth/RequireAuth";

function clsx(...parts: Array<string | boolean | undefined | null>) {
    return parts.filter(Boolean).join(" ");
}

function Header() {
    const { pathname } = useLocation();
    const { isAuthenticated, user, logout, isLoading } = useAuth();

    const nav = [
        { to: "/", label: "Shorten" },
        { to: "/pricing", label: "Pricing" },
        ...(isAuthenticated ? [{ to: "/profile", label: "Profile" }] : []),
    ];

    return (
        <header className="sticky top-0 z-50 border-b border-white/10 bg-[#070A12]/70 backdrop-blur">
            <div className="mx-auto w-full max-w-5xl px-4 py-4 sm:px-6 lg:max-w-6xl lg:px-8 2xl:max-w-7xl 2xl:px-10">
                <div className="flex items-center justify-between gap-3">
                    <Link to="/" className="flex items-center gap-3">
                        <div className="h-10 w-10 rounded-2xl bg-white/10 ring-1 ring-white/15 flex items-center justify-center">
                            <span className="text-sm font-semibold text-white">US</span>
                        </div>
                        <div className="leading-tight">
                            <div className="text-sm font-semibold tracking-wide text-white">UrlShortener</div>
                            <div className="text-xs text-white/60">Short links + QR</div>
                        </div>
                    </Link>

                    <nav className="hidden items-center gap-1 rounded-2xl bg-white/5 p-1 ring-1 ring-white/10 sm:flex">
                        {nav.map((item) => {
                            const active = pathname === item.to;
                            return (
                                <Link
                                    key={item.to}
                                    to={item.to}
                                    className={clsx(
                                        "rounded-xl px-4 py-2 text-sm font-semibold transition",
                                        active ? "bg-white text-[#070A12]" : "text-white/80 hover:bg-white/10"
                                    )}
                                >
                                    {item.label}
                                </Link>
                            );
                        })}
                    </nav>

                    <div className="flex items-center gap-2">
                        {isLoading ? null : isAuthenticated ? (
                            <>
                                <div className="hidden sm:flex items-center gap-2 rounded-2xl bg-white/5 px-3 py-2 ring-1 ring-white/10">
                                    <span className="text-xs text-white/60">Signed in</span>
                                    <span className="text-sm font-semibold text-white/85">{user?.username}</span>
                                </div>

                                <button
                                    type="button"
                                    onClick={logout}
                                    className="rounded-2xl px-4 py-2 text-sm font-semibold ring-1 transition bg-white/5 text-white/85 ring-white/10 hover:bg-white/10"
                                >
                                    Logout
                                </button>
                            </>
                        ) : (
                            <>
                                <Link
                                    to="/login"
                                    className={clsx(
                                        "rounded-2xl px-4 py-2 text-sm font-semibold ring-1 transition",
                                        pathname === "/login"
                                            ? "bg-white text-[#070A12] ring-white/20"
                                            : "bg-white/5 text-white/85 ring-white/10 hover:bg-white/10"
                                    )}
                                >
                                    Sign in
                                </Link>

                                <Link
                                    to="/signup"
                                    className={clsx(
                                        "hidden sm:inline-flex rounded-2xl px-4 py-2 text-sm font-semibold transition",
                                        pathname === "/signup"
                                            ? "bg-white/90 text-[#070A12]"
                                            : "bg-white text-[#070A12] hover:bg-white/90"
                                    )}
                                >
                                    Sign up
                                </Link>
                            </>
                        )}
                    </div>
                </div>

                {/* Mobile nav */}
                <div className="mt-3 flex items-center gap-2 sm:hidden">
                    {nav.map((item) => {
                        const active = pathname === item.to;
                        return (
                            <Link
                                key={item.to}
                                to={item.to}
                                className={clsx(
                                    "flex-1 rounded-2xl px-3 py-2 text-center text-sm font-semibold ring-1 transition",
                                    active
                                        ? "bg-white text-[#070A12] ring-white/20"
                                        : "bg-white/5 text-white/85 ring-white/10 hover:bg-white/10"
                                )}
                            >
                                {item.label}
                            </Link>
                        );
                    })}
                </div>
            </div>
        </header>
    );
}

function Footer() {
    return (
        <footer className="border-t border-white/10 bg-[#070A12]">
            <div className="mx-auto w-full max-w-5xl px-4 py-10 text-sm text-white/60 sm:px-6 lg:max-w-6xl lg:px-8 2xl:max-w-7xl 2xl:px-10">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                    <div>© 2026 UrlShortener</div>
                    <div className="flex flex-wrap gap-4">
                        <a className="hover:text-white/85" href="#">
                            Privacy
                        </a>
                        <a className="hover:text-white/85" href="#">
                            Terms
                        </a>
                        <a className="hover:text-white/85" href="#">
                            Support
                        </a>
                    </div>
                </div>
            </div>
        </footer>
    );
}

function AppLayout({ children }: { children: React.ReactNode }) {
    return (
        <div className="min-h-screen w-full bg-[#070A12] text-white">
            <div className="pointer-events-none fixed inset-0 overflow-hidden">
                <div className="absolute left-1/2 top-[-240px] h-[520px] w-[720px] -translate-x-1/2 rounded-full bg-gradient-to-r from-indigo-500/20 via-fuchsia-500/20 to-cyan-400/20 blur-3xl sm:h-[620px] sm:w-[920px] 2xl:h-[720px] 2xl:w-[1100px]" />
                <div className="absolute bottom-[-240px] right-[-240px] h-[420px] w-[420px] rounded-full bg-gradient-to-r from-cyan-400/10 to-indigo-500/10 blur-3xl sm:h-[520px] sm:w-[520px]" />
                <div className="absolute top-[35%] left-[-220px] h-[360px] w-[360px] rounded-full bg-gradient-to-r from-fuchsia-500/10 to-indigo-500/10 blur-3xl" />
            </div>

            <Header />

            <main className="relative">
                <div className="mx-auto w-full max-w-5xl px-4 py-10 sm:px-6 sm:py-14 lg:max-w-6xl lg:px-8 2xl:max-w-7xl 2xl:px-10">
                    {children}
                </div>
            </main>

            <Footer />
        </div>
    );
}

function AuthLayout({ children }: { children: React.ReactNode }) {
    const { pathname } = useLocation();

    return (
        <div className="min-h-screen w-full bg-[#070A12] text-white">
            <div className="pointer-events-none fixed inset-0 overflow-hidden">
                <div className="absolute left-1/2 top-[-240px] h-[520px] w-[720px] -translate-x-1/2 rounded-full bg-gradient-to-r from-indigo-500/20 via-fuchsia-500/20 to-cyan-400/20 blur-3xl sm:h-[620px] sm:w-[920px] 2xl:h-[720px] 2xl:w-[1100px]" />
                <div className="absolute bottom-[-240px] right-[-240px] h-[420px] w-[420px] rounded-full bg-gradient-to-r from-cyan-400/10 to-indigo-500/10 blur-3xl sm:h-[520px] sm:w-[520px]" />
                <div className="absolute top-[35%] left-[-220px] h-[360px] w-[360px] rounded-full bg-gradient-to-r from-fuchsia-500/10 to-indigo-500/10 blur-3xl" />
            </div>

            <header className="relative border-b border-white/10 bg-[#070A12]/70 backdrop-blur">
                <div className="mx-auto w-full max-w-5xl px-4 py-4 sm:px-6 lg:max-w-6xl lg:px-8 2xl:max-w-7xl 2xl:px-10">
                    <div className="flex items-center justify-between">
                        <Link to="/" className="flex items-center gap-3">
                            <div className="h-10 w-10 rounded-2xl bg-white/10 ring-1 ring-white/15 flex items-center justify-center">
                                <span className="text-sm font-semibold text-white">US</span>
                            </div>
                            <div className="leading-tight">
                                <div className="text-sm font-semibold tracking-wide text-white">UrlShortener</div>
                                <div className="text-xs text-white/60">Short links + QR</div>
                            </div>
                        </Link>

                        <div className="flex items-center gap-2">
                            <Link
                                to="/pricing"
                                className="rounded-2xl px-4 py-2 text-sm font-semibold text-white/85 ring-1 ring-white/10 hover:bg-white/10"
                            >
                                Pricing
                            </Link>
                            <Link
                                to={pathname === "/login" ? "/signup" : "/login"}
                                className="rounded-2xl px-4 py-2 text-sm font-semibold text-white/85 ring-1 ring-white/10 hover:bg-white/10"
                            >
                                {pathname === "/login" ? "Sign up" : "Sign in"}
                            </Link>
                        </div>
                    </div>
                </div>
            </header>

            <main className="relative" style={{height: 'calc(100vh - 72px - 94px)'}}>
                <div className="mx-auto w-full max-w-5xl px-4 py-10 sm:px-6 sm:py-14 lg:max-w-6xl lg:px-8 2xl:max-w-7xl 2xl:px-10">
                    {children}
                </div>
            </main>

            <Footer />
        </div>
    );
}

export default function App() {
    const { isAuthenticated, isLoading } = useAuth();

    if (isLoading) {
        return (
            <div className="min-h-screen w-full bg-[#070A12] text-white grid place-items-center">
                <div className="rounded-3xl bg-white/5 p-6 ring-1 ring-white/10">
                    <div className="text-sm font-semibold">Loading…</div>
                    <div className="mt-1 text-sm text-white/60">Restoring session</div>
                </div>
            </div>
        );
    }

    return (
        <Routes>
            <Route
                path="/"
                element={
                    <AppLayout>
                        <HeroShortenerPage />
                    </AppLayout>
                }
            />

            <Route
                path="/pricing"
                element={
                    <AppLayout>
                        <PricingPage />
                    </AppLayout>
                }
            />

            {/* Protected */}
            <Route
                path="/profile"
                element={
                    <RequireAuth>
                        <AppLayout>
                            <ProfilePage />
                        </AppLayout>
                    </RequireAuth>
                }
            />
            <Route
                path="/details/:id"
                element={
                    <RequireAuth>
                        <AppLayout>
                            <LinkDetailsPage />
                        </AppLayout>
                    </RequireAuth>
                }
            />

            {/* Auth pages (redirect if already logged in) */}
            <Route
                path="/login"
                element={
                    isAuthenticated ? (
                        <Navigate to="/profile" replace />
                    ) : (
                        <AuthLayout>
                            <LoginPage />
                        </AuthLayout>
                    )
                }
            />
            <Route
                path="/signup"
                element={
                    isAuthenticated ? (
                        <Navigate to="/profile" replace />
                    ) : (
                        <AuthLayout>
                            <SignUpPage />
                        </AuthLayout>
                    )
                }
            />

            <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
    );
}
