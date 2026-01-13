import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../lib/api.ts";

export type ShortLinkDto = {
    id: string;
    originalUrl: string;
    shortUrl: string;
    alias: string;
    createdAt: string;
    qrEnabled: boolean;
    clicks: number;
};

export type UserProfileDto = {
    id: string;
    firstName: string;
    lastName: string;
    username: string;
    email: string;
    planName: string;
};

export type PlanSummaryDto = {
    name: string;
    priceMonthly: number;
    maxLinksPerMonth: number;
    customAliasEnabled: boolean;
    qrEnabled: boolean;
};

export type UsageSummaryDto = {
    linksCreatedThisMonth: number;
};

export type ProfilePageDto = {
    user: UserProfileDto;
    plan: PlanSummaryDto;
    usage: UsageSummaryDto;
    links: ShortLinkDto[];
};

function clsx(...parts: Array<string | boolean | undefined | null>) {
    return parts.filter(Boolean).join(" ");
}

function formatDate(iso: string) {
    const d = new Date(iso);
    return d.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "2-digit" });
}

function truncateMiddle(s: string, max = 44) {
    if (s.length <= max) return s;
    const left = Math.max(12, Math.floor(max * 0.55));
    const right = Math.max(10, max - left - 3);
    return `${s.slice(0, left)}...${s.slice(-right)}`;
}

function formatPriceMonthly(v: number) {
    if (!Number.isFinite(v) || v <= 0) return "Free";
    return new Intl.NumberFormat(undefined, {
        style: "currency",
        currency: "USD",
        maximumFractionDigits: v % 1 === 0 ? 0 : 2,
    }).format(v) + "/mo";
}

function percent(a: number, b: number) {
    if (b <= 0) return 0;
    return Math.max(0, Math.min(100, Math.round((a / b) * 100)));
}

export default function ProfilePage() {
    const [query, setQuery] = useState("");
    const [sort, setSort] = useState<"newest" | "clicks">("newest");
    const [data, setData] = useState<ProfilePageDto | null>(null);
    const [busy, setBusy] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const navigate = useNavigate();

    useEffect(() => {
        let alive = true;

        async function load() {
            setBusy(true);
            setError(null);
            try {
                const res = await api.get<ProfilePageDto>("/profile/me");
                if (!alive) return;
                setData(res.data);
            } catch (e: any) {
                if (!alive) return;

                const msg =
                    e?.response?.data && typeof e.response.data === "string"
                        ? e.response.data
                        : e?.message || "Failed to load profile.";

                setError(msg);

                if (e?.response?.status === 401) {
                    navigate("/login", { replace: true });
                }
            } finally {
                if (!alive) return;
                setBusy(false);
            }
        }

        load();
        return () => {
            alive = false;
        };
    }, [navigate]);

    const plan = data?.plan ?? {
        name: "",
        priceMonthly: 0,
        maxLinksPerMonth: 0,
        customAliasEnabled: false,
        qrEnabled: false,
    };

    const usage = data?.usage ?? { linksCreatedThisMonth: 0 };

    const usedPct = percent(usage.linksCreatedThisMonth, plan.maxLinksPerMonth);
    const remaining = Math.max(0, plan.maxLinksPerMonth - usage.linksCreatedThisMonth);

    const filtered = useMemo(() => {
        const q = (query || "").toLowerCase();

        const base = data?.links ? [...data.links] : [];
        let items = base;

        if (q) {
            items = items.filter((l) => {
                const hay = `${l.alias} ${l.originalUrl} ${l.shortUrl}`.toLowerCase();
                return hay.includes(q);
            });
        }

        if (sort === "newest") {
            items.sort((a, b) => +new Date(b.createdAt) - +new Date(a.createdAt));
        } else {
            items.sort((a, b) => b.clicks - a.clicks);
        }

        return items;
    }, [data?.links, query, sort]);

    function onOpenDetails(linkId: string) {
        navigate(`/details?id=${encodeURIComponent(linkId)}`);
    }

    async function copy(text: string) {
        try {
            await navigator.clipboard.writeText(text);
        } catch {}
    }

    if (busy) {
        return (
            <>
                <div className="pointer-events-none fixed inset-0 overflow-hidden">
                    <div className="absolute left-1/2 top-[-240px] h-[520px] w-[720px] -translate-x-1/2 rounded-full bg-gradient-to-r from-indigo-500/20 via-fuchsia-500/20 to-cyan-400/20 blur-3xl sm:h-[620px] sm:w-[920px] 2xl:h-[720px] 2xl:w-[1100px]" />
                    <div className="absolute bottom-[-240px] right-[-240px] h-[420px] w-[420px] rounded-full bg-gradient-to-r from-cyan-400/10 to-indigo-500/10 blur-3xl sm:h-[520px] sm:w-[520px]" />
                    <div className="absolute top-[35%] left-[-220px] h-[360px] w-[360px] rounded-full bg-gradient-to-r from-fuchsia-500/10 to-indigo-500/10 blur-3xl" />
                </div>

                <div className="relative mx-auto w-full max-w-5xl px-4 py-10 sm:px-6 sm:py-14 lg:max-w-6xl lg:px-8 2xl:max-w-7xl 2xl:px-10">
                    <div className="rounded-3xl bg-white/5 p-6 ring-1 ring-white/10 backdrop-blur">
                        <div className="animate-pulse space-y-4">
                            <div className="h-6 w-48 rounded-xl bg-white/10" />
                            <div className="h-4 w-72 rounded-xl bg-white/10" />
                            <div className="h-28 w-full rounded-2xl bg-white/10" />
                        </div>
                    </div>
                </div>
            </>
        );
    }

    if (error) {
        return (
            <>
                <div className="pointer-events-none fixed inset-0 overflow-hidden">
                    <div className="absolute left-1/2 top-[-240px] h-[520px] w-[720px] -translate-x-1/2 rounded-full bg-gradient-to-r from-indigo-500/20 via-fuchsia-500/20 to-cyan-400/20 blur-3xl sm:h-[620px] sm:w-[920px] 2xl:h-[720px] 2xl:w-[1100px]" />
                    <div className="absolute bottom-[-240px] right-[-240px] h-[420px] w-[420px] rounded-full bg-gradient-to-r from-cyan-400/10 to-indigo-500/10 blur-3xl sm:h-[520px] sm:w-[520px]" />
                    <div className="absolute top-[35%] left-[-220px] h-[360px] w-[360px] rounded-full bg-gradient-to-r from-fuchsia-500/10 to-indigo-500/10 blur-3xl" />
                </div>

                <div className="relative mx-auto w-full max-w-5xl px-4 py-10 sm:px-6 sm:py-14 lg:max-w-6xl lg:px-8 2xl:max-w-7xl 2xl:px-10">
                    <div className="rounded-3xl border border-rose-500/30 bg-rose-500/10 p-6 text-sm text-rose-100 ring-1 ring-white/10 backdrop-blur">
                        <div className="font-semibold">Could not load profile</div>
                        <div className="mt-2 text-rose-100/90">{error}</div>
                        <div className="mt-4 flex flex-wrap gap-2">
                            <button
                                type="button"
                                onClick={() => window.location.reload()}
                                className="rounded-2xl bg-white px-4 py-2.5 text-sm font-semibold text-[#070A12] hover:bg-white/90"
                            >
                                Retry
                            </button>
                            <button
                                type="button"
                                onClick={() => navigate("/pricing")}
                                className="rounded-2xl bg-white/5 px-4 py-2.5 text-sm font-semibold text-white/85 ring-1 ring-white/10 hover:bg-white/10"
                            >
                                Go to pricing
                            </button>
                        </div>
                    </div>
                </div>
            </>
        );
    }

    const user = data!.user;

    return (
        <>
            {/* background */}
            <div className="pointer-events-none fixed inset-0 overflow-hidden">
                <div className="absolute left-1/2 top-[-240px] h-[520px] w-[720px] -translate-x-1/2 rounded-full bg-gradient-to-r from-indigo-500/20 via-fuchsia-500/20 to-cyan-400/20 blur-3xl sm:h-[620px] sm:w-[920px] 2xl:h-[720px] 2xl:w-[1100px]" />
                <div className="absolute bottom-[-240px] right-[-240px] h-[420px] w-[420px] rounded-full bg-gradient-to-r from-cyan-400/10 to-indigo-500/10 blur-3xl sm:h-[520px] sm:w-[520px]" />
                <div className="absolute top-[35%] left-[-220px] h-[360px] w-[360px] rounded-full bg-gradient-to-r from-fuchsia-500/10 to-indigo-500/10 blur-3xl" />
            </div>

            <div className="relative mx-auto w-full max-w-5xl px-4 py-10 sm:px-6 sm:py-14 lg:max-w-6xl lg:px-8 2xl:max-w-7xl 2xl:px-10">
                {/* USER + PLAN CARD */}
                <section className="mb-8 grid gap-4 lg:grid-cols-3">
                    {/* user */}
                    <div className="rounded-3xl bg-white/5 p-6 ring-1 ring-white/10 backdrop-blur">
                        <div className="flex items-start gap-4">
                            <div className="h-12 w-12 rounded-2xl bg-white/10 ring-1 ring-white/15 flex items-center justify-center shrink-0">
                <span className="text-sm font-semibold">
                  {(user.firstName?.[0] ?? "U").toUpperCase()}
                    {(user.lastName?.[0] ?? "S").toUpperCase()}
                </span>
                            </div>
                            <div className="min-w-0">
                                <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Profile</div>
                                <div className="mt-1 text-lg font-semibold truncate">
                                    {user.firstName} {user.lastName}
                                </div>
                                <div className="mt-1 text-sm text-white/60 truncate">@{user.username}</div>
                            </div>
                        </div>

                        <div className="mt-5 grid gap-3">
                            <div className="rounded-2xl bg-white/5 p-4 ring-1 ring-white/10">
                                <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Email</div>
                                <div className="mt-2 text-sm font-semibold text-white/80 truncate">{user.email}</div>
                            </div>

                            <div className="rounded-2xl bg-white/5 p-4 ring-1 ring-white/10">
                                <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Plan</div>
                                <div className="mt-2 text-sm font-semibold text-white/80 truncate">{user.planName || plan.name || "—"}</div>
                            </div>
                        </div>
                    </div>

                    {/* plan */}
                    <div className="lg:col-span-2 rounded-3xl bg-white/5 p-6 ring-1 ring-white/10 backdrop-blur">
                        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                            <div>
                                <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Current plan</div>
                                <div className="mt-1 flex flex-wrap items-center gap-2">
                                    <div className="text-xl font-semibold">{plan.name || user.planName || "—"}</div>
                                    <span className="rounded-full bg-white/5 px-3 py-1 text-xs font-semibold text-white/70 ring-1 ring-white/10">
                    {formatPriceMonthly(plan.priceMonthly)}
                  </span>
                                </div>
                                <div className="mt-2 text-sm text-white/65">
                                    Limit: <span className="font-semibold text-white/80">{plan.maxLinksPerMonth}</span> links / month
                                </div>
                            </div>

                            <div className="flex flex-wrap gap-2">
                <span
                    className={clsx(
                        "rounded-full px-3 py-1 text-xs font-semibold ring-1",
                        plan.customAliasEnabled
                            ? "bg-emerald-400/10 ring-emerald-400/20 text-emerald-300"
                            : "bg-white/5 ring-white/10 text-white/70"
                    )}
                >
                  {plan.customAliasEnabled ? "Custom alias" : "No alias"}
                </span>

                                <span
                                    className={clsx(
                                        "rounded-full px-3 py-1 text-xs font-semibold ring-1",
                                        plan.qrEnabled ? "bg-emerald-400/10 ring-emerald-400/20 text-emerald-300" : "bg-white/5 ring-white/10 text-white/70"
                                    )}
                                >
                  {plan.qrEnabled ? "QR enabled" : "No QR"}
                </span>
                            </div>
                        </div>

                        {/* usage */}
                        <div className="mt-6 rounded-3xl bg-white/5 p-5 ring-1 ring-white/10">
                            <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
                                <div>
                                    <div className="text-sm font-semibold">Monthly usage</div>
                                    <div className="mt-1 text-sm text-white/65">
                                        Used{" "}
                                        <span className="font-semibold text-white/85">{usage.linksCreatedThisMonth}</span>{" "}
                                        of{" "}
                                        <span className="font-semibold text-white/85">{plan.maxLinksPerMonth}</span>{" "}
                                        links
                                        <span className="text-white/60"> • {remaining} remaining</span>
                                    </div>
                                </div>
                                <div className="text-sm text-white/70">
                                    <span className="font-semibold text-white/85">{usedPct}%</span> used
                                </div>
                            </div>

                            <div className="mt-4 h-3 w-full rounded-full bg-white/10 ring-1 ring-white/10 overflow-hidden">
                                <div
                                    className="h-full rounded-full bg-white"
                                    style={{ width: `${usedPct}%`, opacity: usedPct >= 95 ? 0.85 : 1 }}
                                />
                            </div>

                            <div className="mt-4 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                                <div className="text-xs text-white/55">Resets monthly (based on created links)</div>
                                <button
                                    type="button"
                                    onClick={() => navigate(`/pricing?from=profile&currentPlan=${encodeURIComponent(plan.name || user.planName || "")}`)}
                                    className="rounded-2xl bg-white px-4 py-2.5 text-sm font-semibold text-[#070A12] hover:bg-white/90"
                                >
                                    Upgrade plan
                                </button>
                            </div>
                        </div>
                    </div>
                </section>

                {/* controls */}
                <section>
                    <div className="grid gap-4 lg:grid-cols-[1fr_auto] lg:items-end">
                        <div>
                            <h1 className="text-2xl font-semibold tracking-tight">Your links</h1>
                            <p className="mt-2 text-sm text-white/65">All your shortened links in one place. Search, sort, and open details.</p>
                        </div>

                        <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-end">
                            <div className="relative w-full sm:w-[22rem] lg:w-[26rem]">
                                <input
                                    value={query}
                                    onChange={(e) => setQuery(e.target.value)}
                                    placeholder="Search by alias or URL..."
                                    className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-2.5 text-sm text-white placeholder:text-white/40 shadow-inner outline-none focus:bg-white/10 focus:ring-2 focus:ring-white/40"
                                />
                                <div className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-white/40">⌕</div>
                            </div>

                            <div>
                                <div className="text-xs font-semibold tracking-wider text-white/60 uppercase text-right">Sort</div>
                                <div className="mt-2 inline-flex rounded-2xl bg-white/5 p-1 ring-1 ring-white/10">
                                    <button
                                        type="button"
                                        onClick={() => setSort("newest")}
                                        className={clsx(
                                            "rounded-xl px-4 py-2 text-sm font-semibold transition",
                                            sort === "newest" ? "bg-white text-[#070A12]" : "text-white/80 hover:bg-white/10"
                                        )}
                                    >
                                        Newest
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => setSort("clicks")}
                                        className={clsx(
                                            "rounded-xl px-4 py-2 text-sm font-semibold transition",
                                            sort === "clicks" ? "bg-white text-[#070A12]" : "text-white/80 hover:bg-white/10"
                                        )}
                                    >
                                        Clicks
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* list */}
                    <div className="mt-6 space-y-3">
                        {filtered.length === 0 ? (
                            <div className="rounded-3xl bg-white/5 p-6 ring-1 ring-white/10">
                                <div className="text-sm font-semibold">No links found</div>
                                <div className="mt-1 text-sm text-white/65">Try a different search, or create your first link.</div>
                            </div>
                        ) : (
                            filtered.map((l) => (
                                <article
                                    key={l.id}
                                    className="rounded-3xl bg-white/5 p-5 ring-1 ring-white/10 hover:bg-white/10 transition"
                                >
                                    <div className="grid gap-4 sm:grid-cols-[1fr_auto] sm:items-center">
                                        <div className="min-w-0">
                                            <div className="flex items-start gap-3">
                                                <div className="h-10 w-10 rounded-2xl bg-white/5 ring-1 ring-white/10 flex items-center justify-center shrink-0">
                                                    {l.qrEnabled ? "▦" : "🔗"}
                                                </div>

                                                <div className="min-w-0">
                                                    <div className="flex flex-wrap items-center gap-2">
                                                        <div className="text-lg font-semibold truncate">{l.alias}</div>

                                                        <span className="rounded-full bg-white/5 px-3 py-1 text-xs font-semibold text-white/80 ring-1 ring-white/10">
                              {l.clicks} clicks
                            </span>

                                                        <span className="rounded-full bg-white/5 px-3 py-1 text-xs font-semibold text-white/70 ring-1 ring-white/10">
                              {formatDate(l.createdAt)}
                            </span>

                                                        <span
                                                            className={clsx(
                                                                "rounded-full px-3 py-1 text-xs font-semibold ring-1",
                                                                l.qrEnabled
                                                                    ? "bg-emerald-400/10 ring-emerald-400/20 text-emerald-300"
                                                                    : "bg-white/5 ring-white/10 text-white/70"
                                                            )}
                                                        >
                              {l.qrEnabled ? "QR enabled" : "No QR"}
                            </span>
                                                    </div>

                                                    <div className="mt-2 text-sm text-white/70">
                                                        <span className="text-white/50">Short:</span>{" "}
                                                        <span className="font-semibold text-white/90">{truncateMiddle(l.shortUrl, 62)}</span>
                                                    </div>

                                                    <div className="mt-1 text-sm text-white/60">
                                                        <span className="text-white/50">Original:</span>{" "}
                                                        <span className="font-semibold text-white/75">{truncateMiddle(l.originalUrl, 62)}</span>
                                                    </div>
                                                </div>
                                            </div>

                                            <div className="mt-4 flex flex-wrap gap-2">
                                                <button
                                                    type="button"
                                                    onClick={() => copy(l.shortUrl)}
                                                    className="rounded-2xl bg-white/5 px-4 py-2.5 text-sm font-semibold text-white/85 ring-1 ring-white/10 hover:bg-white/10"
                                                >
                                                    Copy short URL
                                                </button>
                                                <button
                                                    type="button"
                                                    onClick={() => window.open(l.shortUrl, "_blank")}
                                                    className="rounded-2xl bg-white/5 px-4 py-2.5 text-sm font-semibold text-white/85 ring-1 ring-white/10 hover:bg-white/10"
                                                >
                                                    Open
                                                </button>
                                            </div>
                                        </div>

                                        <div className="shrink-0">
                                            <button
                                                type="button"
                                                onClick={() => onOpenDetails(l.id)}
                                                className="w-full sm:w-auto rounded-2xl bg-white px-5 py-3 text-sm font-semibold text-[#070A12] hover:bg-white/90"
                                            >
                                                Details
                                            </button>
                                        </div>
                                    </div>
                                </article>
                            ))
                        )}
                    </div>
                </section>
            </div>
        </>
    );
}
